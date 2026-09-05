# macOS 运行

Mac 宿主使用 Avalonia 桌面窗口和 AVAudioPlayer。构建脚本支持当前机器架构：Apple Silicon 为 `osx-arm64`，Intel 为 `osx-x64`。

## 从源码启动

需要 .NET SDK **8.0.419** 和 Xcode Command Line Tools（`xcrun clang`）。可在仓库根目录安装项目内 SDK：

```bash
bash scripts/Install-Mac-SDK.sh
```

双击根目录 [Run-Editor-Mac.command](../Run-Editor-Mac.command)，或在终端执行：

```bash
./Run-Editor-Mac.command
```

脚本优先使用 `artifacts/dotnet/dotnet`，否则使用 PATH 中的 `dotnet`，并切换到 `macOS/` 以应用该目录的 SDK 版本设置。

## 打包

```bash
bash scripts/Publish-Mac.sh
```

输出为 `artifacts/macos/FruitsAtelier.app`，包含 .NET 运行时，可双击启动。脚本按本机架构编译并进行 ad-hoc 签名；公开分发还需要 Developer ID 签名和公证。

脱离仓库运行时，缓存和日志写入 `~/Library/Application Support/FruitsAtelier`。

## 操作差异

Mac 同时接受 Command 和 Ctrl 组合快捷键。普通 Delete / Backspace 删除对象；数值输入时 Backspace 删除字符。

打开包含多个难度的谱面包后，从解压目录选择目标 `.osu`。关闭或替换未保存文档时，可以保存、放弃或取消。

其余编辑操作见[编辑操作](EDITOR_UI.md)，测试命令见[构建与测试](TESTING.md)。
