# BPF AI 协作开发流程

这份文档描述当前 BPF 项目在“你提出业务需求，Codex 负责改仓库，并在接入 Unity MCP 时直接读取 Unity 当前状态；改完后 Codex 默认从副工程导出 APK、安装并启动到已连接 PICO；你在真机上验证并反馈”的协作方式。

它不替代 [BPF 前端协作开发流程](./bpf-frontend-collaboration-guide.md)。  
前者偏“人与 AI 的协作节奏”，后者偏“Unity 里具体怎么配节点、题目和步骤树”。

## 1. 适用范围

这套流程适合以下任务：

- 修改业务节点流程
- 修改题目 prefab 和题目数据
- 修改场景中的 3D 交互步骤
- 修正模块脚本逻辑
- 调整 PICO 业务路径中的功能问题

不适合直接靠这套流程解决的内容：

- 完全依赖肉眼拖拽摆放的美术细调
- 只靠描述无法确定对象名或层级关系的场景搭建
- 与 BPF 无关的上游 MRTK 包维护

接入 Unity MCP 后，上面第二条会缩小一部分范围：Codex 可以直接读取当前激活场景、场景层级、控制台和部分编辑器状态。但涉及肉眼判断、真机表现、摆放精度和交互手感的问题，仍然需要你补充截图、录像或 PICO 验证结果。

## 2. 协作分工

### 你负责

- 描述这一步业务需要怎么改
- 在 Unity 中打开工程和必要场景
- 保持 PICO 设备连接、允许需要的 Unity 构建和 `adb` 安装命令
- 反馈真实运行现象、报错、截图或录像

### Codex 负责

- 每轮新对话开始后，先执行 `git fetch --all --prune`，再进入读取、修改或构建流程
- fetch 后检查 `origin/main` 是否包含本地 `main` 没有的提交
- 如果 `origin/main` 有新提交，停止仓库工作并询问是否拉取；未经同意不执行
  `pull`、`merge` 或 `rebase`
- 如果 `origin/main` 没有新提交，继续当前分支任务；不自动把功能分支同步到
  `origin/main`
- 工作区不干净时保护已有改动，只提交本轮直接修改的文件
- 如果因为网络、权限、冲突或分支状态无法同步，Codex 必须把失败命令、失败原因和你下一步该做什么直接说明白
- 每轮开始前先检测 Unity MCP 是否连通，并明确说明当前连接状态
- Unity MCP 已连通时，优先读取当前场景、层级、控制台和必要的编辑器状态，并进入本轮后续任务
- Unity MCP 未连通时，本轮仍继续；避免 Unity-MCP-only 操作，改用仓库文件、命令行 Unity、`adb` 或其他本地工具推进
- 定位要改的模块、场景、prefab、脚本
- 直接修改代码、prefab、scene 文本资源和文档
- 每次完成会影响 PICO 运行行为的修改后，默认从副工程
  `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild` 导出 APK，
  使用 `adb install -r` 安装到所有已连接 PICO，并启动应用供你测试
- AI 侧验证完成后，先本地提交，不立即推送远端
- 只有在你验收通过后，Codex 才推送当前分支并创建 PR
- 如果无法导出或安装，明确说明具体阻塞原因，例如设备未连接、缺少命令权限、编译失败、构建失败或 `adb` 安装失败
- 说明本轮改了什么、改在哪里、你该验证什么
- 每次对话给出结果后，必须明确说明你下一步该做什么，不能只停在“已完成”或“已修改”

## 3. 每轮开发的标准流程

### 步骤 1：你给出需求

每次尽量提供这 6 项信息：

1. 模块名
2. 场景名或节点名
3. 目标行为
4. 成功标准
5. 已知对象名、脚本名、prefab 名或层级线索
6. 是否只在 PICO 出现

推荐描述格式：

```text
模块：
场景/节点：
要改的行为：
成功标准：
已知对象：
补充现象：
```

### 步骤 2：Codex 先同步仓库，再检测连接、读取现场并判定任务类型

Codex 每轮开始前都要先 fetch 所有远端并检查 `origin/main`。这一步在读取
Unity MCP、看仓库文件、改代码、构建、提交之前执行，不能省略。

默认执行顺序：

1. `git fetch --all --prune`
2. 检查本地 `main`、`origin/main`、当前分支和工作区状态
3. 如果 `origin/main` 有本地 `main` 尚未包含的提交，停止并询问是否拉取
4. 如果没有远端新提交，继续当前分支；不自动合并或变基
5. 保留工作区现有改动，只对本轮目标文件进行修改和提交

如果同步失败，例如网络不通、没有权限、快进失败或分支不对，Codex 必须先把失败命令和下一步动作告诉你，再继续决定本轮怎么做。

