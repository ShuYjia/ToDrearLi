# BPF 九步有序放置操作题构建指南

> 状态：现行操作依据
>
> 更新日期：2026-07-21
>
> 适用场景：`Assets/A_MRNet/Sence/03SingleplayerScene.unity`

本文记录“04硬件连接操作”的完整构建方法。该题要求玩家按固定顺序，
把 9 个 `OBJ` 物体移动到各自的 `TAR` 目标位置，并允许在同一 Unity 场景中
切换题目或返回重做。

本文只讲九步题目的业务数据、对象配对、步骤状态和验证。黄色/青色光圈、
方向线、箭头和统一框架文字提示的配置见
[有序放置视觉指引指南](./bpf-ordered-placement-guidance-guide.md)。

## 1. 当前实现的组成

| 层次 | 当前文件或组件 | 职责 |
| --- | --- | --- |
| 业务节点 | `Assets/Resources/BusinessPrefabs/.../01服务器网络搭建.prefab` | 用 `childData` 把 04 加入题目顺序 |
| 题目数据 | `Assets/Resources/BusinessPrefabs/.../01服务器网络搭建/04硬件连接操作.prefab` | 保存 9 条提示、9 个 OBJ 名称和操作题 UI 引用 |
| 通用 UI | `Assets/Resources/TopicPrefabs/操作题.prefab` | 只提供“下一步”，不包含选择题选项或确认按钮 |
| 统一步骤提示 | `Tips  Canvas`、`TexCore_TipsManager` | 通过 `显示标题` 接收当前 `hintTexts[stepIndex]`，不维护第二套步骤状态 |
| 题目逻辑 | `Panel_Operate.cs` | 校验当前 OBJ 名称、推进 `stepIndex`、处理切题和提交 |
| 步骤树 | `TexCore_StepObj.cs` | 根据 Node、Topic、Step 把对象切换为未完成、等待触发或已完成 |
| 放置交互 | `TexCore_OrderedPlacementStep.cs` | 只允许当前 OBJ 移动，释放时判断距离，成功后吸附到 TAR |
| 场景对象 | `03SingleplayerScene.unity` | 保存 9 组 OBJ/TAR、交互组件和步骤树引用 |

当前功能形成于以下提交：

```text
72c0b72  feat(scene): add nine-step hardware connection operation
d9c258d  fix(topic): use dedicated UI for hardware operation
40ed492  feat(scene): add guidance to ordered placement steps
```

提交号用于追溯历史，实际操作仍以当前工作树为准。

## 2. 九步固定映射

题目数组使用 `0–8` 索引，Hierarchy 名称使用便于人工识别的 `01–09` 前缀。

| 步骤 | 索引 | OBJ | OBJ“物体标签UI”名称 | TAR | 提示中的目标接口名 |
| --- | ---: | --- | --- | --- | --- |
| 01 | 0 | `01_OBJ_DiChaChaTou` | 机柜电源插头 | `01_TAR_DiChaChaZuo` | 地插插座 |
| 02 | 1 | `02_OBJ_APDiChaWangXian` | 地插网线水晶头 | `02_TAR_DiChaWangXianKou` | 地插网线口 |
| 03 | 2 | `03_OBJ_FWQWangXianChaTou` | 服务器网线水晶头 | `03_TAR_FWQWangKou` | 服务器网口 |
| 04 | 3 | `04_OBJ_APWangXianChaTou` | AP 网线水晶头 | `04_TAR_APWangKou` | AP 网口 |
| 05 | 4 | `05_OBJ_ACWangXianChaTou` | AC 网线水晶头 | `05_TAR_ACWangKou` | AC 网口 |
| 06 | 5 | `06_OBJ_XianShiQiVGAChaTou` | 显示器 VGA 插头 | `06_TAR_VGAKou` | VGA 接口 |
| 07 | 6 | `07_OBJ_FWQDianYuanChaTou` | 服务器电源插头 | `07_TAR_FWQDianYuanChaKong` | 服务器电源插孔 |
| 08 | 7 | `08_OBJ_JHJDianYuanChaTou` | 交换机电源插头 | `08_TAR_JHJDianYuanChaKong` | 交换机电源插孔 |
| 09 | 8 | `09_OBJ_ACDianYuanChaTou` | AC 电源插头 | `09_TAR_ACDianYuanChaKong` | AC 电源插孔 |

OBJ 提示名必须从该 OBJ 子级的“物体标签UI”实际文本读取，不能从拼音对象名猜测。
TAR 目前是空的目标 Transform，所以面向玩家的接口名由 TAR 的业务语义确定，并与
TAR 名称和 OBJ 配对共同核对。

三个顺序必须完全一致：

```text
题目 Prefab 的 tasks 顺序
  = TexCore_StepObj 的 StepObjs 顺序
  = OBJ 上 TexCore_OrderedPlacementStep.enterTopic.enterStep
```

