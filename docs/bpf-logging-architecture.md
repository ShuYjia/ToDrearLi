# BPF 统一日志与诊断系统架构

> 状态：现行实现依据
> 更新日期：2026-09-11
> 代码位置：`Assets/A_MRNet/Scripts/Diagnostics/`
> 程序集：`Bpf.Diagnostics`（`Bpf.Diagnostics.asmdef`）

## 1. 目标与范围

BPF 主线原先在业务代码中直接调用 `Debug.Log` / `Debug.LogWarning` /
`Debug.LogError` / `print`，缺少级别、分类、事件名、场景与结构化字段，
设备端排查只能依赖 logcat 文本。

本系统提供一个运行时日志门面 `BpfLog`，统一以下能力：

- Unity Console 输出（保留对象上下文与点击定位）
- `Application.persistentDataPath/BpfLogs/` 下的会话 JSONL 文件
- ADB 收集（`Tooling/Collect-BpfLogs.ps1`）
- 全局异常与 Unity 原生日志捕获
- 开发包设备内/编辑器内诊断面板
- 开发包详细日志、正式包最小日志
- 默认脱敏、截断与文件轮转

非目标（首版不做）：

- 远程上传、云端检索、指标与链路追踪
- 修改 `Assets/GameFramework` 现有日志实现
- 修改 OldMRTK、MRTK 样例、PICO 包或第三方源码

## 2. 组件

| 文件 | 职责 |
| --- | --- |
| `BpfLogLevel.cs` | 级别枚举与标签解析/比较 |
| `BpfLogCategory.cs` | 分类常量（Startup/Scene/TexCore/UI/XR/Input/Network/Asset/Performance/Diagnostics）与 `unity.external` |
| `BpfLogField.cs` | 结构化字段键值对与不变文化文本转换 |
| `BpfLogJson.cs` | 极简 JSON/JSONL 转义写出器（不依赖 Unity 序列化） |
| `BpfLogRecord.cs` | 单条记录、固定字段、JSONL 行与控制台行 |
| `BpfLogOptions.cs` | 静态配置（级别、长度、轮转、限频、面板、快捷键） |
| `BpfLogSanitizer.cs` | 脱敏规则与长度截断 |
| `BpfLogMemoryBuffer.cs` | 环形内存缓冲，供面板与导出使用 |
| `BpfLogFileWriter.cs` | JSONL 文件出口：队列、批量落盘、轮转、清理 |
| `BpfLog.cs` | 静态门面：级别/分类过滤、限频、场景、捕获、导出 |
| `BpfLogRuntimeHost.cs` | 常驻对象：`Tick`、场景事件、暂停/失焦/退出刷新 |
| `BpfDiagnosticsPanel.cs` | 面板过滤/暂停/清理/导出状态（两种视图共用） |
| `BpfDiagnosticsOverlay.cs` | 编辑器 IMGUI 面板与设备端开关 |
| `BpfDiagnosticsDevicePanel.cs` | 头显内世界空间面板（开发包） |

## 3. 记录结构

每条记录固定包含以下字段，`fields` 为可选结构化对象：

```json
{"timestamp":"2026-09-11T12:30:45.123Z","level":"Warning","category":"Asset","event":"tex_core_load_assets.down_load_ab","message":"资源包下载失败","scene":"03SingleplayerScene","sessionId":"20260911-123045-a1b2c3","frame":18422,"stackTrace":"","fields":{"url":"https://...","httpCode":"404"},"captured":false}
```

| 字段 | 说明 |
| --- | --- |
| `timestamp` | UTC，`yyyy-MM-ddTHH:mm:ss.fffZ` |
| `level` | `Debug`/`Info`/`Warning`/`Error`/`Exception`/`Fatal` |
| `category` | 业务分类或 `unity.external` |
| `event` | 稳定点分事件名，见第 5 节 |
| `message` | 已脱敏、已截断的可读文本（可为空） |
| `scene` | 产生日志时的活动场景名 |
| `sessionId` | 会话标识，同一次启动的所有记录一致，也是文件名的一部分 |
| `frame` | Unity 帧号（后台线程写入时使用最近的主线程帧号） |
| `stackTrace` | 异常堆栈或捕获到的 Unity 堆栈；Warning 及以上在开发包按配置补调用方堆栈 |
| `fields` | 结构化键值对（对象形式，无字段时为空对象） |
| `captured` | `true` 表示该条来自 Unity 原生日志捕获而非直接调用 |

