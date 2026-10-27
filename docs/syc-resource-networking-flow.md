# `_syc_resource` 老联机流程说明

> Historical legacy-flow snapshot only. 当前默认业务流是
> `Assets/A_MRNet/Sence/01StartUp.unity` ->
> `Assets/A_MRNet/Sence/03SingleplayerScene.unity`，Build Settings 还启用
> `04SingleplayerScene.unity`。本文中的 `Assets/_syc_resource` 和
> `Assets/App` 路径在当前工作树中不存在；实际遗留代码和场景应从
> `Assets/APP/Modules/LegacyMrtkSync`、`Assets/APP/ScenesNGOsync` 与
> `Assets/APP/OldMRTK` 重新定位。见
> [current-project-baseline.md](./current-project-baseline.md)。

这份文档只解释历史 `_syc_resource` 联机流程原先如何串联，不证明这些路径或
对象仍存在于当前工程。

这里不讨论后续重构方案，也不讨论 `Assets/App/Modules` 里的业务模块流程。重点是把老主线讲清楚，方便继续沿着它排查和开发。

## 1. 先看清边界

现在的仓库里：

- 老场景主线还在 `Assets/_syc_resource/_my_scenes`
- 老脚本主体已经迁到 `Assets/App/Scripts/SycResource` 和 `Assets/App/Shared/Scripts/Legacy*`

所以看这套流程时要分两层：

1. 场景流转，按 `_syc_resource/_my_scenes` 看
2. 代码实现，按迁移后的 `Assets/App/...` 看

这也是为什么场景里还能看到老 `_syc_resource` 的流程，但对应脚本文件已经不在 `_syc_resource/_my_scripts` 目录里了。

## 2. 老主线场景顺序

当前老联机主线是这四个场景：

1. `Assets/_syc_resource/_my_scenes/00Init/Init.unity`
2. `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`
3. `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`
4. `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`

主链可以概括成：

`Init -> UDPLobby -> Lobby -> MRTKsync`

其中：

- `Init` 负责起网络管理对象和初始跳转
- `UDPLobby` 负责房间入口和 UDP 找房界面
- `Lobby` 是过渡场景
- `MRTKsync` 才是真正的联机同步场景

## 3. Init 场景做什么

老入口是 `Assets/_syc_resource/_my_scenes/00Init/Init.unity`。

这个场景里最关键的是两个对象：

- `NetworkManager`
- `GameManager`

### 3.1 NetworkManager 对象

`Init.unity` 里的 `NetworkManager` 对象挂了这些联机相关组件：

- `Unity.Netcode.NetworkManager`
- `UnityTransport`
- `ExampleNetworkDiscovery`
- `AutoJoinManager`

对应的现有代码文件分别是：

- `Assets/App/ThirdPartyAdapters/UnityNetcode/Runtime/NetworkManager.cs`
- `Assets/App/ThirdPartyAdapters/UnityNetcode/Runtime/UnityTransport.cs`
- `Assets/App/Scripts/SycResource/UDP/ExampleNetworkDiscovery.cs`
- `Assets/App/Scripts/SycResource/UI/AutoJoinManager.cs`

从场景序列化内容能看出，老设计意图是：

- `UnityTransport` 负责连接传输
- `ExampleNetworkDiscovery` 负责局域网发现
- `NetworkManager` 负责 NGO 风格的会话管理

场景里还能看到这些配置痕迹：

- 连接端口 `7777`
- UDP 发现端口 `47777`
- `ServerName`
- `m_StartWithServer`

这说明 `_syc_resource` 这条老主线本来就是按 `UDP + NGO` 的思路搭的。

### 3.2 GameManager 对象

对应代码在 `Assets/App/Scripts/SycResource/GameCtrl/GameManager.cs`。

它的职责很简单：

- 保持自身常驻
- 启动时如果当前不是 `UDPLobby`，就切到 `UDPLobby`

也就是说，老主线启动后会被导向局域网房间入口，而不是直接进业务场景。

### 3.3 Init 的入口按钮

另一个常见入口是 `Assets/App/Scripts/SycResource/UI/ButtonManager.cs`。

它在 UI 初始化时给开始按钮注册事件，点击后直接：

- `SceneManager.LoadScene("UDPLobby")`

所以从 `Init` 往下走，常见入口有两种：

1. `GameManager` 自动把流程送进 `UDPLobby`
2. UI 按钮显式跳 `UDPLobby`

## 4. UDPLobby 场景做什么

老房间页是 `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`。

这层的设计目标是：

1. Host 广播房间
2. Client 扫描房间
3. 创建房间或快速加入
4. 进入 `Lobby`

### 4.1 主要脚本

对应的现有脚本主要是：

- `Assets/App/Scripts/SycResource/UI/RoomBrowserCtrl.cs`
- `Assets/App/Scripts/SycResource/UI/AutoJoinManager.cs`
- `Assets/App/Scripts/SycResource/UDP/ExampleNetworkDiscovery.cs`

### 4.2 当前调用流程

`RoomBrowserCtrl` 里保留了两个入口：

- `CreateRoom()`
- `QuickJoin()`

两者当前都会调用：

- `LegacyNetworkingSceneFlow.Instance?.LoadLegacyLobby()`

`AutoJoinManager.StartScanning()` 也会直接：

- `LegacyNetworkingSceneFlow.Instance?.LoadLegacyLobby()`

所以现在代码层的实际效果是：

- `UDPLobby` 仍然是房间入口场景
- 但“找房成功后再进 Lobby”这一步已经被简化成“直接进 Lobby”

### 4.3 UDP 发现层当前状态

