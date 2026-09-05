# stable .osu 文件契约

`.osu` reader/writer 由本项目自行实现，直接读写 osu!stable 使用的谱面格式。输出 `osu file format v14`，Catch 使用 `Mode: 2`。文件字段依据 [osu! 官方格式说明](https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29)。

## 输入与输出

- 只接受 v14 / Mode=2；其他格式版本与对象类型由 reader 拒绝。
- 保留 General、Editor、Metadata、Difficulty、Events、TimingPoints、Colours、HitObjects，以及音频和样本引用。
- 保留原始节文本和未编辑对象行；不支持的对象类型报错。
- 自有锚点、贝塞尔控制柄和编辑约束保存到 `.catchproj`，不写入 `.osu` 自定义对象字段。

## 对象和时序规则

| 项目 | 表示 |
| --- | --- |
| 独立 fruit | 写作 hit circle，保留 X、时间、type 标志和样本 |
| Slider / fruit stream | 写作 slider，包含曲线类型/点、行程次数、路径长度和边缘样本；stream 是转换结果 |
| 香蕉雨 | 写作 spinner 的开始/结束时间，逐根香蕉 X 不写入文件 |
| 时间 | 对象时间遵守整数毫秒表示；工程保留双精度并记录写出舍入误差 |
| 坐标 | 写出对象和路径整数坐标，生成时考虑量化误差 |
| Timing | 时间及 beatLength 使用文件允许的小数表示，保留非继承/继承语义及同时间次序 |
| Slider 次数 | 文件 slides 对应模型的 SpanCount，表示行程次数 |
| 节拍细分 | 编辑器 BeatDivisor 和游戏 SliderTickRate 分开处理 |

这些规则按 [官方格式文档](https://github.com/ppy/osu-wiki/blob/master/wiki/Client/File_formats/osu_(file_format)/en.md) 实现。

## Writer 行为

生成器先得到符合目标的二维 slider，再由 writer 序列化。序列化统一使用 invariant culture，固定换行和 UTF-8 策略。对象整数时间与坐标采用中点远离零的舍入规则；原始未编辑整数值保持原值。

输出默认另存新文件，先验证全部对象，再写临时文件并安全替换。失败时保留原文件。宿主在跨目录导出时处理关联资源复制和相对路径；缺失资源或同名内容冲突会报错。

未编辑 slider 不重新采样；改动 SV 时检查同时与后续对象的生效参数，必要恢复点也写入并验证。输出 `.osu` 与保存工程有各自的成功状态和脏标记处理。

编辑 slider 需要改变 SV 时，替换开始时刻的旧绿点，避免同刻叠加不同速度；保留红点和有效样本字段。Catch 的继承 SV 按 stable 的 0.1–10 范围解释，FSlider 所需速度超过 10 时拒绝生成和输出。只有下一原 timing 点或下一编辑 slider 之前的未编辑 slider 仍依赖旧速度时才写恢复点，无依赖则省略。恢复使用头部之后的安全整数时间；临近对象无法同时保持速度时明确拒绝。时长回归测试直接使用输出字段计算 `length × spans × beatLength / (100 × SliderMultiplier × SV)`。

测试入口见[构建与测试](TESTING.md)，底层 API 和来源见[格式模块参考](../src/FruitsAtelier.Core/Formats/REFERENCE.md)。