## 3. Hierarchy 命名与对象准备

### 3.1 推荐结构

在 03 场景的业务交互根节点下，把对象整理为容易核对的结构：

```text
有交互物体
├─ 01_OBJ_DiChaChaTou
├─ 01_TAR_DiChaChaZuo
├─ 02_OBJ_APDiChaWangXian
├─ 02_TAR_DiChaWangXianKou
├─ ...
├─ 09_OBJ_ACDianYuanChaTou
└─ 09_TAR_ACDianYuanChaKong
```

也可以把 OBJ 和 TAR 分别放在两个子根下，但名称和数字前缀必须保持一致。

### 3.2 OBJ 必备组件

每个 OBJ 至少需要：

- 可被 MRTK/XR 选中的 Collider；
- `ObjectManipulator`；
- `TexCore_OrderedPlacementStep`；
- 原模型需要物理运动时配置 `Rigidbody`，否则不是强制项。

`TexCore_OrderedPlacementStep` 带有 `RequireComponent(ObjectManipulator)`，但 Collider
仍需根据模型形状人工确认。

### 3.3 TAR 的要求

TAR 是目标位姿，不是玩家要抓取的物体：

- Transform 的位置和旋转就是最终吸附位置；
- 可以是空 GameObject；
- 不需要 `ObjectManipulator`；
- 不要让 TAR 自己进入 `StepObjs.stepObjs`；
- 名称前缀必须与对应 OBJ 相同。

## 4. 配置 TexCore_OrderedPlacementStep

在每个 OBJ 的 Inspector 中配置：

| 字段 | 推荐值 | 作用 |
| --- | --- | --- |
| `Target` | 同编号 TAR | 成功后吸附到该 Transform 的位置和旋转 |
| `Position Tolerance` | `0.12` | OBJ 距离 TAR 不超过 0.12 米时判定成功 |
| `Rotation Tolerance` | `180` | 当前硬件题不检查玩家释放时的旋转 |
| `Reset When Inactive` | 开启 | 未轮到该步或返回未完成题时恢复初始位姿 |
| `Enter Step` | `0–8` | 与表格中的步骤索引一致 |

脚本会在 `Start()` 中把 `ObjectManipulator.AllowedManipulations` 限制为 `Move`：

- 玩家可以平移 OBJ；
- 不能用手势旋转或缩放 OBJ；
- 成功后由 TAR 的 Transform 决定最终旋转。

不要再给同一个 OBJ 添加另一套会同时改变 Transform 的拖拽脚本，否则释放事件、
复位和吸附可能互相覆盖。

## 5. 配置 TexCore_StepObj 步骤树

场景中的 `TexCore_StepObj.interacterObjs` 是三级结构：

```text
NodeObjs
  -> TopicObjs
     -> StepObjs
```

04 是“01服务器网络搭建”下的第 4 道题。配置时必须从业务 Prefab 的真实
`childData` 顺序确定 Node/Topic 位置，不要只看“04”猜数组索引。

在 04 对应的 Topic 下建立 9 个连续 Step：

```text
Step 0 -> 01_OBJ_DiChaChaTou
Step 1 -> 02_OBJ_APDiChaWangXian
Step 2 -> 03_OBJ_FWQWangXianChaTou
Step 3 -> 04_OBJ_APWangXianChaTou
Step 4 -> 05_OBJ_ACWangXianChaTou
Step 5 -> 06_OBJ_XianShiQiVGAChaTou
Step 6 -> 07_OBJ_FWQDianYuanChaTou
Step 7 -> 08_OBJ_JHJDianYuanChaTou
Step 8 -> 09_OBJ_ACDianYuanChaTou
```

每个 Step 的 `StepScripts`/`stepObjs` 只放当前 OBJ 对应的
`TexCore_OrderedPlacementStep`。当前实现是严格有序，不是九个物体同时可抓取。

## 6. 构建操作题 Prefab

### 6.1 从独立操作题 UI 开始

使用：

```text
Assets/Resources/TopicPrefabs/操作题.prefab
```

不要从 `选择题.prefab` 复制后仅隐藏选项。独立模板的目的就是避免以后选择题
模板变化再次影响操作题。

当前模板只包含“下一步”按钮。题目 Prefab 中应满足：

```text
btn_Next    -> 有效“下一步”对象
btn_Confirm -> None
```

### 6.2 题目基础数据

04 的 `DataClass_Topic` 推荐配置：

| 字段 | 配置 |
| --- | --- |
| `topicType` | `3`（操作题） |
| `tasks` | 按 01–09 顺序填写 9 个 OBJ 的完整名称 |
| `hintTexts` | 与 tasks 一一对应的 9 条操作说明 |
| `hintAudios` | 保持 9 项；暂时无音频时每项留空 |
| `score` | 按课程规则设置；当前 04 为 `0` |
| `isNextShow` | 开启，完成九步后显示“下一步” |
| `isFalseHint` | 开启，操作错误时显示提示 |