## 4. API

```csharp
using Bpf.Diagnostics;

BpfLog.Debug(BpfLogCategory.TexCore, "tex_core_step.enter", "进入步骤", this,
    BpfLogField.Of("stepIndex", stepIndex), BpfLogField.Of("topic", topicName));
BpfLog.Info(BpfLogCategory.Scene, "scene.loaded", "scene loaded");
BpfLog.Warning(BpfLogCategory.Asset, "tex_core_load_assets.down_load_ab", "资源包下载失败", this,
    BpfLogField.Of("url", url), BpfLogField.Of("httpCode", request.responseCode));
BpfLog.Error(BpfLogCategory.Network, "tex_core_background.split", "请求失败", this,
    BpfLogField.Of("error", request.error));
BpfLog.Exception(BpfLogCategory.TexCore, "tex_core_step.complete", exception, "步骤完成异常", this);
BpfLog.Fatal(BpfLogCategory.Startup, "startup.config_missing", "Config.xml 缺失", this);
```

统一签名（`Fatal` 无异常重载）：

```csharp
BpfLog.<Level>(string category, string eventName, string message = null,
               UnityEngine.Object context = null, params BpfLogField[] fields);
BpfLog.Error(string category, string eventName, Exception exception,
             string message = null, UnityEngine.Object context = null, params BpfLogField[] fields);
BpfLog.Exception(string category, string eventName, Exception exception,
                 string message = null, UnityEngine.Object context = null, params BpfLogField[] fields);
```

运行时接口：

| 成员 | 说明 |
| --- | --- |
| `BpfLog.MinimumLevel` | 级别过滤（低于该级别的调用直接返回，不构造记录） |
| `BpfLog.SetCategoryEnabled(category, enabled)` / `IsCategoryEnabled` | 分类开关 |
| `BpfLog.CurrentScene` / `SetScene(name)` / `NotifySceneLoaded(scene, mode)` | 场景字段 |
| `BpfLog.SessionId` / `CurrentLogFilePath` / `LogDirectory` | 会话与文件位置 |
| `BpfLog.RecordCount` / `ErrorCount` / `WarningCount` / `RateLimitedRecordCount` | 计数器 |
| `BpfLog.Snapshot(level, category, keyword, max)` / `GetRecentRecords(max)` | 最近记录 |
| `BpfLog.ClearRecords()` | 清理内存缓存与计数器 |
| `BpfLog.ExportRecent(max)` | 导出最近记录到独立 JSONL，返回路径 |
| `BpfLog.Flush()` | 立即落盘 |
| `BpfLog.RecordWritten` | 记录事件（测试与扩展用） |

