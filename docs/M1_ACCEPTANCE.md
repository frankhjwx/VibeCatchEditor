# 当前验收版本

2026-08-31，Windows x64 / .NET 8。双击根目录 `Run-Editor.cmd` 构建并启动；本次发布的可直接运行版本为 `artifacts/M1-Skin/VibeCatchEditor.App.exe`。

## 操作

- 内置 30 秒、120 BPM 的演示 map。主画布时间向上递增，初始 0 ms 在底部。
- Ctrl + 滚轮缩放时间；“还原 AR 比例”按 AR 和场地宽度恢复下落比例。右侧 AR、CS 可编辑并撤销。
- Fruit 工具支持 1/4、1/5、1/6、1/7、1/8、1/9、1/12、1/16 拍；贝塞尔工具向上添加锚点，Enter 完成，选择后拖动锚点及控制柄。
- VCE Slider 生成 Fruit、Droplet、TinyDroplet；全部对象严格贴合目标曲线，自动 SV 可提高到 stable 上限 10，无解时显示诊断。Tick ×n 与编辑吸附独立。
- 主画布曲线在对象上层；选中时 100% 不透明，未选中时 50%。隐藏曲线不会隐藏生成对象。
- preview 默认隐藏曲线；“调试曲线”独立控制，打开后曲线仍在对象后层。
- 默认皮肤原包保存在 `assets/skins/default.osk`。工具栏“皮肤…”选择其他 `.osk`；应用只解压 Catch PNG 和 skin.ini 到 `artifacts/skins` 的内容哈希缓存。原包不会被修改。
- 红果标记按完整对象序列计算；Droplet 参与，TinyDroplet 不参与。皮肤颜色由 HyperDashFruit / HyperDash 控制。

## 已执行验证

Release 全解决方案编译：0 警告、0 错误。以下均为本项目测试，不代表执行了上游测试：

| 测试程序 | 通过数 | 主要覆盖 |
| --- | ---: | --- |
| Core.Tests | 27 | 坐标、吸附、曲线、事务、AR、slider 事件、RNG、tick 与 tiny 误差及失败边界 |
| App.Tests | 22 | 放置/拖动/取消、数值输入、AR/CS、独立曲线开关、层级与选择透明度 |
| Gameplay.Tests | 12 | CS 尺寸、红果判定、方向/前缀/小数时间、Droplet 参与 |
| SkinArchive.Tests | 8 | ZIP 路径、大小限制、重复目录、缓存完整性、链接拒绝 |
| Skinning.Tests | 8 | @2x、裁剪、透明叠图、三类尺寸、默认包元数据与失败回退 |

共 77 项通过。实际可见窗口已运行并观察到默认皮肤及上层曲线；用户正在操作时未接管其输入。选择透明度和按钮独立性由集成测试验证，不据此宣称全部原生交互已验收。

`--render-check` 实际执行 WIC 图片解码、DX11 / Direct2D 绘制，覆盖 96 / 144 / 192 DPI 渲染目标各两种窗口尺寸以及零尺寸恢复。NVIDIA GeForce RTX 5080 上，1000 个独立 fruit 的 60 帧隐藏窗口测量中位数 8.014 ms、p95 12.553 ms；包含 EndDraw/Present，不代表可见刷新率或跨屏 DPI 交互结论。报告位于 `artifacts/logs/render-check.json`。

## 尚未完成

当前仍为内存演示工程。`.osu` / 自有工程读写、真实音乐播放暂停与 seek 必须在 M2 接通；关闭不会保存编辑内容。

VCE Slider 的 RNG 补偿仍受 X 边界、repeat 共享路径和 stable 的 SV=10 上限约束，不能宣称任意曲线都能无误差输出；无法满足强约束时不生成该轨迹。

皮肤使用 CS 与原图密度/尺寸规则，保持长宽比，省略 rotation。红果外圈为简化显示；接盘、淡入及命中特效未复现，尚不声明 stable 完整像素一致。
