# Slider 交互验收

日期：2026-08-31。运行 [本轮编辑器](../artifacts/M2-Slider/VibeCatchEditor.App.exe)，启动自带可编辑的 30 秒演示 map；旧版运行包保留。

## 操作

- **B / Slider**：向上点击添加无柄控制点，两端无柄时连接直线；按住向上拖动拉出方向柄，形成贝塞尔曲线。Enter 完成，Esc 取消整条草稿。绘制期间也可选中并拖动已有控制点或方向柄，仍以整条草稿为一次撤销。
- **控制点与方向柄**：选中后高亮，可直接拖动；时间递增及 X=0..512 约束始终生效，包括尚未连接下一段的草稿末端出柄。
- **右键轨迹 → 插入控制点**：按点击位置插入默认无柄点；右键控制点或属性按钮可转换为曲线 / 直线控制点。邻点仍有柄时，相邻段可以继续弯曲，默认插点不保证保形；需要保持形状时使用“分割插点 · 保持形状”。
- **导入与 repeat**：导入曲线上右键插点会在同一次撤销中转为可编辑轨迹；回程插点映射到共用的首行程。原有“编辑 Slider”入口保留。
- **右键对象 → 删除 / 剪切 / 复制**：操作整个 fruit 或父 slider。内部控制点另有“删除控制点”；首尾点不能单独删除。删除内部点时，原来不参与绘制的柄不会意外激活。Ctrl+C / X / V 使用应用内部的单对象剪贴板，粘贴起点位于播放头，保留 X、路径、方向柄、repeat 和样本字段，生成独立 ID，支持撤销重做；不使用 Windows 系统剪贴板。
- **左侧对象列表**：点击即切回选择模式；至少两点的草稿先完成，单点草稿取消。选择对象、关闭菜单和复制本身不改变谱面内容。

交互参考 [Adobe 路径编辑说明](https://helpx.adobe.com/photoshop/using/editing-paths.html) 中的锚点、方向柄及角点转换；本项目自行实现，没有采用 Adobe 源码，也不实现自由回折或闭合路径。

## 验证

Release 构建与最终发布成功，构建 0 warning / 0 error。**153 项检查通过**：

| 测试 | 数量 | 覆盖 |
| --- | ---: | --- |
| Core `--fixtures` | 51 | 插点、转换、删除、隐藏柄、边界、撤销及两张用户谱面 |
| App | 47 | 单一 Slider 绘制、点和柄高亮拖动、右键、剪贴板、hierarchy、回程插点及既有交互 |
| Formats | 24 | 工程与 stable 格式回读、样本保留、两张用户谱面及编辑导出 |
| Gameplay | 13 | Catch 尺寸与 hyperdash 回归 |
| Skinning | 10 | 皮肤尺寸、底图 / overlay、实际命中范围 |
| SkinArchive | 8 | OSK 安全导入 |

日志位于 `artifacts/logs/slider-interaction-*-tests.log`，构建 / 发布日志使用同一前缀。两张实图为 Vidro Moyou 与 Oriental Blossom；没有修改原始 `.osz`。音频实现本轮未改，未重跑实际输出设备测试，也未新增硬件延迟校准结论。

实际 Win32 窗口已观察并用鼠标完成：选择整条轨迹、打开右键菜单、插入控制点、转换为曲线点、拖动方向柄和控制点，确认属性及生成对象随之更新。截图：[控制点编辑](../artifacts/slider-interaction-validation/screenshots/control-point-edit.png)、[右键菜单](../artifacts/slider-interaction-validation/screenshots/context-menu.png)。剪切粘贴与草稿列表切换的证据来自 App 输入 API 测试，不计作完整桌面鼠标验收。

最终运行包的 DX11 隐藏窗口验证通过 96 / 144 / 192 DPI 两种尺寸共六组布局、零尺寸恢复及 14 个皮肤位图变体；1000 fruit 的 60 帧测量中位数 9.2943 ms，P95 11.3843 ms。这不是可见窗口刷新率保证。见 [本轮渲染报告](../artifacts/slider-interaction-validation/render-check.json)。

历史音频、导入转换与精度边界见 [M2 编辑验收记录](M2_EDITING_ACCEPTANCE.md)。本轮未增加视频、storyboard、多选或系统剪贴板格式。
