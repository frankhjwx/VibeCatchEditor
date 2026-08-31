# M2 音频与轨迹编辑验收

日期：2026-08-31。程序位于 `artifacts/M2-Editing/VibeCatchEditor.App.exe`，旧验收包保留不覆盖。

## 本版使用方式

- 选择工具下，点击 slider 的任意 Fruit / Droplet / TinyDroplet 可选中整个父 slider；命中范围使用皮肤实际尺寸，隐藏曲线后仍然有效。选中父对象时，其子对象一起标记。
- 导入 slider 选中后，点击右侧 **编辑 Slider** 转成可编辑时间—X 轨迹，再选择锚点拖动或输入数值。一次撤销可恢复转换前的原对象。未进入编辑的 slider 保留原始文件表示。
- **B** 为贝塞尔，**L** 为直线；绘制同一条轨迹时可交替切换，Enter 完成，Esc 取消整个草稿。
- 选中已有锚点后，用 **下一段：直线 / 贝塞尔** 切换它到下一个锚点的段类型；**分割插点 · 保持形状** 插入节点，随后可改段类型和控制柄。
- repeat 保留为同一个 slider 的多个行程，不拆成相邻 slider。编辑首行程节点会更新全部往返；属性区可修改行程次数。
- 主画布播放线固定在距画布底部 **25%** 处。播放、seek、恢复 AR 比例及跟随状态下 resize 保持此位置，谱面首尾允许留白。暂停后仍可手动浏览，恢复播放时重新固定。右侧预览保留游戏下落比例和判定线。
- Banana 使用普通 fruit 的 **0.6 倍静态尺寸**，皮肤底图、overlay 和基础图形一致。这对应 lazer 的接取时尺寸，不包含随机缩放动画；参考见 [皮肤说明](../src/VibeCatchEditor.App/Skinning/REFERENCE.md)。

## 音频修复和证据

原 MP3 seek 回报请求位置，但实际 PCM 落点偏晚：两张用户测试图中段分别测得约 192.245 ms 和 247.664 ms 的偏差。现在后台连续解码成有界 PCM 缓存，后续按完整采样帧定位；时长也使用真实解码帧数，不再使用容器估计值。

两首 MP3 共 16 个定位检查覆盖向前、向后、首帧和接近 EOF，读出的 PCM 与从头连续解码的对应片段逐字节相同。真实输出设备测试验证播放、暂停、seek、结束、取消和恢复。没有添加任意 offset，也没有修改系统音量或设备设置。

MP3 缓存上限为 512 MiB，按 64 KiB 分块，加载在后台且可取消。输出请求缓冲仍为 80 ms，设备时钟每 10 ms 采样；**硬件、蓝牙或扬声器延迟尚未校准**，也未证明与 stable/BASS 的 encoder-padding 策略一致。详见 [音频实现说明](../src/VibeCatchEditor.App/Audio/REFERENCE.md)。

## 构建与自动化验证

统一 Release 构建和发布：0 warning / 0 error。合计 **141 项检查通过**：

| 项目 | 数量 | 本轮重点 |
| --- | ---: | --- |
| Core.Tests `--fixtures` | 42 | 混合段、保形分割、repeat、导入转编辑、原子失败及两张实图 |
| App.Tests | 33 | 固定播放线首尾/缩放/resize、子对象选择、混合绘制、导入编辑、撤销与保存 |
| Formats.Tests | 24 | SpanCount、逐段类型、样本保留、工程往返、实图修改导出 |
| Audio.Tests | 11 | 实际设备控制、PCM 对齐、帧时长、取消与恢复 |
| Gameplay.Tests | 13 | Banana 静态比例、CS 与 hyperdash |
| Skinning.Tests | 10 | Banana base/overlay、@2x、裁剪及实际绘制范围 |
| SkinArchive.Tests | 8 | OSK 安全导入回归 |

两张测试图共 7 组代表案例覆盖 L/B/P 及 repeat。转为编辑模型后对象类型和时刻保持一致，最大 X 变化约 0.000031；随后修改首段为贝塞尔、保存工程重开并输出 `.osu`，全部对象序列一致。回读时间差为 0，整数路径输出的最大 X 差异如下：

| 测试图 | 路径 / 行程 | X 最大差异 |
| --- | --- | ---: |
| Vidro Moyou | B / 1 | 0.345001 |
| Vidro Moyou | L / 1 | 0.263611 |
| Vidro Moyou | P / 1 | 0.099549 |
| Oriental Blossom | L / 1 | 0.450439 |
| Oriental Blossom | P / 1 | 0.742478 |
| Oriental Blossom | L / 2 | 0 |
| Oriental Blossom | B / 1 | 0.461243 |

这些数值是本项目转换前后和输出回读之间的比较，不是 stable 客户端验证或所有地图的误差上界。导入转换先验证事件再替换，保留父 ID、源顺序和音效样本；无法转换时明确报错并保留原对象。

## 实际窗口与限制

新版已打开 Oriental Blossom，观察到播放中的固定水平线、真实音频时长和两视图。检测到用户输入后停止自动鼠标操作，因此按钮与混合段编辑的交互证据来自上述 App 输入 API 测试，不能称为完整桌面鼠标验收。[窗口截图](../artifacts/m2-editing-validation/screenshots/editor.png)。

DX11 render-check 通过 96 / 144 / 192 DPI 两种尺寸共六组布局及零尺寸恢复，皮肤解码 14 个位图变体。1000 fruit 的 60 帧隐藏窗口测量中位数 8.2758 ms、P95 8.6965 ms；这不是可见窗口帧率保证。[渲染报告](../artifacts/logs/render-check.json)。

导入轨迹以折线近似原路径投影，不能恢复原作者的贝塞尔控制柄；转换后可逐段改成贝塞尔。导入轨迹默认保留实际 Tiny RNG 偏移，不自动受全局 Tiny 贴合开关影响；新轨迹按全局设置生成。重复行程共享路径时 Tiny 贴合目标可能冲突，届时明确报告并退回实际未补偿的 Tiny。仍不加载视频或 storyboard，不加入完整游戏客户端。
