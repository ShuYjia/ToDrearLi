# BPF 局域网多人协同 — 双 PICO Ladder 检查单（阶段 5）

> 计划来源：`C:\Users\garfi\.workbuddy\plans\quantum-beacon-darwin.md` 阶段 5。
> 本文是该阶段的执行检查单：环境准备、8 级 ladder、日志命令、通过标准。
> **导出 APK 属于本阶段动作，须经 Garfield 明确确认后才执行。**

## 0. 环境与前置

| 项 | 状态 / 要求 | 检查命令 |
| --- | --- | --- |
| adb | Unity 自带：`D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` | `adb devices` 应列出 2 台 |
| 两台 PICO | USB 连接，开发者模式 + USB 调试已开，弹窗点"允许" | `adb devices` 两台都是 `device` 而非 `unauthorized` |
| 同一 WiFi | 同一 IPv4 子网；路由器**关闭 AP 隔离** | 两端 `设置-WLAN` 看 IP 是否同网段 |
| UDP 放行 | 7777（NGO 传输）+ 47777（房间广播） | 公司/机房网络可能拦，家用路由器一般没事 |
| 同一 APK | bundle id `com.mrtksync`，versionCode 一致（协议 1 + 应用/内容版本要匹配，否则审批拒） | 两端都装同一次构建产物 |
| 物理标记 | 三个校准标记点（原点 / 前方 / 右侧），两端都能走到并看到 | 提前贴好 |
| 构建场景 | Build Settings 已启用 `01StartUp` + `03SingleplayerScene` | 已确认 |
| 架构 | ARM64（AndroidTargetArchitectures=2） | 已确认 |

设备未连接时的现象：`adb devices` 列表为空 —— 先解决连接，再往下走。

## 1. Ladder 步骤

每步格式：**操作 → 预期 → 不过时记什么**。两端日志都要留（见第 3 节）。

### L1 建房（Host 端 = A 机）

1. A 机启动 App → 主页面 →「协同」子页 → 创建房间（可改房间名/昵称）。
2. 预期：状态显示 `Lobby`，日志 `Bootstrap.server_started`、`RoomService.host_started`、`Session.spawned`、`Avatar.avatar_spawned`。
3. 不过：房间建不起来 → 抓 `Bpf|` 全量日志 + 界面截图。

### L2 发现与加入（Client 端 = B 机）

1. B 机进「协同」子页 → 扫描 → 列表出现 A 的房间（房间名/Host 名/端口，`compatible=True`）→ 点加入。扫不到就手填 A 的 IP。
2. 预期：`Approval.connection_approved`，阶段同步到 `Lobby`，B 收到 Host 推送的场景。
3. 不过：审批拒绝 → 日志有 `Approval` 拒绝原因（版本不匹配/满员/阶段不符）；连不上 → 查同网段、AP 隔离、IP 填错。

### L3 进场互见（Gate C/D）

1. 两端都应看到对方头像（头 + 双手 + 名牌），自己这边紧跟本机 XR Rig。
2. 预期：远端姿态连续跟随，无明显抽动；断开一端另一端头像消失。
3. 不过：看不到头像 → `PoseSync`/`Avatar` 日志 + 两端截图；位置整体偏移 → 先做完 L4 校准再判断。

### L4 三点校准（Gate E）

1. 房主（A）在业务场景里点面板「开始校准」（面板自动切紧凑模式跟在镜头前）。
2. A 依次走到物理"原点 / 前方 / 右侧"，在每个点用任一只手**扣一下扳机**记录（面板提示"第 N 个物理标记点"）；B 同样做一遍。
3. 预期：阶段 `Calibrating → Playing`；Host 自身解算为单位变换（log 里 pos≈0）；B 的解算非单位（log 有具体值）；此后共享物体在两端坐标一致。
4. 不过：阶段不动 → 三点是否共线/太近（解算拒绝会打日志）、扳机是否被业务交互占用；坐标仍偏 → 核对三点是否真的是同一组物理标记。

### L5 协作做题（Gate G）

1. 进业务题目，B 点选项/步骤，A 观察；再交换。
2. 预期：B 的点击有本地音效反馈；分数/步骤只由 Host 算，0.5s 内双端一致；同一对象两端同时点只有先到者生效。
3. 不过：双端分数分叉 → 抓 `TexCoreBridge.click_*` / `TopicState` 日志 + 两端题目截图。

### L6 抓取（阶段 4 验收）

1. A 抓业务/测试物体移动 → B 看：应连续跟随、无跳变；B 抓同一物体 → 被拒（物体仍归 A）；A 释放 → 两端落点一致。
2. 换 B 抓、A 看，重复一遍（验 Client 拥有者位姿流经 Host 转发）。
3. 不过：B 看到跳变 → `Interactive.pose_*` 日志 + 两端位姿对比；拒绝不生效 → `InteractiveResult` 广播是否到达。

### L7 音视频（阶段 4 验收）

1. 触发步骤语音 / 视频播放（A 端触发为主）。
2. 预期：双端起播误差 < 100ms（肉眼/录屏核对；log 有 `AVSync.event_broadcast` / `event_executed` 两侧时间戳）。
3. 不过：误差大 → 看两端 `event_executed` 时间差；单端不播 → 该端资源是否齐全（同 APK 应有同资源）。

### L8 断线 / 退出（不死锁）

1. B 直接杀 App（进程级）。预期 A 侧：B 头像消失、B 占用的物体自动释放、界面不卡、可继续操作。
2. B 重连回房间。预期：拿到完整快照（占用表 + 抓取物位姿），与 A 一致。
3. A 点"离开"。预期：A 回大厅、阶段回 `Offline`、NGO 关停干净（`Bootstrap.client_stopped`），之后可再次建房（L1 重跑不报错）。
4. 不过：任一端死锁/黑屏 → 保留双方日志 + 复现步骤，带回给 Claude 诊断。

## 2. 通过标准

- L1~L8 全部双端行为一致；
- 抓取跟随连续无跳变；
- 音视频双端误差 < 100ms；
- 断线可恢复、Host 退出回大厅不死锁；
- 全程无 `Bpf][Error]`（日志过滤见下）。

## 3. 日志命令（两端都留）

```powershell
adb -s <设备号> logcat -c
adb -s <设备号> logcat -v time > ladder_<A|B>_<步骤>.log
# 实时关注：
adb logcat -v time | Select-String "Bpf|Netcode|UnityTransport|Network"
```

关键日志点：`Bootstrap.server_started` / `Approval.connection_approved` /
`Session.phase_synced` / `PoseSync` / `Interactive.pose_*` / `InteractiveResult` /
`AVSync.event_broadcast` / `AVSync.event_executed` / `TexCoreBridge.click_*` / `TopicState`。

## 4. APK 导出（须 Garfield 确认后才执行）

1. 批工程路径：`D:\gitea_obj\_unity projection\gitea_BPF__batchbuild`（**当前不存在**，需确认用该路径重建还是另选）。
2. Unity：Build Settings → Android（ARM64）→ 直接 Build APK，或先 Export Project 到批工程再出包。
3. 安装：`adb install -r <apk>` 到两台；`adb shell pm list packages | findstr mrtksync` 核对版本一致。
4. 注意：导出产物与日志一起归档，便于回溯是哪次构建。

## 5. 当前环境状态（2026-10-25 记录）

- adb 可用（Unity 自带 SDK），**当前 0 台设备连接**；
- 批工程目录不存在；
- origin（192.168.11.14 gitea）不可达，本地领先 5 个提交、无已知落后；push 前需重试 fetch。
