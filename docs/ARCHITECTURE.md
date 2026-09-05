# 技术架构

## 实际技术组合

| 层 | 当前实现 | 职责 |
| --- | --- | --- |
| 应用 | C# 12 / .NET 8，Windows x64 | 编辑状态、布局和宿主协调 |
| 窗口 | Win32 / HWND | 消息循环、鼠标键盘、DPI、原生对话框 |
| 图形 | Direct3D 11 + DXGI | 设备、交换链、呈现和 resize |
| 绘制 | Direct2D / DirectWrite | 自绘面板、网格、曲线、控件和文字 |
| PNG | Windows Imaging Component | 解码为预乘 alpha 像素，再由 Direct2D 绘制 |
| 绑定 | Vortice 3.6.2 | DirectX COM 接入 |
| 音频 | NAudio / WinMM、Media Foundation、NVorbis | 真实输出、MP3 / OGG / WAV 解码、播放位置与 seek |

保留系统标题栏、窗口边框和原生文件对话框，客户端布局与交互由项目自己实现。DX11 为当前唯一后端；不提前建立多后端、渲染图或插件框架。

`global.json`固定 SDK 10.0.400，`Directory.Build.props`固定 C# 12。App 目标为 `net8.0-windows`，Core 为 `net8.0`。已还原并编译的直接包为 Vortice.Direct3D11 / Direct2D1 / DXGI 3.6.2；锁文件还固定 DirectX 3.6.2、Mathematics 1.9.2、SharpGen.Runtime / COM 2.2.0-beta。包与许可证见[第三方声明](../THIRD_PARTY_NOTICES.md)。当前构建使用本机 .NET 8 运行时，不附带运行时。

## 模块边界

```text
src/
  FruitsAtelier.App/
    Platform/      Win32、输入、DPI、文件对话框、受限谱面包与皮肤 ZIP 导入
    Audio/         串行 transport、设备时钟、MP3 分块 PCM 缓存与音频生命周期
    Rendering/     ICanvas、DX11/D2D/DWrite、WIC、图像缓存
    Editor/        自绘布局、命中、控制点交互、内部剪贴板、数值输入与转换缓存
    Skinning/      Catch PNG 映射、skin.ini、密度和裁剪
    Diagnostics/   日志、离屏 render-check、真实谱面 M2 检查
  FruitsAtelier.Core/
    Model/         FSlider、Legacy Slider / 香蕉、timing、原始文件上下文
    Formats/       自有 stable v14 reader/writer、工程 JSON、原子写入
    Editing/       文档事务、撤销、时间—X 变换
    Timing/        多 timing 查询、局部节拍吸附与网格、Catch AR 下落比例
    Curves/        时间单调的混合线性/贝塞尔段、求值、保形分割和约束
    Conversion/    Legacy 转 FSlider、两类 repeat、香蕉、RNG、Tiny 补偿与诊断
    Gameplay/      CS 尺寸与完整对象序列的 hyperdash 判定
    Localization/  英文主表、语言表发现、格式化与键校验
tests/
  FruitsAtelier.Core.Tests/
  FruitsAtelier.App.Tests/
  FruitsAtelier.Gameplay.Tests/
  FruitsAtelier.Skinning.Tests/
  FruitsAtelier.SkinArchive.Tests/
  FruitsAtelier.Formats.Tests/
  FruitsAtelier.Audio.Tests/
assets/skins/default.osk  可选本地皮肤，不纳入 Git
artifacts/         包缓存、日志、截图、参考源码、谱面/皮肤缓存、工程与输出
```

依赖方向为 App → Core。Core 不引用 HWND、COM、Direct2D 或 Vortice。Editor 向 `ICanvas`提交绘制操作，不持有图形设备；Skinning 计算源矩形与目标尺寸，由 Rendering 解码、着色并绘制。

文件读写位于 Core，原生对话框、谱面包资源提取和音频设备由 App 管理。音频包版本固定为 NAudio Core / WinMM / Wasapi 2.2.1、NAudio.Vorbis 1.5.0、NVorbis 0.10.4；实际输出使用 WinMM WaveOutEvent。

## 编辑、转换与绘制

Win32 消息 → 客户区物理像素 → DIP → 命中测试 → 编辑事务 → 文档更新 → 派生转换 → 绘制。DIP 使用 `physicalPixels × 96 / dpi`，布局和命中使用同一坐标系。

