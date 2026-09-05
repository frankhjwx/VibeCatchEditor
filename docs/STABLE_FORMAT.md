# stable .osu 文件契约

`.osu` reader/writer 由本项目自行实现，直接读写 osu!stable 使用的谱面格式。首版输出 `osu file format v14`，Catch 使用 `Mode: 2`。文件字段依据 [osu! 官方格式说明](https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29)，本轮读取了其 [官方 wiki 源文档](https://github.com/ppy/osu-wiki/blob/master/wiki/Client/File_formats/osu_(file_format)/en.md)。

## 输入与输出

- 首版首先验证 v14 / Mode=2 输入与输出；旧 stable 格式逐版本增加解析和语义测试，不能只改头部升级。
- 保留 General、Editor、Metadata、Difficulty、Events、TimingPoints、Colours、HitObjects，以及音频和样本引用。
- 未编辑字段和对象尽量保留原文。未知内容不能静默丢弃；可能影响语义时给出诊断，禁止不完整写出。
- 自有锚点、贝塞尔控制柄和编辑约束保存到 `.catchproj`，不写入 `.osu` 自定义对象字段。

## 对象和时序规则

| 项目 | 实现要求 |
| --- | --- |
| 独立 fruit | 写作 hit circle，保留 X、时间、type 标志和样本 |
| Slider / fruit stream | 写作 slider，包含曲线类型/点、行程次数、路径长度和边缘样本；stream 是转换结果 |
| 香蕉雨 | 写作 spinner 的开始/结束时间，逐根香蕉 X 不写入文件 |
| 时间 | 对象时间遵守整数毫秒表示；工程保留双精度并记录写出舍入误差 |
| 坐标 | 写出对象和路径整数坐标，生成时考虑量化误差 |
| Timing | 时间及 beatLength 使用文件允许的小数表示，保留非继承/继承语义及同时间次序 |
| Slider 次数 | 文件 slides 是行程次数；内部 repeatCount 若表示折返次数，需要加一转换 |
| 节拍细分 | 编辑器 BeatDivisor 和游戏 SliderTickRate 分开处理 |

这些规则按 [官方格式文档](https://github.com/ppy/osu-wiki/blob/master/wiki/Client/File_formats/osu_(file_format)/en.md) 实现，具体数值行为还需 stable 对照样本。

## Writer 行为

生成器先得到符合目标的二维 slider，再由 writer 序列化。序列化统一使用 invariant culture，固定换行和 UTF-8 策略，并用中文 metadata 样本验证。对象整数时间与坐标采用明确、可测试的舍入规则；原始未编辑整数值保持原值。

输出默认另存新文件，先验证全部对象，再写临时文件并安全替换。失败时保留原文件。输出到不同目录时显式处理音频、背景及样本相对路径，不生成资源引用已经失效却提示成功的文件。

未编辑 slider 不重新采样；改动 SV 时检查同时与后续对象的生效参数，必要恢复点也写入并验证。输出 `.osu` 与保存工程有各自的成功状态和脏标记处理。

编辑 slider 需要改变 SV 时，替换开始时刻的旧绿点，避免同刻叠加不同速度；保留红点和有效样本字段。Catch 的继承 SV 按 stable 的 0.1–10 范围解释，FSlider 所需速度超过 10 时拒绝生成和输出。只有下一原 timing 点或下一编辑 slider 之前的未编辑 slider 仍依赖旧速度时才写恢复点，无依赖则省略。恢复使用头部之后的安全整数时间；临近对象无法同时保持速度时明确拒绝。输出时长另用 stable 相同的 SV 边界和原始字段公式验证，不能只以自身 reader 回读成功作为依据。

## 验收

1. 官方格式样例和实际 stable Catch 谱面均有解析 fixture；覆盖 circle、slider、spinner、timing 与样本。
2. 读取后不编辑再输出，比较支持范围内的语义，不只检查文件存在或字符串相同。
3. 编辑 fruit/曲线后输出并回读，比较对象类型、数量、顺序、time、X 及 slider 生成结果。
4. 验证 1/6 拍舍入、贝塞尔坐标量化、repeat、同时间控制点、SV 恢复、缺失资源和保存失败。
5. 使用 stable 打开并验证代表性输出；未完成客户端验证时如实记录，不能仅靠自己的 reader 接受文件证明完整兼容。

文件格式正确和 Catch 数值/RNG 完全一致分别验证；stable 是两者的目标。
