# BPF 场景名配置指南

本文档说明当前 BPF 业务框架中场景名的配置位置、填写规则和验证方法，适用于：

- 配置启动界面进入哪个单人业务场景
- 配置 L1 业务节点是否切换 Unity 场景
- 识别 L2 题目中保留的场景数据字段
- 配置场景到 Unity Build Settings
- 排查场景名错误导致的加载失败

本文档以当前业务流程为例：

```text
01StartUp
  -> 03SingleplayerScene
     -> 01服务器网络搭建（L1）
        -> 各业务题目（L2）
```

当前目标是在 `03SingleplayerScene` 内切换 L1、L2 和题目步骤，不在 L1/L2 点击时切换 Unity 场景。

## 1. 场景名填写规则

场景名必须与 `.unity` 文件名完全一致，但不要填写扩展名或资源路径。

假设场景文件为：

```text
Assets/A_MRNet/Sence/03SingleplayerScene.unity
```

正确填写：

```text
03SingleplayerScene
```

错误示例：

```text
03SingleplayerScene.unity
Assets/A_MRNet/Sence/03SingleplayerScene.unity
03singleplayerscenesyc
```

场景名区分大小写，并且不能多出空格。

## 2. 配置单人业务入口场景

启动场景位置：

```text
Assets/A_MRNet/Sence/01StartUp.unity
```

在 Unity 中执行：

1. 打开 `01StartUp.unity`。
2. 在 Hierarchy 中选择 `MainPage`。
3. 在 Inspector 的 `MainPage` 组件中找到 `Solo Scene`。
4. 填写单人模式需要进入的场景名。
5. 保存场景。

当前项目应配置为：

```text
MainPage
└─ MainPage
   └─ Solo Scene: 03SingleplayerScene
```

对应运行流程：

```text
运行 01StartUp
  -> 点击单人模式
  -> 加载 03SingleplayerScene
```

框架默认业务场景常量位于：

```text
Assets/A_MRNet/ProgramResources/ProgramResources/
CommonScripts/Managers/TexCore_LoadScene.cs
```

当前值为：

```csharp
public const string BusinessSceneName = "03SingleplayerScene";
```

日常业务配置优先在 `MainPage -> Solo Scene` 中完成。只有确实需要更换整个业务主线场景时，才同步评估是否需要修改该常量及其别名加载逻辑。

## 3. 把场景加入 Build Settings

所有通过 `SceneManager` 加载的场景都必须加入 Build Settings 并处于启用状态。

在 Unity 中执行：

1. 打开 `File -> Build Settings`。
2. 在 `Scenes In Build` 中查找目标场景。
3. 如果目标场景不存在，先打开该场景，再点击 `Add Open Scenes`。
4. 确认目标场景左侧的复选框已勾选。
5. 保持 `01StartUp` 为启动入口场景。

当前项目已启用：

```text
Assets/A_MRNet/Sence/01StartUp.unity
Assets/A_MRNet/Sence/03SingleplayerScene.unity
Assets/A_MRNet/Sence/04SingleplayerScene.unity
```

如果要加载 `02SingleplayerScene`，还需要将下面的场景加入 Build Settings：

```text
Assets/A_MRNet/Sence/02SingleplayerScene.unity
```

仅修改 `Solo Scene` 而不加入 Build Settings，会导致运行时无法加载目标场景。

## 4. 配置 L1 业务节点的场景名

业务节点预制体位于：

```text
Assets/Resources/BusinessPrefabs
```

以服务器网络搭建节点为例：

```text
Assets/Resources/BusinessPrefabs/
MR混合现实多人协作平台/
01货到人拣选电商仓储实施安装与调试实训系统/
01服务器网络搭建.prefab
```

在 Unity 中执行：

1. 双击打开 `01服务器网络搭建.prefab`，进入 Prefab Mode。
2. 选择挂载 `Aa_Node` 的根物体。
3. 在 Inspector 中找到以下字段：

