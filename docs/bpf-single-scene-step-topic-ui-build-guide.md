# BPF 同场景步骤与题目 UI 构建经验手册

本文总结本项目中 `02/03/04` 业务步骤、服务器网络搭建题目、题目空间 UI 和场景菜单的配置与排错经验。目标是让后续开发者能区分“切换 Unity 场景”和“在同一场景中切换业务步骤”，并避免题干、选项、按钮在空间中散开或被手势旋转。

适用范围：

- 启动场景进入单人业务场景
- `Single Menu Canvas` 自动生成 L1/L2 菜单
- `TexCore_StepObj` 配置同场景交互步骤
- `Panel_Select` 选择题 Prefab
- MRTK `SolverHandler`、`Follow` 和 `ObjectManipulator`
- `Buttons Canvas`、`背包UI` 等场景 UI 的职责判断

## 1. 先区分三种“切换”

### 1.1 Unity 场景切换

这是从一份 `.unity` 文件进入另一份 `.unity` 文件，例如：

```text
01StartUp.unity
  -> 03SingleplayerScene.unity
```

主要由以下内容控制：

- `MainPage` 的单人入口按钮和 `Solo Scene`
- `TexCore_LoadScene`
- Unity Build Settings

当前业务主线是：

```text
01StartUp
  -> 点击 MainPage/SoloScrollView/Viewport/Content/Gallery/Row/HeroButton (1)
  -> 加载 03SingleplayerScene
```

关键脚本：

```text
Assets/A_MRNet/ProgramResources/ProgramResources/
CommonScripts/Managers/TexCore_LoadScene.cs
```

当前默认业务场景常量：

```csharp
public const string BusinessSceneName = "03SingleplayerScene";
```

注意：场景里可能存在多个同名按钮。必须按完整 Hierarchy 路径确认目标，不能只按名字搜索后随便取第一个。

### 1.2 同一 Unity 场景内切换 L1/L2

点击“服务器网络搭建”或其子题目时，当前需求不是加载另一份 `.unity`，而是在 `03SingleplayerScene` 中切换业务面板、题目 Prefab 和步骤状态。

因此：

- L1 节点的 `Scene Data -> Scene Name` 保持为空。
- L2 题目的 `Topic Data -> Scene Data -> Scene Name` 也保持为空。
- L1/L2 名称不是 Unity 场景名。
- 不要为了题目步骤切换而调用 `SceneManager.LoadScene`。

详细的场景名规则见：

```text
docs/bpf-scene-name-configuration-guide.md
```

### 1.3 同一题目内切换 Step

题目内部的步骤变化由业务题目脚本和 `TexCore_StepObj` 控制，例如提交选择题后触发步骤对象、显示下一步按钮，或让某个三维对象进入等待交互状态。

这不是 Unity 场景切换，也不是手工启停一批无关根物体。

## 2. 业务数据与菜单生成

### 2.1 业务 Prefab 是配置源

业务节点和题目位于：

```text
Assets/Resources/BusinessPrefabs
```

通用题目模板位于：

```text
Assets/Resources/TopicPrefabs
```

框架通过业务 Prefab 生成或读取业务配置。不要手工修改：

```text
Assets/StreamingAssets/Config.xml
```

手工修改生成文件容易在下次生成时被覆盖，也会造成 Prefab 与运行时数据不一致。

### 2.2 L1 节点配置

L1 节点根物体使用 `Aa_Node`，重点检查：

```text
selfDatas[0].sceneData
childData
```

`childData` 按顺序填写子题目名称，不是拖入场景物体引用。名称必须与题目 Prefab 文件名完全一致。

### 2.3 Single Menu Canvas 的职责

`Single Menu Canvas` 保存菜单模板。运行时大致流程为：

```text
BusinessPrefabs
  -> TexCore_ConfigManager 读取数据
  -> TexCore_HeadMenu.InitBtns()
  -> 自动生成 L1/L2 按钮
```

因此：

- 不需要在 Hierarchy 中手工复制每个 L1/L2 按钮。
- 菜单顺序来自业务 Prefab 和 `childData`。
- 菜单按钮切换业务内容，不等于切换 Unity 场景。

## 3. TexCore_StepObj 步骤树

场景中的 `StepObjs` 根物体挂载：

```text
TexCore_StepObj
```

脚本路径：

```text
Assets/A_MRNet/ProgramResources/ProgramResources/
ObjState/TexCore_StepObj.cs
```

`interacterObjs` 是严格的三级结构：

```text
NodeObjs
  -> TopicObjs
     -> StepObjs
```

