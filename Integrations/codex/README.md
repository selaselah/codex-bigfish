# Codex 与大肥鱼联动

保留官方 **Mini** 的快速聊天、语音与活动控件；Unity 桌宠作为独立透明窗口接收本机任务状态。没有将 Unity 原生窗口嵌入 Mini，不修改客户端安装文件、不启动另一套模型、不使用 API Key。

## 当前桌面方案

```sh
python3 Integrations/codex/install-observer.py
```

安装一个当前用户的 macOS LaunchAgent，登录后自动运行 `observe.py`。它监听本机 `$CODEX_HOME/sessions`（默认 `~/.codex/sessions`）今天及昨天的会话文件，只提取生命周期事件；不保存用户提示、工具参数或回答正文，不发送网络请求。记录的是任务标识、状态、到期时间与事件时间。

桌面版当前使用自己的后台进程。实测本聊天没有运行新安装的 CLI hooks；共享 App Server 的运行状态也不包含这条聊天。因此采用本机会话事件日志兼容适配。**日志格式不是稳定的公开 API**，需随 Codex 升级验证。不能将它宣称为官方 Mini 插件接口。

| 本机事件 | 大肥鱼 |
| --- | --- |
| task_started、活动回合中的进度事件 | 工作中 |
| 可识别的审批/提问事件（若客户端写入） | 等待确认，属尽力支持 |
| task_complete | 本回合结束；其他聊天也空闲时庆祝 |
| turn_aborted、shutdown_complete | 清理该聊天状态 |

同时跟踪多个本机会话：等待确认优先，其次工作中，最后完成。一个聊天结束不会把其他仍在工作的聊天切回待机；但每个新结束的回合都会触发一次庆祝与 4 秒提示，即使还有其他聊天在工作。状态条显示剩余任务数量；所有聊天结束后 12 秒回到待机。回合结束不代表用户目标一定成功实现。未知事件不会被推测成错误或成功。

监听器每秒检查文件追加，每 5 秒发现新会话；首次只恢复最近的生命周期边界，不重播历史庆祝。状态一小时无刷新后过期，避免异常退出后永久卡住。启动前已经运行超过一小时且没有近期生命周期边界的任务，可能到新事件出现后才被识别。只覆盖本机今天和昨天目录中的会话，云端、其他主机和更早的长会话不在当前保证范围内。

健康记录位于 `~/Library/Application Support/LocalCompanions/BigFatBlueFish/codex-observer.json`。日志位于 `~/Library/Logs/LocalCompanions/BigFatBlueFish/codex-observer*.log`，仅用于程序异常，不写聊天正文。

移除桌面监听：

```sh
python3 Integrations/codex/install-observer.py --remove
```

移动工程后需重新安装，因为 LaunchAgent 指向当前工程路径。安装与移除均保留原模型、本地应用和其他后台服务。

## 可选的官方 CLI hooks 方案

```sh
python3 Integrations/codex/install.py
```

安装器合并 `$CODEX_HOME/hooks.json`，保留已有 handlers、修改前自动备份。然后在 Codex CLI 的 `/hooks` 中审阅并信任标记为 **BigFish: local desktop state** 的 8 个 hooks。安装器不会写信任记录或改变审批权限。

此方式已通过真实 CLI 回合验证，适合能够加载这些 hooks 的会话。请在两种方式中选用一种，避免重复事件。本机当前启用的是桌面监听器，先前的 8 个 hooks 已从配置中移除；其他 hooks 和原有 notify 设置保留。

```sh
python3 Integrations/codex/install.py --remove
```

## 验证

```sh
python3 -m unittest discover -s Integrations/codex -v
```

2026-10-20：真实桌面聊天“测试”的 task_started 被识别，桌宠状态为 working，后台监听器健康心跳正常。九项 Python 测试覆盖了多会话、旧回合晚到、重复完成、半行追加、重启不重播完成，以及安装时保留其他 hooks。Unity macOS 构建与等待/庆祝视觉检查通过。当前仍有其他聊天运行，因此本聊天结束后不应立即庆祝。

官方资料：[Mini 与 Pets](https://learn.chatgpt.com/docs/pets)、[Hooks](https://learn.chatgpt.com/docs/hooks)、[App Server](https://learn.chatgpt.com/docs/app-server)。

可见反馈：角色脚下显示工作/等待/休息及任务数，工作时轻微歪头点头、左右轻摆；右键菜单可关闭状态提示。完成指令保留 4 秒以跨过并发进度写入，Unity 按 command_id 去重，避免庆祝被反复重启。runtime-status.json 记录应用实际读取到的状态，便于区分传输与渲染问题。
