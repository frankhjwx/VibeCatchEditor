# macOS 原生运行与 Windows 兼容性

Mac 入口是独立的 `src/VibeCatchEditor.Mac` 项目，使用 Avalonia 11.3.7 的原生桌面窗口与自绘接口，音频输出为系统 AVAudioPlayer；无需 Windows、Wine 或浏览器。当前在 Apple Silicon / macOS 本机验证。Intel 构建路径已提供，但未在 Intel Mac 验证。

## 本机启动

双击仓库根目录的 `Run-Editor-Mac.command`。启动脚本先构建再打开窗口。需要 .NET SDK 8.0.419 和 Xcode Command Line Tools（`xcrun clang`）。SDK 已安装到本次工作目录的 `artifacts/dotnet`；此目录不提交。

新机器可安装指定 SDK，或在仓库目录执行 `bash scripts/Install-Mac-SDK.sh`，将微软 SDK 安装到项目内。Mac 脚本切换到 `macOS/`，使用该目录的 SDK 固定版本；根目录 `global.json` 中的 Windows SDK 10.0.400 保持不变。

`bash scripts/Publish-Mac.sh` 按当前机器架构生成自包含应用：

```text
artifacts/macos/VibeCatchEditor.app
```

该应用可双击启动，不需要单独安装 .NET。构建结果仅作本机签名，尚未进行 Developer ID 签名、公证或公开发行。应用包和运行产物不上传 GitHub。脱离仓库运行时，缓存和日志写入 `~/Library/Application Support/VibeCatchEditor`。

## 复用与平台边界

- Mac 通过链接源码复用全部 `EditorView`、`ICanvas`、皮肤布局、谱面包导入和资源复制逻辑；领域模型、转换、读写和本地化来自同一个 Core 项目。
- Windows 的 `.csproj`、解决方案、Win32 窗口、DirectX 绘制、Windows 音频及 `Run-Editor.cmd` 保持原样。共享 EditorView 仅增加只读数值输入状态及可配置状态栏资源键，默认仍为原 Windows 状态栏。
- Mac 同时接受 Command 和 Ctrl 快捷键；普通 Delete/Backspace 删除对象，数值输入时 Backspace 删除一个字符。鼠标捕获、双击、滚轮、取消交互映射到原有编辑器方法。
- Mac 的原生文件选择器支持 `.osz` / `.osu` / `.catchproj`、工程另存、谱面导出、音频和 `.osk`。多个难度时在解压目录选择 `.osu`。关闭和替换脏文档时提供保存、放弃、取消。
- MP3/WAV 交给 AVAudioPlayer，OGG 由固定版本 NVorbis 解码到有容量上限的 PCM WAV 缓存再播放。播放位置读取播放器，未使用 UI 定时器模拟音频。加载取消和过期结果隔离；EOF 后新建播放器，避免已完成会话影响重播。
- 界面定时刷新约 16 ms，不宣称与 Windows 的垂直同步策略或硬件音频延迟相同。

## 验证

运行 `bash scripts/Test-Mac.sh`：复用原 Core、Gameplay、Formats、App、Skinning、SkinArchive 测试，另运行 Mac 输入映射和静音设备测试。`--native-only` 仅运行 Mac 测试；无设备 CI 使用 `--skip-device-tests` 并明确打印跳过记录。

Formats 默认仍执行全部测试。脚本显式传入 `--skip-external-fixtures`，跳过两项需要未提交真实谱面的检查；不会将跳过项计为通过。可选本地用户工程和默认皮肤同样未包含在仓库中。

`./Run-Editor-Mac.command --smoke-check` 打开真实窗口，保存中英文画布截图，检查放置水果、撤销与工程文件往返后退出。截图位于 `artifacts/macos-check`，日志位于 `artifacts/logs/macos.log`。这是窗口运行与程序驱动的检查，不等于完整人工鼠标、文件对话框和听感验收。

本机结果：193 项现有回归检查通过；两项外部谱面检查显式跳过。Mac 静音设备检查覆盖 WAV/OGG 加载、真实时钟推进、暂停、播放中定位、加载竞争、缺失文件、EOF 和重播。Windows 解决方案使用 .NET 8.0.419 交叉编译通过，零警告、零错误；根目录固定的 Windows SDK 和 Windows 实际窗口、音频设备尚未在本机运行验证。MP3、Intel Mac、跨屏 DPI 和实际听感需要进一步验证。

GitHub 工作流在 Windows 上构建原解决方案并执行原编辑器测试，在 macOS 上运行共享回归并构建应用包；CI 不声称验证真实音频设备或人工交互。
