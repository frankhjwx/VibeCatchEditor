# Catch 绘制与转换

两视图共用实际转换结果、AR 下落比例、CS 尺寸和皮肤绘制。当前条件为 NM / 1×，支持多 timing、独立 fruit、导入 L/B/P/C slider、混合线性/贝塞尔轨迹及 repeat、香蕉雨。真实音频 transport 驱动当前时间，无音频时允许明确标注的手动定位。

## AR 与中心位置

- 场地横向为 512 单位；下落起点 Y=−100，接取线 Y=340，行程为 440，不能把场地几何高度 384 当作下落距离。
- AR ≤ 5 时 preempt 为 `1200 + 120 × (5 − AR)` ms；AR > 5 时为 `1200 − 150 × (AR − 5)` ms。
- AR 先转换为 float，分段结果截断到整数毫秒。AR 0 / 5 / 8 / 10 对应 1800 / 1200 / 750 / 450 ms。
- 有效显示场地宽 W 时，`DIP/ms = (440 / preemptMs) × (W / 512)`。
- 剩余时间 Δt 时，`screenY = catchLineY − Δt × DIP/ms`。预览只显示 `0 ≤ Δt ≤ preemptMs`的转换对象。

预览将 512:440 范围等比装入面板，保持横纵比例并留边。主画布“还原 AR 比例”使用同一公式，当前时间固定在距绘图区底部 25% 的播放线；Ctrl + 滚轮可自由缩放。还原模式随宽度与 AR 更新，不改变模型。

主画布播放线为 `plotBottom − plotHeight × 0.25`；播放、seek、AR 还原与 resize 保持定位，谱面起止允许留白，暂停可以手动平移。底部导航连续移动。播放通过 `WM_PAINT` 持续重绘和 `Present(1)` 跟随显示器垂直刷新，没有固定 60 FPS 限制；该机制不保证实际帧率。

MP3 在后台连续解码为 PCM，按实际采样帧确定 seek 和时长；播放位置来自 seek 基点与设备时钟，不累加绘制帧时间。解码定位修复不等于硬件延迟校准，也不证明与 stable 解码器的 padding 处理一致。

固定来源：[CatchPlayfieldAdjustmentContainer.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/UI/CatchPlayfieldAdjustmentContainer.cs)、[CatchHitObject.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Objects/CatchHitObject.cs)、[IBeatmapDifficultyInfo.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Beatmaps/IBeatmapDifficultyInfo.cs)。

当前采用整数 preempt；该固定 lazer 版本实际滚动的 time range 未取整，因此小数 AR 不宣称与其逐像素一致。下落几何来自其 stable 适配代码，尚未以固定 stable 客户端逐帧验证。

## CS 与皮肤尺寸

CatchScale 参考 legacy CircleSize 计算，保留 float 运算边界：

```text
cs = (float)CircleSize
scale = (float)(1.0f − 0.7f × ((cs − 5) / 5)) / 2
名义 fruit 直径 = 128 × scale
完整 catcher 宽度 = 106.75 × (2 × scale)
实际接取宽度 = 完整 catcher 宽度 × 0.8
```

上述几何再乘视图宽度 / 512。CS=5 时名义 fruit 直径为 64 单位。基础图形回退的 Droplet / TinyDroplet 半径分别为 `16 × scale` / `8 × scale`；它们与 legacy PNG 的可见尺寸规则不同。

PNG 按原始逻辑宽高显示，@2x 的逻辑尺寸为像素尺寸的一半。每轴从中心裁剪到最多 160 逻辑像素，不将大图整体等比缩小。目标尺寸为 `裁剪后逻辑尺寸 × 名义 fruit 直径 / 128 × 视图宽度 / 512`，drop 额外乘 0.8，tiny 额外乘 0.4，banana 额外乘 0.6。透明边距参与尺寸，overlay 不乘底图颜色。

香蕉在两视图使用静态到达尺寸 0.6，底图与 overlay 同步缩放，几何回退半径为 `FruitRadius(CS) × 0.6`。随机缩放和旋转动画不实现；视觉简化不改变香蕉 RNG 的消耗顺序。

