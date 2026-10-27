# BPF 日志与诊断运行手册

> 状态：现行操作依据
> 更新日期：2026-09-11
> 架构说明见 `docs/bpf-logging-architecture.md`

## 1. 一分钟定位

| 我需要… | 去哪里 |
| --- | --- |
| 编辑器里实时看日志 | Play Mode 按 `F9` 打开 BPF Diagnostics 面板 |
| 设备里实时看日志 | 开发包左手菜单键打开世界空间面板 |
| 拉取设备日志文件 | `Tooling/Collect-BpfLogs.ps1` |
| 找会话文件 | 编辑器：`%USERPROFILE%\AppData\LocalLow\<公司名>\<产品名>\BpfLogs\`；设备：`/sdcard/Android/data/com.mrtksync/files/BpfLogs/` |
| 查一条日志的完整字段 | 在 JSONL 里检索 `"event":"<事件名>"` |
| 关闭某类日志噪声 | `BpfLog.SetCategoryEnabled(BpfLogCategory.UI, false)` 或 `BpfLog.MinimumLevel = BpfLogLevel.Warning` |

## 2. 日志位置与文件

- 目录名固定为 `BpfLogs`，位于 `Application.persistentDataPath` 之下。
- 文件名：`bpf-<会话ID>.jsonl`；单文件超过 5MB 轮转为 `bpf-<会话ID>.p2.jsonl` 等。
- 只保留最新 10 个文件（`BpfLogOptions.MaxRetainedFiles`）。
- 每行一条 JSON 记录，字段定义见架构文档第 3 节。
- 会话开始会写入 `log.session_started`，包含 `sessionId`、`logPath`、
  `platform`、`appVersion`、`deviceModel` 等字段，便于和设备日志对账。

编辑器（Windows）实际路径示例：

```
C:\Users\<用户>\AppData\LocalLow\<CompanyName>\<ProductName>\BpfLogs\bpf-20260911-123045-a1b2c3.jsonl
```

Android / PICO：

```
/sdcard/Android/data/com.mrtksync/files/BpfLogs/bpf-20260911-123045-a1b2c3.jsonl
```

## 3. 诊断面板

### 3.1 编辑器

1. 打开 `Assets/A_MRNet/Sence/01StartUp.unity`，进入 Play Mode。
2. 按 `F9` 显示/隐藏面板（快捷键定义在 `BpfLogOptions.OverlayToggleKey`）。
3. 面板操作：
   - `Level`：`Debug`…`Fatal` 级别过滤（过滤的是“最低级别”）
   - `Category`：分类过滤（横向滚动）
   - `Keyword`：对 message/event/category/fields 做不区分大小写检索
   - `Pause` / `Resume`：冻结当前列表，便于逐条阅读
   - `Clear`：清空内存缓存与计数器
   - `Export`：导出最近记录到 `BpfLogs/bpf-export-<时间>.jsonl`，面板下方显示路径
   - `Path`：显示/隐藏当前会话文件路径

### 3.2 开发包（PICO）

| 输入 | 作用 |
| --- | --- |
| 左手菜单键 | 打开/关闭面板 |
| 右手摇杆 上/下 | 查看更早/更新的日志 |
| 右手 A | 切换级别过滤 |
| 右手 B | 清空缓存 |
| 左手 X | 暂停/继续 |

面板显示最近 20 行、级别/分类/错误计数与当前日志路径。
正式包不会创建该面板。

## 4. ADB 收集

### 4.1 使用脚本

```powershell
# 默认收集所有已授权设备，输出到 Tooling/out/bpf-logs/<序列号>/
pwsh -File Tooling/Collect-BpfLogs.ps1

# 指定设备并附带 logcat
pwsh -File Tooling/Collect-BpfLogs.ps1 -Serial A9210ABC123 -IncludeLogcat

