# 第三条视频制作流程

## 1 锁定录制输入

使用当时应用 275、模型 212、头发动态 217。归档 `source/` 的运行时代码和原生玻璃按 276 发布记录的 SHA 找回；部分当时未提交，不能用当前同名源码替代。渲染还需要本地保存的 4999 面／58 骨骼／六表情模型、场景、材质、shader 和编辑器环境。

```sh
python3 docs/videos/archive.py verify
python3 docs/videos/archive.py stage 03-cofish-features --apply
mkdir -p work/demo-cofish-276/assets work/demo-cofish-276/audio
```

放入 `release.json` 列出的鲸鱼图、原始示意图、Icon-v6 预览、MenuIcon-v7 PDF，以及第一版女声源和伴奏。输入素材只在本机准备。这里的 `scripts/FishCoFishMovie.cs` 是已发布录制脚本，先在准备好的历史工程中核对，不自动写入当前 `Assets/`。

## 2 Unity 透明模型序列

```sh
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod DesktopFish.Editor.FishCoFishMovie.Render \
  -logFile work/demo-cofish-276/Unity-render.log
```

输出完整 1800 帧、每帧任务与动作 `metadata.jsonl`、模型验证 JSON。预览使用 `COFISH_MOVIE_PREVIEW=1`。必须看到 `PASS: CoFish movie`，没有编译／shader／执行异常，面数 4999、骨骼 58、取景边距通过。

锅位置使用经过变换的 `BoxCollider.center` 和宿主窗口坐标，不能使用过期 `Collider.bounds` 或再次加 40 pt 偏移。重生成模型序列时同步重生成 metadata；完成通知的 UUID 排序跨运行可能改变，不把不同次序的旧 metadata 拼进新帧。

## 3 原生玻璃合成录屏

直接编译发布时最终的 `capture_movie.m`，宏指向本目录保存的玻璃源码：

```sh
xcrun clang -fobjc-arc -framework Cocoa -framework QuartzCore \
  -framework ScreenCaptureKit \
  "-DFISH_SOURCE=\"$PWD/docs/videos/03-cofish-features/source/Native/FishStatusGlass/FishGlassStatus.m\"" \
  work/demo-cofish-276/capture_movie.m -o work/demo-cofish-276/capture_movie
codesign --force --sign - --timestamp=none work/demo-cofish-276/capture_movie
/usr/bin/caffeinate -di work/demo-cofish-276/capture_movie \
  "$PWD/work/demo-cofish-276" preview
```

检查预览后将 `preview` 改为 `full`，要求成功标记 `PASS: CoFish native movie capture 1800 frames`。任务 TTL 在宿主内转换为未来时间，使用虚构 UUID 和隔离偏好；ScreenCaptureKit 仅包含自身进程，Mac 保持解锁。逐帧检查原生 Regular 玻璃、卡片数、折叠展开、已读后列表保持展开以及示意图透明度。

`make_capture.py` 是当时从第二条录屏基底生成宿主的过程脚本；`capture_demo_base.m` 保留该基底。最终发布源以 `capture_movie.m` 和 SHA 为准，不依赖当前根目录的 `capture_demo.m`，也不要求再次运行文本替换生成器。

## 4 混音与导出

```sh
"$VIDEO_PYTHON" work/demo-cofish-276/mix_audio.py
"$VIDEO_PYTHON" work/demo-cofish-276/compose_final.py
```

混音使用第一版的 `natural-young.wav`、词边界 JSON 和原始 `instrumental.wav`。人声原增益 ×0.5，伴奏 ×0.80，结尾延长采用 0.25 秒交叉渐变；不开全局归一化。相位误差断言仍为小于 0.01 度，不让嘿哟进入欢呼段。

`compose_final.py` 验证全部 1800 帧审计、指定任务状态和示意图关键帧，再合成 H.264 / AAC 192k，检查 1080×1080、30 FPS、60 秒、完整解码，并提取 24 个编码后探针、海报和分镜图。

## 5 封面与验收

按 `prompts/` 中的实际图像编辑 prompt 制作封面，以正确的当时人物／道具截图和批准的应用图标作为参考。检查 16:9 完整图和居中 4:3 裁剪，文字与完整角色、锅和发电机均在安全区。发布的 README 封面身份与源原片分开记录。

检查全片没有任务数量触发缩放、没有裁切头发或发电机，已读完成卡片不会折叠其他列表。菜单图形是示意，不据此宣称 Dock 图标、系统菜单或所有多屏操作经过实际验收。

`finalize_review.py` 保留了原审阅记录生成过程，但硬编码当时的 PID 98952 和保护清单；只作历史记录，不直接用于当前验收。新制作应重新采集保护快照和实际进程身份。原片身份用 `archive.py verify --local-media` 核对，所有成片、音轨、帧和应用二进制不入 Git。
