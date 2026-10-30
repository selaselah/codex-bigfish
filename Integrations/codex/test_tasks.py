import json
from pathlib import Path
import sqlite3
import tempfile
import unittest
import subprocess
import sys
import time
from datetime import datetime, timezone

from bridge import update
from tasks import MessageTail, TaskCatalog, local_threads

SID = '00000000-0000-0000-0000-000000000001'
SECOND = '00000000-0000-0000-0000-000000000002'


def event(kind, **fields):
    return dict(timestamp='2026-10-30T03:00:00Z', type='event_msg',
                payload=dict(type=kind, turn_id='turn1', **fields))


def response(kind, **fields):
    return dict(timestamp='2026-10-30T03:00:01Z', type='response_item',
                payload=dict(type=kind, **fields))


class TaskTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.home = Path(self.temp.name) / 'codex'
        self.directory = Path(self.temp.name) / 'pet'
        self.sessions = self.home / 'sessions/2026/10/20'
        self.sessions.mkdir(parents=True)
        self.path = self.sessions / ('rollout-2026-10-20T12-00-00-' + SID + '.jsonl')
        self.path.write_text('')
        self.db = self.home / 'state_5.sqlite'
        with sqlite3.connect(self.db) as db:
            db.execute('CREATE TABLE threads (id TEXT,title TEXT,name TEXT,rollout_path TEXT,updated_at INTEGER,archived INTEGER,agent_role TEXT)')
            db.execute('INSERT INTO threads VALUES (?,?,?,?,?,?,?)',
                       (SID, 'Generated title', None, str(self.path), 100, 0, None))

    def write(self, *records):
        self.path.write_text(''.join(json.dumps(r) + '\n' for r in records))

    def start(self, kind='UserPromptSubmit', **kw):
        update(dict(hook_event_name=kind, session_id=SID, turn_id='turn1', **kw),
               self.directory, now=100)

    def catalog(self):
        return TaskCatalog(self.home, self.directory)

    def test_two_line_title_and_latest_public_message(self):
        self.start()
        self.write(event('task_started'), response('message', role='user',
                   content=[dict(type='input_text', text='PRIVATE PROMPT')]),
                   response('message', role='assistant', phase='commentary',
                   content=[dict(type='output_text', text='正在修复布局\n请稍等')]))
        self.home.joinpath('session_index.jsonl').write_text(json.dumps(dict(id=SID, thread_name='检查安卓界面间距'))+'\n')
        task = self.catalog().publish(101)[0]
        self.assertEqual(task['title'], '检查安卓界面间距')
        self.assertEqual(task['summary'], '正在修复布局 请稍等')
        self.assertNotIn('PRIVATE', (self.directory/'codex-tasks.json').read_text())

    def test_reasoning_is_generic_not_saved(self):
        self.start()
        self.write(event('task_started'), event('item_completed', item=dict(
            type='Reasoning', raw_content='SECRET THOUGHT', summary_text='SECRET SUMMARY')))
        task = self.catalog().publish(101)[0]
        self.assertEqual(task['summary'], '正在思考')
        self.assertEqual(task['message'], '')
        self.assertNotIn('SECRET', (self.directory/'codex-tasks.json').read_text())

    def test_command_and_tool_arguments_never_saved(self):
        self.start()
        self.write(response('custom_tool_call', input='SECRET ARGUMENT'),
                   event('item_completed', item=dict(type='CommandExecution',
                         command='SECRET COMMAND', aggregated_output='SECRET OUTPUT')))
        self.assertEqual(self.catalog().publish(101)[0]['summary'], '已运行命令')
        self.assertNotIn('SECRET', (self.directory/'codex-tasks.json').read_text())

    def test_completed_item_and_response_formats(self):
        tail = MessageTail(self.path)
        tail.consume(event('item_completed', item=dict(type='AgentMessage', content='Public reply')))
        self.assertEqual(tail.message, 'Public reply')
        tail.consume(response('message', role='assistant', content=[dict(type='output_text', text='New reply')]))
        self.assertEqual(tail.summary, 'New reply')

    def test_wait_and_completed_use_correct_preview(self):
        self.start()
        self.write(response('message', role='assistant', content=[dict(type='output_text', text='Finished')]),
                   response('reasoning', summary=['SECRET']))
        self.start('PermissionRequest')
        cat = self.catalog()
        self.assertEqual(cat.publish(101)[0]['summary'], '等你回复')
        self.start('Stop')
        self.assertEqual(cat.publish(102)[0]['summary'], 'Finished')
        self.assertEqual(cat.publish(113), [])

    def test_partial_append_restart_and_truncation(self):
        tail = MessageTail(self.path)
        tail.poll()
        raw = json.dumps(response('message', role='assistant', content='Hello'))+'\n'
        self.path.write_text(raw[:30]);tail.poll()
        self.assertEqual(tail.message, '')
        with self.path.open('a') as f:f.write(raw[30:])
        tail.poll();self.assertEqual(tail.message, 'Hello')
        self.write(event('task_started'))
        tail.poll();self.assertEqual(tail.message, '')

    def test_new_turn_resets_old_message_and_rejects_late_item(self):
        tail = MessageTail(self.path)
        tail.consume(event('task_started'))
        tail.consume(response('message', role='assistant', content='Old turn'))
        record = event('task_started');record['payload']['turn_id']='turn2';tail.consume(record)
        tail.consume(event('item_completed', item=dict(type='AgentMessage', content='Late old reply')))
        self.assertEqual(tail.summary, '正在思考')
        self.assertEqual(tail.message, '')

    def test_old_origin_active_chat_discovered_readonly(self):
        before = self.db.read_bytes()
        found = local_threads(self.home, now=101)
        self.assertEqual(found[SID]['path'], self.path.resolve())
        self.assertEqual(before, self.db.read_bytes())

    def test_archive_subagent_and_external_file_not_used(self):
        with sqlite3.connect(self.db) as db:
            db.execute('UPDATE threads SET agent_role=? WHERE id=?', ('worker', SID))
            db.execute('INSERT INTO threads VALUES (?,?,?,?,?,?,?)',
                       (SECOND, 'Other', None, str(Path(self.temp.name)/'secret'), 100, 0, None))
        self.assertEqual(local_threads(self.home, 101), {})

    def test_title_changes_refresh_and_previews_are_bounded(self):
        self.start();self.write(response('message', role='assistant', content='x'*1000))
        cat=self.catalog();self.assertEqual(len(cat.publish(101)[0]['message']),600)
        with sqlite3.connect(self.db) as db:db.execute('UPDATE threads SET name=?', ('Renamed',))
        self.assertEqual(cat.publish(106)[0]['title'],'Renamed')

    def test_explicit_name_wins_over_old_generated_index_title(self):
        with sqlite3.connect(self.db) as db:db.execute('UPDATE threads SET name=?',('Custom title',))
        (self.home/'session_index.jsonl').write_text(json.dumps(dict(id=SID,thread_name='Old generated title'))+'\n')
        self.assertEqual(local_threads(self.home,101)[SID]['title'],'Custom title')

    def test_catalog_does_not_change_lifecycle_or_celebrate(self):
        self.start();before=(self.directory/'state.json').read_bytes()
        self.catalog().publish(101)
        self.assertEqual((self.directory/'state.json').read_bytes(),before)

    def test_live_observer_publishes_and_cleans_up_on_shutdown(self):
        now=time.time()
        with sqlite3.connect(self.db) as db:db.execute('UPDATE threads SET updated_at=?',(int(now),))
        start=event('task_started');start['timestamp']=datetime.now(timezone.utc).isoformat()
        self.write(start,response('message',role='assistant',content='Ready'))
        process=subprocess.Popen([sys.executable,str(Path(__file__).with_name('observe.py')),
            '--codex-home',str(self.home),'--state-dir',str(self.directory)],
            stdout=subprocess.PIPE,stderr=subprocess.PIPE)
        try:
            output=self.directory/'codex-tasks.json'
            deadline=time.monotonic()+4
            ready=False
            while time.monotonic()<deadline:
                if output.exists() and json.loads(output.read_text()).get('tasks'):
                    ready=True;break
                if process.poll() is not None:break
                time.sleep(.02)
            self.assertTrue(ready)
            self.assertEqual(json.loads(output.read_text())['tasks'][0]['summary'],'Ready')
            process.terminate();_,error=process.communicate(timeout=3)
            self.assertEqual(process.returncode,0,error.decode())
            self.assertEqual(json.loads(output.read_text())['tasks'],[])
        finally:
            if process.poll() is None:process.kill();process.communicate()

    def test_maximum_unicode_catalog_fits_native_reader(self):
        self.directory.mkdir()
        sessions={}
        with sqlite3.connect(self.db) as db:
            db.execute('DELETE FROM threads')
            for i in range(64):
                sid='00000000-0000-0000-0000-'+str(i+1).zfill(12)
                path=self.sessions/('rollout-2026-10-20T12-00-00-'+sid+'.jsonl')
                path.write_text(json.dumps(response('message',role='assistant',content='🍚'*600))+'\n')
                db.execute('INSERT INTO threads VALUES (?,?,?,?,?,?,?)',
                           (sid,'🍚'*160,None,str(path),100,0,None))
                sessions[sid]=dict(turn_id='turn1',state='working',updated_at=100,expires_at=1000)
        (self.directory/'codex-sessions.json').write_text(json.dumps(sessions))
        self.assertEqual(len(self.catalog().publish(101)),64)
        self.assertLess((self.directory/'codex-tasks.json').stat().st_size,256*1024)


if __name__ == '__main__':
    unittest.main()
