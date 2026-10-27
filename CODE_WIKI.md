# BPF 项目 Code Wiki

> **项目名称**: BPF（Business Process Framework）— MR 混合现实多人协作实训平台
> **仓库**: gitea_BPF
> **引擎**: Unity 2021.3.29f1c1
> **目标平台**: PICO 4 (Android / PXR)
> **XR 框架**: MRTK3 + Unity XR Interaction Toolkit + PXR（Android）/ OpenXR（Standalone、WSA）
> **核对日期**: 2026-07-21

> 当前场景、XR、构建和目录事实以
> [docs/current-project-baseline.md](docs/current-project-baseline.md) 及工程文件为准。

---

## 目录

1. [项目概述](#1-项目概述)
2. [整体架构](#2-整体架构)
3. [主要模块职责](#3-主要模块职责)
   - [3.1 GameFramework 框架层](#31-gameframework-框架层)
   - [3.2 A_MRNet 业务模块](#32-a_mrnet-业务模块)
   - [3.3 BPF 业务资源层](#33-bpf-业务资源层)
   - [3.4 MRTK3 集成与 XR 配置](#34-mrtk3-集成与-xr-配置)
   - [3.5 PICO SDK 适配](#35-pico-sdk-适配)
   - [3.6 遗留 MRTK 同步模块](#36-遗留-mrtk-同步模块)
   - [3.7 构建与发布](#37-构建与发布)
4. [关键类与函数说明](#4-关键类与函数说明)
5. [依赖关系](#5-依赖关系)
6. [项目运行方式](#6-项目运行方式)
7. [目录结构速查](#7-目录结构速查)
8. [常见问题与约束](#8-常见问题与约束)

---

## 1. 项目概述

### 1.1 项目定位

BPF 是一个基于 Unity 的 **MR（混合现实）多人协作实训平台**，面向职业教育和企业培训场景。当前核心业务为"货到人拣选电商仓储实施安装与调试实训系统"，包含服务器网络搭建和软件系统安装两大实训章节。

### 1.2 技术栈

| 层级 | 技术选型 |
|------|---------|
| 游戏引擎 | Unity 2021.3.29f1c1 |
| 脚本语言 | C# (.NET Standard 2.1) |
| XR 框架 | Android：ARCore + PXR；Standalone / WSA：OpenXR；Unity XR Interaction Toolkit 2.4.0 |
| MR 框架 | MRTK3 (Mixed Reality Toolkit v3) |
| 网络框架 | Unity Netcode for GameObjects (NGO) 1.5.2 |
| 基础框架 | GameFramework (自研/开源模块化框架) |
| 动画引擎 | DOTween (Demigiant) |
| 空间音频 | PICO Spatial Audio SDK 3.4.0 |
| 目标平台 | Android (PICO 4) / Windows Standalone |
| 渲染管线 | URP (Universal Render Pipeline) |
| 构建后端 | IL2CPP + ARM64 |

### 1.3 核心业务场景

| 场景文件 | 用途 |
|---------|------|
| `Assets/A_MRNet/Sence/01StartUp.unity` | 启动场景，XR 手部校准入口 |
| `Assets/A_MRNet/Sence/02SingleplayerScene.unity` | 保留参考场景，当前未加入 Build Settings |
| `Assets/A_MRNet/Sence/03SingleplayerScene.unity` | 默认业务场景（含完整交互） |
| `Assets/A_MRNet/Sence/04SingleplayerScene.unity` | 额外启用的业务场景，非首页默认目标 |

### 1.4 业务课程结构

```
MR 混合现实多人协作平台
└── 01 货到人拣选电商仓储实施安装与调试实训系统
    ├── 01 服务器网络搭建
    │   ├── 01 服务器网络搭建练习题（选择题）
    │   ├── 02 硬件连接操作（模型操作题）
    │   ├── 02 服务器硬件组件选择（选择题）
    │   └── 03 Web 服务器类型选择（选择题）
    └── 02 软件系统安装
        ├── 01 软件系统练习题（选择题）
        ├── 02 操作测试（操作题）
        └── 03 操作测试（操作题）
```

---

## 2. 整体架构

### 2.1 分层架构图

```
┌─────────────────────────────────────────────────────────┐
│                    应用层 (Application)                   │
│  MainPage / StudyPage / RankPage / UIManager             │
│  ProcessDrag / ObjectPlacer / KnowledgeManager            │
│  Panel_ShowModel / Panel_Select / Panel_Operate           │
├─────────────────────────────────────────────────────────┤
│                  业务框架层 (Business Framework)           │
│  TexCore 系列 (RuntimeAssetService / EventCenter /        │
│  BusinessManager / ConfigManager / LoadScene / etc.)      │
│  NodePanelBase / MutilNodePanelBase / ToolsManager        │
│  Topic 面板系统 (PanelTopicBase / Panel_Input / etc.)     │
├─────────────────────────────────────────────────────────┤
│                  基础框架层 (Foundation)                   │
│  GameFramework (18 个核心管理器)                          │
│  - Resource / UI / Event / Fsm / Procedure / Entity       │
│  - ObjectPool / DataTable / Config / Scene / Sound        │
│  - Network / Download / WebRequest / Localization         │
├─────────────────────────────────────────────────────────┤
│                  XR / MR 平台层 (Platform)                 │
│  MRTK3 (Input / UX / SpatialManipulation / Accessibility) │
│  Unity XR Interaction Toolkit 2.4.0                       │
│  Android: ARCore + PXR / Standalone、WSA: OpenXR         │
│  Unity XR Hands + PICO Spatial Audio                      │
├─────────────────────────────────────────────────────────┤
│                  Unity 引擎层 (Engine)                     │
│  Unity 2021.3.29f1c1 / URP / IL2CPP / Android ARM64       │
└─────────────────────────────────────────────────────────┘
```

### 2.2 核心设计模式

| 模式 | 应用场景 |
|------|---------|
| **管理器模式** | GameFramework 的 18 个核心管理器 + TexCore 全局管理器 |
| **组件模式** | 所有 GameObject 上挂载的 MonoBehaviour 脚本 |
| **接口-实现分离** | GameFramework 模块定义接口，由 Runtime 层提供 Unity 实现 |
| **事件驱动** | TexCore_EventCenter 事件广播 + GameFramework EventPool |
| **状态机** | ProcedureComponent 流程管理 + FsmComponent 通用 FSM |
| **单例模式** | GameEntry / ObjectPlacer / ObjectUIManager / DataManager 等 |
| **对象池** | GameFramework ObjectPool + ReferencePool |
| **Helper 模式** | GameFramework 各子系统的可替换 Helper（如 JsonHelper / ZipHelper） |
| **预制体 + 配置驱动** | BusinessPrefabs + Config.xml 驱动题目 UI |

### 2.3 数据流

```
Config.xml ──加载──> TexCore_ConfigManager ──解析──> 题目数据
                                                      │
BusinessPrefabs (预制体) ──实例化──> Topic 面板 ──────┤
                                                      │
用户交互 (点击/拖拽/放置) ──事件──> TexCore_EventCenter │
                                                      │
考核判定 ──数据──> TexCore_ExamData ──提交──> 后端 API  │
```

---

## 3. 主要模块职责

### 3.1 GameFramework 框架层

**路径**: `Assets/GameFramework/`

GameFramework 是整个项目的基础设施，基于 [GameFramework](https://gameframework.cn/) 开源项目。采用三层架构：纯 C# 核心库 → Unity 运行时适配层 → 编辑器工具层。

#### 3.1.1 程序集定义（3 个 .asmdef）

| 程序集 | 路径 | 引用 |
|--------|------|------|
| `GameFramework` | `Libraries/GameFramework/` | 无外部依赖，`allowUnsafeCode: true` |
| `UnityGameFramework.Runtime` | `Scripts/Runtime/` | 引用 `GameFramework` |
| `UnityGameFramework.Editor` | `Scripts/Editor/` | 引用 `Runtime` + `GameFramework` |

#### 3.1.2 18 个核心管理器

通过 `GameFrameworkEntry.GetModule<T>()` 按需获取，所有实现均为 `internal`，对外暴露接口。

| 管理器 | 接口 | 核心职责 |
|--------|------|---------|
| **Base** | — | `GameFrameworkEntry` 全局入口，模块注册/轮询/关闭生命周期 |
| **EventPool** | — | 泛型事件池，高效事件分发 |
| **ReferencePool** | — | 引用池，通过 `IReference` 接口复用对象 |
| **ResourceManager** | `IResourceManager` | 资源加载/更新/释放，Editor 和 Runtime 双模式 |
| **UIManager** | `IUIManager` | UI 界面打开/关闭/分组管理 |
| **EntityManager** | `IEntityManager` | 游戏实体（GameObject）生命周期管理 |
| **SceneManager** | `ISceneManager` | 场景加载/卸载 |
| **EventManager** | `IEventManager` | 事件订阅与广播 |
| **FsmManager** | `IFsmManager` | 有限状态机创建与销毁 |
| **ProcedureManager** | `IProcedureManager` | 流程管理器（状态机风格的游戏流程） |
| **DataTableManager** | `IDataTableManager` | 数据表加载与查询（CSV/TXT） |
| **ObjectPoolManager** | `IObjectPoolManager` | GameObject 对象池 |
| **ConfigManager** | `IConfigManager` | 配置加载/解析 |
| **DownloadManager** | `IDownloadManager` | HTTP 下载管理（限速、断点续传） |
| **NetworkManager** | `INetworkManager` | TCP 网络通信（心跳、同步/异步） |
| **SoundManager** | `ISoundManager` | 声音播放管理 |
| **LocalizationManager** | `ILocalizationManager` | 多语言字典管理 |
| **SettingManager** | `ISettingManager` | PlayerPrefs 游戏设置持久化 |
| **WebRequestManager** | `IWebRequestManager` | HTTP Web 请求管理 |

#### 3.1.3 Unity 运行时适配

- **`GameEntry`**（静态类）：Unity 侧全局入口，管理所有 `GameFrameworkComponent` 的注册和检索
- **`GameFrameworkComponent`**（抽象类）：所有 Unity 组件的基类，在 `Awake()` 中自动注册
- **`BaseComponent`**：框架核心初始化，连接 Unity 主循环与核心模块
- **每个管理器对应一个 Unity Component**（如 `UIComponent`、`ResourceComponent` 等），业务代码通过 `GameEntry.GetComponent<T>()` 获取

#### 3.1.4 预制体

- `Prefabs/GameFramework.prefab`：框架载体预制体，挂载所有核心 Component

---

### 3.2 A_MRNet 业务模块

**路径**: `Assets/A_MRNet/`

A_MRNet 是项目的核心业务模块，包含场景、UI、3D 交互、绳索模拟、知识库等。

#### 3.2.1 场景

| 场景 | 推断用途 |
|------|---------|
| `01StartUp.unity` | 启动场景，XR 手部校准 |
| `02SingleplayerScene.unity` | 保留参考场景，当前未加入 Build Settings |
| `03SingleplayerScene.unity` | 默认业务场景（`TexCore_LoadScene.BusinessSceneName`） |
| `04SingleplayerScene.unity` | 额外启用场景，非首页默认目标 |

#### 3.2.2 UI 层 (`Scripts/UI/`)

| 类 | 基类 | 职责 |
|----|------|------|
| `UIPanel` | MonoBehaviour（抽象） | 所有 UI 面板基类，定义 `OnShow(args)` / `OnHide()` |
| `UIManager` | MonoBehaviour | UI 页面管理器，基于 Stack 的导航栈，管理 6 种页面类型 |
| `MainPage` | UIPanel | 主页（Solo/Coop 模式切换，场景入口） |
| `RankPage` | UIPanel | 排行榜页面（空实现） |
| `StudyPage` | UIPanel | 学习页面（空实现） |
| `DashHolsterRecenter` | MonoBehaviour | XR 仪表板重新居中定位 |

**UIManager 页面类型**:
```csharp
enum UIPage { Home, Rank, Study, UserCenter, Bag, Setting }
```

**MainPage 子页面**:
```csharp
enum GameSubPageId { SoloPractice, Coop }
```

#### 3.2.3 XR 校准系统 (`Scripts/XR/`)

| 类 | 职责 |
|----|------|
| `HandAnchorCalibrationController` | 手部锚点校准主控制器，场景绑定+锁定+可见性 |
| `HandAnchorCalibrationSession`（静态） | 校准会话状态存储（`IsCalibrated` / `Store()` / `Clear()`） |
| `HandAnchorCalibrationApplier` | 协程式校准应用器（5 秒超时） |
| `EditorMouseCoordinateAdapter` | 编辑器下 MRTK 鼠标导航适配（仅 `UNITY_EDITOR`） |

#### 3.2.4 核心交互 (`Scripts/`)

| 类 | 职责 |
|----|------|
| `StepTrigger` | 碰撞触发步进（进入时设置 Camera 目标并执行射线检测） |
| `DistriStepTrigger` | 分布式步进触发器（额外设置 Interacter 目标） |
| `ObjectClickDetector` | 鼠标点击 3D 物体检测，桥接 ToolsManager 和 CameraTarget |

#### 3.2.5 绳索系统 (`Third-Party/Rope/`)

**命名空间**: `GogoGaga.OptimizedRopesAndCables`

| 类 | 职责 |
|----|------|
| `Rope` | 绳索主类：有理贝塞尔曲线模拟，LineRenderer 渲染，物理参数可调 |
| `RopeMesh` | 管状绳索 Mesh 生成器：圆形截面+两端圆盖 |
| `RopeWindEffect` | 绳索风效：Perlin 噪声模拟随机风力 |
| `RopeEditor` | 自定义 Inspector：一键添加 Mesh/WindEffect 组件 |

#### 3.2.6 Zn 模块 (`Zn/`)

| 类 | 职责 |
|----|------|
| `BackgroundMusic` | 背景音乐管理（单例，随机播放 AudioClip） |
| `CameraController` | 右键旋转 + 滚轮缩放 FOV + 射线点击交互 |
| `MultilevelMenu` | 多级菜单递归生成（支持文本/图片/视频节点） |
| `ObjectRotator` | 鼠标右键拖拽旋转 + 滚轮缩放 |
| `ProcessDrag` | 流程图拖拽匹配游戏（名称匹配验证步骤，事件广播提示） |
| `UIDrawLine` | Canvas 自由线条绘制（LineRenderer，支持撤销） |

#### 3.2.7 控制物体子系统 (`Zn/控制物体/`)

| 类 | 职责 |
|----|------|
| `CameraMove` | WASD + Q/E + 右键旋转自由摄像机 |
| `ToolObjectDragger` | 拖拽 Tool Layer 物体（Lerp 平滑跟随 + 滚轮远近） |
| `DataManager` | 场景键值对数据存储（单例） |
| `FormPrompt` | 练习/考核模式判断 + 提示文本广播 |
| `KnowledgeManager` | 知识库管理：按名称查找、内容搜索、本地绿色高亮 |
| `ObjectPlacer` | 3D 物体放置系统：Resources.Load 加载预制体 + 射线跟随 + GraphicRaycaster UI 穿透检测 |
| `ObjectUIManager` | 物体 UI 管理（单例 DontDestroyOnLoad）：≤5 个物体管理 + 悬浮提示 + 漫游按钮 |
| `ParentHandler` | 物体最终放置逻辑：MoveTo → CreateObject → ToggleState → 通知 ObjectUIManager + 业务逻辑 |
| `TriggerNotifierBase` | 触发器通知抽象基类（事件：Detected / Removed / StayDetected） |
| `TriggerTriggerPoint` | 基于 `validNames[]` 优先级碰撞检测 |
| `TriggerLineDetection` | 简化版无条件触发检测 |
| `CompleteNavigationPath` | NavMeshAgent 路径计算 + LineRenderer 绘制绿色导航线 |
| `UIControl` | UI 始终面向玩家（负 Z 轴朝向） |

#### 3.2.8 Shader (`Arrow.shader`)

- Shader 名: `Custom/Arrow`
- 属性: `_MainTex`（纹理）、`_MSpeed`（滚动速度 1-3）
- 效果: UV 横向滚动（`uv.x - _MSpeed * _Time.y`），双面透明渲染

#### 3.2.9 ProgramResources

| 类 | 职责 |
|----|------|
| `RulerAnim` | 卷尺动画：DOTween 移动卷尺 RulerBody 从起点到终点 |
| `RulerBody` | 卷尺实体：LineRenderer 连接头尾 + TMP 实时距离显示 |

#### 3.2.10 TexCore 业务框架子模块

位于 `A_MRNet/ProgramResources/ProgramResources/` 下：

| 子目录 | 关键类 | 职责 |
|--------|--------|------|
| `CommonScripts/Managers/` | `TexCore_LoadScene`, `TexCore_WindowManager`, `TexCore_ConfigManager`, `TexCore_BusinessManager`, `TexCore_EventCenter`, `TexCore_RuntimeAssetService`, `TexCore_AudioManager` | 全局管理器集合 |
| `CommonScripts/InteractionScript/` | `TexCore_Interaction`, `TexCore_Interaction_UGUI`, `TexCore_MouseCameraTarget`, `TexCore_HighlightManager` | 通用交互系统 |
| `CommonScripts/Event/` | `TexCore_EventCenter`, `EventType`, `CallBack` | 事件中心 |
| `BusinessLogic/Topic/` | `Panel_ShowModel`, `Panel_Select`, `Panel_Place`, `Panel_Operate`, `Panel_Input`, `Panel_Drawing`, `Panel_DistributeOperate`, `Panel_Dialogue`, `PanelTopicBase`, `ToolsManager` | 业务话题面板系统 |
| `BusinessLogic/NodeScripts/` | `NodePanelBase`, `MutilNodePanelBase`, `Page01Start`~`Page03Data`, `Special_Node`, `Aa_Node` | 业务节点流 |
| `BusinessLogic/Background/` | `TexCore_ExamData`, `TexCore_Background`, `Background_Common` | 考核背景系统 |
| `BusinessLogic/Panel/` | `SelectRolePanel`, `NavigationPanel`, `LoginPanel`, `FlieAssetPanel` | 业务面板 |
| `VRInteractLogic/` | `TexCore_VRTool`, `TexCore_VirtualCircular` | VR 虚拟交互设备 |
| `VRInteractLogic/SyncPlayer/` | `TexCore_RPCHand`, `RPCHandTool`, `VisualHandCollider` | VR 手部同步 |
| `ObjState/` | `TexCore_StepObj`, `InteracterBase`, `InteracterEvent`, `InteracterFunction` | 交互状态机 |
| `Unity/BaseScripts/` | `UIRoot`, `WorldRoot` | 底层根节点 |
| `Unity/Helper/` | `UIDepth`, `EventTriggerListener` | UI 辅助工具 |

---

### 3.3 BPF 业务资源层

**路径**: `Assets/Resources/`

#### 3.3.1 BusinessPrefabs（业务预制体）

定义实训系统的章节和题目面板预制体。结构如下：
```
BusinessPrefabs/
└── MR混合现实多人协作平台/
    └── 01货到人拣选电商仓储实施安装与调试实训系统/
        ├── 01服务器网络搭建.prefab          (章节总面板)
        │   └── 01服务器网络搭建/
        │       ├── 01服务器网络搭建练习题.prefab
        │       ├── 02硬件连接操作.prefab
        │       ├── 02服务器硬件组件选择.prefab
        │       └── 03Web服务器类型选择.prefab
        └── 02软件系统安装.prefab            (章节总面板)
            └── 02软件系统安装/
                ├── 01软件系统练习题.prefab
                ├── 02操作测试.prefab
                └── 03操作测试.prefab
```

#### 3.3.2 TopicPrefabs（通用题目组件）

| 预制体 | 用途 |
|--------|------|
| `选择题.prefab` | 答题 UI 组件 |
| `确定.prefab` | 确认按钮组件 |
| `下一步.prefab` | "下一步"按钮组件 |
| `错误标识.prefab` | 错误标识 UI 组件 |
| `主摄像机.prefab` | 主摄像机预制体 |

#### 3.3.3 Config.xml（题目数据定义）

`Assets/StreamingAssets/Config.xml` 定义了所有题目的数据：

- 题目类型：`文本选择`（选择题）、`模型操作`（交互操作题）
- 选择题结构：`hintTexts`（提示文本）、`tasks`（选项列表）、`rightTasks`（正确答案）、`score`（分值）
- 操作题结构：10 个交互步骤标识（如 `JiaoHuanJiGongDian`, `ACKaiJi`）
- 场景数据：镜头位置/角度

#### 3.3.4 StreamingAssets

| 文件 | 用途 |
|------|------|
| `Config.xml` | 业务题目 XML 配置 |
| `UserLogin.json` | 测试用户登录凭据（含 JWT token，勿用于生产） |
| `Scenes/Main.dat` | GameFramework 场景二进制数据 |
| `Scenes/MRTK.dat` | GameFramework MRTK 场景数据 |
| `Scenes/SycObjTest.dat` | 同步对象测试数据 |
| `GameFrameworkVersion.dat` | 框架版本信息 |

---

### 3.4 MRTK3 集成与 XR 配置

#### 3.4.1 MRTK3 本地嵌入包（14 个）

全部位于 `Packages/` 下以 `file:` 方式本地嵌入：

| 包名 | 职责 |
|------|------|
| `org.mixedrealitytoolkit.core` | MRTK 核心定义（子系统、服务、事件） |
| `org.mixedrealitytoolkit.input` | 输入系统（控制器、手部、语音、凝视） |
| `org.mixedrealitytoolkit.uxcore` | UX 核心（边界框、操作器、解算器） |
| `org.mixedrealitytoolkit.uxcomponents` | UX 组件（按钮、滑块、菜单、对话框） |
| `org.mixedrealitytoolkit.uxcomponents.noncanvas` | 非 Canvas UX 组件 |
| `org.mixedrealitytoolkit.spatialmanipulation` | 空间操作（ObjectManipulator 等） |
| `org.mixedrealitytoolkit.accessibility` | 无障碍子系统 |
| `org.mixedrealitytoolkit.audio` | 音频子系统 |
| `org.mixedrealitytoolkit.data` | 数据绑定子系统 |
| `org.mixedrealitytoolkit.diagnostics` | 诊断工具（性能统计） |
| `org.mixedrealitytoolkit.extendedassets` | 扩展资产 |
| `org.mixedrealitytoolkit.standardassets` | 标准资产（着色器、材质） |
| `org.mixedrealitytoolkit.tools` | 开发工具 |
| `org.mixedrealitytoolkit.windowsspeech` | Windows 语音（PICO 禁用） |

#### 3.4.2 XR 配置 (`Assets/XR/`)

| 平台 | Loader 配置 | InitOnStart |
|------|------------|-------------|
| **Standalone** | Open XR Loader（单一） | true |
| **Android** | AR Core Loader + PXR Loader | true |
| **WSA** | Open XR Loader（单一） | false |

Android 当前由 ARCore 与 PXR loader 组成。PICO OpenXR feature 资产虽共存，
但不能据此把 Android 写成 OpenXR loader 路线。

#### 3.4.3 MRTK Profile 配置

| Profile | 用途 | 关键特征 |
|---------|------|---------|
| `MRTKSamplesProfile` | Editor 示例（参考） | OpenXR Hands + Windows 语音 |
| `BpfOpenXRAndroidProfile` | Android/PICO 运行时 | **UnityHandsSubsystem** + 无语音 |
| `BpfOpenXRStandaloneProfile` | Standalone 运行时 | **UnityHandsSubsystem** + 无语音 |

关键决策：Android/Standalone Profile 使用 `UnityHandsSubsystem` 替代 `OpenXRHandsSubsystem`，确保在 PICO 设备上的最大兼容性。

---

### 3.5 PICO SDK 适配

**PICO SDK 版本**: 3.4.0

#### 3.5.1 PICO 包结构 (`Packages/PICO/`)

| 目录 | 说明 |
|------|------|
| `Runtime/` | Unity XR Plugin 运行时（`Unity.XR.PICO`） |
| `Runtime/Subsystem/` | AR Foundation 子系统：Camera、Session、Anchor、Raycast、Face、HumanBody |
| `Runtime/Scripts/Utils/` | 工具类：Log、Constants、Type、PermissionRequest |
| `SpatialAudio/` | PICO 空间音频包（`Pico.Spatializer`） |
| `Platform/` | PICO Platform SDK（社交、成就、房间管理） |

#### 3.5.2 PICO 项目设置

| 配置文件 | 关键设置 |
|---------|---------|
| `PICOProjectSetting.asset` | `isHandTracking: 1`, `MRSafeguard: 1`, `handTrackingSupportType: 2` |
| `PXR_ProjectSetting.asset` | `handTracking: 0`, `portalInited: 1` |
| `PXR_Settings.asset` | `stereoRenderingModeAndroid: 1` (Multiview), `optimizeBufferDiscards: 1` |

#### 3.5.3 AR Foundation 子系统

PICO 包提供了基于 AR Foundation 5/6 的完整 MR 子系统：
- `PXR_CameraSubsystem`：相机帧和透视控制
- `PXR_SessionSubsystem`：会话生命周期
- `PXR_AnchorSubsystem`：空间锚点
- `PXR_RaycastSubsystem`：空间射线检测
- `PXR_FaceSubsystem`：面部追踪
- `PXR_HumanBodySubsystem`：人体追踪

#### 3.5.4 AndroidManifest 注入

`Assets/BuildAssets/AndroidManifestPatcher.cs` 实现 `IAndroidManifestRequirementProvider`：
- `pvr.app.type = vr`（VR 应用类型）
- `enable_vst = 1`（启用视频透视 VST）
- `android.permission.CAMERA`（相机权限）

但该 provider 的 `SupportedXRLoaders` 仅包含 `OpenXRLoader`，当前 Android
active loaders 为 ARCore＋PXR，因此不能认定上述项目会通过该 provider
进入当前构建。PXR 包还会按 `PXR_ProjectSetting.videoSeeThrough` 处理 VST
与 CAMERA，而当前值为 0；最终结果以实际 Gradle 导出的 Manifest 为准。

---

### 3.6 遗留 MRTK 同步模块

**路径**: `Assets/APP/Modules/LegacyMrtkSync/`

这是一个基于 NGO + UDP 发现的遗留多人联网系统。`04SingleplayerScene`
仍引用其中的启动与自动加入组件，但它不是当前 01 -> 03 业务主线的默认
开发落点。

#### 3.6.1 核心组件

| 类 | 职责 |
|----|------|
| `LegacyMrtkSync04Bootstrap` | 网络引导：配置 UnityTransport + autoStartHost/Client |
| `NetworkDiscovery<T>` | UDP 广播发现抽象基类（广播端口 47777） |
| `ExampleNetworkDiscovery` | 发现实现：服务端广播 + 客户端扫描 |
| `GlobalSyncManager` | 全局同步管家：管理所有 SceneItemSync 的 ID 字典 |
| `SceneItemSync` | 场景物品同步：Transform + Active 状态 |
| `MRTKNetTransform` | MRTK 物体抓取同步：所有权切换 + Rigidbody 管理 |
| `ClientNetworkTransform` | 客户端权威 NetworkTransform |
| `AutoJoinManager` | 客机自动加入：UDP 回调线程安全转主线程 |
| `RoomBrowserCtrl` | 房间浏览器：建房 + 快速加入 |

---

### 3.7 构建与发布

#### 3.7.1 构建入口

`Assets/BuildAssets/BuildApp.cs` (`MixedReality.Toolkit.Examples.Build` 命名空间)

| 方法 | 用途 |
|------|------|
| `StartBpfCommandLineBuild()` | **BPF 主构建入口**：解析命令行参数 → PICO MR 准备 → 构建 |
| `StartPureProbeCommandLineBuild()` | 纯净探针构建：仅构建 `PicoMrMinimal_20260610.unity` + `TEXCORE_PURE_PROBE_BUILD` 宏 |
| `StartCommandLineBuild()` | 原始 MRTK3 示例场景构建（离线） |

**命令行参数**：
- `-buildOutput <path>`：输出目录
- `-sceneList <scenes>`：覆盖构建场景列表
- `-debug`：启用手部调试可视化
- `-androidExportProject`：导出 Gradle 项目

#### 3.7.2 副工程构建工作流

项目通过 **Junction（目录联接）** 建立副工程：
- 主工程：`d:\gitea_obj\_github_obj\github_gitea_bpf_714`
- 副工程：`D:\gitea_obj\_unity projection\gitea_BPF__batchbuild`

共享内容：
- `Assets/` — 通过 Junction 同步
- `Packages/` — 通过 Junction 同步
- `ProjectSettings/` — 通过 Junction 同步

独立内容：
- `Library/` — 副工程独立
- `Builds/` — 构建产物
- `Logs/` — 构建日志
- `UserSettings/` — 用户设置

---

## 4. 关键类与函数说明

### 4.1 框架入口

#### GameEntry (静态类)
- **文件**: `Assets/GameFramework/Scripts/Runtime/Base/GameEntry.cs`
- **职责**: Unity 侧全局入口，管理所有 `GameFrameworkComponent`
- **关键方法**:
  - `GetComponent<T>()` — 获取已注册的框架组件
  - `Shutdown(ShutdownType)` — 关闭/重启/退出游戏

#### GameFrameworkEntry (静态类)
- **文件**: `Assets/GameFramework/Libraries/GameFramework/Base/GameFrameworkEntry.cs`
- **职责**: 核心库全局入口，按需创建/获取/轮询/关闭所有管理器模块
- **关键方法**:
  - `GetModule<T>()` — 通过接口类型获取模块实例（反射创建）
  - `Update()` — 按优先级轮询所有模块
  - `Shutdown()` — 按优先级倒序关闭所有模块

#### UIManager
- **文件**: `Assets/A_MRNet/Scripts/UI/UIManager.cs`
- **职责**: BPF 业务层的 UI 页面管理器，基于 Stack 实现导航栈
- **关键方法**:
  - `Show(UIPage page, object args, bool pushHistory)` — 显示指定页面
  - `Back()` — 返回上一页面
- **页面类型**: `Home, Rank, Study, UserCenter, Bag, Setting`

#### TexCore_LoadScene
- **文件**: `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_LoadScene.cs`
- **职责**: 场景加载管理器（单例）
- **关键常量**:
  - `StartupSceneName = "01StartUp"`
  - `BusinessSceneName = "03SingleplayerScene"`
- **关键方法**:
  - `LoadScene(string sceneName)` — 加载场景
  - `ReturnHome()` — 返回启动场景
  - `PrepareForBusinessSceneLoad()` — 进入业务场景前清理非持久化对象

### 4.2 交互系统

#### ObjectPlacer
- **文件**: `Assets/A_MRNet/Zn/控制物体/脚本/ObjectPlacer.cs`
- **职责**: 3D 物体放置系统（单例）
- **关键方法**:
  - `DisplaySelectedObject(string objName)` — Resources.Load 加载并显示预制体
  - `KeyYesObject()` — 左键确认放置，调用 ParentHandler.CreateObject
  - `IsPointerOverUIElement()` — UI 射线穿透检测，防止 UI 遮挡时误放置

#### ParentHandler
- **文件**: `Assets/A_MRNet/Zn/控制物体/脚本/ParentHandler.cs`
- **职责**: 物体最终放置/拾取逻辑
- **关键方法**:
  - `MoveTo(Vector3 pos)` — 射线跟随/放置点定位
  - `CreateObject()` — 最终放置 + 通知 ObjectUIManager + 触发业务逻辑
  - `ToggleState()` — 拾起/放回切换
- **事件订阅**: `TriggerTriggerPoint.OnTriggerDetected` 等

#### TexCore_EventCenter
- **文件**: `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Event/TexCore_EventCenter.cs`
- **职责**: 全局事件广播中心
- **常用事件**: `"显示提示"` — 触发 UI 提示文本显示

#### TexCore_RuntimeAssetService
- **文件**: `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_RuntimeAssetService.cs`
- **职责**: 运行时资源服务，统一 Android/PICO 上的资源访问路径

### 4.3 题目系统

#### PanelTopicBase
- **文件**: `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/Topic/PanelTopicBase.cs`
- **职责**: 所有题目面板的基类
- **派生类**: `Panel_ShowModel`, `Panel_Select`, `Panel_Place`, `Panel_Operate`, `Panel_Input`, `Panel_Drawing`, `Panel_DistributeOperate`, `Panel_Dialogue`

#### NodePanelBase / MutilNodePanelBase
- **文件**: `BusinessLogic/NodeScripts/`
- **职责**: 业务节点流面板基类
- **派生类**: `Page01Start`, `Page02Navigation`, `Page03Data`, `Aa_Node`, `Special_Node`

#### ProcessDrag
- **文件**: `Assets/A_MRNet/Zn/ProcessDrag.cs`
- **职责**: 流程图拖拽匹配游戏
- **关键方法**:
  - `HandleButtonClick()` — 从右侧按钮创建拖拽副本
  - `ValidateProcessStep()` — 验证拖放位置是否匹配流程步骤名

### 4.4 XR 校准

#### HandAnchorCalibrationController
- **文件**: `Assets/A_MRNet/Scripts/XR/HandAnchorCalibrationController.cs`
- **职责**: 手部锚点校准主控制器
- **关键方法**:
  - `TryResolveSceneBindings()` — 解析场景绑定
  - `TryLockHand()` — 锁定手部锚点

#### HandAnchorCalibrationApplier
- **文件**: `Assets/A_MRNet/Scripts/XR/HandAnchorCalibrationApplier.cs`
- **职责**: 校准协程式应用器
- **关键方法**: `ApplyWhenReady()` — 带 5 秒超时的校准应用协程

### 4.5 知识库与导航

#### KnowledgeManager
- **文件**: `Assets/A_MRNet/Zn/控制物体/脚本/KnowledgeManager.cs`
- **职责**: 知识库管理（单例）
- **关键方法**:
  - `FindNode(string name)` — 按名称查找知识节点
  - `SearchContent(string keyword)` — 全文搜索
- **数据模型**: `Node` (name, List\<Data\>) → `Data` (type: 纯文本/图片/视频, data: 内容)

#### CompleteNavigationPath
- **文件**: `Assets/A_MRNet/Zn/控制物体/脚本/CompleteNavigationPath.cs`
- **职责**: 导航路径计算与绘制
- **实现**: NavMeshAgent 计算路径 → LineRenderer 绘制插值平滑绿色导航线（+0.3 高度偏移）

#### MultilevelMenu
- **文件**: `Assets/A_MRNet/Zn/MultilevelMenu.cs`
- **职责**: 多级菜单递归生成（支持文本/图片/视频节点）
- **关键方法**: `GenerateMenu()` — 递归生成菜单树

---

## 5. 依赖关系

### 5.1 Unity 包依赖 (Packages/manifest.json)

```
核心依赖:
├── com.unity.xr.openxr@1.8.1              OpenXR 插件
├── com.unity.xr.management@4.4.0           XR 插件管理
├── com.unity.xr.interaction.toolkit@2.4.0 XR 交互工具包
├── com.unity.xr.picoxr (file:PICO)        PICO XR Loader（本地嵌入）
├── com.unity.xr.hands@1.3.0               Unity XR Hands（手部跟踪）
├── com.unity.netcode.gameobjects@1.5.2    NGO 网络框架
├── com.unity.inputsystem@1.6.3            输入系统
├── com.unity.textmeshpro@3.0.6            TMP 文本渲染
├── com.unity.cinemachine@2.10.3           Cinemachine 摄像机
├── com.unity.visualeffectgraph@12.1.12    VFX Graph
├── com.unity.xr.arcore@5.0.5              ARCore
├── com.unity.xr.arfoundation@5.0.5        AR Foundation
├── com.coplaydev.unity-mcp (GitHub main)  Unity MCP（编辑器工具）
├── com.microsoft.mixedreality.openxr@1.11.2    MS Mixed Reality OpenXR
├── com.microsoft.mrtk.graphicstools.unity@0.8.0 MRTK Graphics Tools
└── com.microsoft.spatialaudio.spatializer.unity@2.0.55 空间音频
```

### 5.2 MRTK3 本地嵌入包（14 个）

全部为 `file:` 本地引用，不再重复列出。

### 5.3 第三方插件

| 插件 | 位置 | 用途 |
|------|------|------|
| **DOTween** | `Assets/Plugin/Demigiant/DOTween/DOTween.dll` | 动画引擎（免费版） |
| **Rope** | `Assets/A_MRNet/Third-Party/Rope/` | 绳索物理模拟（GogoGaga.OptimizedRopesAndCables） |
| **PICO SDK** | `Packages/PICO/` | PICO XR Plugin 3.4.0 |

### 5.4 程序集依赖图

```
Assembly-CSharp (默认)
├── UnityGameFramework.Runtime
│   └── GameFramework
├── MixedReality.Toolkit (MRTK3 各包)
│   ├── Unity.XR.Interaction.Toolkit
│   ├── Unity.XR.Hands
│   └── Unity.InputSystem
├── Unity.XR.PICO
│   ├── Unity.XR.OpenXR
│   ├── Unity.XR.Management
│   └── Unity.XR.ARSubsystems
├── DOTween.dll
└── Unity.Netcode.Runtime (NGO)
```

---

## 6. 项目运行方式

### 6.1 编辑器运行（Windows Standalone）

1. 使用 **Unity Hub** 打开项目，确保 Unity 版本为 **2021.3.29f1c1**
2. 打开场景 `Assets/A_MRNet/Sence/01StartUp.unity`
3. 确认 `Build Settings` 目标平台为 **PC, Mac & Linux Standalone**
4. 点击 **Play** 运行
5. 编辑器下通过鼠标右键旋转视角，WASD 移动（`CameraMove` / `CameraController` 组件）

### 6.2 PICO 4 设备部署（Android Build）

#### 6.2.1 标准构建流程

先在当前 Unity Editor 中验证改动，并询问用户是否需要 APK。只有用户明确确认
本次导出后，才执行以下脚本：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1
```

该脚本从批工程导出 Gradle、执行 release 构建，并在未使用 `-SkipInstall`
时安装到所有已连接设备并启动 `com.mrtksync`。如果用户只确认构建、不允许访问
设备，使用 `-SkipInstall`。完整参数和日志路径见
[docs/batchbuild-project-workflow.md](docs/batchbuild-project-workflow.md)。

#### 6.2.2 构建参数

| 参数 | 值 |
|------|-----|
| 平台 | Android |
| 架构 | ARM64 |
| 后端 | IL2CPP |
| Target API Level | Android 10+ (API 29+) |
| 构建场景 | 01StartUp + 03SingleplayerScene + 04SingleplayerScene |

#### 6.2.3 APK 安装

标准脚本会对每台状态为 `device` 的 adb 设备执行 `adb install -r`，随后启动
`com.mrtksync`。不得在用户尚未确认 APK 导出/设备访问时执行。

### 6.3 副工程设置

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\setup-batchbuild-project.ps1 `
  -SourceProjectPath 'D:\gitea_obj\_github_obj\github_gitea_bpf_714' `
  -BatchProjectPath 'D:\gitea_obj\_unity projection\gitea_BPF__batchbuild'
```

每次发布构建前重新读取 `Assets`、`Packages`、`ProjectSettings` 三个 Junction
的实际目标。目标不符时才使用 `-ForceRepair`；脚本不会自动删除普通目录。

---

## 7. 目录结构速查

```
github_gitea_bpf_714/
├── Assets/
│   ├── A_MRNet/                ★ 核心业务模块（场景/脚本/UI/模型）
│   │   ├── Sence/              ★ 4 个业务场景（01-04）
│   │   ├── Scripts/
│   │   │   ├── UI/             UI 面板（UIPanel/UIManager/MainPage）
│   │   │   ├── XR/             手部校准系统
│   │   │   ├── StepTrigger.cs  碰撞步进触发器
│   │   │   └── ...
│   │   ├── Zn/                 核心交互模块
│   │   │   ├── 控制物体/       物体放置/导航/碰撞检测
│   │   │   ├── ProcessDrag.cs  流程图拖拽
│   │   │   ├── CameraController.cs
│   │   │   └── MultilevelMenu.cs
│   │   ├── Third-Party/Rope/   绳索模拟系统
│   │   └── ProgramResources/   TexCore 业务框架
│   │
│   ├── GameFramework/          ★ 基础框架层（18 个核心管理器）
│   │   ├── Libraries/GameFramework/ 纯 C# 核心库
│   │   ├── Scripts/Runtime/          Unity 运行时适配
│   │   ├── Scripts/Editor/           编辑器工具（资源构建/分析）
│   │   └── Prefabs/GameFramework.prefab
│   │
│   ├── Resources/
│   │   ├── BusinessPrefabs/    ★ 业务章节/题目面板预制体
│   │   ├── TopicPrefabs/       ★ 通用题目 UI 组件
│   │   ├── PICOProjectSetting.asset
│   │   └── PXR_*.asset
│   │
│   ├── StreamingAssets/
│   │   ├── Config.xml          ★ 题目 XML 配置数据
│   │   ├── UserLogin.json      测试用户凭据
│   │   └── Scenes/*.dat        GameFramework 场景数据
│   │
│   ├── BuildAssets/
│   │   ├── BuildApp.cs         ★ 命令行构建入口
│   │   └── AndroidManifestPatcher.cs  PICO AndroidManifest 注入
│   │
│   ├── XR/                     XR Loader 配置
│   ├── XRI/                    XR Interaction Toolkit 设置
│   ├── Profiles/               MRTK Profile（BpfOpenXR*.asset）
│   ├── MRTK.Generated/         MRTK3 自动生成配置
│   ├── Modules/LegacyMrtkSync/ 遗留 MRTK 联网同步模块
│   ├── Plugin/Demigiant/DOTween/ DOTween 动画引擎
│   └── Scripts/                MRTK3 示例脚本
│
├── Packages/
│   ├── PICO/                   ★ PICO SDK 3.4.0
│   │   ├── Runtime/            Unity XR Plugin 运行时
│   │   ├── SpatialAudio/       PICO 空间音频
│   │   └── Platform/           PICO Platform SDK
│   ├── manifest.json           ★ Unity 包依赖清单
│   └── packages-lock.json
│
├── docs/                       ★ 项目文档
│   ├── bpf-frontend-collaboration-guide.md
│   ├── bpf-ai-collaboration-workflow.md
│   ├── pico-readiness-audit.md
│   ├── framework-pico-mainline-notes.md
│   └── mrtk3-overview/         MRTK3 文档归档
│
├── AGENTS.md                   ★ AI 协作行为准则
├── README.md                   项目说明
├── .mcp.json                   Unity MCP 配置
└── .editorconfig               代码风格规范
```

---

## 8. 常见问题与约束

### 8.1 开发约束

| 约束 | 说明 |
|------|------|
| **Unity 版本锁定** | 必须使用 Unity 2021.3.29f1c1 |
| **构建后端** | Android 必须使用 IL2CPP + ARM64 |
| **XR 路线** | Android 使用 ARCore + PXR；Standalone / WSA 使用 OpenXR |
| **手部跟踪** | Android 使用 UnityHandsSubsystem（非 OpenXRHandsSubsystem） |
| **语音功能** | Android Profile 不加载 Windows 语音子系统 |
| **场景入口** | 主线为 01StartUp → 03SingleplayerScene；04 额外启用；02 仅保留参考 |
| **TMP InputField** | Android/PICO 上统一走系统键盘桥接 |
| **Quest overlay keyboard** | 已关闭，不再注入 |
| **编码规范** | `.editorconfig`: 4 空格缩进，禁 Tab，禁 var，大括号换行 |

### 8.2 已知问题

| 问题 | 状态 | 说明 |
|------|------|------|
| **PICO MR Passthrough** | 当前禁用 | OpenXR Passthrough feature、PXR `videoSeeThrough` 均为 0；启用前需明确需求并重新做构建、Manifest 与真机验证 |
| **模块隔离** | 历史迁移结论 | `Assets/App/Modules` / `Assets/App/Shared` 当前不存在；旧的 10 模块拆分只作迁移资料 |
| **PICOProjectSetting vs PXR_ProjectSetting 手部追踪差异** | 需验证 | `isHandTracking: 1` vs `handTracking: 0` 的实际运行时影响 |
| **UserLogin.json 硬编码** | 安全隐患 | 包含过期 JWT token，不应出现在发布版本 |

### 8.3 关键文档索引

| 文档 | 路径 |
|------|------|
| AI 协作行为准则 | [AGENTS.md](AGENTS.md) |
| 当前工程基线 | [docs/current-project-baseline.md](docs/current-project-baseline.md) |
| BPF 前端协作开发流程 | [docs/bpf-frontend-collaboration-guide.md](docs/bpf-frontend-collaboration-guide.md) |
| BPF AI 协作开发流程 | [docs/bpf-ai-collaboration-workflow.md](docs/bpf-ai-collaboration-workflow.md) |
| PICO 4 业务发布说明 | [docs/pico-readiness-audit.md](docs/pico-readiness-audit.md) |
| GameFramework PICO 兼容性 | [docs/framework-pico-mainline-notes.md](docs/framework-pico-mainline-notes.md) |
| 副工程构建工作流 | [docs/batchbuild-project-workflow.md](docs/batchbuild-project-workflow.md) |
| MRTK3 上游 README | [docs/mrtk-upstream-readme-archive.md](docs/mrtk-upstream-readme-archive.md) |

---

> **文档维护说明**: 本文档为 AI 辅助自动生成，建议在项目重大架构变更后更新。维护者可由 AI 重新运行分析任务生成新版本。
