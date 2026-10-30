"""Local task titles and public assistant messages for the native task list.

This is a read-only compatibility adapter for Codex's local storage, not a
public API. Never copy user prompts, reasoning, command text or tool output.
"""
import json
from contextlib import closing
from pathlib import Path
import re
import sqlite3
import time

from bridge import atomic_json

SESSION = re.compile(r'^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$')
MAX_SCAN = 2 * 1024 * 1024
MAX_PREVIEW = 600


def clean(value, limit=MAX_PREVIEW):
    return ' '.join(value.split())[:limit] if isinstance(value, str) else ''


def text_content(value):
    if isinstance(value, str):
        return clean(value)
    if not isinstance(value, list):
        return ''
    return clean(' '.join(p.get('text', '') for p in value if isinstance(p, dict)
                          and p.get('type') in ('output_text', 'text')
                          and isinstance(p.get('text'), str)))


def local_threads(home, now=None):
    """Read only the metadata columns needed by the local task list."""
    home = Path(home)
    now = time.time() if now is None else now
    result = {}
    databases = sorted(home.glob('state_*.sqlite'),
                       key=lambda p: int(p.stem[6:]) if p.stem[6:].isdigit() else -1,
                       reverse=True)
    for path in databases:
        try:
            with closing(sqlite3.connect(path.as_uri() + '?mode=ro', uri=True, timeout=.2)) as db:
                db.execute('PRAGMA query_only=ON')
                columns = {r[1] for r in db.execute('PRAGMA table_info(threads)')}
                if not {'id', 'title', 'rollout_path', 'updated_at', 'archived'} <= columns:
                    continue
                optional = [n for n in ('name', 'agent_role') if n in columns]
                sql = ('SELECT id,title,rollout_path,' +
                       ','.join(optional + ['updated_at']) +
                       ' FROM threads WHERE archived=0 AND updated_at>=? '
                       'ORDER BY updated_at DESC LIMIT 256')
                for row in db.execute(sql, (now - 172800,)):
                    item = dict(zip(['id', 'title', 'rollout_path'] + optional + ['updated_at'], row))
                    if not isinstance(item['id'], str) or not SESSION.fullmatch(item['id']) or item.get('agent_role'):
                        continue
                    rollout = Path(item['rollout_path']).resolve()
                    if not rollout.is_relative_to((home / 'sessions').resolve()) or not rollout.is_file():
                        continue
                    result[item['id']] = dict(title=clean(item.get('name') or item['title'], 160),
                                              path=rollout, named=bool(clean(item.get('name'),160)))
            break
        except (OSError, sqlite3.Error, TypeError, ValueError):
            continue
    # App title changes also appear in this append-only index. Prefer its
    # latest title over generated/older database metadata.
    try:
        path = home / 'session_index.jsonl'
        with path.open('rb') as stream:
            start = max(0, path.stat().st_size - 4 * MAX_SCAN)
            stream.seek(start)
            if start:
                stream.readline()
            for line in stream:
                try:
                    item = json.loads(line)
                    if (item.get('id') in result and not result[item['id']]['named']
                            and clean(item.get('thread_name'), 160)):
                        result[item['id']]['title'] = clean(item['thread_name'], 160)
                except (ValueError, AttributeError):
                    continue
    except OSError:
        pass
    return result


