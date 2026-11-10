from pathlib import Path
from datetime import datetime, timezone
import hashlib, json, plistlib, subprocess

ROOT = Path(__file__).resolve().parents[2]
WORK = Path(__file__).resolve().parent
OUT = ROOT/'outputs/CoFish-Demo-276'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
now = datetime.now(timezone.utc).isoformat()
protected = json.loads((WORK/'Protected-SHA.json').read_text())
changed = [name for name,h in protected.items() if not (ROOT/name).is_file() or sha(ROOT/name)!=h]
prefs = subprocess.run(['defaults','export','local.desktop.rebuiltbigfish.tasknotifications','-'], capture_output=True,check=True)
prefs_same = plistlib.loads(prefs.stdout)==plistlib.loads((WORK/'Before-Preferences.plist').read_bytes())
player = subprocess.run(['ps','-p','98952','-o','pid=,command='],capture_output=True,text=True,check=True).stdout.strip()
assert not changed and prefs_same and 'Builds/RebuiltBigFish.app/Contents/MacOS/' in player
protection = dict(protected_files=len(protected),changed=changed,real_preferences_unchanged=prefs_same,
                  real_player_pid=98952,player_present=True,old211_unchanged=sha(ROOT/'outputs/BigFish-Task-Demo-211/BigFish-Task-Demo-1080.mp4')=='936c2cd3f186d47fa93a62440baeaed2634cc4da6e6b1ccceff40746e75a264b')
assert protection['old211_unchanged']
(WORK/'Protection-Validation.json').write_text(json.dumps(protection,indent=2)+'\n')
paths = [ROOT/'Assets/DesktopFish/Editor/FishCoFishMovie.cs', ROOT/'Assets/DesktopFish/Editor/FishCoFishMovie.cs.meta',
         ROOT/'Native/FishStatusGlass/FishGlassStatus.m', ROOT/'Native/FishStatusGlass/capture_demo.m',
         ROOT/'Assets/DesktopFish/Runtime/FishMotion.cs', ROOT/'Assets/DesktopFish/Runtime/FishWorkStation.cs', ROOT/'Assets/DesktopFish/Runtime/FishTaskLedger.cs',
         ROOT/'Design/CoFish/Icon-v6/Previews/CoFish-Default-512.png', ROOT/'Design/CoFish/MenuIcon-v7/MenuBar/CoFish-Menu.pdf',
         WORK/'capture_movie.m',WORK/'plan.json'] + list(WORK.glob('*.py'))
(WORK/'Source-SHA.json').write_text(json.dumps({str(p.relative_to(ROOT)):sha(p) for p in paths},ensure_ascii=False,indent=2)+'\n')
delivery=json.loads((WORK/'Delivery-Validation.json').read_text());assert delivery['passed']
features = ['approved applicationIcon-v6 and monochrome menuIcon-v7','current4999tri avatar and accepted217 hair inertia',
            'native Regular glass/outer-glass small controls; hover close/hide','three task presentation modes/animation',
            'cooker promotes its chat to stack front; same-front shake','notification close leaves live cooker; click cooker restores card',
            'status drag demonstration with native dock following','retained completed bowl and green card',
            'same-chat continuation green-to-white, bowl-to-cooker; one card per chat',
            'completed-card click dismisses linked bowl/card; remaining list stays expanded',
            'menu settings entry illustration','fixed-size overlapping cooker/bowl pile and per-run strip upgrades']
review=dict(iteration=276,type='video only; application275/model212/motion217 unchanged',request='New video including latest application/menu icons and features since211',
            created_utc=now,delivery=delivery,features=features,timeline='plan.json',
            visuals='24 encoded probes and28 native preview frames inspected; native Regular lenses captured with macOS compositor',
            capture=dict(source='current saved Unity scene/runtime; offline fake chats; production native UI compiled in separate host',
                         privacy='ScreenCaptureKit filtered to own offline process; live desktop/other apps excluded',
                         menu='Presentation graphic using exact accepted menu PDF, not a recording of the system menu bar',
                         interactions='Scripted native API/action calls; overlay cursor; no real Codex chat opened or hidden',
                         drag='Offline avatar window moved, native anchored status/card windows follow; not a multi-display clamp test',
                         avatar_scale='one fixed movie camera for all counts; station original1.75 prop scale',
                         diagram='12..17 seconds; .5s in/4s hold/.5s out, before folded state'),
            model_validation=json.loads((WORK/'model-validation.json').read_text()),
            audio_validation={k:v for k,v in json.loads((WORK/'audio-validation.json').read_text()).items() if k!='schedule'},
            preservation=protection,source_manifest='Source-SHA.json',
            failures_and_corrections=[
                'First native preview rejected fake expired10000 timestamps; only offline host translates expiry to future wall clock.',
                'Preview exposed missing redraw on changing title/icon/diagram; explicit needsDisplay fixes compositing.',
                'Initial encoded cursor used stale Collider.bounds and extra40pt offset; rejected image retained, now direct transformed BoxCollider center/window coordinates.',
                'Cross-run preview equality failed after simultaneous completions because generated notification UUID tie order varies; no equality claim. Final full model rerender produces matching metadata for final capture.',
                'No production validator, tolerance, geometry, icon or material source changed.'],
            limitations=['60s scripted showcase, not exhaustive UI/animation/collision/performance QA',
                         'Existing261 actual Dock-display feedback remains independent; video shows approved icon assets',
                         'Settings entry only; no settings content invented'],
            committed=False,pushed=False)
