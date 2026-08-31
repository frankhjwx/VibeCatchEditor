# M2 验收记录

记录日期：2026-08-31。可运行程序：`artifacts/M2-Acceptance/VibeCatchEditor.App.exe`。不覆盖旧的 `artifacts/M2` 程序或用户提供的 `.osz`。

## 当前可验收行为

- 文件菜单 / Ctrl+O 打开 `.osz`、v14 / Mode=2 `.osu` 或 `.catchproj`；多难度谱面包通过文件选择器选择难度。只提取谱面、音频和图片，不加载视频或故事板。
- Ctrl+S 保存工程，Ctrl+Shift+S 工程另存，Ctrl+E 输出新的 stable `.osu`。工程保留创作曲线、控制柄、原始导入内容和资源路径；`.osu` 输出包含量化及回读转换诊断。禁止覆盖导入的原谱面。
- 红线 BPM/offset/拍号和绿线 SV 查询、网格、1/4 / 1/5 / 1/6 / 1/7 / 1/8 / 1/9 / 1/12 / 1/16 吸附使用同一 timing 模型。导入 L/B/P/C 路径、折返和香蕉雨参与完整谱面转换与 RNG。
- Legacy Slider 原路径保持只读，可选中、整条删除或转换为 VCE Slider；VCE Slider 可以编辑并输出 `.osu` slider。转换不声称恢复原作者控制柄。
- MP3 / OGG / WAV 加载、播放、暂停、seek 使用真实音频设备；空格切换播放，底部进度条连续定位。播放中 seek 保持播放，暂停时 seek 保持暂停。
- 主画布按播放头相对视口的 offset 连续跟随；向后定位也立即跟随。滚轮、缩放和平移可以改变这个偏移。底部可见范围连续移动，右侧预览共用播放时钟及 AR 下落计算。
- 播放时连续请求绘制，通过 `Present(1)` 同步显示器刷新；没有固定 60 FPS 上限。暂停时主要由输入和状态变化触发绘制，最小化时不绘制。

## 执行结果

统一 Release 构建及发布成功：0 warning / 0 error。119 项检查通过（Core 数量包含一次双实图 fixture 检查；资源导出的一项检查内含六个场景）：

| 项目 | 通过数量 | 重点 |
| --- | ---: | --- |
| Core.Tests `--fixtures` | 35 | 坐标、曲线、timing 边界、吸附、导入路径、repeat、香蕉 RNG、两张实图完整转换 |
| App.Tests | 29 | 编辑输入、撤销、绘制图层、文件回调、连续跟随/后退/暂停后平移再播放、OSZ 安全、资源复制 |
| Formats.Tests | 19 | 原文保留、量化后顺序、SV 恢复、冲突阻止、工程重开、失败时文件保护 |
| Audio.Tests | 8 | 真实默认输出设备上的 WAV/OGG/两张图 MP3、暂停/seek/EOF/失败恢复及多 reader 生命周期 |
| Gameplay.Tests | 12 | CS 和红果判定、Droplet/Tiny 规则 |
| Skinning.Tests | 8 | 皮肤纹理、密度、裁剪、尺寸及回退 |
| SkinArchive.Tests | 8 | OSK 安全提取、缓存和限制 |

App 测试通过公开输入 API 和绘制记录验证交互，不等同于所有原生文件对话框均已手动验收。音频测试验证设备状态和时钟，不证明低延迟或逐采样对齐。

### 两张测试图

原始包来自用户提供的 `E:/osu!/Exports`，只读导入。中间文件、工程和输出放在 `artifacts/m2-validation`；完整数据见 [report.json](../artifacts/m2-validation/report.json)。

| 项目 | Vidro Moyou | Oriental Blossom |
| --- | ---: | ---: |
| Timing 点 / 红线 / 不同 BPM 数 | 51 / 1 / 1 | 214 / 27 / 17 |
| 独立 fruit / slider / 香蕉雨 | 972 / 229 / 2 | 915 / 613 / 2 |
| 转换后 Fruit / Droplet / Tiny / Banana | 1430 / 73 / 415 / 257 | 2181 / 146 / 739 / 162 |
| 未编辑输出回读：时间 / X 最大差异 | 0 ms / 0 | 0 ms / 0 |
| 工程保存重开，含新增贝塞尔控制柄 | 一致 | 一致 |
| 修改 fruit、新增贝塞尔后输出对象序列 | 一致 | 一致 |
| 修改后整数输出回读：时间 / X 最大差异 | 0.030535 ms / 0.934479 | 0.142858 ms / 0.786149 |

以上差异比较本项目编码前和回读后的实际转换对象，不是与 stable 客户端比较，也不是所有谱面的误差上界。

### 实际绘制与窗口观察

新版可见窗口已打开 Oriental Blossom，观察到真实音频播放状态、时间导航和 AR 比例画布；没有向用户正在操作的窗口发送自动化输入。截图：[暂停状态](../artifacts/m2-validation/screenshots/oriental-paused.png)、[播放状态](../artifacts/m2-validation/screenshots/oriental-playing.png)。

当前机器 RTX 5080、168 DPI，播放期间五秒采样日志为 **120.5 FPS**，约 120 FPS。这里只记录一次窗口场景，不能保证所有地图或显示器都满刷新率。呈现同步含义参考 Microsoft 的 [IDXGISwapChain::Present](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present)。

独立 DX11 render-check 通过 96 / 144 / 192 DPI 的两种尺寸共六组布局以及零尺寸恢复；解码 14 个皮肤位图变体，1000 fruit 场景 60 帧中位数 8.312 ms、P95 10.4354 ms。此隐藏窗口测试不表示可见刷新率，详见 [render-check.json](../artifacts/logs/render-check.json)。

## 保留的限制

- **用户反馈的音频延迟仍待研究。** 本轮没有调整音频缓冲、时钟采样或偏移补偿。设备 clock 驱动 UI 已接入不等于延迟问题解决。
- 输入当前限 v14 / Mode=2；不支持格式或不完整转换会明确报错。原始 slider 路径只读，尚无将其重塑成创作曲线的 UI。
- 没有加载视频、故事板、波形或游戏命中音效。原 `.osu` 中视频/故事板文本可保留，资源不加载或复制。
- 工程通过相对路径引用资源，不内嵌音乐。移动工程时需保留其资源位置关系；导出复制音频/图片，已有同名不同内容资源会拒绝覆盖。
- 暂无 stable 客户端逐帧或逐采样对照、mods、跨设备音频延迟验证、完整皮肤动画和 GPU 设备丢失自动恢复。

## 复查入口

```powershell
dotnet build VibeCatchEditor.sln -c Release --no-restore
dotnet run --project tests/VibeCatchEditor.Core.Tests -c Release --no-build -- --fixtures
dotnet run --project tests/VibeCatchEditor.App.Tests -c Release --no-build
dotnet run --project tests/VibeCatchEditor.Formats.Tests -c Release --no-build
```

`Audio.Tests` 会使用系统默认输出设备播放测试音频，宜在不进行其他音频验收时单独运行。验收程序支持以 `.osz` / `.osu` / `.catchproj` 路径作为命令行参数，省略参数时显示内存演示 map。
