#!/usr/bin/env python3
"""Install the local desktop lifecycle observer as a per-user macOS LaunchAgent."""
import argparse
import os
from pathlib import Path
import plistlib
import subprocess
import sys
import time

LABEL = 'local.desktop.bigfatbluefish.codex-observer'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--remove', action='store_true')
    args = parser.parse_args()
    domain = f'gui/{os.getuid()}'
    path = Path.home() / 'Library/LaunchAgents' / (LABEL + '.plist')
    subprocess.run(['launchctl', 'bootout', domain + '/' + LABEL],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    # launchd can acknowledge bootout before the old registration is removed.
    for _ in range(30):
        status = subprocess.run(['launchctl', 'print', domain + '/' + LABEL],
                                stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if status.returncode:
            break
        time.sleep(0.1)
    if args.remove:
        path.unlink(missing_ok=True)
        print('Removed desktop observer.')
        return
    log_dir = Path.home() / 'Library/Logs/LocalCompanions/BigFatBlueFish'
    log_dir.mkdir(parents=True, exist_ok=True)
    path.parent.mkdir(parents=True, exist_ok=True)
    config = {
        'Label': LABEL,
        'ProgramArguments': [sys.executable, str(Path(__file__).with_name('observe.py').resolve()),
                             '--codex-home', os.environ.get('CODEX_HOME', str(Path.home() / '.codex'))],
        'RunAtLoad': True, 'KeepAlive': {'SuccessfulExit': False}, 'ThrottleInterval': 15,
        'ProcessType': 'Background',
        'StandardOutPath': str(log_dir / 'codex-observer.log'),
        'StandardErrorPath': str(log_dir / 'codex-observer-error.log'),
    }
    with path.open('wb') as stream:
        plistlib.dump(config, stream)
    for attempt in range(5):
        result = subprocess.run(['launchctl', 'bootstrap', domain, str(path)],
                                capture_output=True, text=True)
        if not result.returncode:
            break
        if attempt == 4:
            raise RuntimeError(result.stderr.strip())
        time.sleep(0.3)
    print('Desktop observer installed:', path)


if __name__ == '__main__':
    main()