映射关系：

| 层级 | 含义 | 数据来源 |
| --- | --- | --- |
| `NodeObjs` | 一个 L1 业务节点 | L1 Prefab 顺序 |
| `TopicObjs` | L1 下的一道 L2 题目 | `childData` 顺序 |
| `StepObjs` | 题目中的一步 | 题目真实 `stepIndex` |

### 3.1 stepIndex 规则

- 选择题通常只触发 `stepIndex = 0`。
- 多步骤操作题按实际操作顺序从 `0` 连续递增。
- 不要根据物体名称猜索引。
- `TopicObjs` 顺序必须与运行时题目顺序一致。

选择题提交时，`Panel_Select.Submit()` 会广播当前节点、题目和步骤索引，并切换“确定/下一步”按钮。

### 3.2 可进入步骤树的对象

进入 `StepObjs.stepObjs` 的物体至少需要：

- `TexCore_InteracterBase` 的实现，常用 `TexCore_InteracterFunction`
- 使用点击或物理交互时需要 Collider

`TexCore_InteracterFunction` 可按四种状态控制：

```text
未完成
进行中
触发
已完成
```

启用以下能力时必须检查依赖：

| 配置 | 必需依赖 |
| --- | --- |
| `isSetState` | 兼容的 `TexCore_SelectBase` |
| `isSetCollider` | 目标物体或子物体存在 Collider |
| `isPlayAnimation` | 有效 Animator 和正确动画名 |
| `isTarget` | 非空目标 Transform |

## 4. 选择题 Prefab 的稳定结构

服务器网络搭建的三道选择题位于：

```text
Assets/Resources/BusinessPrefabs/
MR混合现实多人协作平台/
01货到人拣选电商仓储实施安装与调试实训系统/
01服务器网络搭建/
```

对应文件：

```text
01服务器网络搭建练习题.prefab
02服务器硬件组件选择.prefab
03Web服务器类型选择.prefab
```

推荐 Hierarchy：

```text
题目 Prefab 根
├─ SolverHandler
├─ Follow
├─ Panel_Select
└─ TopicCanvas
   └─ 题目
      ├─ BackPlate
      ├─ root                 # 选项
      ├─ 确定
      ├─ 下一步
      └─ 题干
```

核心原则：题干、选项、按钮和可拖动背板必须归属于同一个“题目”根节点。

## 5. 题干与选项散开的根因和修复

### 5.1 错误结构

原结构使用两套独立空间跟随：

```text
场景根 Tips Canvas
└─ 公共题干

题目 Prefab 根
└─ 选项、按钮、BackPlate
```

`Tips Canvas` 和题目 Prefab 各自有 `SolverHandler + Follow`。切换步骤或重新启用对象后，两套 Solver 会分别计算位置，无法保持固定相对关系，表现为题干和选项散开。

### 5.2 正确修复

使用框架已有的题目内标题模式：

```yaml
topicTitle: <题目/题干 的 TextMeshProUGUI 引用>
isHeadline: 1
```

`PanelTopicBase.Start()` 在 `isHeadline = true` 时会：

1. 关闭公共标题。
2. 将 `topicData.hintTexts[stepIndex]` 写入题目内部的 `topicTitle`。

关键脚本：

```text
Assets/A_MRNet/ProgramResources/ProgramResources/
BusinessLogic/Topic/PanelTopicBase.cs
```

同时，BackPlate 上 `ObjectManipulator.hostTransform` 应指向共同的“题目”根节点，而不是只移动 BackPlate 自己：

```yaml
hostTransform: <TopicCanvas/题目>
```

这样移动时会带着以下内容整体移动：

```text
题干 + 选项 + 确定 + 下一步 + BackPlate
```

### 5.3 不推荐的修复

- 只调两个独立 Follow 的 Offset。
- 在每次切换步骤后用脚本硬编码题干世界坐标。
- 给题干和选项分别增加新的跟随器。
- 只让 BackPlate 移动，其他 UI 仍留在原处。

这些做法只能暂时对齐，重新启用、移动玩家或改变视角后仍可能散开。

## 6. 面板保持朝向玩家并禁止手势旋转

这里有两套不同的旋转来源，必须分别处理。

### 6.1 Follow 控制自动朝向

题目 Prefab 根的 `Follow` 决定面板如何朝向玩家。

推荐配置：

```yaml
orientationType: 3
faceTrackedObjectWhileClamped: 0
```

含义：

