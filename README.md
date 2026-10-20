# codex-bigfish

基于 Unity 的 macOS 3D 桌面伴侣，适配大肥鱼角色的二头身比例、圆润手形与程序动作。

支持透明置顶窗口、拖动、中文右键菜单、表情切换、鼠标跟随，以及挥手、行礼、小憩、小碎步和欢呼。默认窗口为 240×320 原生点，30 FPS。

## 模型需自行导入

本仓库包含源码、工程配置与开源依赖，**不包含角色模型、贴图、生成网格或已打包应用**。

## 构建

1. 安装 Unity **2022.3.22f1 Apple Silicon** 和 Xcode Command Line Tools。
2. 将本地模型导入 `Assets/Mzkagami/`，保持贴图与材质引用。该目录下需有唯一的角色 Prefab。当前工程用于桌宠，不依赖 VRChat SDK；导入后需检查、移除或禁用仅用于 VRChat 的组件，并确认模型正确显示。
3. 在 Unity 中打开工程，选择 **Desktop Fish → Create Scene**。场景及 Q 版网格会在本机生成。
4. 执行 `./Build.command`，或选择 **Desktop Fish → Build macOS App**。应用位于 `Builds/BigFatBlueFish.app`。

命令行可通过 `UNITY_EDITOR_PATH` 环境变量或本机 `.unity-editor-path` 文件指定 Unity 可执行文件。减面工具首次构建时由 `clang++` 编译。

## 本地状态接口

```sh
python3 set-state.py working
python3 set-state.py complete
python3 set-state.py idle --action wave
```

状态包括 `idle`、`working`、`complete`、`waiting`、`error`；动作包括 `wave`、`bow`、`doze`、`step`、`cheer`。这是本地文件接口，尚未自动订阅 Codex 聊天状态。

## 实现与验证

程序生成独立的 Q 版网格和绑定数据，保留 137 个表情。当前本机角色从 65,083 个三角面降至 43,815 个；没有测量 CPU/GPU 帧耗时收益。原版 VRChat PhysBone 尚未完整迁移。

Unity 编辑器工具可渲染预览、采样动作并检查表情。`FishReadabilityReview` 的 2D 对照步骤另需在本机准备 `work/reference-dsh/front-reference.png`；参考视频和图片不随仓库分发。最近一次本机构建及桌面显示检查通过。设计与历史验证记录见 `README.txt` 和 `STYLE_REVIEW.txt`。

视觉与动作参考：[PC2005-cloud/dsh-pet](https://github.com/PC2005-cloud/dsh-pet)。运行时使用自身的 3D 网格与程序动作。

开源依赖包括 MToon（UniVRM 0.128.1）、UniWindowController 0.9.8，以及离线 meshoptimizer；各自许可随源码保留。
