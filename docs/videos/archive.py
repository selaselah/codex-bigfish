#!/usr/bin/env python3
"""Verify release archives or restore only their ignored local work scripts."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import sys

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
RELEASES = ("01-generator", "02-task-cards", "03-cofish-features")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def child(root, relative):
    path = (root / relative).resolve()
    if not path.is_relative_to(root.resolve()) or path == root.resolve():
        raise ValueError(f"Path leaves its allowed directory: {relative}")
    return path


def manifest(name):
    directory = HERE / name
    return directory, json.loads((directory / "release.json").read_text())


def verify(local_media=False):
    total = 0
    for name in RELEASES:
        directory, data = manifest(name)
        for item in data["archive_files"]:
            path = child(directory, item["path"])
            if not path.is_file() or digest(path) != item["sha256"]:
                raise ValueError(f"Archive mismatch: {name}/{item['path']}")
            total += 1
        timeline = json.loads((directory / "timeline.json").read_text())
        end = 0
        for stage in timeline["timeline"]:
            if stage["start"] != end or stage["end"] <= stage["start"]:
                raise ValueError(f"Non-contiguous timeline: {name}")
            end = stage["end"]
        if end != data["media"]["duration_seconds"]:
            raise ValueError(f"Timeline duration mismatch: {name}")
        if local_media:
            media = data["media"]
            path = child(REPO, media["local_path"])
            if not path.is_file() or digest(path) != media["sha256"]:
                raise ValueError(f"Local published video missing or changed: {path}")
        print(f"PASS {name}: {len(data['archive_files'])} files, {end}s timeline")
    print(f"Verified {len(RELEASES)} releases and {total} archived files")


def stage(name, apply=False, root=None):
    directory, data = manifest(name)
    root = (root or REPO).resolve()
    if not root.is_relative_to(REPO):
        raise ValueError("Staging root must stay inside the current repository")
    operations = []
    # Check the entire plan before writing; never overwrite a differing file.
    for item in data["archive_files"]:
        if not item.get("stage_to"):
            continue
        source = child(directory, item["path"])
        if digest(source) != item["sha256"]:
            raise ValueError(f"Archive mismatch: {source}")
        destination = child(root, item["stage_to"])
        if not destination.is_relative_to(root / "work"):
            raise ValueError("Only ignored work scripts may be restored")
        if destination.exists() and (not destination.is_file() or digest(destination) != item["sha256"]):
            raise ValueError(f"Existing file differs; left unchanged: {destination}")
        operations.append((source, destination))
    for source, destination in operations:
        exists = destination.exists()
        print(f"{'KEEP' if exists else 'COPY' if apply else 'WOULD COPY'} {destination.relative_to(root)}")
        if apply and not exists:
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source, destination)
    if not apply:
        print("Dry run only; use --apply to restore missing files")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    check = commands.add_parser("verify")
    check.add_argument("--local-media", action="store_true")
    restore = commands.add_parser("stage")
    restore.add_argument("release", choices=RELEASES)
    restore.add_argument("--apply", action="store_true")
    restore.add_argument("--root", type=Path, help="Optional sandbox under this repository for a restore rehearsal")
    args = parser.parse_args()
    try:
        if args.command == "verify":
            verify(args.local_media)
        else:
            stage(args.release, args.apply, args.root)
    except (OSError, ValueError, KeyError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