(WORK/'Review.json').write_text(json.dumps(review,ensure_ascii=False,indent=2)+'\n')
artifacts = list(OUT.rglob('*.png'))+[OUT/'CoFish-Demo-1080.mp4']
artifacts += [WORK/n for n in ['Review.json','Source-SHA.json','Delivery-Validation.json','audio-validation.json','model-validation.json','Protection-Validation.json','native-frame-audit.json','FFprobe.json']]
(WORK/'Artifact-SHA.json').write_text(json.dumps({str(p.relative_to(ROOT)):sha(p) for p in artifacts},ensure_ascii=False,indent=2)+'\n')
(OUT/'README.md').write_text('''# CoFish 新版演示视频

60 秒，1080×1080，30 FPS。开头 idle 3 秒，沿用鲸鱼背景与原配乐；“嘿哟”人声保持之前降低50%的音量，并重新对齐摇柄节奏。

内容包括新应用／菜单栏图标、发丝惯性、玻璃卡片、点锅切换／抖动、悬停关闭／隐藏按钮、拖动状态栏、完成后保留米饭与绿卡、继续聊天时原卡片变白、已读后保持其余列表展开，以及多任务原尺寸堆叠。

使用离线演示任务与当前运行代码制作，没有改动真实聊天或运行中的应用。菜单片段为采用正式图标的入口示意。详细时间轴及检查见 ../../work/demo-cofish-276/Review.json。
''')
progress=ROOT/'work/continuation-20261024-1800.json';state=json.loads(progress.read_text())
assert state['application_iteration']==275
state['previous_video']={'iteration':211,'video':state['latest_video'],'commit':state.get('latest_video_commit')}
state['latest_video_iteration']=276;state['latest_video']=delivery['video'];state['latest_video_review']='work/demo-cofish-276/Review.json'
state['latest_video_commit']=None;state['latest_video_features']=features;state['last_updated_utc']=now
progress.write_text(json.dumps(state,ensure_ascii=False,indent=2)+'\n')
with (ROOT/'work/overnight-modeling.md').open('a') as f:
    f.write(f'''\n\n## 276 New CoFish feature showcase video (2026-11-08)\n\n60sec/1080square/30fps1800frames: approvedIcon-v6/Menu-v7, current4999tri/58bone avatar and217hair inertia, nativeRegular task modes/promote/shake/hover controls, status drag, retainedrice/green completion, same-chat green->white, linkedack keeps remaininglist expanded, menuentry illustration and275fixed-size piles/strip upgrades. Idle3sec, originalwhale background, supplieddiagram12..17with.5s fades. Current savedscene/runtime and native274views in isolatedown-process SCK host; fake sessions/no realdeep-links/bridge orprefs writes. ExactmenuPDF used in presentationgraphic, not actualsystemmenucapture; no multi-displayclamp/Dock proof claimed.\n\nInitialexpiredfakeTTL failedgate; host clocks corrected. Preview redraw/title/diagram omission fixed. Firstencodedcursor was belowpot due staleCollider.bounds+40ptoffset; rejectedimage retained, corrected transformedBoxCollider.center/windowcoords. Cross-runcompletionUUIDtie order makes end-bowl arrangement differ, equality assertion rejected; final all1800model frames and correspondingmetadata regenerated together. Finalnative1800frame audit, scene4999tri/58bones/margin gate, phase-sync62syllables(maxerror<.002deg), 60secAAC/H264 full decode and24encodedprobes inspected. Acceptedstockyouthfulfemininevoice*.5, originalmusic*.8 extended with.25sec crossfade, no normalization.\n\n402 protectedruntime/generated/scene/settings/Design/selected-and-legacyappfiles exact; realnotificationpreferences and originalPlayer98952 retained, old211video SHA exact, offlinewindows/processes ended. Neweditor moviehelper only; application275/model212/motion217 remain. No application build/commit/push. Review work/demo-cofish-276/Review.json; video {delivery['video']}. Prior261 actualDock-displayfeedback remains separate. {now}\n''')
print('Review/progress saved; protected402 exact, live Player retained')
