#!/usr/bin/env python3
"""Translate official Codex lifecycle hooks into the local desktop pet protocol."""
import argparse
import fcntl
import json
import os
from pathlib import Path
import tempfile
import time

EVENTS = ('SessionStart', 'UserPromptSubmit', 'PreToolUse', 'PermissionRequest',
          'PostToolUse', 'Stop', 'Interrupt', 'SessionEnd')
STATE_DIR = Path.home() / 'Library/Application Support/LocalCompanions/BigFatBlueFish'
ACTIVE_TTL = 3600
COMPLETE_TTL = 12


def atomic_json(path, value):
    with tempfile.NamedTemporaryFile(mode='w', dir=path.parent, delete=False) as stream:
        temporary = Path(stream.name)
        json.dump(value, stream)
    try:
        os.replace(temporary, path)
    finally:
        temporary.unlink(missing_ok=True)


def update(payload, directory=STATE_DIR, now=None, source='codex-hooks'):
    """Persist ids and states only; never retain prompts, tool arguments or transcripts."""
    now = time.time() if now is None else now
    if not isinstance(payload, dict):
        return
    event = payload.get('hook_event_name')
    session = payload.get('session_id')
    if event not in EVENTS or not isinstance(session, str) or not session:
        return
    session = session[:256]
    turn = str(payload.get('turn_id') or '')[:256]
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    with (directory / 'codex-bridge.lock').open('a') as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        registry_path = directory / 'codex-sessions.json'
        try:
            sessions = json.loads(registry_path.read_text())
            if not isinstance(sessions, dict):
                sessions = {}
        except (OSError, ValueError):
            sessions = {}
        sessions = {key: value for key, value in sessions.items()
                    if isinstance(value, dict)
                    and isinstance(value.get('expires_at'), (int, float))
                    and value['expires_at'] > now}
        previous = sessions.get(session)
        new_completion = event == 'Stop' and not (
            previous and previous.get('state') == 'complete' and previous.get('turn_id') == turn)
        # A delayed completion from an older turn must not stop a newer turn.
        if (previous and turn and previous.get('turn_id')
                and previous['turn_id'] != turn and event != 'UserPromptSubmit'):
            return
        if event == 'SessionStart':
            # Resuming a second chat should not erase another chat's work.
            pass
        elif event in ('Interrupt', 'SessionEnd'):
            sessions.pop(session, None)
        else:
            if event == 'Stop':
                state = 'complete'
            elif event == 'PermissionRequest' or (
                    event == 'PreToolUse' and payload.get('tool_name') in
                    ('request_user_input', 'AskUserQuestion')):
                state = 'waiting'
            else:
                state = 'working'
            # Keep turn identity when a session-scoped event omits turn_id.
            sessions[session] = {
                'turn_id': turn or (previous or {}).get('turn_id', ''),
                'state': state, 'updated_at': now,
                'expires_at': now + (COMPLETE_TTL if state == 'complete' else ACTIVE_TTL),
            }
        atomic_json(registry_path, sessions)
        # A chat finishing must not cancel the busy state of another chat.
        priorities = {'waiting': 3, 'working': 2, 'complete': 1}
        selected = max(sessions.values(), default=None,
                       key=lambda item: (priorities.get(item.get('state'), 0), item['updated_at']))
        message = {'source': source, 'state': 'idle', 'expires_at': 0,
                   'active_count': sum(item.get('state') in ('working', 'waiting') for item in sessions.values())}
        if selected:
            message.update(state=selected['state'], expires_at=selected['expires_at'])
        state_path = directory / 'state.json'
        # Keep a short completion signal through concurrent progress writes.
        try:
            old = json.loads(state_path.read_text())
        except (OSError, ValueError):
            old = {}
        if old.get('notice_until', 0) > now:
            for key in ('notice', 'notice_until', 'action', 'command_id'):
                if key in old:
                    message[key] = old[key]
        if new_completion:
            message.update(action='cheer', command_id=time.time_ns(),
                           notice='一个任务结束啦', notice_until=now + 4)
        atomic_json(state_path, message)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--state-dir', type=Path, default=STATE_DIR)
    args = parser.parse_args()
    try:
        # Hook payloads can contain tool output; bound the amount read and ignore its content.
        import sys
        raw = sys.stdin.read(2 * 1024 * 1024 + 1)
        if len(raw) <= 2 * 1024 * 1024:
            update(json.loads(raw), args.state_dir)
    except (OSError, ValueError, TypeError, KeyError):
        # A decorative integration must never block a Codex turn.
        pass
    print('{}')


if __name__ == '__main__':
    main()
