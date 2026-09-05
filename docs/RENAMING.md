# FruitsAtelier 改名说明

项目由 VibeCatchEditor 更名为 **FruitsAtelier**，可编辑的 `VCE Slider` 更名为 **FSlider**；导入后尚未转换的对象仍称为 Legacy Slider。

本次同步更新应用标题、画布品牌、工具/菜单/诊断/新建对象文案，中英文语言表，C# 命名空间与资源标识，解决方案、项目文件与目录，Windows manifest，Mac bundle ID、原生音频库与导出符号，构建脚本、CI 和当前架构文档。启动脚本继续使用 `Run-Editor.cmd` 和 `Run-Editor-Mac.command`。

`.catchproj` 的 schema 1、属性名和扩展名保持不变；`.osu` 输出规则也不变。加载、保存或切换语言不会改写已有工程标题与对象 Name。新转换的对象使用 FSlider 默认名。`tests/FruitsAtelier.Formats.Tests/Fixtures/vibecatch-schema1.catchproj` 是改名前保存格式的固定兼容性样本，故意保留旧品牌和对象名称。

历史 `*_ACCEPTANCE.md` 是旧版本的验收记录，保留当时的名称及本地产物路径；不将旧证据表述为 FruitsAtelier 的新验收结果。第三方许可证保持原文。

改名后的 Mac 独立安装使用 `~/Library/Application Support/FruitsAtelier`。旧版缓存目录不删除；工程中引用的既有资源路径继续按原路径读取。运行于源码仓库时仍使用项目内 `artifacts`。

本机验证：Windows 解决方案交叉编译零警告、零错误；194 项共享回归（包含新增旧工程兼容性检查）和 13 项 Mac 输入/静音音频检查通过。两项未提供外部真实谱面的检查显式跳过。窗口检查涵盖中英文绘制、PNG 着色、放置水果、撤销和工程往返；Windows 的实际窗口和音频设备仍待人工验收。
