# 第一条视频制作流程

在仓库根目录执行。Python 环境需安装上一级 `requirements.txt`；FFmpeg / FFprobe 在原制作机位于 `/opt/homebrew/bin/`。以下是按已保存代码整理的复现命令，不是完整的原始终端会话。

## 1 准备历史输入

使用 `fc90ab041380ac7fc98045f62521f6cf170a8326` 对应的代码和当时本地保存的 151 模型、189 动作与道具。模型和参考图不在本档案中。归档的 `scripts/FishDemoVideo.cs` 包含 `Render`、`ExportAudioTiming`，不能把后来 4999 面模型的渲染称作第一条视频的原样复现。

恢复本地脚本：

```sh
python3 docs/videos/archive.py stage 01-generator --apply
mkdir -p work/demo-video-190/audio work/demo-voice-191/audio
```

## 2 渲染画面和动作时序

在已准备好的对应 Unity 工程中，确认 Editor 脚本与归档指纹一致，再运行：

```sh
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod DesktopFish.Editor.FishDemoVideo.Render \
  -logFile work/demo-video-190/Unity-render.log
"$UNITY_EDITOR" -batchmode -quit -projectPath "$PWD" \
  -executeMethod DesktopFish.Editor.FishDemoVideo.ExportAudioTiming \
  -logFile work/demo-voice-191/Unity-timing.log
```

检查 `Square demo video capture passed`、1500 帧、纯白背景和固定相机。编译、shader、执行方法异常必须停止后续处理。透明渲染的预乘边缘在原脚本中合成到白底。

## 3 恢复伴奏和女声

`generate_instrumental.py` 从原作曲脚本移除了已否决的人声输入，保留相同随机种子、乐器和完成音效；生成的 `instrumental.wav` 应与 `release.json` 中的伴奏 SHA 相同。

```sh
"$VIDEO_PYTHON" work/demo-video-190/generate_instrumental.py
"$VIDEO_PYTHON" work/demo-voice-191/generate_natural_variants.py
ffmpeg -y -i work/demo-voice-191/audio/natural-young.mp3 \
  -ac 1 -ar 48000 -c:a pcm_s16le work/demo-voice-191/audio/natural-young.wav
"$VIDEO_PYTHON" work/demo-voice-191/mix_voice.py
```

首次语音生成需要联网；有已接受的缓存时跳过生成。`mix_voice.py` 读取 1500 行 `motion-timing.csv`，使用实际工作权重、摇柄相位和 8 个音节切片；输出 50 秒、48 kHz 双声道音轨，伴奏原始增益 0.80，淡入 0.35 秒、淡出 1.25 秒。

## 4 编码与换音轨

当时的 191 直接复用已经编码的 190 画面流，只重新编码音轨。没有本地 190 成片时，可先从 1500 张白底帧建立新的无声中间片，H.264、CRF 18、yuv420p、BT.709、30 FPS：

```sh
ffmpeg -n -framerate 30 -i work/demo-video-190/frames/frame%04d.png \
  -c:v libx264 -crf 18 -pix_fmt yuv420p -color_primaries bt709 \
  -color_trc bt709 -colorspace bt709 -movflags +faststart \
  work/demo-video-190/video-only.mp4
```

下面保留历史换音轨命令，已有原片时不要覆盖它；新复现时改用新的输出文件名。若输入改用上述无声中间片，需要按 `timeline.json` 另建章节元数据，不能宣称继承了旧片章节：

```sh
mkdir -p outputs/BigFish-Work-Demo-191
ffmpeg -y -i outputs/BigFish-Work-Demo-190/BigFish-Demo-1080.mp4 \
  -i work/demo-voice-191/audio/soundtrack.wav \
  -map 0:v:0 -map 1:a:0 -map_metadata 0 -map_chapters 0 \
  -c:v copy -c:a aac -ar 48000 -b:a 192k -t 50 -movflags +faststart \
  outputs/BigFish-Work-Demo-191/BigFish-Demo-1080-YoungVoice.mp4
```

`scripts/compose_original_audio.py` 同时含 190 的旧机械人声混音，只作历史记录，191 不运行那个人声阶段。

## 5 验收

检查 1080×1080 / 30 FPS / 1500 帧 / 50 秒、全片解码无错；191 与 190 的压缩视频流应相同。检查 idle 和欢呼阶段无人声、工作权重至少 0.90、重音相位误差小于 0.01 度、音轨不削波，抽看 1/2/3 锅和完成后剩余两锅。

发布后原片通过 `python3 docs/videos/archive.py verify --local-media` 校验。神经语音服务版本更新后重新生成的声音可能不同，原片身份以已保存的 SHA 为准。
