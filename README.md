# VibeCatchEditor

# ⚠️ 仍在开发中，不提供工具使用教学

**本项目仍处于开发与验证阶段，功能、文件格式及交互可能变化，不保证稳定性或完整兼容性。请自行备份原始谱面与工程；作者不负责工具使用教学。**

**Work in progress. No tool usage tutorials or training are provided. Back up your beatmaps and projects before use.**

独立 Windows osu!catch 编辑器，使用 C# / .NET 8、Win32 窗口和 DX11 + Direct2D/DirectWrite 自绘界面。主画布为“横向位置 X × 向上递增的时间”。

**M2 已接入真实 `.osz` / `.osu` 打开、`.catchproj` 工程保存重开、stable v14 / Mode=2 输出，以及音乐播放、暂停和拖动定位。** 支持对象与锚点多选、批量操作及中英文界面；启动提供可编辑的演示 map。本轮验证见 [多选与本地化验收记录](docs/MULTISELECT_LOCALIZATION_ACCEPTANCE.md)。

## 启动与试用

Windows x64 上双击 [Run-Editor.cmd](Run-Editor.cmd) 构建并启动。构建 SDK 固定为 10.0.400，目标框架为 .NET 8；需要本机安装相应 SDK 和 .NET 8 运行时，当前输出不打包运行时。

本地最新验收程序为 `artifacts/M2-AudioExportFix/VibeCatchEditor.App.exe`。`artifacts/` 内的程序、谱面、日志和截图不提交到 Git；文档中指向该目录的链接仅供本地验收使用，克隆仓库后请自行构建。音频停止超时恢复、导出 SV 冲突修复及修复谱面见 [音频与导出修复验收](docs/AUDIO_EXPORT_FIX_ACCEPTANCE.md)。

启动后可看到 30 秒、120 BPM 的演示 map：

- `Ctrl + O` 打开 `.osz`、v14 Catch `.osu` 或 `.catchproj`；谱面包有多个难度时选择目标难度，关联音频自动加载，也可从文件菜单更换。
- 选择 `Fruit`，以 `1/4`、`1/6` 或自由时间放置和移动水果。
- `V` / `F` 下拖动空白框选完整对象，`Ctrl` 点选增减、`Ctrl` 框选叠加。slider 任一子 Fruit / Droplet / TinyDroplet 被选中即选整个父对象，多个子对象不会重复计入。
- 选中 slider 后按 `B` 编辑锚点：空白拖动框选，`Ctrl` 点选增减。未选 slider 时，B 点击创建无柄点、按住向上拖出曲线柄；已有 slider 编辑中用属性区“新 Slider”开始另一条。`Enter` 完成，`Esc` 取消草稿。
- 右键轨迹可插入无柄控制点，右键控制点可转换为曲线/直线控制点；邻点有柄时相邻段仍可弯曲，插点不保证保形。需要保持形状时使用原有分割按钮。
- 对象右键菜单与 `Ctrl + X / C / V` 支持整批剪切、复制、粘贴；最早起点对齐播放头，其余相对时间不变，父对象与节点均使用新 ID。`Delete` 在 V/F 下批量删除对象，在 B 下批量删除锚点（含端点）；不足两个锚点时删除整条 slider。每次批量修改可一步撤销。
- 点击左侧对象列表会完成至少两点的草稿或取消单点草稿，并切回选择工具。
- 在选择工具下点击 slider 的任意 Fruit / Droplet / TinyDroplet，按皮肤实际尺寸命中并选中整条。`.osu` 导入对象称为 Legacy Slider，可从属性或右键转换为 VCE Slider，再编辑节点、柄与行程次数；转换为一步撤销。
- 滚轮浏览时间，Ctrl + 滚轮以鼠标时间为中心缩放；“还原 AR 比例”恢复 AR 下落比例并定位当前时间。右上 AR / CS 可编辑。
- 主画布显示实际转换对象，曲线位于对象之上，选中不透明、未选中 50% 透明度，可整体隐藏。右侧预览的“调试曲线”独立控制，默认关闭。
- `Tiny 贴合`控制派生补偿；`Tick ×…`修改全图 SliderTickRate，与编辑吸附独立。转换失败或补偿无法达到目标时显示诊断。
- 仓库不附带默认皮肤。未加载皮肤时使用基础图形；“皮肤…”可选择本地 `.osk`，只导入 Catch PNG 与配置到 `artifacts/skins` 缓存。可选本地默认包见 [皮肤说明](assets/skins/README.md)。
- `Space` 播放/暂停，底部进度条点击或拖动 seek。播放线固定在主绘图区距底部 25% 处；播放、seek、AR 还原与 resize 保持该位置，起止允许留白，暂停时可手动平移。底部播放头与视口框连续移动。
- `Ctrl + S` 保存 `.catchproj`，`Ctrl + Shift + S` 另存工程，`Ctrl + E` 输出新 `.osu` 并报告回读误差；输出谱面不能代替保存可编辑工程。
- 顶部语言按钮切换中文与英文；界面和应用自有诊断使用资源键。用户文件、谱面标题、已存在的对象名称不自动翻译，也不因切换语言而变脏。
- Legacy Slider 保留 L/B/P/C 原表示与 osu Tiny RNG。转换后的 VCE Slider 保留父 ID、源顺序、repeat 与样本信息；首个 span 的节点由后续行程共享，并要求实际对象贴合目标曲线。