一次拖动或数值提交形成一个撤销事务；统一 Slider 工具（B）从首锚点到完成为一个事务，点击创建无柄点，按住向上拖动创建方向柄。`Anchor.OutgoingKind` 指定出段类型，null 继承 `CurveTrack.Kind`；求值、分割、绘制与命中使用同一段类型。曲线/直线控制点转换更新柄与相邻段类型，右键插入无柄点允许形状改变，保形分割独立保留。Esc 或捕获取消恢复起始文档；文本焦点隔离画布快捷键。

V/F 使用完整父对象选择集合，B 使用当前编辑轨迹的锚点集合；Ctrl 点选增减，Ctrl 框选叠加，普通框选替换当前集合。任一 slider 子对象按 SourceId 选择父对象并去重；单独锚点及柄的拖动仅在 B 模式进行。已有轨迹编辑与新建草稿由“新 Slider”显式区分。列表选择先完成至少两点的草稿或取消单点草稿，再切回对象模式。

框选记录开始时的选择，取消时恢复；播放时暂缓视口跟随以保持框与对象坐标一致，结束后恢复跟随，音频时钟继续推进。锚点删除允许批量与端点，Core 验证合并后的段与柄，App 在不足两个剩余点时删除父 slider。内部剪贴板深复制一批完整父对象，粘贴将最早起点移到播放头并保留其余相对时间，重建全部父对象/节点 ID；剪切、删除、粘贴各自为一个事务，失败整批回滚，不使用系统剪贴板。

选择工具以皮肤底图和 overlay 的实际目标范围命中，保留最小点击容差；没有纹理时用几何尺寸。命中 slider 的任意子对象后按 SourceId 选择整个父对象。属性或右键“转换为 FSlider”以单次事务调用 Legacy 转换：验证对象类型、顺序、时刻和 TinyDroplet 贴合后替换为时间—X 轨迹，保留父 ID、源顺序、repeat 与原始样本信息，失败不替换。新建和转换后的 FSlider 使用强制 Tiny 贴合，自动 SV 上限为 stable 可表达的 10。

窗口消息、编辑、转换和绘制在同一线程，音频命令由独立串行 worker 处理；默认输出使用共享模式 WASAPI，停止后等待播放线程退出再释放设备与缓冲区。播放时 `WM_PAINT` 读取 transport 状态，绘制后再次请求重绘，`Present(1)` 随显示器垂直刷新节奏呈现；没有固定 60 FPS 限制，也未据此保证实测帧率。暂停时由状态变化请求重绘，最小化及零尺寸不呈现。

转换缓存比较文档快照内容和 Tiny 补偿开关，变化后重新计算完整文档，包含 Legacy Slider、FSlider、repeat 和香蕉。切换语言也失效缓存，以重新生成当前语言的诊断。转换输入不受可见时间裁剪影响；不完整的曲线草稿暂不生成。两视图共用转换结果，再以完整对象序列计算 hyperdash。转换仍同步执行，没有异步 revision 调度；复杂谱面的耗时隔离需按测量结果处理。

派生路径保留独立几何 Y，按弧长取位置得到实际对象。失败轨迹不输出伪造对象，结果标为不完整；此时 RNG 仅对应成功生成的子集。算法、容量边界与精度见[模型规范](PROJECT_MODEL.md)和[Catch 绘制与转换](CATCH_RENDERING.md)。

## 界面语言

应用自有 GUI 文案与领域诊断统一通过 `FruitsAtelier.Localization.Strings.Get` 读取。`Core/Localization/en.json` 为主表，`zh-CN.json` 为中文表，构建嵌入该目录的 `*.json`，运行时发现语言；顶部按钮切换语言。`Strings.Validate` 检查键集合和复合格式占位符，缺译回退英文，未知键显示键名。用户文件、已存在 Name 不自动翻译；系统或第三方异常不保证完整自译。新增词条与语言见[本地化维护](LOCALIZATION.md)。

## 皮肤导入与资源

仓库不附带皮肤；构建时仅在本地存在 `assets/skins/default.osk` 时复制，启动时存在才导入，没有皮肤则使用基础图形。“皮肤…”使用原生文件对话框。导入器只读取 ZIP 内的 `skin.ini`和 `fruit-*.png`，支持根目录或一个包装目录中的单个皮肤，写入 `artifacts/skins/<SHA256>`。

