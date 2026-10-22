# codex-bigfish

基于 Unity 的 macOS 3D 桌面伴侣，适配大肥鱼角色的二头身比例、圆润手形与程序动作。

支持透明置顶窗口、拖动、中文右键菜单、表情切换、鼠标跟随，以及挥手、行礼、小憩、小碎步和欢呼。默认窗口为 240×320 原生点，30 FPS。

## 重建模型

本仓库包含 Unity 源码、程序生成的重建模型、绘制纹理，以及可编辑的 Blender 和 GLB 文件。当前模型仍在细化，旧版本和本地参考资产不随仓库分发。

运行时模型位于 `Assets/DesktopFish/Rebuilt/`，生成器为 `FishRebuiltModel.cs`；可编辑文件位于 `Modeling/RebuiltBigFish/`。当前使用两个材质槽、一张绘制纹理、51 根骨骼和 6 个表情通道。具体面数见生成目录中的 `provenance.json`。

## 构建

1. 安装 Unity **2022.3.22f1 Apple Silicon** 和 Xcode Command Line Tools。
2. 在 Unity 中打开工程，选择 **Desktop Fish → Rebuilt Model → Render Preview**，生成模型、场景和动作预览。
3. 执行 `./Build.command`，或选择 **Desktop Fish → Rebuilt Model → Build macOS App**。应用位于 `Builds/RebuiltBigFish.app`。
4. 修改建模后，按照 `Modeling/RebuiltBigFish/README.md` 同步生成 Blender/GLB 文件。

命令行可通过 `UNITY_EDITOR_PATH` 环境变量或本机 `.unity-editor-path` 文件指定 Unity 可执行文件。重建模型的构建不需要导入额外角色包。

## 本地状态接口

```sh
python3 set-state.py working
python3 set-state.py complete
python3 set-state.py idle --action wave
```

状态包括 `idle`、`working`、`complete`、`waiting`、`error`；动作包括 `wave`、`bow`、`doze`、`step`、`cheer`。

桌面 Codex 自动状态联动：执行 `python3 Integrations/codex/install-observer.py` 安装本机后台监听器，只提取本机会话日志的生命周期事件。此方式兼容当前客户端，依赖非稳定的日志格式；官方 hooks 适配器另保留用于 CLI。官方 Mini 继续负责聊天，Unity 作为独立 3D 窗口响应状态。详见 [接入说明](Integrations/codex/README.md)。

## 实现与验证

重建模型的顶点、拓扑、骨骼、纹理和表情由工程生成。当前 Unity macOS 构建通过；本轮后发拓扑调整后检查了 8 个头部跟随、摆动和工作状态样本，覆盖无效顶点与画面边界。此前做过 150 个动作姿势检查，本轮未重复全套。GLB 已检查纹理、UV、顶点颜色、骨骼和初始表情权重。尚未专项测量 CPU/GPU 帧耗时，动作检查不等于视觉效果达到参考水平。

正面、35 度和侧面预览由 `FishRebuiltModel.RenderAngles` 生成。设计与旧版验证记录见 `README.txt` 和 `STYLE_REVIEW.txt`；本地视觉对照工具需要另外准备参考文件。

视觉与动作参考：[PC2005-cloud/dsh-pet](https://github.com/PC2005-cloud/dsh-pet)。运行时使用自身的 3D 网格与程序动作。

开源依赖包括 MToon（UniVRM 0.128.1）、UniWindowController 0.9.8，以及离线 meshoptimizer；各自许可随源码保留。
