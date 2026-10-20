import json
from pathlib import Path
import tempfile
import unittest

from bridge import update
from install import configure, LABEL


class BridgeTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def send(self, event, session='one', turn='turn1', **extra):
        update(dict(hook_event_name=event, session_id=session, turn_id=turn, **extra),
               self.root, now=100)
        return json.loads((self.root / 'state.json').read_text())

    def test_lifecycle_and_no_prompt_retention(self):
        self.assertEqual(self.send('UserPromptSubmit', prompt='private prompt')['state'], 'working')
        self.assertEqual(self.send('PermissionRequest')['state'], 'waiting')
        self.assertEqual(self.send('PostToolUse')['state'], 'working')
        completed = self.send('Stop')
        self.assertEqual(completed['action'], 'cheer')
        self.assertEqual(completed['command_id'], self.send('Stop')['command_id'])
        self.assertEqual(self.send('Interrupt')['state'], 'idle')
        self.assertNotIn('private prompt', (self.root / 'codex-sessions.json').read_text())

    def test_multiple_chats_and_stale_turn(self):
        self.send('UserPromptSubmit')
        self.send('UserPromptSubmit', session='two')
        self.assertEqual(self.send('Stop')['state'], 'working')
        self.assertEqual(self.send('PermissionRequest', session='two')['state'], 'waiting')
        self.send('UserPromptSubmit', turn='turn2')
        self.send('Stop', turn='turn1')
        self.assertEqual(json.loads((self.root / 'codex-sessions.json').read_text())['one']['state'], 'working')
        self.assertEqual(self.send('SessionEnd', session='two')['state'], 'working')

    def test_completion_visible_while_another_chat_is_working(self):
        self.send('UserPromptSubmit')
        self.send('UserPromptSubmit', session='two')
        completed = self.send('Stop')
        self.assertEqual(completed['state'], 'working')
        self.assertEqual(completed['active_count'], 1)
        self.assertEqual(completed['action'], 'cheer')
        progress = self.send('PostToolUse', session='two')
        self.assertEqual(progress['command_id'], completed['command_id'])
        self.assertEqual(progress['notice'], completed['notice'])

    def test_lease_and_malformed_input(self):
        self.send('UserPromptSubmit')
        update(dict(hook_event_name='SessionStart', session_id='two'), self.root, now=4000)
        self.assertEqual(json.loads((self.root / 'state.json').read_text())['state'], 'idle')
        update([], self.root)
        update({'hook_event_name': 'unknown'}, self.root)

    def test_install_is_idempotent_and_preserves_other_hooks(self):
        original = {'hooks': {'Stop': [{'hooks': [{'type': 'command', 'command': 'other'}]}]}}
        (self.root / 'hooks.json').write_text(json.dumps(original))
        script = Path(__file__).with_name('bridge.py')
        configure(self.root, script)
        first = (self.root / 'hooks.json').read_text()
        configure(self.root, script)
        self.assertEqual(first, (self.root / 'hooks.json').read_text())
        self.assertIn('other', first)
        self.assertIn(LABEL, first)
        configure(self.root, script, remove=True)
        self.assertNotIn(LABEL, (self.root / 'hooks.json').read_text())
        self.assertIn('other', (self.root / 'hooks.json').read_text())


if __name__ == '__main__':
    unittest.main()