# 多设备 + 自定义输出目录
pwsh -File Tooling/Collect-BpfLogs.ps1 -Serial dev1,dev2 -OutputDirectory D:\bpf-logs
```

脚本参数：

| 参数 | 说明 |
| --- | --- |
| `-Serial` | 一个或多个设备序列号；省略时使用 `adb devices` 中所有 `device` 状态设备 |
| `-OutputDirectory` | 输出根目录，默认 `Tooling/out/bpf-logs` |
| `-AppIdentifier` | 应用包名，默认 `com.mrtksync` |
| `-LogDirectoryName` | 应用内日志目录名，默认 `BpfLogs` |
| `-IncludeLogcat` / `-LogcatLines` | 是否同时抓取 logcat 及其行数（默认 400） |

每个设备目录会写入 `collection-info.json`，记录采集时间（UTC）、设备序列号、
设备型号、应用包名、应用内日志路径、已采集文件与大小、logcat 文件。

退出码：`0` 成功；`2` 未采集到任何 JSONL（应用未运行过或诊断系统未启用）。

### 4.2 手工命令

开发包（debuggable）优先使用 `run-as`，Android 11+ 下也可读取应用私有目录：

```bash
adb devices -l
adb -s <序列号> shell run-as com.mrtksync ls -1 files/BpfLogs
adb -s <序列号> exec-out run-as com.mrtksync cat files/BpfLogs/<文件名>.jsonl > .\bpf.jsonl
adb -s <序列号> logcat -d -v threadtime | Select-String '\[Bpf\]'
```

注意：不要用 PowerShell 的 `>` 直接重定向 `adb exec-out`（会写成 UTF-16），
脚本内部通过 `cmd /c` 重定向以保证字节一致。

### 4.3 检索示例

```powershell
# 只看错误与异常
Get-Content .\bpf.jsonl | Select-String '"level":"(Error|Exception|Fatal)"'

# 某个事件的全部字段
Get-Content .\bpf.jsonl | Select-String '"event":"tex_core_load_assets.down_load_ab"'

# 确认没有敏感字段残留
Get-Content .\bpf.jsonl | Select-String -Pattern 'password=|Bearer [A-Za-z0-9]|access_token":"[^*]'
```

## 5. 事件与分类速查

- 会话与生命周期：`log.session_started`、`log.session_stopped`、`app.paused`、
  `app.resumed`、`app.quit`、`log.exported`、`log.export_failed`
- 场景：`scene.loaded`（字段 `scene`/`previousScene`/`mode`/`buildIndex`）
- 捕获的 Unity 日志：`unity.log`、`unity.warning`、`unity.error`、
  `unity.exception`、`unity.assert`（分类 `unity.external`）
- 限频汇总：任意事件名加 `.rate_limited`，字段 `suppressed`
- 业务事件：`<文件蛇形名>.<方法蛇形名>`，例如
  `tex_core_load_assets.down_load_ab`、`panel_operate.on_click`、
  `node_panel_base.load_child`

## 6. 故障排查

| 现象 | 排查顺序 |
| --- | --- |
| 找不到日志文件 | 1) 确认处于 Play Mode 或应用已启动；2) 查 `BpfLog.CurrentLogFilePath`（面板 Path 行）；3) 检查 `persistentDataPath` 是否可写；4) 面板 `ErrorCount` 与 Console 是否出现 `file.write_failed` |
| 面板打不开 | 编辑器：确认 Play Mode 且按了 `F9`，`BpfDiagnosticsOverlay.Instance` 不为空；设备：确认是 Development Build，左手菜单键绑定 `<XRController>{LeftHand}/menuButton` 是否被占用 |
| 面板空白/中文显示为方框 | 设备缺少中文字体，改用 JSONL 文件与 ADB 收集；面板框架文本为英文 |
| 日志过多影响帧率 | 提高 `BpfLog.MinimumLevel`；关闭分类；`BpfLogOptions.EnableRateLimiting` 保持开启 |
| 缺少某条业务日志 | 查 `MinimumLevel`、分类开关、限频（看 `.rate_limited` 记录） |
| 怀疑敏感信息入盘 | 用第 4.3 节命令扫描；确认调用处没有绕过 `BpfLog` 直接写文件 |
| 正式包 Console 太安静 | 这正是设计：正式包默认 `Warning`，面板不创建，文件仍写入 `BpfLogs` |
| 编译报缺少 `BpfLog` | 确认文件有 `using Bpf.Diagnostics;`，且没把代码放在 `Assets/A_MRNet/Third-Party` 之类被排除目录 |

## 7. 新增日志规范

1. 先选分类（`BpfLogCategory`），再取事件名；关键流程用稳定短名，
   不要在稳定事件里加入变量（把变量放进 `fields`）。
2. 对象名、步骤号、Topic、模块名等一律作为字段：

   ```csharp
   BpfLog.Info(BpfLogCategory.TexCore, "step.entered", "进入步骤", this,
       BpfLogField.Of("stepIndex", stepIndex),
       BpfLogField.Of("topic", topicName),
       BpfLogField.Of("stepObject", stepObject.name));
   ```

3. 需要 Console 点击定位时传入 `this`（或相关组件）作为 `context`。
4. 失败路径用 `Error`/`Exception`，并把原始错误放进字段，不要只在消息里拼接。
5. 不要在每帧调用的方法里输出 `Info` 级别以上的日志；确有需要时依赖限频，
   但优先降低级别或做状态变化判断。
6. 不要绕过 `BpfLog` 直接 `Debug.Log` 或写文件；第三方源码除外。

## 8. 验证清单

### 8.1 静态回归

```powershell
# 1) 业务目录不应再有未迁移的直接日志调用（Third-Party 与 Diagnostics 自身除外）
Get-ChildItem Assets/A_MRNet -Recurse -Filter *.cs |
  Where-Object { $_.FullName -notmatch 'Third-Party|Scripts\\Diagnostics' } |
  Select-String -Pattern 'Debug\.Log|Debug\.LogWarning|Debug\.LogError|Debug\.LogException|\bprint\s*\(' |
  Where-Object { $_.Line.TrimStart() -notmatch '^//' }