Codex 每轮开始前都要先检测一次 Unity MCP 连接状态。这是本仓库的门禁规则，必须先检测、先说明，再决定是否继续。本轮回复开头应明确说明当前属于下面哪一种：

- 已连通：`Unity MCP：已连接，本轮继续。`
- 未连通：`Unity MCP：未连接，本轮继续。`

如果 Unity MCP 已连通，Codex 会先做这些读取动作：

1. 读取当前激活场景
2. 读取相关根对象或层级
3. 读取 Unity Console 中的报错或警告
4. 确认当前问题更像配置问题、脚本问题，还是运行链路问题

如果 Unity MCP 未连通，Codex 仍继续本轮任务；但不得执行只能依赖 Unity MCP 的操作，应改用仓库文件、命令行 Unity、`adb` 或其他本地工具推进，并说明哪些现场信息暂时无法通过 MCP 读取。

### 步骤 3：Codex 判定任务类型

收到需求后，先判断任务属于哪一类：

1. 流程节点配置
2. 题目 UI 配置
3. 场景步骤树配置
4. 模块脚本逻辑修改
5. PICO 发布链路问题

这个判断会决定本轮主要改的是：

- `Assets/Resources/BusinessPrefabs`
- `Assets/Resources/TopicPrefabs`
- `Assets/A_MRNet`
- `Assets/APP/BPF/Compatibility/Fonts`
- `Assets/APP/MRTKSamples`（仅作为上游 MRTK 示例与兼容依赖）
- `Assets/` 下当前真实存在的业务或模块目录

### 步骤 4：Codex 修改仓库

Codex 会优先做这些动作：

1. 先找模块归属和真实入口场景
2. 找现有同类实现，优先复制或沿用
3. 只改和这次需求直接相关的文件
4. 改完后说明你应该重点验证哪些现象
5. 结果发出前明确写出你下一步该执行的动作，例如进哪个场景、点哪个对象、看哪个日志，或直接进入下一条需求

### 步骤 5：Codex 导出安装，你在 Unity 或 PICO 验证

验证顺序建议固定：

1. Unity 里确认能正常进入目标业务路径
2. 本地 Editor 先看基本交互是否符合预期
3. Codex 按默认流程从副工程
   `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild` 导出 APK
4. Codex 使用 `adb install -r` 安装到所有已连接 PICO，并启动应用
5. Codex 在 AI 侧验证通过后先做本地提交，等待你的验收结论
6. 你在真机上测试真实设备表现并记录实际现象
7. 你明确回复“验收通过”后，Codex 再推送远端

即使 Unity MCP 已连通，PICO 真机验证仍然不能省略。Unity MCP 可以帮助 Codex缩小场景、对象和控制台范围，但不能替代设备侧交互手感、显示结果和平台差异验证。

### 步骤 6：你反馈结果

反馈时尽量带这些信息：

- 是否复现
- 期望行为和实际行为
- 报错文本
- 截图或录像
- 第几步触发异常
- 只在 PICO 出现，还是 Editor 里也出现

推荐反馈格式：

```text
结果：
实际现象：
报错：
发生步骤：
PICO / Editor：
附件：
```

### 步骤 7：Codex 继续迭代

如果验证失败，Codex 根据你的反馈继续收敛问题；  
如果验证通过，再进入下一步需求。

## 4. 当前业务开发入口

如果没有额外说明，日常业务配置通常从这张场景进入：

- `Assets/A_MRNet/Sence/03SingleplayerScene.unity`

当前 Build Settings 启用三张场景：首页默认从 01 进入 03，04 作为额外启用
场景保留：

- `Assets/A_MRNet/Sence/01StartUp.unity`
- `Assets/A_MRNet/Sence/03SingleplayerScene.unity`
- `Assets/A_MRNet/Sence/04SingleplayerScene.unity`

开始具体任务前，还要先确认当前 Unity 现场是不是已经带着旧改动：

- 当前激活场景是否已经是 dirty 状态
- 当前要改的 prefab 是否已经有未保存修改
- 这些修改是不是本轮任务的一部分

如果现场本来就有未保存改动，先区分“你的存量改动”和“本轮要新增的改动”，避免把两轮内容混在一起。

## 5. 各模块常用场景

### 当前业务入口

当前仓库默认从以下入口判断业务主线：

- `Assets/A_MRNet/Sence/01StartUp.unity`
- `Assets/A_MRNet/Sence/03SingleplayerScene.unity`
- `Assets/A_MRNet/Sence/04SingleplayerScene.unity`

如果任务要求定位某个历史模块或旧场景名，先在 `Assets/` 根层真实目录里核对，再决定是否需要追到遗留目录：

- `Assets/APP/OldMRTK/*`：历史模块快照，只作参考
- `Assets/APP/ScenesNGOsync/*`
  和 `Assets/APP/Modules/LegacyMrtkSync/*`：旧联机链路

