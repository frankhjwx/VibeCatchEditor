# 研究依据与待验证项

核对日期：2026-08-31。

## 需求来源

已阅读用户提供的[历史任务](codex://threads/01a057bf-132d-7f43-9f21-aa5bb977cbe3)。本轮需求明确独立项目、先做编辑器界面、C# / .NET 8、Win32 与自绘，并允许 DX11、DX12 或 Vulkan。

用户确认的核心工作流包含 Catch beatmap 读写、4/5/6/7/8/9/12/16 分拍 fruit 编辑、手绘贝塞尔 slider 转换为 fruit stream，以及音乐播放、暂停和进度条拖动。`.osu` 读写由本项目直接按 stable 格式实现。先做界面是实施顺序；这些功能都属于首个可用版本。

## 历史技术研究的可复用部分

以下为历史任务中的源码研究结论与来源，本轮未重新执行转换或 stable 实机对照：

| 结论 / 用途 | 来源 |
| --- | --- |
| stable 兼容转换和整图 RNG 值得单独研究；相同 seed 不足以保证全部兼容 | [Viewer 的 Catch 转换实现](https://github.com/Exsper/osucatch-editor-realtimeviewer/blob/6ce250223658a7481d6c622a3d8728d51363a558/osucatch-editor-realtimeviewer/BeatmapConverterOsuStable.HitObjectManagerCatch.cs) |
| 数值行为可能影响对象数量，继而改变后续随机序列 | [Viewer 的 slider 数值兼容说明](https://github.com/Exsper/osucatch-editor-realtimeviewer/blob/6ce250223658a7481d6c622a3d8728d51363a558/osucatch-editor-realtimeviewer/BeatmapConverterOsuStable.LegacySliderAdditionalData.cs) |
| 时间—X 到二维 slider 的算法参考 | [ppy/osu JuiceStreamPath](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Objects/JuiceStreamPath.cs) |
| 上游路径转换有测试，但不能据此声称本工具兼容 stable | [JuiceStreamPathTest](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch.Tests/JuiceStreamPathTest.cs) |
| EditorReader 适用于读取现有编辑器状态；独立项目不依赖它启动 | [EditorReader](https://github.com/Karoo13/EditorReader) |
| 最终输出格式与对象字段的核对入口 | [osu! 官方文件格式](https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29) |

上述仓库是研究或后续算法参考，并非当前工程已经加入的依赖。引入代码前重新读取固定 commit 的实现与许可证，记录实际采用的文件。

本轮核对的 lazer 功能参考包括 beat divisor、fruit 放置、贝塞尔控制点、slider/Catch 转换、播放控制和 seek。具体方法、适配边界及固定链接见 [lazer 实现参考](LAZER_REFERENCE.md)。stable 文件实现依据见 [stable 规范](STABLE_FORMAT.md)。

## 本轮已核对的技术资料

| 事实 | 来源 |
| --- | --- |
| .NET 8 支持到 2026-11-10 | [.NET 官方支持政策](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) |
| DX12 比 DX11 要求应用显式承担更多同步与资源管理职责 | [Microsoft Direct3D 说明](https://learn.microsoft.com/en-us/windows/win32/direct3d12/important-changes-from-directx-11-to-directx-12) |
| Vulkan 的显式同步责任 | [Khronos Vulkan Guide](https://docs.vulkan.org/guide/latest/synchronization.html) |
| Direct2D 与 Direct3D 可通过 DXGI surface 互操作 | [Microsoft 互操作说明](https://learn.microsoft.com/en-us/windows/win32/direct2d/direct2d-and-direct3d-interoperation-overview) |
| DIP 与物理像素需要按 DPI 换算 | [Microsoft DPI 说明](https://learn.microsoft.com/en-us/windows/win32/learnwin32/dpi-and-device-independent-pixels) |
| Vortice 主线 README 声明 .NET 9/10 目标 | [Vortice README](https://raw.githubusercontent.com/amerkoleci/Vortice.Windows/main/README.md) |
| 两个 3.6.2 图形绑定包包含 net8.0 目标 | [Direct3D11](https://www.nuget.org/packages/Vortice.Direct3D11/3.6.2)、[Direct2D1](https://www.nuget.org/packages/Vortice.Direct2D1/3.6.2) |

以上支持 DX11 首版方案的可行性判断，不构成本项目已编译或已运行的证据。

## 尚未验证与默认决策

- **依赖组合**：未下载或编译 Vortice 候选版本，尚未验证其完整传递依赖和本机呈现。M1-01 负责。
- **运行时生命周期**：文档按 .NET 8；到期前的升级需要实际实施与回归，不因安装了 .NET 10 SDK 自动完成。
- **视觉与交互偏好**：主画布按用户要求以底部为时间起点，时间向上递增。深色主题、固定分区和快捷键为首版默认，需通过可运行界面评估。
- **中文编辑与无障碍**：中文显示是 M1 验证项；完整 IME 文本编辑和辅助技术支持尚未设计或验证。
- **格式与兼容目标**：`.catchproj` 为草案；`.osu` 明确以 stable 格式为目标，首版写出 v14 / Mode=2；Catch 行为先验证 NM，目前没有逐对象实机精度结论。
- **音乐后端**：已核对 lazer 的 transport 与时钟设计，尚未在本项目验证实际解码和有声播放；这是 M2 必须完成的工作。
- **贝塞尔适配**：锚点和控制柄、时间单调约束、自适应采样为本项目实现方案，尚未完成与真实导出 stream 的对照。
- **性能与平台**：尚无本项目性能测量，也未声明跨平台、多后端或所有 Windows 环境支持。
