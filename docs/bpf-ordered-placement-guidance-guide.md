# BPF 有序放置视觉指引指南

> 状态：现行操作依据
>
> 更新日期：2026-07-21
>
> 实现脚本：`Assets/A_MRNet/Scripts/TexCore_OrderedPlacementStep.cs`

本文专门记录有序放置题的视觉和文字指引模块。当前效果是：

- 当前可抓取 OBJ 显示黄色呼吸光圈；
- 对应 TAR 显示青绿色呼吸光圈；
- OBJ 到 TAR 之间显示渐变方向线；
- TAR 端显示箭头；
- 框架统一 `Tips  Canvas` 显示当前部件、目标接口和操作动作；
- 完成、切题或复位时自动隐藏。

九步 OBJ/TAR、题目数据和步骤树配置见
[九步有序放置操作题构建指南](./bpf-nine-step-ordered-placement-build-guide.md)。

## 1. 为什么这样实现

视觉指引直接封装在 `TexCore_OrderedPlacementStep` 中，而不是为九步分别手工创建
箭头和光圈。原因是该组件已经知道：

- 当前 OBJ；
- 对应 TAR；
- 当前步骤是否处于 `UnderWay`；
- 步骤何时未完成、触发、完成或禁用。

这样一套默认代码就能覆盖所有九步，并保证指引生命周期与交互生命周期一致。

未采用的旧模块：

| 模块 | 未采用原因 |
| --- | --- |
| `TexCore_HighlighPlus` | 当前源码整体被注释，不能直接挂载使用 |
| `CompleteNavigationPath` | 面向 NavMesh 人物导航，不适合插头到插孔的短距离配对 |
| 03 场景旧 `ZhiYin/任务提示UI_*` | 九份旧面板内容不完整且依赖独立按钮，不与九步状态树绑定 |
| 每步手工搭建箭头 Prefab | 重复对象多，容易漏绑，调整颜色和尺寸时需修改九份 |

文字提示没有新增 Presenter 或第二套状态。`Panel_Operate` 本来就会在进入题目和
推进步骤时广播 `显示标题`；`TexCore_TipsManager` 接收后更新框架的 `TitleText`。
本题只把九条 `hintTexts` 统一为“步骤 + 当前部件 + 目标接口 + 操作”格式。

## 2. 统一文字提示 UI

使用场景现有对象：

```text
Tips  Canvas
└─ Canvas
   └─ DialogPool
      └─ Text        # TexCore_TipsManager.TitleText
```

控制对象是 `TexCore/TipsManager` 上的 `TexCore_TipsManager` 组件：

- `TitleImage` 指向 `Tips  Canvas`；
- `TitleText` 指向上述 `Text`；
- `Title(string)` 接收 `显示标题` 并刷新布局；
- `开关标题` 事件负责在不同题型/面板之间显示或关闭统一提示。

04 的九条文案来自题目 Prefab，不写死在场景 UI 中。当前 OBJ 名称以各 OBJ 子级
“物体标签UI”的真实文字为准；目标接口名与对应 TAR 一一匹配。OBJ 名称使用黄色，
TAR 接口使用青色，与空间光圈的颜色语义相同。

不要给旧 `ZhiYin/任务提示UI_*` 添加九步按钮绑定；这些对象继续保持关闭，只作为
遗留场景资源保留。

## 3. 空间指引的运行组成

当某一步第一次进入当前状态时，脚本在运行时创建：

```text
<OBJ名称>_StepGuidance
├─ CurrentObjectMarker  # 当前 OBJ 黄色圆环
├─ TargetMarker         # TAR 青绿色圆环
├─ DirectionLine        # OBJ 到 TAR 的渐变线
└─ ArrowHead            # TAR 端箭头
```

这些对象使用 `HideFlags.DontSave`：

- 不会写入 03 场景；
- 不需要额外 Prefab；
- 不会在退出 Play Mode 后污染 Hierarchy；
- 组件销毁时会同时销毁运行时材质和指引根对象。

渲染使用内置管线的 `Sprites/Default` Shader 和 `LineRenderer`，不增加外部
材质资源。当前项目的 Graphics/Quality Settings 没有自定义 SRP，因此该方案
与现有渲染基线一致。

## 4. 自动生命周期