## 6. Codex 侧的改动落点规则

### 流程节点和题目配置

优先改这里：

- `Assets/Resources/BusinessPrefabs`
- `Assets/Resources/TopicPrefabs`

不要手改：

- `Assets/StreamingAssets/Config.xml`

### 模块业务脚本和资源

优先改这里：

- `Assets/A_MRNet`
- `Assets/APP/BPF/Compatibility/Fonts`
- `Assets/Resources/BusinessPrefabs`
- `Assets/Resources/TopicPrefabs`
- `Assets/` 下当前真实存在的目录

`Assets/APP/MRTKSamples` 是共享只读的上游示例与当前兼容依赖，不是 BPF
业务脚本或资源的默认落点。

如果任务明确涉及历史模块名、旧 prefab 快照或旧联机场景，再额外核对：

- `Assets/APP/OldMRTK/*`
- `Assets/APP/ScenesNGOsync/*`
- `Assets/APP/Modules/LegacyMrtkSync/*`

不要仅凭历史迁移方案，就断言某个模块已经稳定落在 `Assets/App/Modules`
或 `Assets/App/Shared`。

### 运行框架入口

只有确实涉及框架流程时才看这里：

- `Assets/A_MRNet`

### 默认不要碰的遗留目录

如果任务没有明确提到遗留联机链路、迁移工作或历史资源整理，默认不要把改动落到这些位置：

- `Assets/APP/OldMRTK/*`
- `Assets/APP/ScenesNGOsync/*`
- `Assets/APP/Modules/LegacyMrtkSync/*`
- 历史联机场景链路，如 `Init`、`Lobby`、`UDPLobby`、`MRTKsync`

这几处可以作为线索来源或历史参考，但不应默认当作当前业务主线的开发落点。

## 7. 你给需求时，怎样描述最省时间

最省时间的方式不是“这里有个 bug”，而是直接给业务语义和对象线索。

例如：

### 好的描述

```text
模块：ServerAssembly
场景：WHY_FuWuQi.unity
行为：第二步点击 U 盘后，显示系统面板，并自动进入下一步
成功标准：PICO 上点击一次即可进入下一步，不能重复触发
已知对象：UPan、UI_XiTong、TaskUPan
```

### 不够好的描述

```text
服务器模块不对，帮我看看
```

## 8. 每轮结束前的固定检查

### Codex 检查

- 本轮开始前是否已经检测并说明 Unity MCP 连接状态
- 如果 Unity MCP 未连通，是否已避免 Unity-MCP-only 操作，并改用可用的本地工具推进
- 改动是否只覆盖本轮需求
- prefab、scene、script 是否落在正确位置
- 是否留下明显无关改动
- 是否说明了验证重点
- 是否明确说明了使用者下一步该做什么
- AI 侧验证通过后是否已先本地提交、但尚未在验收前提前推送
- 如果本轮修改影响 PICO 运行，是否已从副工程导出、安装并启动到已连接 PICO
- 如果没有完成导出安装，是否已说明具体阻塞原因和下一步需要的动作

### 你检查

- Unity 或 PICO 是否能正常进入目标业务路径
- Codex 安装启动后的 PICO 上是否复现或修复
- 是否有新报错
- 反馈是否包含具体步骤和现象

### 协作过程里的额外注意项

- 如果 Codex 正通过 Unity MCP 修改某个 scene 或 prefab，你先不要同时手动改同一个对象
- 更稳的节奏是：Codex 改一轮，你验证一轮，再根据结果进入下一轮
- 如果你已经手动改过当前对象，最好在发需求时直接说明，避免后续排查时把手工改动误判为本轮结果

## 9. 什么时候需要你补充更多信息

如果出现下面情况，通常需要你补充对象线索或现场结果：

- 同一个模块里有多个名字相近的对象
- 层级里存在大量历史残留对象
- 需求依赖物体摆放位置、朝向、尺寸
- 只在 PICO 真机出现问题
- 场景里需要肉眼判断哪个物体才是目标对象

如果 Unity MCP 已连通，第一类和第二类问题有时可以先由 Codex 直接读取场景层级来缩小范围；但只要涉及视觉判断、设备表现或真实交互体感，仍然需要你补充现场结果。

另外，下面这些问题即使接入了 Unity MCP，也通常不能指望一次自动化完成，仍然需要你重点回测：

- 复杂美术摆位、朝向、尺寸微调
- 需要肉眼比较的 UI 或模型显示效果
- 精细动画状态机、Timeline 或首次激活时序
- PICO 真机上的交互手感、输入差异和设备表现
- 多人同步、联机时序或跨端状态一致性

## 10. 一句话原则

**你负责给出业务目标和真机结果，Codex 负责把仓库改到最接近目标的状态，然后通过一轮轮 PICO 验证把问题收敛掉。**