class MessageTail:
    def __init__(self, path, turn=''):
        self.path = Path(path)
        self.turn = turn
        self.offset = 0
        self.inode = None
        self.message = ''
        self.message_at = ''
        self.summary = ''
        self.kind = 'thinking'
        self.at = ''

    def consume(self, record):
        payload = record.get('payload', {})
        if not isinstance(payload, dict):
            return
        kind = payload.get('type')
        if record.get('type') == 'event_msg' and kind == 'task_started':
            self.turn = str(payload.get('turn_id') or '')
            self.message = ''
            self.message_at = ''
            self.summary = '正在思考'
            self.kind = 'thinking'
            self.at = str(record.get('timestamp') or '')
            return
        turn = payload.get('turn_id') or payload.get('root_turn_id')
        if turn and self.turn and turn != self.turn:
            return
        item = None
        if record.get('type') == 'event_msg' and kind == 'item_completed':
            item = payload.get('item')
        elif record.get('type') == 'event_msg' and kind == 'agent_message':
            item = dict(type='AgentMessage', content=payload.get('message'))
        elif record.get('type') == 'event_msg' and kind == 'agent_reasoning':
            item = dict(type='Reasoning')
        elif record.get('type') == 'response_item':
            if kind == 'message' and payload.get('role') == 'assistant':
                item = dict(type='AgentMessage', content=payload.get('content'))
            elif kind in ('reasoning', 'custom_tool_call', 'function_call'):
                item = dict(type='Reasoning' if kind == 'reasoning' else 'ToolStarted')
        if not isinstance(item, dict):
            return
        tag = item.get('type')
        if tag == 'AgentMessage':
            message = text_content(item.get('content'))
            if not message:
                return
            self.message = self.summary = message
            self.message_at = str(record.get('timestamp') or '')
            self.kind = 'message'
        else:
            hints = {'Reasoning': ('thinking', '正在思考'),
                     'CommandExecution': ('command', '已运行命令'),
                     'FileChange': ('file', '已更新文件'),
                     'ToolStarted': ('tool', '正在使用工具'),
                     'Extension': ('tool', '已使用工具')}
            if tag not in hints:
                return
            self.kind, self.summary = hints[tag]
        self.at = str(record.get('timestamp') or '')

    def poll(self):
        try:
            stat = self.path.stat()
            if self.inode != stat.st_ino or stat.st_size < self.offset:
                self.message = self.summary = ''
                self.message_at = ''
                self.offset = max(0, stat.st_size - MAX_SCAN)
                self.inode = stat.st_ino
                first = True
            else:
                first = False
            with self.path.open('rb') as stream:
                stream.seek(self.offset)
                if first and self.offset:
                    stream.readline()
                while True:
                    line = stream.readline(MAX_SCAN + 1)
                    if not line:
                        break
                    if not line.endswith(b'\n'):
                        if len(line) <= MAX_SCAN:
                            break
                        while line and not line.endswith(b'\n'):
                            line = stream.readline(MAX_SCAN + 1)
                        self.offset = stream.tell()
                        continue
                    self.offset = stream.tell()
                    try:
                        self.consume(json.loads(line))
                    except (ValueError, TypeError, AttributeError):
                        continue
        except OSError:
            pass


class TaskCatalog:
    def __init__(self, home, directory):
        self.home, self.directory = Path(home), Path(directory)
        self.metadata, self.tails = {}, {}
        self.next_refresh = 0
        self.last = None

    def refresh(self, now):
        if now >= self.next_refresh:
            self.metadata = local_threads(self.home, now)
            self.next_refresh = now + 5
        return [v['path'] for v in self.metadata.values()]

    def publish(self, now=None):
        now = time.time() if now is None else now
        self.refresh(now)
        try:
            sessions = json.loads((self.directory / 'codex-sessions.json').read_text())
        except (OSError, ValueError):
            sessions = {}
        tasks = []
        if not isinstance(sessions, dict):
            sessions = {}
        for session, state in sessions.items():
            if not SESSION.fullmatch(session) or not isinstance(state, dict):
                continue
            if not isinstance(state.get('expires_at'), (int, float)) or state['expires_at'] <= now:
                continue
            if state.get('state') not in ('working', 'waiting', 'complete'):
                continue
            info = self.metadata.get(session, {})
            if info.get('path'):
                tail = self.tails.get(session)
                if not tail or tail.path != info['path']:
                    tail = self.tails[session] = MessageTail(info['path'], state.get('turn_id', ''))
                tail.poll()
            else:
                tail = None
            default = {'working': '正在思考', 'waiting': '等你回复', 'complete': '本回合结束'}[state['state']]
            summary = tail.summary if tail and tail.summary else default
            if state['state'] == 'waiting':
                summary = '等你回复'
            elif state['state'] == 'complete':
                summary = tail.message if tail and tail.message else '本回合结束'
            tasks.append(dict(session_id=session, title=info.get('title') or '未命名任务',
                              summary=clean(summary, 120), message=tail.message if tail else '',
                              activity=tail.kind if tail else 'status',
                              updated_at=state.get('updated_at', 0),
                              message_at=tail.message_at if tail else '',
                              activity_at=tail.at if tail else '',
                              state=state['state'], expires_at=state['expires_at']))
        tasks.sort(key=lambda t: (t['state'] == 'waiting', t['updated_at']), reverse=True)
        tasks = tasks[:64]
        self.tails = {k: v for k, v in self.tails.items() if any(t['session_id'] == k for t in tasks)}
        if tasks != self.last:
            self.directory.mkdir(parents=True, exist_ok=True)
            atomic_json(self.directory / 'codex-tasks.json', dict(version=1, tasks=tasks), ensure_ascii=False)
            self.last = tasks
        return tasks