| 步骤状态 | `TexCore_OrderedPlacementStep` 方法 | 指引效果 |
| --- | --- | --- |
| 未完成 | `UnFinished()` | 隐藏指引，禁止拖动，按配置复位 OBJ |
| 当前步骤 | `UnderWay()` | 开启拖动并显示四个指引绘制器 |
| 触发成功 | `OnTrigger()` | 隐藏指引，吸附 TAR，禁止继续拖动 |
| 已完成 | `Finish()` | 保持完成位姿并隐藏指引 |
| 题目/对象禁用 | `OnDisable()` | 立即隐藏指引并解绑释放事件 |
| 对象销毁 | `OnDestroy()` | 清理运行时对象和材质 |

只有当前步骤调用 `UpdateGuidance()`。不是当前步骤的 8 个 OBJ 不会持续更新
指引，因此不会同时出现九组箭头。

## 5. 如何启用

对已经配置好的有序放置 OBJ，无需额外创建子物体：

1. OBJ 挂载 `TexCore_OrderedPlacementStep`。
2. `Target` 绑定对应 TAR。
3. `Show Guidance` 保持开启。
4. 让该 OBJ 正确进入 `TexCore_StepObj` 的当前 Step。
5. 保存场景并进入 Play Mode。

已有组件如果还没有把新增字段保存进场景，会使用脚本中的默认值；在 Inspector
中修改任一指引参数并保存场景后，该 OBJ 会保存自己的覆盖值。

## 6. Inspector 参数

| 参数 | 默认值 | 效果 |
| --- | ---: | --- |
| `Show Guidance` | 开启 | 是否为该 OBJ 显示整套指引 |
| `Source Guide Color` | 黄色 `(1, 0.78, 0.05, 1)` | 当前 OBJ 圆环和方向线起点颜色 |
| `Target Guide Color` | 青色 `(0.05, 0.95, 1, 1)` | TAR 圆环、方向线终点和箭头颜色 |
| `Source Marker Radius` | `0.06 m` | OBJ 光圈半径 |
| `Target Marker Radius` | `0.07 m` | TAR 光圈半径 |
| `Guide Line Width` | `0.008 m` | 圆环、方向线和箭头的线宽 |
| `Guide Vertical Offset` | `0.015 m` | 指引朝玩家方向离开模型表面的距离 |
| `Pulse Amount` | `0.18` | 呼吸缩放幅度 |
| `Pulse Speed` | `3` | 呼吸变化速度 |

`Position Tolerance` 和 `Rotation Tolerance` 是放置判定参数，不是视觉参数。
增大光圈不会自动增大成功容差。

## 7. 画面效果细节

### 7.1 OBJ 与 TAR 光圈

两个圆环均朝向 `Camera.main`，玩家移动视角后仍能看到完整圆形，不会因为 TAR
自身旋转而变成难以辨认的细线。

光圈尺寸按正弦曲线变化：

```text
radius = baseRadius * (1 + sin(unscaledTime * pulseSpeed) * pulseAmount)
```

使用 `unscaledTime`，即使业务暂停了 `Time.timeScale`，提示仍然可见。

### 7.2 方向线与箭头

方向线每帧读取 OBJ 和 TAR 的世界坐标，所以拖动 OBJ 时起点会实时跟随。
线条从黄色渐变到青色，箭头固定在 TAR 端。

当 OBJ 与 TAR 几乎重合时，方向线和箭头会临时关闭，避免零长度方向计算产生
异常；光圈仍可显示。

### 7.3 遮挡关系

当前指引使用普通透明 LineRenderer，不会穿透不透明模型或墙体。这样不会让
指引破坏场景深度感，但在目标被大型模型完全遮挡时，可能仍需玩家改变视角。

如果目标仅与表面重叠，可优先增大 `Guide Vertical Offset`，不要先把线宽调得
非常大。

## 8. 推荐调参方法

一次只改一类参数，并在 Unity Play Mode 和 PICO 中分别观察。

| 现象 | 建议调整 |
| --- | --- |
| OBJ 光圈太小 | 增大 `Source Marker Radius`，例如 `0.06 -> 0.08` |
| TAR 不容易发现 | 增大 `Target Marker Radius`，例如 `0.07 -> 0.10` |
| 线条太细 | 增大 `Guide Line Width`，建议先试 `0.010` |
| 线条遮挡模型 | 减小 `Guide Line Width` 或光圈半径 |
| 光圈与模型表面闪烁 | 增大 `Guide Vertical Offset`，例如 `0.015 -> 0.025` |
| 呼吸太急、容易疲劳 | 减小 `Pulse Speed`，例如 `3 -> 2` |
| 呼吸变化太夸张 | 减小 `Pulse Amount`，例如 `0.18 -> 0.10` |
| 黄色/青色与模型混在一起 | 调整 `Source/Target Guide Color`，保持两者明显不同 |
| 个别大型 OBJ 需要不同尺寸 | 只修改该 OBJ 组件上的半径覆盖值 |

