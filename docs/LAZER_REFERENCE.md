# osu!lazer 实现参考

参考版本为 [ppy/osu commit 48c4800e3ae4ee752452cdff83bd3787ccf3105f](https://github.com/ppy/osu/commit/48c4800e3ae4ee752452cdff83bd3787ccf3105f)。

此索引记录编辑操作和时钟设计的参考入口。实际改编的算法与许可见[第三方声明](../THIRD_PARTY_NOTICES.md)。

## 文件职责

本项目自行实现 stable `.osu` reader/writer，文件规则见 [stable 格式](STABLE_FORMAT.md)。本参考文档用于编辑、曲线、Catch 对象转换与音频功能。自有工程额外保存锚点和贝塞尔控制柄。

## 分拍吸附和水果放置

| 源码 | 职责 | 本项目使用方式 |
| --- | --- | --- |
| [BindableBeatDivisor](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/BindableBeatDivisor.cs) | 预设 divisor 包含 4、6 等值 | 滑条提供 1/4、1/5、1/6、1/7、1/8、1/9、1/12、1/16，最右端为 1/16 |
| [BeatDivisorPresetCollection](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/Compose/Components/BeatDivisorPresetCollection.cs) | 常规组包含 1、2、4、8、16，三连音组包含 1、3、6、12 | 参考网格分组，不强制照搬 UI |
| [EditorClock.SeekSnapped](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/EditorClock.cs) | 以当前 timing offset 和拍长/divisor 求吸附位置，处理下一 timing 边界 | 参考 timing 边界；本项目的网格与对象编辑共用独立吸附服务 |
| [FruitPlacementBlueprint](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Edit/Blueprints/FruitPlacementBlueprint.cs) | 放置时使用 composer 吸附结果并处理横向位置 | 参考 fruit 放置流程，按本项目坐标与命令系统实现 |

`SeekSnapped()` 提供时钟吸附；本项目在 `Timing` 模块中实现对象吸附和网格。

## 贝塞尔与 Catch stream

| 源码 | 职责 | 本项目使用方式 |
| --- | --- | --- |
| [PathControlPointVisualiser](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Osu/Edit/Blueprints/Sliders/Components/PathControlPointVisualiser.cs) | osu 规则集的控制点选择、拖动、删除及 BEZIER 路径类型 | 参考曲线编辑事务，不能直接作为时间—X 控制柄 UI 使用 |
| [SliderPath](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Rulesets/Objects/SliderPath.cs) | 路径控制点、计算路径、累计长度和按进度求位置 | 参考生成后的二维 slider 采样；参数 u、时间、弧长分别处理 |
| [JuiceStreamPath](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Objects/JuiceStreamPath.cs) | 时间—X 折线；`ComputeRequiredVelocity()`、`ConvertToSliderPath()`、`ConvertFromSliderPath()` | 参考反向几何和纵向折叠；自行增加贝塞尔到时间节点的采样适配 |
| [Catch EditablePath](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Edit/Blueprints/Components/EditablePath.cs) | 路径变化后调整 SV、生成 slider、吸附末端 | 参考更新顺序；SV 被限制后不一定保留目标路径，本项目要报告失败与误差 |
| [JuiceStream](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Objects/JuiceStream.cs) | 从 slider event 生成 Fruit、Droplet、TinyDroplet，应用 timing/SV | 真实 stream 转换入口的参考，不把均匀曲线点替代生成对象 |

`JuiceStreamPath` 使用折线表示；本项目另外保存贝塞尔控制柄。

## 香蕉雨

| 源码 | 职责 | 本项目使用方式 |
| --- | --- | --- |
| [BananaShower](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game.Rulesets.Catch/Objects/BananaShower.cs) | 父对象保存开始时间和持续时间，逐根香蕉由 nested hit objects 派生 | Banana 工具左键设置开始、右键设置结束；属性面板编辑时间范围，逐根 X 继续由完整谱面 RNG 生成 |

## 音乐、编辑时钟与拖动进度

| 源码 | 职责 | 本项目使用方式 |
| --- | --- | --- |
| [PlaybackControl](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/Components/PlaybackControl.cs) | 播放按钮调用编辑时钟 Start/Stop | 本项目播放按钮与 Space 都经过同一 transport |
| [EditorClock](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/EditorClock.cs) | 关联 Track，提供 Start/Stop/Seek、长度、状态与末尾处理 | 分离音频后端和编辑时钟，统一时间源 |
| [MarkerPart](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Screens/Edit/Components/Timelines/Summary/Parts/MarkerPart.cs) | 进度条点击、拖动、释放映射到 Seek；播放中拖动节流 | 即时更新指针、合并音频请求、释放提交最终目标 |
| [FramedBeatmapClock](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/Beatmaps/FramedBeatmapClock.cs) | 音轨时间、平滑帧时钟、offset 与 seek 映射 | 参考时钟分工；不照搬其平台或后端偏移常数 |

这些类依赖 osu!framework。项目自身的音频实现见[技术架构](ARCHITECTURE.md)。

## 依赖和许可证

该 commit 的 [osu.Game.csproj](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/osu.Game/osu.Game.csproj) 目标为 net8.0，但同时引用 Realm、osu!framework、资源等依赖；本项目不引用该项目。

仓库许可见 [LICENCE](https://github.com/ppy/osu/blob/48c4800e3ae4ee752452cdff83bd3787ccf3105f/LICENCE)。实际复用时保留要求的版权和许可声明，记录源文件与 commit；原生音频依赖、资源与其他仓库单独核查，不因主仓库许可推断其所有依赖许可。

已适配的 Catch 转换、红果判定和皮肤尺寸规则及其源文件记录见 [第三方声明](../THIRD_PARTY_NOTICES.md)。