当前转换支持多 timing、继承 SV、repeat、香蕉雨、整图确定性 RNG 和 hyperdash 判定。网格随红点 BPM / offset / 拍号变化，绿点不重置拍格；slider 锁定起始 timing。香蕉在两视图静态使用 0.6 倍尺寸，不实现随机缩放动画。视频和 storyboard 不加载，皮肤旋转、命中特效等动画简化。

MP3 在后台连续解码为 PCM，再按采样帧 seek，时长来自实际帧数。该定位修复不等于硬件延迟校准；尚未进行 stable 客户端对照。没有音频时可手动定位，播放禁用。默认皮肤的公开再分发权限见 [第三方声明](THIRD_PARTY_NOTICES.md)。

播放绘制使用 `WM_PAINT` 持续请求重绘与 `Present(1)` 跟随显示器垂直刷新，没有固定 60 FPS 限制；这不是对实际刷新率或音画延迟的测量保证。

## 首个可用版本的目标

自由读取和输出 stable Catch `.osu`、保存可编辑工程、按 4/6 分拍编辑 fruit、手绘贝塞尔生成 slider / stream，以及真实音乐播放、暂停和拖动定位，均为必需能力。自有工程保存主节点与控制柄；`.osu`、生成路径与 RNG 是派生结果。首版输出目标为 v14 / Mode=2，由本项目自行读写；lazer 只作为功能和算法参考。

## 文档导航

| 文档 | 用途 |
| --- | --- |
| [AGENTS.md](AGENTS.md) | AI 开发入口与范围约束 |
| [产品定义](docs/PRODUCT.md) | 产品目标、当前能力与首版边界 |
| [技术架构](docs/ARCHITECTURE.md) | 实际模块、依赖、输入与资源生命周期 |
| [编辑器界面](docs/EDITOR_UI.md) | 当前布局、工具、图层和交互 |
| [工程与数据模型](docs/PROJECT_MODEL.md) | 创作数据、导入上下文、派生对象与持久化 |
| [Catch 绘制与转换](docs/CATCH_RENDERING.md) | AR、CS、皮肤、RNG、hyperdash 及验证边界 |
| [stable 文件规范](docs/STABLE_FORMAT.md) | `.osu` 读写契约 |
| [开发任务](docs/IMPLEMENTATION_PLAN.md) | 当前实现范围与后续验证 |
| [M1 验收记录](docs/M1_ACCEPTANCE.md) | 实际执行的检查、截图与限制 |
| [M2 验收记录](docs/M2_ACCEPTANCE.md) | 真实谱面、文件往返与首轮音频验证 |
| [M2 编辑验收记录](docs/M2_EDITING_ACCEPTANCE.md) | 前轮编辑、混合段、播放线与 PCM seek 验证 |
| [Slider 交互验收记录](docs/SLIDER_INTERACTION_ACCEPTANCE.md) | 前轮统一 Slider、控制点、右键菜单与剪贴板验证 |
| [多选与本地化验收记录](docs/MULTISELECT_LOCALIZATION_ACCEPTANCE.md) | 本轮多选、批量操作和中英文界面验证 |
| [本地化维护](docs/LOCALIZATION.md) | 语言表、占位符、校验与新增语言流程 |
| [lazer 实现参考](docs/LAZER_REFERENCE.md) | 固定上游源码入口 |
| [研究依据](docs/REFERENCES.md) | 技术研究依据 |
| [第三方声明](THIRD_PARTY_NOTICES.md) | 依赖、改编算法与资源许可 |