# 2) 不应改动 OldMRTK/MRTK/PICO/GameFramework
git status --porcelain -- Assets/APP/OldMRTK Assets/APP/MRTKSamples Packages Assets/GameFramework
```

### 8.2 Unity EditMode / PlayMode

```powershell
# EditMode（项目有 Library 锁时改用批量工程）
& "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "D:\gitea_obj\_unity projection\gitea_BPF__batchbuild" `
  -runTests -testPlatform EditMode `
  -testFilter Bpf.Diagnostics.Tests `
  -testResults "$env:TEMP\bpf-editmode.xml" -logFile "$env:TEMP\bpf-editmode.log" -quit

# PlayMode
& "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "D:\gitea_obj\_unity projection\gitea_BPF__batchbuild" `
  -runTests -testPlatform PlayMode `
  -testFilter Bpf.Diagnostics.Tests `
  -testResults "$env:TEMP\bpf-playmode.xml" -logFile "$env:TEMP\bpf-playmode.log" -quit
```

### 8.3 编辑器手测

1. 打开 `01StartUp`，进入 Play Mode，按 `F9` 打开面板；
   确认出现 `log.session_started`、`scene.loaded`（`scene=01StartUp`）。
2. 切换到 `03SingleplayerScene`，确认面板中 `scene.loaded` 的 `scene` 字段
   变为 `03SingleplayerScene`，Console 输出的前缀为 `[Bpf][<级别>][<分类>]`。
3. 在任意脚本临时加入 `Debug.Log("probe")`，确认面板出现分类
   `unity.external` 的 `unity.log` 记录（验证捕获链路），随后删除该行。
4. 点击 Console 中的 `[Bpf]` 条目，确认能定位到调用脚本（上下文保留）。
5. 面板 `Export`，确认导出文件生成且内容为 JSONL。

### 8.4 设备验收（需明确设备授权）

```powershell
adb devices -l
pwsh -File Tooling/Collect-BpfLogs.ps1 -IncludeLogcat
Get-Content .\Tooling\out\bpf-logs\<序列号>\collection-info.json
```

确认：日志文件可拉取、每行可解析、`collection-info.json` 中
`appIdentifier`/`deviceSerial`/`inAppLogPath` 正确、无敏感字段残留。