`BpfLog` 由 `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 在
`Assets/A_MRNet/Scripts/Diagnostics/BpfLogRuntimeHost.cs` 中自动初始化，
并创建常驻对象 `Bpf Diagnostics Runtime`（`DontDestroyOnLoad`）。
`SubsystemRegistration` 阶段会重置静态状态，兼容关闭 Domain Reload 的场景。

## 5. 分类与事件命名

分类取值见 `BpfLogCategory`。迁移期的分类由“文件名/目录规则表”决定
（顺序匹配，先命中先归属，规则表位于迁移脚本，等价实现见下表）：

| 顺序 | 规则（文件名或路径匹配） | 分类 |
| --- | --- | --- |
| 1 | `/(Editor|Debug)/` 目录、`CodexTemp*` | Diagnostics |
| 2 | `performance`、`profiler`、`fps` | Performance |
| 3 | `bootstrap`、`startup` | Startup |
| 4 | `loadassets`、`assetbundle`、`bundle`、`download`、`multilevelmenu`、`background` | Asset |
| 5 | `network`、`server`、`http`、`webrequest`、`ipconfig`、`sync`、`login`、`navigation`、`exam*`、`datamanager` | Network |
| 6 | `hand`、`joystick`、`pico`、`camera`、`origin`、`holster`、`gaze`、`anchor`、`room`、`calibrat` | XR |
| 7 | `interact`、`click`、`grab`、`drag`、`press`、`pointer`、`touch`、`circular`、`rotate` | Input |
| 8 | `ui`、`page`、`panel`、`menu`、`button`、`canvas`、`rank`、`study`、`main`、`text`、`label`、`draw` | UI |
| 9 | `scene`、`step`、`node`、`topic`、`flow`、`level`、`chapter`、`plane`、`gate`、`rack`、`backpack`、`module`、`config`、`objstate` | Scene |
| 10 | 其余 | TexCore |

事件名约定：

- 自动迁移的调用：`<文件蛇形名>.<方法蛇形名>`，例如
  `tex_core_load_assets.down_load_ab`、`panel_operate.on_click`。
- 关键流程使用稳定短名，禁止随意改名（会破坏检索统计）：
  `scene.loaded`、`log.session_started`、`log.session_stopped`、
  `app.paused`、`app.resumed`、`app.quit`、`unity.log`、`unity.warning`、
  `unity.error`、`unity.exception`、`unity.assert`。
- 新增关键流程时优先使用 `<模块>.<动作>` 形式，例如
  `step.entered`、`step.completed`、`asset.load_failed`。
- 运行时自动派生的补充事件：`<event>.rate_limited`（限频汇总）。

参数表达式的迁移规则：

- 只含字符串字面量的表达式保留为 `message`（不再拼接新的长文本）。
- 形如 `filepath`、`www.error`、`obj.name` 的简单表达式改写为
  `BpfLogField.Of("<最后一段蛇形名>", <表达式>)`，`message` 置空。
- `Debug.Log(msg, this)` 的 `this` 作为 `context` 保留，Console 点击定位不受影响。

## 6. 级别策略

| 构建类型 | 默认 `MinimumLevel` | 内存面板 | 说明 |
| --- | --- | --- | --- |
| 编辑器 / 开发包 | `Debug` | 启用（2000 条环形） | 记录 Debug 及以上 |
| 正式包 | `Warning` | 不创建 | 只保留 Warning/Error/Exception/Fatal |

`BpfLogOptions.IsDevelopmentBuild` 由 `UNITY_EDITOR || DEVELOPMENT_BUILD`
推导，正式包不会创建诊断面板对象。

## 7. 出口与落盘

- 目录：`Application.persistentDataPath/BpfLogs/`
- 文件名：`bpf-<sessionId>.jsonl`，轮转后为 `bpf-<sessionId>.p2.jsonl`、`.p3.jsonl` …
- 队列：`ConcurrentQueue`，日志调用不写盘；`BpfLog.Tick` 每 0.5s 或累计 64 条
  批量写出（`BpfLogFileWriter.Drain`）。
- 刷新点：初始化、`OnApplicationPause`、失焦、`OnApplicationQuit`、
  Error/Exception/Fatal 写入后、`BpfLog.Flush()`。
- 轮转：单文件超过 `BpfLogOptions.MaxFileBytes`（默认 5MB）后新开分片。
- 保留：按最后写入时间（同时间按分片号）保留最新 `MaxRetainedFiles`
  （默认 10）个文件，其余删除。
- 写失败：`IsFaulted=true`，停止写入并输出一次 `file.write_failed` 警告，
  不会抛出到业务代码。

## 8. Unity 原生日志捕获与去重

- `BpfLog.EnsureInitialized()` 订阅 `Application.logMessageReceivedThreaded`。
- 同时订阅 `AppDomain.CurrentDomain.UnhandledException`，把 Unity 日志回调
  覆盖不到的托管线程异常记录为 `app.unhandled_exception`（字段 `isTerminating`），
  并立即刷新到磁盘。
- 未迁移的 `Debug.*`、第三方插件、Unity 内部日志统一归类为
  `unity.external`，级别映射：`Log→Info`、`Warning→Warning`、
  `Error→Error`、`Assert→Warning`、`Exception→Exception`。
- 去重：`BpfLog` 转发到 Console 时使用线程静态 `forwardingDepth` 计数，
  捕获回调在计数大于 0 时直接返回，因此同一条记录不会二次入库。
- 捕获记录不再回写 Console，避免回环。
- 捕获同样受 `MinimumLevel` 与分类开关约束，可用
  `BpfLog.SetCategoryEnabled(BpfLogCategory.UnityExternal, false)` 整体关闭。

## 9. 限频

- 目的：避免 PICO 设备上重复日志刷屏导致掉帧。
- 适用：`Debug`/`Info`/`Warning` 且键（级别+分类+事件+消息）相同的重复记录。
- 参数：`RateLimitWindowSeconds`（默认 1s）、`RateLimitMaxRecordsPerWindow`
  （默认 5）。
- 窗口结束时若发生过抑制，补写一条 `<event>.rate_limited` 记录，
  字段 `suppressed` 为被抑制条数。
- `Error`/`Exception`/`Fatal` 永不被抑制。

## 10. 脱敏与截断

`BpfLogSanitizer` 在入队前统一处理 `message`、`fields`、`stackTrace`。

| 规则 | 匹配内容 | 结果 |
| --- | --- | --- |
| 键值脱敏 | `token`/`access_token`/`refresh_token`/`password`/`pwd`/`secret`/`authorization`/`auth`/`cookie`/`api_key`/`session_id`/`user_id`/`openid`/`unionid`/`phone`/`mobile`/`email`/`device_id` 等键的 `key=value`、`key: value`、`"key":"value"` | `key=***`（保留键名、分隔符与引号） |
| 认证头 | `Bearer <token>`、`Basic <token>` | `Bearer ***` |
| URL 凭据 | `?token=`、`?sign=`、`?signature=`、`?key=`、`?ticket=` 等查询参数 | `?sign=***` |
| JWT | `eyJ...xxx.yyy.zzz` | `***` |
| 原始报文 | `request/response/body/payload/content` 后跟 64 字符以上无空白串 | `<前缀>***` |
| 截断 | `MaxMessageCharacters`(4000)、`MaxFieldCharacters`(512)、`MaxStackTraceCharacters`(4000) | 尾部追加 `...[truncated N chars]` |

## 11. 开发诊断面板

- 编辑器：`OnGUI` 浮窗，默认隐藏，`F9` 切换（`BpfLogOptions.OverlayToggleKey`）。
- 开发包（PICO）：世界空间面板，主动跟随相机，左手菜单键切换。
  - 右手摇杆上下：滚动历史
  - 右手 A：切换级别过滤
  - 右手 B：清空缓存
  - 左手 X：暂停/继续
  - 面板首行显示级别、分类、错误/警告计数、当前日志路径
- 面板能力（两种视图共用 `BpfDiagnosticsPanel`）：级别过滤、分类过滤、
  关键词检索（编辑器）、错误计数、暂停滚动、清理缓存、显示日志路径、导出最近日志。
- 正式包不创建面板对象（`BpfLogRuntimeHost` 使用
  `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 编译期守卫）。