- `orientationType: 3` 对应 `SolverOrientationType.YawOnly`。
- 面板只绕世界 Y 轴改变方向。
- X、Z 旋转保持为 0，避免面板前后或左右倾斜。
- 取消 `faceTrackedObjectWhileClamped`，防止超出约束时临时恢复完整三轴朝向。

MRTK 对应实现：

```text
Packages/org.mixedrealitytoolkit.spatialmanipulation/
Solvers/Follow.cs
Utilities/SolverOrientationType.cs
```

`YawOnly` 的核心计算等价于：

```csharp
Quaternion.Euler(0f, targetYRotation, 0f);
```

### 6.2 ObjectManipulator 控制手势权限

仅设置 `Follow = YawOnly` 不足以禁止用户手势旋转。BackPlate 上的 `ObjectManipulator` 默认可能仍允许：

```text
Move | Rotate | Scale = 7
```

要禁止手势旋转但保留移动、缩放，应设置：

```text
Move | Scale = 5
```

Prefab YAML：

```yaml
allowedManipulations: 5
```

位标志定义：

```text
Move   = 1
Rotate = 2
Scale  = 4
```

因此：

```text
1 + 4 = 5
```

对应代码：

```csharp
objectManipulator.AllowedManipulations =
    TransformFlags.Move | TransformFlags.Scale;
```

最终职责是：

```text
手势：只能移动、缩放
Follow：独占面板朝向控制，只绕 Y 轴面向玩家
```

## 7. Buttons Canvas 与背包UI

### 7.1 Buttons Canvas

`Buttons Canvas` 是独立的世界空间按钮列表示例，结构包含：

```text
Buttons Canvas
└─ ListItems
   ├─ 标题/说明
   └─ ParentObject
      └─ 多个 List Item
```

目前它主要提供：

- MRTK 按钮视觉
- 悬停/按压反馈
- 点击音效
- “任务提示”等展示项

它不负责：

- L1/L2 菜单生成
- `TexCore_StepObj` 步骤切换
- 三道题的题干、选项或提交

如果当前业务不使用它，可以先在 Hierarchy 中禁用后运行验证；不要仅凭名字判断它是业务按钮系统。

### 7.2 背包UI

`背包UI` 当前是一个世界空间菜单容器，包含：

```text
背包UI
├─ MenuExample
└─ Button Bar_Horizontal
```

可见入口包括：

```text
操作指南 / 学习操作
设置 / 场景设置
目录 / 选择场景
背包 / 选择工具
```

当前 03 场景中的实际状态：

- 菜单可以显示。
- 按钮可以产生视觉和声音反馈。
- 没有配置业务层 `UIManager` 页面映射。
- “背包”按钮没有接通工具列表。
- `BagManager` 相关调用在 `ObjectPlacer.cs` 和 `NodePanelBase.cs` 中已被注释。

因此它现在是“可显示、可按压的菜单外壳”，不是完整可用的背包系统。禁用根物体不会影响现有答题和步骤树，但会同时隐藏这组菜单入口。

判断某个 UI 是否真正有业务作用时，必须同时检查：

1. Hierarchy 是否存在且激活。
2. 按钮是否有 `PressableButton`/交互组件。
3. `OnClicked` 是否绑定真实业务方法。
4. 业务管理器是否在当前场景实例化并配置数据。
5. 点击后是否改变业务状态，而不只是播放动画或声音。

## 8. Unity MCP 与文件验证经验

### 8.1 每轮重新检测

Unity MCP 连接状态不能沿用上一轮结果。每次开始都应重新调用轻量只读探针。

即使返回 `success=true` 且消息是 `queued`，也可判定当次探针已连接。

### 8.2 MCP 短暂断开不等于编辑器退出

Prefab 导入或场景加载时，MCP 可能返回：

```text
Unity session not available; please retry
```

处理顺序：

1. 检查 Unity 进程是否仍响应。
2. 稍后重新调用轻量探针。
3. 查看 `Editor.log` 中的 Asset Import artifact。
4. 用静态 YAML 检查确认关键引用和枚举值。
5. MCP 恢复后再做层级回读。

不要因为一次 `no_unity_session` 就重复启动第二个 Unity 实例。

### 8.3 Prefab 文本编辑后的验证

至少检查：

- `topicTitle` 引用非空。
- `isHeadline = 1`。
- `题干` 的父节点是共同“题目”根。
- `ObjectManipulator.hostTransform` 指向共同“题目”根。
- `allowedManipulations = 5`，旋转位未开启。
- `Follow.orientationType = 3`。
- `faceTrackedObjectWhileClamped = 0`。
- Unity 能导入 Prefab，没有 YAML parse 或 invalid serialized file 错误。

