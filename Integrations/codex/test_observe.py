import json
from pathlib import Path
import tempfile
import unittest

from observe import Tail, event_record

FILENAME = 'rollout-2026-10-20T12-00-00-00000000-0000-0000-0000-000000000001.jsonl'


def record(kind, turn='one'):
    return {'timestamp': '2026-10-20T12:00:00Z', 'type': 'event_msg',
            'payload': {'type': kind, 'turn_id': turn}}


class ObserverTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.path = self.root / FILENAME
        self.path.write_text('')
        self.tail = Tail(self.path)

    def state(self):
        return json.loads((self.root / 'state.json').read_text())['state']

    def test_real_record_lifecycle_and_late_completion(self):
        self.tail.consume(record('task_started'), self.root)
        self.assertEqual(self.state(), 'working')
        self.tail.consume(record('task_started', 'two'), self.root)
        self.tail.consume(record('task_complete', 'one'), self.root)
        self.assertEqual(self.state(), 'working')
        self.tail.consume(record('task_complete', 'two'), self.root)
        self.assertEqual(self.state(), 'complete')
        self.tail.consume(record('token_count', 'two'), self.root)
        self.assertEqual(self.state(), 'complete')

    def test_partial_appends_and_ordinary_messages_are_ignored(self):
        self.tail.initial()
        data = json.dumps(record('task_started'))
        self.path.write_text(data[:50])
        self.assertEqual(self.tail.read(), [])
        with self.path.open('a') as stream:
            stream.write(data[50:] + '\n')
        self.assertEqual(len(self.tail.read()), 1)
        self.assertIsNone(event_record(b'{"type":"response_item","payload":{"text":"private"}}'))

    def test_restart_does_not_replay_old_completion(self):
        self.path.write_text(json.dumps(record('task_complete')) + '\n')
        for item in self.tail.initial():
            self.tail.consume(item, self.root, initial=True)
        self.assertFalse((self.root / 'state.json').exists())

    def test_partial_record_present_at_start_is_not_lost(self):
        data = json.dumps(record('task_started'))
        self.path.write_text(data[:70])
        self.assertEqual(self.tail.initial(), [])
        with self.path.open('a') as stream:
            stream.write(data[70:] + '\n')
        self.assertEqual(len(self.tail.read()), 1)

    def test_wait_and_resume(self):
        for kind, expected in [('task_started', 'working'), ('exec_approval_request', 'waiting'),
                               ('item_completed', 'working'), ('turn_aborted', 'idle')]:
            self.tail.consume(record(kind), self.root)
            self.assertEqual(self.state(), expected)

    def test_restart_recovers_old_turn_with_recent_progress(self):
        start=record('task_started')
        progress=record('token_count')
        progress['timestamp']='2026-10-20T14:00:00Z'
        self.path.write_text(json.dumps(start)+'\n'+json.dumps(progress)+'\n')
        from observe import timestamp
        for item in self.tail.initial():
            self.tail.consume(item,self.root,now=timestamp(progress)+10,initial=True)
        self.assertEqual(self.state(),'working')
        self.assertNotIn('action',json.loads((self.root/'state.json').read_text()))


if __name__ == '__main__':
    unittest.main()
