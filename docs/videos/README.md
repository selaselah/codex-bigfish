# 已发布视频制作档案

三个目录分别对应用户确认已发布的三个最终版本。这里保存分镜、原始需求、生成 prompt、实际交付脚本、素材清单与复现流程；视频、音频、渲染帧、录屏应用和临时报告保留在本地 `work/`、`outputs/`，不入 Git。制作日期不代表平台发布日期。

| 视频 | 制作完成 | 最终版本 | 规格 | 目录 |
|---|---|---|---|---|
| 手摇发电机与任务电饭锅 | 2026-10-29 | 191，替换 190 的机械配音 | 50 秒，1080×1080，30 FPS | [01 手摇发电机](01-generator/README.md) |
| 鲸鱼背景与玻璃任务卡片 | 2026-10-31 | 211，修订 210 的示意图时序 | 50 秒，1080×1080，30 FPS | [02 任务卡片](02-task-cards/README.md) |
| CoFish 新图标与新功能 | 2026-11-08 | 276，含封面制作 | 60 秒，1080×1080，30 FPS | [03 CoFish 新功能](03-cofish-features/README.md) |

## 每个目录的内容

- `SCRIPT.md`：发布版分镜和准确时间轴。
- `PROMPTS.md`：用户原始要求与实际保存的生成 prompt，区分最终方案和修订过程。
- `WORKFLOW.md`：制作顺序、命令、输入、验收与复现限制。
- `scripts/`：当时实际使用的代码快照；不是要求当前应用切回旧实现。
- `source/`：录制所需的历史源码，第三版同时保存尚未单独提交的运行时代码快照。
- `timeline.json`、`release.json`：机器可读时间轴、原片指纹、输入素材和归档源码的 SHA256。

归档代码保持发布时内容，包括历史 `work/` 目录布局。不要直接从 `docs/` 路径运行这些旧脚本：部分脚本依赖自身所在层级。先按各版流程恢复到本地工作目录。`finalize_review.py` 等历史审阅脚本含当时的 PID 和保护清单，仅作过程记录，不能用它判断当前应用是否通过验证。

## 检查和恢复

以下检查只依赖 Python 标准库，不启动 Unity、不联网，也不读取真实聊天：

```sh
python3 docs/videos/archive.py verify
python3 docs/videos/archive.py verify --local-media
```

第一条验证三组档案与源码指纹。第二条另核对本机三份最终 MP4；其他机器没有本地成片时会明确报缺失，归档本身仍可单独检查。

恢复可执行的本地制作脚本，默认先预览；仅 `--apply` 写入，且只写 `work/`，遇到内容不同的已有文件就停止：

```sh
python3 docs/videos/archive.py stage 03-cofish-features
python3 docs/videos/archive.py stage 03-cofish-features --apply
```

模型与参考贴图、已生成的人声、鲸鱼图、示意图及图标仍是本地输入，位置与指纹见各版 `release.json`。归档不包含第三方角色参考资产；不能将现在的模型或运行时代码当作历史版本。旧版本的代码基线和已保留的快照用于准备对应环境，当前运行中的应用不受本档案整理影响。

## 制作环境

Unity 2022.3.22f1 Apple Silicon、macOS 原生 AppKit / ScreenCaptureKit、Xcode Command Line Tools、FFmpeg / FFprobe。玻璃录屏需要支持原生 Liquid Glass 的系统，录制期间保持解锁；只捕获独立演示进程，使用虚构任务，不连接真实聊天。

Python 依赖记录在 [requirements.txt](requirements.txt)。声音生成步骤使用 `edge-tts` 在线生成固定文本；已有缓存时可离线混音。神经语音服务与系统玻璃渲染会随版本变化，源码和参数可恢复不等于重新渲染会逐字节复现原片。

已授权展示的 README 封面继续保留在 `docs/images/cofish-cover.png`；其生成 prompt 收录在第三版。其他导出产物统一由根目录 `.gitignore` 排除。
