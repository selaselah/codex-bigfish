#!/usr/bin/env python3
"""Install/remove this bridge's hooks, preserving other user hook definitions."""
import argparse
import json
import os
from pathlib import Path
import shlex
import shutil
import sys
import time

from bridge import EVENTS, atomic_json

LABEL = 'BigFish: local desktop state'


def configure(codex_home, bridge, remove=False):
    codex_home.mkdir(parents=True, exist_ok=True)
    path = codex_home / 'hooks.json'
    config = json.loads(path.read_text()) if path.exists() else {}
    hooks = config.setdefault('hooks', {})
    # Refuse unfamiliar structures rather than overwrite an existing configuration.
    if not isinstance(hooks, dict) or any(not isinstance(v, list) for v in hooks.values()):
        raise ValueError('Unexpected hooks.json shape; existing configuration was not changed.')
    for event, groups in list(hooks.items()):
        retained = []
        for group in groups:
            group = dict(group)
            group['hooks'] = [h for h in group.get('hooks', []) if h.get('statusMessage') != LABEL]
            if group['hooks']:
                retained.append(group)
        hooks[event] = retained
    if not remove:
        command = shlex.join([sys.executable, str(bridge.resolve())])
        for event in EVENTS:
            hooks.setdefault(event, []).append({'hooks': [{
                'type': 'command', 'command': command, 'timeout': 3,
                'statusMessage': LABEL,
            }]})
    if path.exists() and json.loads(path.read_text()) == config:
        return path
    if path.exists():
        shutil.copy2(path, path.with_name(f'hooks.json.bigfish-backup-{time.time_ns()}'))
    atomic_json(path, config)
    return path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--codex-home', type=Path,
                        default=Path(os.environ.get('CODEX_HOME', Path.home() / '.codex')))
    parser.add_argument('--remove', action='store_true')
    args = parser.parse_args()
    path = configure(args.codex_home, Path(__file__).with_name('bridge.py'), args.remove)
    print(('Removed BigFish hooks from ' if args.remove else 'Installed BigFish hooks in ') + str(path))
    if not args.remove:
        print('Review and trust the BigFish hooks with /hooks in Codex CLI, then start/resume a chat.')
        print('This installer does not grant hook trust or alter approval settings.')


if __name__ == '__main__':
    main()