`Serialization depth limit` 和 `Recursive Serialization is not supported` 在本项目可能是既有框架警告，不能看到错误字样就直接归因于本次 Prefab 修改。应对照修改前日志和具体资产导入结果判断。

## 9. 推荐验证顺序

### 9.1 静态检查

1. 确认场景名、Prefab 名、`childData` 完全一致。
2. 确认同场景业务节点的 `Scene Name` 为空。
3. 检查 `StepObjs` 的 Node/Topic/Step 顺序。
4. 检查题干、选项、按钮属于同一个根。
5. 检查 Follow 和 ObjectManipulator 的关键字段。
6. 执行：

```powershell
git diff --check
```

### 9.2 Unity 编辑器运行检查

1. 从 `01StartUp` 进入单人业务。
2. 确认加载 `03SingleplayerScene`。
3. 点击“01服务器网络搭建”L1。
4. 依次进入三道选择题。
5. 在题目之间和步骤之间切换。
6. 确认题干、选项、按钮始终整体显示。
7. 用手势移动面板，确认整体移动。
8. 用双手缩放，确认仍可缩放。
9. 尝试旋转，确认面板不会被手势旋转。
10. 转动玩家视角，确认面板只绕 Y 轴保持面向玩家。
11. 检查公共 `Tips Canvas` 在内置题干模式下不会与题内标题重复显示。

### 9.3 可选 UI 检查

1. 禁用 `Buttons Canvas`，确认答题和步骤不受影响。
2. 禁用 `背包UI`，确认答题和步骤不受影响。
3. 如果要保留背包，单独建立工具列表、页面映射和按钮业务事件，不要默认现有菜单已经可用。

## 10. Git 与交付边界

### 10.1 只提交任务相关字段

Unity 保存 Prefab 时可能重排 YAML 或补齐空格。工作区还可能存在用户自己的场景、XR 设置或其他 Prefab 修改。

提交前必须：

- 查看 `git status --short`。
- 审查每个目标文件的 diff。
- 只暂存本任务实际字段。
- 不覆盖或提交用户的无关改动。

本次问题的提交链可作为定位参考：

```text
0c866e6  move server questions as one panel
972d5f0  keep question stems with option panels
93391eb  lock question panels toward player
```

### 10.2 构建与设备验证

常规 PICO 行为修改完成后，项目默认流程是批处理工程导出、安装和启动测试。但用户明确说“不用导出”时，应停止 APK 构建和设备安装，只完成编辑器、Prefab、静态和运行检查，并在交付说明中写明未导出。

纯文档任务也不需要导出 APK。

## 11. 快速排错表

| 现象 | 优先检查 | 常见根因 |
| --- | --- | --- |
| 点击单人入口没反应 | 完整 HeroButton 路径、`MainPage.btnSolo`、加载器存活位置 | 绑定了同名错误按钮或加载器随页面失活 |
| 点击 L1/L2 意外换场景 | L1 `Scene Name` | 同场景节点误填了场景名 |
| L1/L2 没生成 | BusinessPrefabs、名称、`childData`、Single Menu Canvas 模板 | 名称不一致或配置源修改错误 |
| 当前步骤物体不能点 | `StepObjs` 索引、Collider、交互脚本 | Topic/Step 顺序错或缺少交互组件 |
| 题干和选项散开 | 两者的根节点和 Follow 数量 | 公共 Tips Canvas 与题目 Prefab 分别跟随 |
| 拖动只移动背景 | `ObjectManipulator.hostTransform` | Host 指向 BackPlate 而不是共同题目根 |
| 面板会倾斜 | `Follow.orientationType` | 使用三轴 FaceTrackedObject |
| 面板仍能被手势旋转 | `allowedManipulations` | Rotate 位仍开启，值还是 7 |
| 背包按钮点了没业务反应 | `OnClicked`、UIManager、BagManager | 只有外观和音效，业务管理器未接通 |
| MCP 突然断开 | Unity 进程、重新探针、Editor.log | 导入/加载期间会话短断，不一定是编辑器崩溃 |

## 12. 最终原则

```text
场景切换看 TexCore_LoadScene 和 Build Settings；
L1/L2 切换看 BusinessPrefabs、childData 和 Single Menu Canvas；
步骤切换看 TexCore_StepObj；
题目内容看 Panel_Select/PanelTopicBase；
空间位置看共同根节点；
自动朝向看 Follow；
手势权限看 ObjectManipulator；
UI 是否有用看真实事件和管理器，不看名字。
```