不要为了视觉好看修改 `Target` 或成功容差；它们属于交互判定。

## 9. 新题复用方法

任何使用 `TexCore_OrderedPlacementStep` 的有序放置题都可以复用指引：

1. 为每个可移动对象配置独立 TAR。
2. 把每个对象放入连续的步骤树。
3. 保持 `Show Guidance = true`。
4. 使用统一默认颜色；只有视觉冲突时才做单对象覆盖。
5. 在 Play Mode 检查当前步骤只有一组指引。

不需要复制本次 04 的运行时 `_StepGuidance` 对象。它们由代码自动生成，手工
复制反而会造成重复光圈。

## 10. 验证清单

### 10.1 代码与场景检查

- 目标 OBJ 有 `TexCore_OrderedPlacementStep` 和 `ObjectManipulator`；
- `Target` 不为空；
- `Show Guidance` 开启；
- `enterStep` 连续且步骤树引用正确；
- 场景中没有手工保存的 `<OBJ>_StepGuidance`；
- 编译时能找到 `Sprites/Default`；
- Unity Console 没有材质、空 Target 或 LineRenderer 异常。

### 10.2 Play Mode 检查

1. 进入操作题后，只有当前 OBJ 出现黄色光圈。
2. 对应 TAR 出现青色光圈。
3. 方向线从 OBJ 指向 TAR，箭头位于 TAR 端。
4. 拖动 OBJ 时方向线起点实时跟随。
5. 成功释放后四种指引全部隐藏。
6. 下一步只出现下一组指引。
7. 切换题目后没有残留线条。
8. 未完成题返回时，第 1 步指引重新出现。
9. 九步完成后不再显示任何指引。
10. 框架 `Tips  Canvas` 每步只显示一套文字提示，部件名和目标接口与当前 OBJ/TAR 一致。
11. 切换到其他题目后，04 文案不残留，旧 `ZhiYin/任务提示UI_*` 仍保持关闭。

### 10.3 当前验证结果

2026-07-21 已在 Unity 2021.3.29f1c1 批处理工程中完成：

- C# 编译 0 错误；
- Unity 脚本导入正常退出；
- 加载 `03SingleplayerScene` 后找到 9 个放置组件；
- 每个组件进入 `UnderWay` 时生成 4 个 LineRenderer；
- 每个组件进入 `UnFinished` 时指引根节点关闭；
- 运行验证无异常。

该验证证明生命周期与组件创建正确。颜色亮度、大小、遮挡和视觉舒适度仍需在
实际 Unity Game 视图或 PICO 头显中人工验收。

## 11. 常见故障

| 现象 | 原因 | 处理 |
| --- | --- | --- |
| 完全没有指引 | 该 OBJ 未进入 `UnderWay`、Target 为空或 Show Guidance 关闭 | 先检查步骤树和 Target |
| 九组指引同时出现 | 九个对象都被错误设为当前步骤 | 检查 `TexCore_StepObj` 的 Step 分组 |
| 只有光圈没有方向线 | OBJ 与 TAR 几乎重合 | 检查 TAR 是否误放在 OBJ 初始位置 |
| 指引方向反了 | Target 绑定到错误 TAR | 按两位数字重新核对 OBJ/TAR |
| 光圈随 TAR 倾斜不可见 | 使用了旧实现或额外手工光圈 | 保留当前视角朝向实现，删除重复提示对象 |
| 切题后仍残留 | 对象没有正常执行 Disable/UnFinished | 检查题目切换和步骤状态广播 |
| PICO 中太刺眼 | 颜色、线宽或呼吸幅度过大 | 按第 8 节逐项减小参数 |

## 12. 性能与维护边界

- 当前步骤每帧更新 2 个圆环、1 条线和 1 个箭头；
- 圆环各使用 48 个顶点；
- 一步完成后绘制器关闭，不再执行该步的 `UpdateGuidance()`；
- 九步走完后运行时最多创建 9 组轻量指引对象，只有当前组激活；
- 不要把圆环段数无理由提高到数百；PICO 中视觉收益很小；
- 如果未来迁移到 URP/HDRP，需要重新验证 `Sprites/Default` Shader 可用性。

## 13. 最终原则

```text
指引跟随步骤状态，不单独维护第二套流程；
OBJ 用黄色，TAR 用青色，箭头永远指向 TAR；
运行时生成，不保存进场景；
视觉参数与成功判定参数分开调整；
编译和生命周期可自动验证，头显舒适度必须人工验收。
```
