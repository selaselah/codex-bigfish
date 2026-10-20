#!/usr/bin/env python3
"""Manual state bridge; this does not subscribe to existing Codex desktop chats."""
import json
import argparse
import os
from pathlib import Path
import re
import tempfile
import time

allowed = ('idle', 'working', 'complete', 'waiting', 'error')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('state', choices=allowed)
parser.add_argument('--action', choices=('wave', 'bow', 'doze', 'step', 'cheer'))
args = parser.parse_args()
path = Path.home() / 'Library/Application Support/LocalCompanions/BigFatBlueFish/state.json'
log = Path.home() / 'Library/Logs/LocalCompanions/BigFatBlueFish/Player.log'
if log.exists():
    matches = re.findall(r'^Desktop Fish state file: (.+)$', log.read_text(errors='replace'), re.M)
    if matches:
        path = Path(matches[-1])
path.parent.mkdir(parents=True, exist_ok=True)
with tempfile.NamedTemporaryFile(mode='w', dir=path.parent, delete=False) as f:
    message = {'state': args.state}
    if args.action:
        message.update(action=args.action, command_id=time.time_ns())
    json.dump(message, f)
    temporary_path = f.name
os.replace(temporary_path, path)
print('State:', args.state, 'Action:', args.action or 'none')
