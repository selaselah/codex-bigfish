#!/usr/bin/env python3
"""Compatibility adapter for desktop Codex lifecycle records (not a stable public API)."""
import argparse
from datetime import datetime, timedelta, timezone
import fcntl
import json
import os
from pathlib import Path
import re
import signal
import time

from bridge import STATE_DIR, atomic_json, update

LIFECYCLE = {'task_started': 'UserPromptSubmit', 'task_complete': 'Stop',
             'turn_aborted': 'Interrupt', 'shutdown_complete': 'SessionEnd'}
WAIT_EVENTS = {'exec_approval_request', 'apply_patch_approval_request', 'request_user_input'}
UUID = re.compile(r'([0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12})\.jsonl$')
MAX_RECORD = 32 * 1024 * 1024


def timestamp(record):
    try:
        return datetime.fromisoformat(record['timestamp'].replace('Z', '+00:00')).timestamp()
    except (ValueError, TypeError, KeyError):
        return 0


def event_record(line):
    # Filter before parsing: never decode ordinary assistant/user response records.
    if b'event_msg' not in line[:160] or len(line) > MAX_RECORD:
        return None
    try:
        record = json.loads(line)
        if record.get('type') == 'event_msg' and isinstance(record.get('payload'), dict):
            return record
    except (ValueError, AttributeError):
        pass
    return None


class Tail:
    def __init__(self, path):
        self.path = path
        self.session = UUID.search(path.name).group(1)
        self.offset = 0
        self.inode = None
        self.turn = ''
        self.active = False
        self.last_event = None

    def initial(self):
        """Find the latest lifecycle boundary backwards, without replaying old turns."""
        size = self.path.stat().st_size
        end = size
        committed_size = size
        suffix = b''
        with self.path.open('rb') as stream:
            while end > 0:
                start = max(0, end - 256 * 1024)
                stream.seek(start)
                chunk = stream.read(end - start) + suffix
                lines = chunk.split(b'\n')
                if end == size and lines[-1]:
                    committed_size = size - len(lines[-1])
                    lines.pop()  # Read the unfinished append again on the next poll.
                suffix = lines.pop(0) if start else b''
                for line in reversed(lines):
                    record = event_record(line)
                    if record and record['payload'].get('type') in LIFECYCLE:
                        self.offset = committed_size
                        self.inode = self.path.stat().st_ino
                        return [record]
                end = start
        self.offset = committed_size
        self.inode = self.path.stat().st_ino
        return []

    def read(self):
        stat = self.path.stat()
        if self.inode != stat.st_ino or stat.st_size < self.offset:
            self.turn = ''
            self.active = False
            return self.initial()
        records = []
        with self.path.open('rb') as stream:
            stream.seek(self.offset)
            while True:
                line = stream.readline(MAX_RECORD + 1)
                if not line:
                    break
                if not line.endswith(b'\n'):
                    # Wait for an incomplete append. Skip oversized complete records safely.
                    if len(line) <= MAX_RECORD:
                        break
                    while line and not line.endswith(b'\n'):
                        line = stream.readline(MAX_RECORD + 1)
                    self.offset = stream.tell()
                    continue
                self.offset = stream.tell()
                record = event_record(line)
                if record:
                    records.append(record)
        return records

    def consume(self, record, state_dir, now=None, initial=False):
        now = time.time() if now is None else now
        payload = record['payload']
        kind = payload.get('type')
        event = LIFECYCLE.get(kind)
        event_time = timestamp(record)
        turn = str(payload.get('turn_id') or payload.get('root_turn_id') or self.turn)
        if kind == 'task_started':
            self.turn, self.active = turn, True
        elif kind in LIFECYCLE:
            # Ignore an old turn completing after a newer one has already started.
            if self.turn and turn and self.turn != turn:
                return
            self.active = False
        elif self.active and kind in WAIT_EVENTS:
            event = 'PermissionRequest'
        elif self.active and kind in ('item_completed', 'token_count'):
            event = 'PostToolUse'
        if not event:
            return
        self.last_event = {'event': kind, 'at': event_time, 'session_id': self.session}
        # Never celebrate a historical completion when the observer starts.
        if initial and (kind != 'task_started' or now - event_time > 3600):
            return
        update({'hook_event_name': event, 'session_id': self.session, 'turn_id': turn},
               state_dir, now=event_time if initial else now, source='codex-rollout-events')


def candidates(codex_home):
    # Include yesterday's sessions so crossing midnight does not lose a running chat.
    roots = set()
    for clock in (datetime.now(), datetime.now(timezone.utc)):
        for days in (0, 1):
            date = clock - timedelta(days=days)
            roots.add(codex_home / 'sessions' / date.strftime('%Y/%m/%d'))
    return [path for root in roots if root.exists()
            for path in root.glob('rollout-*.jsonl') if UUID.search(path.name)]


def run(codex_home, state_dir, once=False):
    state_dir.mkdir(parents=True, exist_ok=True)
    with (state_dir / 'codex-observer.lock').open('a') as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise SystemExit('The desktop observer is already running.')
        running = True

        def stop(*_):
            nonlocal running
            running = False

        signal.signal(signal.SIGTERM, stop)
        signal.signal(signal.SIGINT, stop)
        tails = {}
        next_discovery = 0
        try:
            while running:
                now = time.time()
                if now >= next_discovery:
                    for path in candidates(codex_home):
                        if path not in tails:
                            tail = tails[path] = Tail(path)
                            for record in tail.initial():
                                tail.consume(record, state_dir, now, initial=True)
                    next_discovery = now + 5
                for tail in list(tails.values()):
                    try:
                        for record in tail.read():
                            tail.consume(record, state_dir)
                    except FileNotFoundError:
                        tails.pop(tail.path, None)
                        update({'hook_event_name': 'SessionEnd', 'session_id': tail.session}, state_dir,
                               source='codex-rollout-events')
                events = [tail.last_event for tail in tails.values() if tail.last_event]
                atomic_json(state_dir / 'codex-observer.json', {
                    'pid': os.getpid(), 'updated_at': time.time(), 'source': 'local-rollout-events',
                    'watched_sessions': len(tails),
                    'last_event': max(events, key=lambda e: e['at'], default=None),
                })
                if once:
                    break
                time.sleep(1)
        finally:
            if not once:
                # Clear only sessions this observer had seen actively working.
                for tail in tails.values():
                    if tail.active:
                        update({'hook_event_name': 'SessionEnd', 'session_id': tail.session}, state_dir,
                               source='codex-rollout-events')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--codex-home', type=Path,
                        default=Path(os.environ.get('CODEX_HOME', Path.home() / '.codex')))
    parser.add_argument('--state-dir', type=Path, default=STATE_DIR)
    parser.add_argument('--once', action='store_true')
    args = parser.parse_args()
    run(args.codex_home, args.state_dir, args.once)


if __name__ == '__main__':
    main()
