# Native-glass task video

The 210 demo is a 50-second, 1080×1080, 30 fps offline recording. It uses the selected 4999-triangle model, runtime work animations, generator and cooker effects. Its status capsule and task cards are the production `FishGlassStatus.m` views with Regular Liquid Glass and the production three-state animation driver.

The recording host is a separate application, `local.desktop.rebuiltbigfish.offline210`. It supplies fictional task UUIDs, titles and summaries without reading or writing the live task bridge. ScreenCaptureKit includes only that host's process, with no cursor or other applications. Keep the Mac unlocked throughout capture; the capture display inventory is empty while locked. Use `caffeinate -di` during recording.

## Timeline

| Seconds | Model and task UI |
| --- | --- |
| 0–3 | Idle; task cards hidden |
| 3–11 | One job and cooker; cards hidden |
| 11–19 | Two jobs and cookers; cards hidden |
| 19–27 | Three jobs and cookers; cards hidden |
| 27–29 | Folded card stack |
| 29–31 | Supplied three-state diagram and “精美的交互设计（胡说）” |
| 31–34 | Expanded native cards |
| 34–37 | Cards hidden |
| 37–50 | Complete one job; lid/bowl/particles, generator and two cookers remain |

## Rendering and composition

Run Unity's `DesktopFish.Editor.FishTaskDemoVideo.Render` in batch mode from this repository. Confirm the success marker and reject compiler, shader or execute-method exceptions before continuing. `FISH_TASK_VIDEO_PREVIEW=1` produces only probe images; the final movie requires all 1500 images in `work/demo-native-210/model-frames`. The model pass uses a fixed camera and transparent 936×1248 images; runtime state is simulated without saving the scene or changing the selected model.

The native host displays those images in a 312×416 pt avatar canvas on a 540×540 pt background. The fixed movie camera has additional foot padding. The canvas bounds origin is therefore calibrated to Y = −40 pt, and the image view retains the same bounds, keeping the visible model fixed while raising the native dock by 40 pt. This correction belongs only to the offline composition. Production status positioning, avatar size and task animation are unchanged. The status stays below the pet with a fixed center across hidden/folded/expanded modes; cards expand upward.

Build the separate capture host, substituting this checkout's absolute path:

```sh
xcrun clang -fobjc-arc -framework Cocoa -framework QuartzCore \
  -framework ScreenCaptureKit \
  '-DFISH_SOURCE="/absolute/repo/Native/FishStatusGlass/FishGlassStatus.m"' \
  Native/FishStatusGlass/capture_demo.m \
  -o work/demo-native-210/TaskMovie.app/Contents/MacOS/TaskMovie
codesign --force --sign - --timestamp=none work/demo-native-210/TaskMovie.app
/usr/bin/caffeinate -di \
  work/demo-native-210/TaskMovie.app/Contents/MacOS/TaskMovie \
  /absolute/repo/work/demo-native-210 full
```

The local app bundle must have its offline identifier and an executable matching the output above. Use `preview` instead of `full` to inspect 11 probe frames. Capture must end with `Native task movie capture passed: 1500 frames`. `native-frame-audit.json` records task count, mode, animation, avatar reservation and fixed status coordinates per frame. Liquid Glass is asserted before every captured frame. Failed attempts and their logs are retained locally; a zero process exit alone is insufficient.

## Artwork and sound

Local inputs are `work/demo-native-210/assets/Whale-Icon.png` and `Interaction-Diagram.png`. The diagram is the exact supplied image. The whale is a transparent extraction/cleanup of the supplied printed whale, generated with the built-in image tool. Generation details and the exact prompt are recorded in `work/demo-native-210/production-notes.json`. Tiles are 88×59 pt on a 112×88 pt staggered grid, at 20% opacity over white.

`compose_demo.py audio` reuses the accepted 191 feminine “嘿哟” voice at 0.5 amplitude (−6.0206 dB). Four early syllables reuse the same voice clips at whole-turn offsets for the new 3-second idle opening. The accompaniment retains the accepted source, 0.80 source gain and original fades. There is no global normalization that would change its level. The audio report checks crank-phase alignment and clipping.

With the local NumPy/Pillow environment available, run:

```sh
work/demo-native-210/venv/bin/python Native/FishStatusGlass/compose_demo.py audio
work/demo-native-210/venv/bin/python Native/FishStatusGlass/compose_demo.py finish
```

`finish` requires the successful native `video-only.mp4`. It muxes the soundtrack and chapters, checks 1080×1080 / 30 fps / 1500 frames / 50 seconds, decodes the full movie, and creates an encoded-frame storyboard and poster. Inspect the encoded samples, particularly the fixed status position, animation transitions, diagram caption, and completion effect. The local delivery is `outputs/BigFish-Task-Demo-210/BigFish-Task-Demo-1080.mp4`; generated media and reference inputs remain local. Preserve selected model/runtime hashes, both application manifests and project settings before submission.
