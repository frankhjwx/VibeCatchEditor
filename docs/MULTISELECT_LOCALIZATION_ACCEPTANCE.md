# 多选与本地化验收

日期：2026-08-31。运行 [新版编辑器](../artifacts/M2-Multiselect/VibeCatchEditor.App.exe)，启动自带可编辑演示 map；已有运行包保留。

## 操作

- **V / F 对象模式**：空白处拖动框选，Ctrl 点选增减，Ctrl 框选叠加。命中 slider 任意生成 fruit、droplet 或 tiny droplet 都选择整个父对象，同一父对象只计一次。选择不修改谱面或撤销历史。
- **批量对象操作**：Delete 删除，Ctrl+C / X / V 复制、剪切、粘贴；右键已选对象保留整批选择。粘贴将最早起点对齐播放头，保留相对时间与 X，生成独立 ID。剪切、粘贴、删除各可一步撤销；无效成员或时间溢出不产生部分修改。使用应用内部剪贴板。
- **B / Slider 锚点模式**：编辑当前轨迹的锚点，空白处框选，Ctrl 点选增减；绘制草稿时使用 Ctrl 拖动框选，以区分添加新点。Delete 批量删除，支持端点；剩余不足两个点则删除整条 slider。未删除点保留原时刻，整批可一步撤销。属性区“新 Slider”显式开始另一条曲线。
- **取消与播放**：Esc、丢失捕获或 resize 取消框选并恢复原选择；框选期间暂缓主视口跟随，音频时钟继续推进。选择多个对象或锚点后不会误拖其中单个成员；本轮没有增加整组拖动。
- **中英文**：顶部“中文 / EN”切换自有界面、菜单、状态与诊断，保留选择和谱面数据。英文为主表，每种语言一张同键 JSON；新增语言表后重新构建即可发现。已有名称不翻译，系统和第三方原始消息不保证由应用翻译。维护方法见 [本地化维护](LOCALIZATION.md)。

## 自动验证

最终 Release 构建、发布成功，0 warning / 0 error。**174 项检查通过**：

| 测试 | 数量 | 覆盖 |
| --- | ---: | --- |
| Core `--fixtures` | 60 | 批量删点、句柄边界、撤销、语言表、两张用户谱面 |
| App | 59 | 多选模式、父对象去重、批量操作、取消、播放跟随、语言切换及既有交互 |
| Formats | 24 | 工程与 stable 格式回读、样本保留、真实谱面编辑导出 |
| Gameplay | 13 | Catch 尺寸与 hyperdash |
| Skinning | 10 | 皮肤尺寸、overlay 和命中范围 |
| SkinArchive | 8 | OSK 安全导入 |

两张语言表各 460 个键；静态审计 0 findings，.NET 格式及参数编号校验通过。日志为 `artifacts/logs/multiselect-*-tests.log`，构建与发布使用同一前缀。发布的 App / Core DLL 与测试构建哈希一致。

Vidro Moyou 与 Oriental Blossom 实图回归通过，没有修改原始 `.osz`。音频提示已本地化，播放管线没有变更；本轮未重跑音频输出设备测试，不新增硬件延迟校准结论。

## 实际窗口与渲染

通过实际 Win32 窗口鼠标操作确认中英文切换、9 个对象框选及父轨迹高亮、英文右键菜单、Slider 按钮进入锚点模式、3 个锚点框选和语言切换后选择保持。截图：[英文对象多选](../artifacts/multiselect-validation/screenshots/objects-en.png)、[英文菜单](../artifacts/multiselect-validation/screenshots/context-en.png)、[英文锚点多选](../artifacts/multiselect-validation/screenshots/anchors-en.png)、[中文锚点多选](../artifacts/multiselect-validation/screenshots/anchors-zh.png)。

批量删除、剪切粘贴、Ctrl 点选及撤销的证据来自 App 输入 API 测试，未将其计作完整桌面鼠标键盘验收。桌面检查中中文输入法组合态拦截了一次 B 字母键，本轮通过 Slider 按钮进入模式；输入法下的字母快捷键仍需单独处理。

最终包的隐藏 DX11 窗口通过 96 / 144 / 192 DPI 两种尺寸共六组布局、零尺寸恢复及 14 个皮肤位图变体检查。1000 fruit 的 60 帧测量中位数 10.367 ms，P95 14.1434 ms；这是隐藏窗口测量，不代表可见刷新率保证。见 [渲染报告](../artifacts/multiselect-validation/render-check.json)。

本轮未增加视频、storyboard、系统剪贴板格式或音频校准。stable 客户端精确对照、跨屏 DPI 与所有窄窗口英文布局仍需继续人工验收。