选择工具命中范围使用底图与 overlay 实际目标矩形的并集，保留最小点击容差，不含扩大 hyperdash 层，也不作逐像素 alpha 检测。缺失纹理按对应几何尺寸回退。点击 slider 的任意 Fruit / Droplet / TinyDroplet，以 SourceId 选择整个 slider，选择本身不改变转换结果。

水果变体按完整父对象顺序的索引循环 pear / grapes / apple / orange；slider nested fruit 继承其父对象索引。仓库不附带皮肤；可选本地默认包由 `assets/skins/default.osk` 导入，其他包通过“皮肤…”选择，无皮肤时使用基础图形。ZIP 限制见[架构](ARCHITECTURE.md)，资源权限见[第三方声明](../THIRD_PARTY_NOTICES.md)。

尺寸来源：[LegacyRulesetExtensions.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Rulesets/Objects/Legacy/LegacyRulesetExtensions.cs)、[Catcher.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/UI/Catcher.cs)。皮肤来源及具体映射见[Skinning/REFERENCE.md](../src/FruitsAtelier.App/Skinning/REFERENCE.md)。

## 实际 stream、Tiny 和 hyperdash

时间—X 轨迹按每个锚点的 OutgoingKind（null 继承轨迹 Kind）求值，可混合线性与贝塞尔段。生成满足速度约束的首 span 路径，再按 SpanCount 产生 head / tick / repeat / legacy-last-tick / tail 和 tiny 事件。首 span 节点共用于后续往返；对象按路径弧长定位，叠加固定 Legacy RNG。分割插点保持该段形状，不自动成为 tick；SliderTickRate 与编辑分拍吸附独立。

Legacy Slider 按原 L/B/P/C 路径、声明长度和 repeat 生成，反向 span 保留对应 tick 位置并生成折返 fruit。选中后可从属性或右键执行“转换为 FSlider”，以一个事务转为首 span 的时间—X 线性节点，保留父 ID、源顺序、SpanCount 和原始 sample 信息。转换前后比较对象类型、时刻并验证全部 TinyDroplet 贴合，失败不替换 Legacy Slider。

整图父对象的开始时间与原始顺序决定 RNG 消耗，即使时间重叠也先处理一个 stream 的全部 nested 对象，再处理下个父对象。普通 Droplet 的 X 位于路径上，只消耗旋转随机数；获得横向 RNG 偏移的是 TinyDroplet。Legacy Slider 保留该偏移，FSlider 通过反向调整导出路径来贴合目标时间—X 轨迹。新建或由 Legacy 转换的 FSlider 将贴合作为强约束，无法满足边界、共享 repeat 路径或 SV 条件时不生成该轨迹；自动 SV 上限为 stable 的 10。失败轨迹使结果不完整，不显示伪造 stream。

Hyperdash 对时间稳定排序的 Fruit / Droplet 计算，TinyDroplet 和香蕉不参与。使用每个时间先截断到整数、`1000f / 60f / 4`的时间余量、完整 catcher 半宽及前一方向/剩余移动量；余量小于零时标记当前起跳对象。不能只按相邻 X 差或一个固定阈值判断，也不能省略视口前的上下文。来源：[CatchBeatmapProcessor.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Beatmaps/CatchBeatmapProcessor.cs)及[CatchBeatmap.cs](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Beatmaps/CatchBeatmap.cs)。

普通对象绘制白色底图 tint；hyperdash 用皮肤 HyperDashFruit（回退 HyperDash / 红色）的 1.2 倍底层标记。当前不完整复刻 additive 光晕、旋转、combo 色和命中特效。

## 图层与验证边界

主画布曲线位于实际对象之上；选中 100% 不透明，未选中 50%，隐藏后对象仍保留。Legacy Slider 的覆盖线来自原始路径，FSlider 按本地混合段绘制；不使用随机 tiny 连线冒充轨迹。

多 timing、repeat、混合段、Legacy 转 FSlider、香蕉尺寸、PCM seek 和输出往返的当前验证见 [M2 编辑验收记录](M2_EDITING_ACCEPTANCE.md)。工程保存逐段类型与 SpanCount，输出 `.osu` 保留 repeat 与可保留的 sample 字段。

这些证据不等于 stable 精确一致。stable 客户端逐帧对照、mods 和音频延迟校准仍未完成；跨屏 DPI 交互与真实听感不能由离屏或纯算法测试代替。视频和 storyboard 不加载。