导入限制为包 256 MiB、选中文件各 16 MiB、选中解压总量 64 MiB、ZIP 条目 20000。所有条目都检查路径，拒绝绝对路径、穿越、Windows 别名、大小写重复和链接。缓存路径拒绝 reparse point；临时目录提取完成且可被皮肤加载器识别后才发布缓存，复用时检查完成标记和文件集合。不会执行包中内容。失败显示错误，已加载皮肤不被失败导入覆盖。

WIC 解码 PNG 并保留 alpha，底图乘色、overlay 保持白色。香蕉底图、overlay 和几何回退统一使用 0.6 倍静态尺寸，不实现随机缩放动画。图像缓存受 64 MiB 和条目数量限制；无效或缺失图片回退为基础图形。缓存是可重建显示资源，不属于创作文档；用户提供的图片许可另见第三方声明。

## 窗口与设备生命周期

交换链使用物理像素，布局使用 DIP。resize 先释放依赖 back buffer 的 Direct2D target，再调整 DXGI buffer 并重建 target；零尺寸跳过绘制。COM 资源由绘制对象统一释放，初始化失败和退出路径清理持有资源。

实现处理 `WM_DPICHANGED`、鼠标捕获与失焦取消；自动化及离屏渲染不等于跨屏桌面交互验证。当前覆盖范围见 [多选与本地化验收记录](MULTISELECT_LOCALIZATION_ACCEPTANCE.md)。设备错误不会主动清空 Core 文档，但不宣称已实现或验证自动设备恢复。软件渲染回退、远程桌面和其他 CPU 架构没有作为已验证能力交付。

DirectWrite 仅负责排版；当前文本编辑限于数值输入。完整 IME、无障碍和可编辑工程名称需要后续独立验证。

## M2 文件与音频接入

文件模块自行实现 stable v14 / Mode=2 reader/writer，保留原始节、未编辑对象行、完整 timing 和资源引用；`.catchproj` schema 1 JSON 保存锚点、控制柄、OutgoingKind、SpanCount、轨迹 Tiny 覆盖值及导入上下文。输出保留行程次数和可保留的样本字段，回读报告量化后的转换差异；导出路径不作为权威创作数据。

`.osz` 只提取 `.osu`、MP3 / OGG / WAV 与 JPG / PNG 等受支持资源至 `artifacts/beatmaps/<SHA256>`，原包不修改；多个难度由用户选择。路径、重复条目、链接和解压容量有校验。视频和 storyboard 不加载，也不由该资源复制流程补齐。

MP3 使用 Windows Media Foundation 在后台连续解码为分块 PCM 缓存，按采样帧 seek，时长取实际帧数；缓存上限 512 MiB，加载可取消。OGG 使用 NVorbis，WAV 使用 NAudio 读取。transport 提供加载、播放、暂停、seek、位置、长度、错误及结束状态；播放位置为 seek 基点加设备已播放时间，不使用解码器读指针，也不分别累加 UI 帧时间。

主绘图区的播放线距底部固定为高度的 25%，`viewStart = playhead − plotHeight × 0.25 / pixelsPerMs`。播放、seek、AR 还原与 resize 保持定位，起止允许留白；暂停时手动平移可离开固定位置，下次定位或播放恢复。底部播放头与视口框连续更新。

暂停 seek 保持暂停，播放 seek 保持播放意图；版本化命令过滤过期加载/定位请求。缺失或损坏音频允许继续编辑并明确禁用播放。MP3 采样定位修复不等于硬件延迟校准；输出请求缓冲仍为 80 ms，设备时钟约每 10 ms 采样，不外推。不能将 timing offset 或视口位置当作音频延迟补偿，stable 客户端尚未对照。

每个输出会话只启动一次，暂停后恢复会按设备位置重建输出，避免 EOF 与恢复播放争用旧停止回调。等待停止超时后隔离旧输出及 reader，收到停止回调才释放；seek/replay 每次加载最多自动重建一次，再次失败保持错误直到显式重载。旧回调不能改变新会话的时钟。自动设备测试输出静音 PCM，不修改系统音量；采样一致性在静音前比较。