```text
Aa_Node
└─ Data Class
   └─ Scene Data
      └─ Scene Name
```

4. 根据业务需求填写或留空。
5. 保存 Prefab。

### 同一场景内切换

如果点击 L1 只切换业务面板、L2 题目和三维步骤，不切换 Unity 场景：

```text
Scene Name: 留空
```

当前 `01服务器网络搭建` 就属于这种情况，应保持为空。

### 进入独立场景

如果点击 L1 后需要进入一份独立场景，例如：

```text
Assets/A_MRNet/Sence/05NetworkCabinet.unity
```

则填写：

```text
Scene Name: 05NetworkCabinet
```

同时必须将 `05NetworkCabinet.unity` 加入 Build Settings。

`Aa_Node` 只有在 `Scene Name` 非空时才会调用场景加载器，并等待目标场景成为当前活动场景。因此，不需要切场景的节点不要填写当前场景名，也不要填写 L1 名称。

## 5. L2 题目的 Scene Data 字段

题目预制体位于对应 L1 节点的子目录中，例如：

```text
Assets/Resources/BusinessPrefabs/
MR混合现实多人协作平台/
01货到人拣选电商仓储实施安装与调试实训系统/
01服务器网络搭建/
02硬件连接操作.prefab
```

在 Unity 中执行：

1. 双击打开题目 Prefab。
2. 选择挂载题目脚本的根物体。
3. 在 Inspector 中找到：

```text
PanelTopicBase 或其子类
└─ Topic Data
   └─ Scene Data
      └─ Scene Name
```

4. 当前业务主线保持 `Scene Name` 为空。
5. 保存 Prefab。

需要注意：题目 Prefab 虽然保留了 `Topic Data -> Scene Data` 数据结构，但当前 `PanelTopicBase.PanelInit()` 不会读取 `sceneName` 并加载场景。现阶段不能依靠这个字段实现 L2 题目切换 Unity 场景。

当前 03 场景内的以下 L2 题目都应留空：

```text
01服务器网络搭建练习题
02服务器硬件组件选择
03Web服务器类型选择
02硬件连接操作
```

这些题目通过 `TexCore_HeadMenu` 和 `NodePanelBase` 在同一场景中切换面板，不应把题目名称写进 `Scene Name`。

如果将来确实要求某个 L2 题目加载另一份 `.unity` 场景，不能只填写该字段。应先明确业务设计，再选择以下实现之一：

- 将场景切换放到对应 L1 节点的 `Aa_Node -> Data Class -> Scene Data -> Scene Name`。
- 对题目加载流程做明确的框架开发，使 `PanelTopicBase.PanelInit()` 读取并处理题目 `sceneName`。

第二种属于代码功能扩展，不是现有配置功能。

## 6. 当前 03 场景推荐配置

当前“服务器网络搭建”业务的正确配置如下：

| 配置位置 | 字段 | 当前值 |
| --- | --- | --- |
| `01StartUp/MainPage` | `Solo Scene` | `03SingleplayerScene` |
| `TexCore_LoadScene` | `BusinessSceneName` | `03SingleplayerScene` |
| Build Settings | `03SingleplayerScene` | 已加入并启用 |
| `01服务器网络搭建.prefab` | `Data Class -> Scene Data -> Scene Name` | 留空 |
| 四个 L2 题目 Prefab | `Topic Data -> Scene Data -> Scene Name` | 留空，当前题目流程不读取该字段进行切场景 |

运行时预期行为：

```text
01StartUp
  -> 单人模式
  -> 进入 03SingleplayerScene
  -> Single Menu Canvas 根据业务配置自动生成 L1/L2 按钮
  -> 点击 L1/L2 只切换业务面板和步骤
  -> 活动 Unity 场景始终为 03SingleplayerScene
```

## 7. 与 L1/L2 菜单的关系

`03SingleplayerScene` 中的 `Single Menu Canvas` 保存的是禁用的 L1/L2 按钮模板。

运行时流程：