`ExampleNetworkDiscovery` 的意图仍然很明确：

- `StartServer()`
- `StartClient()`
- `StopDiscovery()`
- `ClientBroadcast(...)`
- `OnServerFound`

但当前实现是占位的：

- `StartServer()` 空实现
- `StartClient()` 空实现
- `StopDiscovery()` 空实现
- `ClientBroadcast(...)` 只是伪造一个本地 `127.0.0.1:7777` 的房间结果

所以要区分两件事：

1. 老流程设计上，`UDPLobby` 是 UDP 找房层
2. 当前代码实现上，这一层已经被简化，真实发现逻辑没有接完整

## 5. Lobby 场景做什么

过渡场景是 `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`。

这里最关键的对象就是：

- `LobbyCtrl`

对应代码在：

- `Assets/App/Scripts/SycResource/SYNC/LobbyCtrl.cs`

逻辑很简单：

1. `Start()`
2. `Invoke(nameof(AutoJumpToGame), 0.25f)`
3. `AutoJumpToGame()`
4. `LegacyNetworkingSceneFlow.Instance?.LoadLegacySync()`

也就是说，`Lobby` 不是最终联机业务场景，只是一个短暂中转页，负责把流程继续送进 `MRTKsync`。

## 6. MRTKsync 场景做什么

真正的老联机业务场景是：

- `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`

这层的核心不是房间 UI，而是场景物体同步。

### 6.1 核心脚本

最关键的两个脚本是：

- `Assets/App/Scripts/SycResource/SYNC2/GlobalSyncManager.cs`
- `Assets/App/Scripts/SycResource/SYNC2/SceneItemSync.cs`

`MRTKsync.unity` 里能看到：

- 一个 `GlobalSyncManager`
- 大量挂了 `SceneItemSync` 的场景物体

### 6.2 同步机制

`GlobalSyncManager` 的职责：

1. `Awake()` 时扫描场景里所有 `SceneItemSync`
2. 按 `uniqueID` 注册到字典
3. 提供统一入口：
   - `RequestSetActive(...)`
   - `RequestSetTransform(...)`

`SceneItemSync` 的职责：

1. 记录本物体的 `uniqueID`
2. 监控本地：
   - 显隐变化
   - `localPosition`
   - `localRotation`
3. 当状态变化时，调用 `GlobalSyncManager.Instance`
4. 再由 `GlobalSyncManager` 找到同 ID 物体应用状态

这套逻辑说明：老 `_syc_resource` 主线真正想同步的是**场景内已有物体的状态**，而不是只靠动态生成对象。

### 6.3 当前真实行为

这层现在仍然保留了“联机同步外形”，但实现上是本地回调：

- `RequestSetActive(...)` 直接本地调用 `ApplyNetworkActive(...)`
- `RequestSetTransform(...)` 直接本地调用 `ApplyNetworkTransform(...)`

也就是说，`MRTKsync` 的这套同步结构还在，但当前并没有真正把消息发到远端。

## 7. 一个典型业务调用例子

老链路里一个比较典型的例子是：

- `Assets/App/Shared/Scripts/LegacyGameplay/NewS/UpanBack.cs`

这个脚本的流程是：

1. 找到目标物体 `Upan`
2. 切换它的显隐
3. 把它移动到目标点
4. 读取它身上的 `SceneItemSync`
5. 通过 `GlobalSyncManager.RequestSetTransform(...)` 触发同步

这说明老主线的业务交互脚本，很多并不是自己做网络发送，而是依赖：

- `SceneItemSync`
- `GlobalSyncManager`

来做统一同步。

## 8. 现在应当怎么理解这套老流程

如果只看 `_syc_resource` 老主线，正确理解应该是：

### 8.1 设计意图

- `Init` 起网络对象
- `UDPLobby` 做局域网房间入口
- `Lobby` 做过渡
- `MRTKsync` 做场景物体同步

### 8.2 当前实现状态

- 场景链路还在
- 脚本映射还在
- NGO/UDP 的结构也还在
- 但关键网络发送层已经被替换成兼容占位实现

所以这条老主线现在更像是：

- 场景流程可读
- 组件关系可追
- 业务同步入口可定位
- 但真实联机能力未完全接通

## 9. 阅读顺序建议

如果后面要继续沿着这条老主线排查，建议按这个顺序读：

1. `Assets/_syc_resource/_my_scenes/00Init/Init.unity`
2. `Assets/App/Scripts/SycResource/GameCtrl/GameManager.cs`
3. `Assets/App/Scripts/SycResource/UI/ButtonManager.cs`
4. `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`
5. `Assets/App/Scripts/SycResource/UI/RoomBrowserCtrl.cs`
6. `Assets/App/Scripts/SycResource/UDP/ExampleNetworkDiscovery.cs`
7. `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`
8. `Assets/App/Scripts/SycResource/SYNC/LobbyCtrl.cs`
9. `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`
10. `Assets/App/Scripts/SycResource/SYNC2/GlobalSyncManager.cs`
11. `Assets/App/Scripts/SycResource/SYNC2/SceneItemSync.cs`
12. `Assets/App/Shared/Scripts/LegacyGameplay/NewS/UpanBack.cs`

## 10. 一句话结论

`_syc_resource` 老联机主线不是 Mirror 房间流，而是：

- 用 `Init -> UDPLobby -> Lobby -> MRTKsync` 串场景
- 用 `UDP + NGO` 的结构组织入口
- 用 `GlobalSyncManager + SceneItemSync` 做场景物体同步

只是现在这套结构里的很多网络行为，已经被替换成占位实现，保留的是流程骨架，不是完整可用的联机底座。
