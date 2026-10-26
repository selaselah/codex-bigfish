#!/usr/bin/env python3
"""Build the small AppKit status overlay for the Apple Silicon desktop player."""
from pathlib import Path
import hashlib,json,plistlib,subprocess
root=Path(__file__).resolve().parents[2]
source=Path(__file__).with_name('FishGlassStatus.m')
bundle=root/'Assets/DesktopFish/Plugins/macOS/FishGlassStatus.bundle'
contents=bundle/'Contents'; binary=contents/'MacOS/FishGlassStatus'; manifest=contents/'Resources/build.json'
fingerprint=hashlib.sha256(source.read_bytes()+Path(__file__).read_bytes()).hexdigest()
if binary.exists() and manifest.exists() and json.loads(manifest.read_text()).get('source_sha256')==fingerprint:
    subprocess.run(['codesign','--verify','--strict',str(bundle)],check=True)
    print('AppKit status plugin up to date',flush=True)
else:
    binary.parent.mkdir(parents=True,exist_ok=True);manifest.parent.mkdir(parents=True,exist_ok=True)
    subprocess.run(['xcrun','clang','-arch','arm64','-mmacosx-version-min=12.0','-fobjc-arc','-fvisibility=hidden','-bundle','-framework','Cocoa','-framework','QuartzCore',str(source),'-o',str(binary)],check=True)
    (contents/'Info.plist').write_bytes(plistlib.dumps(dict(CFBundleExecutable='FishGlassStatus',CFBundleIdentifier='local.desktop.rebuiltbigfish.statusglass',CFBundleName='FishGlassStatus',CFBundlePackageType='BNDL',CFBundleVersion='1',CFBundleShortVersionString='1.0',LSMinimumSystemVersion='12.0')))
    manifest.write_text(json.dumps(dict(source_sha256=fingerprint,architecture='arm64',minimum_macos='12.0',system_liquid_glass_macos='26.0'),indent=2)+'\n')
    subprocess.run(['codesign','--force','--sign','-','--timestamp=none',str(bundle)],check=True)
    subprocess.run(['codesign','--verify','--strict',str(bundle)],check=True)
    print('Built AppKit status plugin',flush=True)