```text
Assets/Resources/BusinessPrefabs
  -> TexCore_ConfigManager 读取业务数据
  -> TexCore_HeadMenu.InitBtns()
  -> 自动生成 L1/L2 按钮
```

因此：

- 不需要在 Hierarchy 中手工复制 L1/L2 按钮。
- L1/L2 名称来自业务预制体及其 `childData`。
- L1 节点的 `Aa_Node.Scene Name` 负责决定是否加载另一份 Unity 场景。
- L2 题目虽然保留 `Scene Data`，但当前主线不读取它进行切场景。
- L1/L2 菜单名称不是场景名。

## 8. 不要手动修改 Config.xml

框架根据 `Assets/Resources/BusinessPrefabs` 下的预制体数据生成或读取业务配置。

正确做法：

```text
节点配置 -> 修改节点 Prefab
题目配置 -> 修改题目 Prefab
三维步骤 -> 修改业务 Scene 中的 TexCore_StepObj
场景入口 -> 修改 MainPage 的 Solo Scene
```

不要手动修改：

```text
Assets/StreamingAssets/Config.xml
```

手动修改生成配置可能会被重新生成的数据覆盖，也容易造成 Prefab 数据与运行时配置不一致。

## 9. 验证步骤

配置完成后按下面顺序验证。

### 编辑器静态检查

1. `MainPage -> Solo Scene` 与目标 `.unity` 文件名一致。
2. 目标场景已加入 Build Settings 并勾选。
3. 不需要切换场景的 L1，其 `Scene Name` 为空。
4. 当前 L2 题目的 `Scene Name` 保持为空。
5. 需要切换场景的 L1 节点，其 `Scene Name` 与目标文件名完全一致。
6. 所有修改过的 Prefab 和 Scene 已保存。

### 运行检查

1. 从 `01StartUp.unity` 进入 Play Mode。
2. 点击单人模式。
3. 确认进入 `03SingleplayerScene`。
4. 点击 `01服务器网络搭建` L1。
5. 依次点击四个 L2 题目。
6. 观察 Unity 当前活动场景始终为 `03SingleplayerScene`。
7. 确认题目面板、提示和三维步骤正常切换。
8. 检查 Console 中没有“场景无法加载”或场景名相关错误。

## 10. 常见错误

### 点击节点后没有反应

检查：

- 业务节点名称与 Prefab 文件名是否一致。
- 父节点 `childData` 中的名称是否与子 Prefab 名称一致。
- L1/L2 按钮是否在运行时成功生成。

### 出现场景无法加载错误

检查：

- 是否错误填写了 `.unity` 扩展名。
- 是否填写了完整的 `Assets/...` 路径。
- 场景是否已加入 Build Settings。
- `Scene Name` 是否存在大小写或空格差异。

### 点击 L1/L2 后意外切换场景

优先检查对应 L1 节点 Prefab：

```text
Scene Data -> Scene Name
```

如果业务应在同一个场景内切换，将 L1 节点中的该字段清空并保存 Prefab。当前 L2 题目字段不会主动触发场景加载，但仍建议保持为空。

### 修改后运行结果没有变化

检查：

- 是否保存了 Prefab。
- 是否保存了 Scene。
- 是否修改了真正被业务路径引用的 Prefab。
- 是否误改了 `Config.xml`，而没有修改源 Prefab。

## 11. 配置决策速查

```text
点击这个 L1 节点后，需要进入另一份 .unity 场景吗？

├─ 否
│  └─ Scene Name 留空
│     使用 L1/L2 和 TexCore_StepObj 在当前场景内切换
│
└─ 是
   ├─ Scene Name 填目标场景文件名，不含 .unity
   ├─ 将目标场景加入 Build Settings
   └─ 从 01StartUp 完整运行验证
```

当前 `03SingleplayerScene` 的服务器网络搭建流程选择“否”，所以 L1 的 `Scene Name` 应保持为空；四个 L2 的保留字段也保持为空。
