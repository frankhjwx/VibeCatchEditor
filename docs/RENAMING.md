# 旧项目迁移

VibeCatchEditor 更名为 **FruitsAtelier**，可编辑的 `VCE Slider` 更名为 **FSlider**。导入后尚未转换的对象仍称为 Legacy Slider。

- 解决方案、项目目录、命名空间和应用名称使用 FruitsAtelier。启动脚本仍为 `Run-Editor.cmd` 与 `Run-Editor-Mac.command`。
- `.catchproj` 继续使用 schema 1 和原有属性名，无需转换文件。已有工程标题和对象名称保留原值；新建对象使用 FSlider 默认名。
- Mac 独立应用的缓存目录为 `~/Library/Application Support/FruitsAtelier`。已有工程引用的资源继续按原路径读取，旧缓存目录不会自动迁移或删除。

旧格式样例位于 [vibecatch-schema1.catchproj](../tests/FruitsAtelier.Formats.Tests/Fixtures/vibecatch-schema1.catchproj)，由 Formats 测试检查读写兼容性。