`tasks` 必须写完整 GameObject 名称，因为 `Panel_Operate` 使用：

```csharp
obj.name.Equals(topicData.tasks[stepIndex])
```

名称大小写、下划线或数字前缀有一处不同，就不会推进步骤。

### 6.3 当前九条提示格式

每条 `hintTexts` 使用同一结构：

```text
步骤 N/9  当前部件：<OBJ 物体标签UI名称>
目标接口：<对应 TAR 的业务接口名>
操作：拖动该插头或水晶头对准目标，靠近后释放。
```

当前部件用黄色 `#FFD84A`，目标接口用青色 `#37E6EF`，颜色分别呼应 OBJ 和 TAR
光圈。Prefab 中使用 TMP 富文本 `<b>`、`<color>` 和 `\n`，由现有
`TexCore_TipsManager.Title()` 原样交给 `TitleText` 渲染。

提示顺序、tasks 顺序和场景 Step 顺序必须一起修改，不能只调整显示文字。若模型
标签名称变更，也要同步修改对应的 `hintTexts`。

### 6.4 旧 ZhiYin 面板的处理

03 场景 `无交互物体/NetWork-03_2/MR_ChangJing/JIGUI/ZhiYin` 下原有的
`任务提示UI_*` 是遗留教学面板，内容与九步映射不完整，继续保持关闭。04 不再通过
这些面板的按钮 `SetActive` 切换文案，也不复制九份新面板。

04 使用框架已经存在的唯一链路：

```text
Panel_Operate.stepIndex
  -> hintTexts[stepIndex]
  -> 显示标题事件
  -> TexCore_TipsManager
  -> Tips  Canvas / TitleText
```

因此切换步骤、切换题目和开关标题仍由框架控制，不会出现提示 UI 与真实步骤脱节。

## 7. 把 04 接入父节点

在：

```text
Assets/Resources/BusinessPrefabs/
MR混合现实多人协作平台/
01货到人拣选电商仓储实施安装与调试实训系统/
01服务器网络搭建.prefab
```

的 `Aa_Node.datas.childData` 中加入：

```text
04硬件连接操作
```

名称必须与以下三处完全一致：

1. 题目 Prefab 文件名；
2. 题目根节点及 `datas.name`；
3. 父节点 `childData`。

这是同一场景中的题目切换，所以 `sceneData.sceneName` 保持为空。

不要手工修改 `Assets/StreamingAssets/Config.xml`。它是生成结果，不是配置源。

## 8. 运行时顺序为什么是强制的

运行链路如下：

```text
Panel_Operate 当前 stepIndex
  -> TexCore_StepObj 设置当前 Step 状态
  -> 只有当前 TexCore_OrderedPlacementStep 进入 UnderWay
  -> 只有当前 OBJ 的 ObjectManipulator 被启用
  -> 玩家把 OBJ 释放到 TAR 容差内
  -> OBJ 吸附到 TAR
  -> TexCore_BusinessManager.OnClick(OBJ)
  -> Panel_Operate 核对 tasks[stepIndex]
  -> stepIndex + 1
  -> 激活下一步
```

因此提前抓取后面的 OBJ 不会推进步骤；正常情况下它的 Manipulator 也是关闭的。

九步完成后 `stepIndex == tasks.Count`，也就是 `9`。这个值是框架的终态哨兵，
用来让步骤树把 `enterStep 0–8` 全部标记为已完成，不要把它强行改回 8。

## 9. 切换题目时的状态

当前 `Panel_Operate.OnEnable()` 规则：

- 未提交题目重新进入时，`stepIndex` 从 0 开始；
- `steps` 重新初始化；
- 场景对象按第 0 步状态复位；
- 已提交题目重新进入时保留终态；
- 操作题进入时关闭知识面板，避免遮挡 OBJ/TAR；
- 完成全部任务后显示“下一步”。

如果希望“未完成题返回时从离开位置继续”，需要另行修改产品规则和状态持久化，
不能只删除 `stepIndex = 0`，否则场景对象位姿和已做步骤列表可能不一致。

## 10. 步骤音频配置

`Panel_Operate` 的“步骤语音”区域按 `stepIndex` 配置 `stepAudioClips`：

| 步骤 | 当前音频 | 对应操作 |
| --- | --- | --- |
| 1 | 空 | 旧音频没有“机柜电源插头接地插插座”的匹配内容 |
| 2 | 空 | 旧音频没有“地插网线接地插网口”的匹配内容 |
| 3 | `1—4.MP3` | 服务器网线连接 |
| 4 | `1—6.MP3` | AP 网线连接 |
| 5 | `1—5.MP3` | AC 网线连接 |
| 6 | `1—7.MP3` | 服务器与显示器 VGA 连接 |
| 7 | `1—2.MP3` | 服务器电源连接 |
| 8 | 空 | 旧音频没有“交换机电源插头接交换机”的匹配内容 |
| 9 | 空 | 旧音频没有“AC 电源插头接 AC”的匹配内容 |

