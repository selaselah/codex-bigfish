# 第二条视频制作流程

## 1 准备输入

使用提交 `be0bffb47d840ccf5c68bf2b57308c0b3ab49bdd` 对应代码和当时 4999 面、58 骨骼、六表情模型。当前模型、玻璃组件和运行时代码若已变化，应先按版本与 SHA 准备历史输入，不能直接声称得到相同画面。

```sh
python3 docs/videos/archive.py stage 02-task-cards --apply
mkdir -p work/demo-native-211/assets work/demo-native-211/audio
```

本地放入 `release.json` 列出的 `Whale-Icon.png`、`Interaction-Diagram.png`、第一版 `voice-only.wav` 与原伴奏。原始参考图不复制进 Git。

## 2 Unity 渲染

`scripts/FishTaskDemoVideo.cs` 的历史输出目录是 `work/demo-native-210`，即使最终视频版本是 211 也不改该目录。确认对应工程内脚本与归档一致后：

```sh
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod DesktopFish.Editor.FishTaskDemoVideo.Render \
  -logFile work/demo-native-210/Unity-render.log
```

输出 1500 张 936×1248 透明模型帧和 `motion-timing.csv`。`FISH_TASK_VIDEO_PREVIEW=1` 仅生成探针，不可作为全片通过。将完整模型帧、验证 JSON 与时序 CSV 复用到 211 目录，原制作使用同仓库内链接；不要再次移动相机或缩放人物。

## 3 构建独立原生录屏宿主

`scripts/Info.plist` 保留原始独立应用标识；`archive.py stage` 会恢复它。以下宏指向归档的 209 版生产玻璃源码，不引用当前可能已修改的组件：

```sh
mkdir -p work/demo-native-211/TaskMovie.app/Contents/MacOS
xcrun clang -fobjc-arc -framework Cocoa -framework QuartzCore \
  -framework ScreenCaptureKit \
  "-DFISH_SOURCE=\"$PWD/docs/videos/02-task-cards/source/Native/FishStatusGlass/FishGlassStatus.m\"" \
  work/demo-native-211/capture_demo.m \
  -o work/demo-native-211/TaskMovie.app/Contents/MacOS/TaskMovie
codesign --force --sign - --timestamp=none work/demo-native-211/TaskMovie.app
/usr/bin/caffeinate -di \
  work/demo-native-211/TaskMovie.app/Contents/MacOS/TaskMovie \
  "$PWD/work/demo-native-211" preview
```

保持 Mac 解锁，查看 16 张探针，尤其是示意图淡入淡出和折叠边界。通过后将最后一个参数改为 `full`，成功标记必须是 `Native task movie capture passed: 1500 frames`。只捕获宿主进程；虚构任务不读取或修改真实聊天。

## 4 声音与合成

```sh
"$VIDEO_PYTHON" work/demo-native-211/compose_demo.py audio
"$VIDEO_PYTHON" work/demo-native-211/compose_demo.py finish
```

`audio` 使用第一版人声 ×0.5 振幅，伴奏原始增益仍为 0.80；补齐开头 3–7 秒的工作音节，整周平移保持相位。211 实际交付沿用已通过的 210 音轨，没有再次混音。`finish` 要求完整原生录屏、模型验证和成功日志，检查全部 150 帧示意图透明度、玻璃及状态条坐标，再输出原片、分镜和海报。

## 5 验收

检查 1080×1080 / 30 FPS / 1500 帧 / 50 秒、全片解码；示意图 24–29 秒与两个 0.5 秒渐变；折叠开始于 29 秒；完成后发电机和两锅仍在。211 与 210 的 AAC 有效载荷应完全一致。

`inspect_fades.py` 是当时的抽帧审阅脚本，含制作机的绝对仓库路径；再次使用需先改成本机路径，不以历史脚本的运行结果冒充新版本验证。发布原片身份通过上一级归档工具核对；不提交 MP4、PNG、AAC、录屏应用或逐帧审计输出。