## 12. 程序集与测试

```
Assets/A_MRNet/Scripts/Diagnostics/
  Bpf.Diagnostics.asmdef                  (Bpf.Diagnostics, 自动被 Assembly-CSharp 引用)
  *.cs
  Tests/EditMode/Bpf.Diagnostics.EditModeTests.asmdef   (+ BpfLogCoreTests / BpfLogRecordTests / BpfLogFileWriterTests)
  Tests/PlayMode/Bpf.Diagnostics.PlayModeTests.asmdef   (+ BpfLogPlayModeTests)
```

测试覆盖面：

- EditMode：级别过滤、分类开关、JSONL 字段与转义、脱敏与截断、
  限频、统一日志不重复入库、环形缓冲、文件批量写入/轮转/保留、导出。
- PlayMode：会话文件生成、`Debug.Log/Warning/Error/Exception` 捕获与级别映射、
  面板过滤/暂停/导出、`01StartUp` 与 `03SingleplayerScene` 场景字段更新。

## 13. 已知边界

- `Assets/A_MRNet/Third-Party/**` 按项目规则未迁移（第三方源码）。
- `Assets/GameFramework` 现有日志实现仅作参考，未改动、未依赖。
- 设备端面板的中文字形依赖系统动态字体回退（Noto Sans CJK / Droid Sans Fallback 等），
  若设备缺少中文字体则可能显示为方框，此时以 JSONL 文件与 ADB 收集为准。
- 首版不做日志上报与云端检索。