`1—1.MP3` 是检查 AC/AP 接线，`1—8.MP3` 是按服务器电源按钮，当前九步中
没有同义操作，所以不绑定。不要仅按文件编号把八段音频顺序塞入步骤 1–8。

运行时规则：

- 第一次进入 04 时播放第 1 步音频；
- 每次成功操作并推进 `stepIndex` 后，先停止上一段，再播放新步骤音频；
- 切到其他题目或禁用 04 面板时立即停止；
- 对应槽为空或步骤超出列表时保持静默，不报错；
- 音频使用题目自身的 2D `AudioSource`，不会激活旧 `ZhiYin` UI 子物体；
- 其他未配置 `stepAudioClips` 的 `Panel_Operate` 题目继续沿用原音频路径逻辑。

手工更换语音时，只需在 04 Prefab 根节点的 `Panel_Operate > 步骤语音`
列表中替换对应 `AudioClip`；列表索引 0 对应步骤 1，索引 8 对应步骤 9。

## 11. 验证清单

### 10.1 静态检查

- 业务父节点包含精确名称 `04硬件连接操作`；
- `topicType = 3`；
- `tasks`、`hintTexts`、`hintAudios` 都是 9 项；
- `stepAudioClips` 是 9 项；步骤 3–7 非空，其余步骤为空；
- `btn_Next` 非空，`btn_Confirm` 为空；
- 选择题模板 GUID 没有出现在 04 Prefab 中；
- 场景有 9 个 `TexCore_OrderedPlacementStep`；
- 9 个 Target 都不为空；
- `enterStep` 恰好覆盖 `0–8`，没有重复或缺失；
- 03 场景已保存；
- 不存在与任务无关的 Unity 自动改动。

### 10.2 编译检查

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore -v:q -clp:ErrorsOnly
git diff --check
```

### 10.3 Play Mode 检查

1. 从 `01StartUp` 进入 `03SingleplayerScene`。
2. 打开“01服务器网络搭建”中的“04硬件连接操作”。
3. 确认没有选择题选项和“确定”按钮。
4. 尝试先抓第 2 个 OBJ，确认不能推进。
5. 按 01–09 完成所有放置。
6. 每次释放成功后确认 OBJ 自动吸附到 TAR。
7. 第 9 步完成后确认出现“下一步”。
8. 切到其他题目再返回：未完成题应从第 1 步开始，已完成题应保持终态。
9. 检查 Console 没有空引用、数组越界或重复事件错误。
10. 确认步骤 3–7 各播放一次对应语音，其余步骤静默，切题后声音立即停止。

## 12. 常见故障

| 现象 | 优先检查 | 常见原因 |
| --- | --- | --- |
| 04 显示成选择题 | 04 的 TopicCanvas Prefab 来源 | 仍引用 `选择题.prefab` |
| 当前 OBJ 无法抓取 | StepObjs、`enterStep`、Collider、Manipulator | 步骤映射错或交互组件缺失 |
| 抓后释放不成功 | Target、`positionTolerance` | TAR 未绑定或容差过小 |
| 放错物体也进入下一步 | `tasks` 和 OBJ 名称 | tasks 顺序或名称配置错误 |
| 完成一步后下一步不激活 | `Panel_Operate.NextStepState`、步骤树 | Topic 索引或 Step 列表不连续 |
| 返回 04 后物体位置混乱 | `resetWhenInactive`、题目提交状态 | 复位关闭或状态规则不一致 |
| 最后一步没有完成态 | 是否广播 `stepIndex == 9` | 把终态哨兵错误限制为 8 |
| 切步后仍播放上一段 | `PlayConfiguredStepAudio` 是否执行 | 步骤推进分支未接入音频切换 |
| 空音频槽仍报加载错误 | `stepAudioClips` 对应元素 | 使用了路径字符串而不是空 `AudioClip` |
| 修改 Config 后又失效 | 配置源位置 | 手改了生成的 `Config.xml` |

## 13. 最终原则

```text
OBJ/TAR 用相同两位数字配对；
业务 tasks、步骤树和 enterStep 使用同一顺序；
操作题使用独立 UI，不复用选择题面板；
当前步骤才允许抓取；
释放进入容差后由 TAR 决定最终位姿；
第 9 步完成后的 stepIndex=9 是正常终态；
步骤音频与 stepIndex 同索引，空 Clip 保持静默；
配置改 Prefab 和 Scene，不手改 Config.xml。
```
