# BPF 当前工程基线

> 状态：现行操作依据  
> 核对日期：2026-07-22
> 适用仓库：`D:\gitea_obj\_github_obj\github_gitea_bpf_714`

本文集中记录当前 BPF 工程中容易发生漂移的场景、XR、构建和目录边界。
其他文档与本文冲突时，应先重新核对工程文件；在工程没有新改动的前提下，
以本文和下列直接证据为准。

## 1. 场景主线

当前默认单人业务流为：

```text
01StartUp
  -> 03SingleplayerScene
```

- 启动场景：`Assets/A_MRNet/Sence/01StartUp.unity`
- 默认业务场景：`Assets/A_MRNet/Sence/03SingleplayerScene.unity`
- 额外启用场景：`Assets/A_MRNet/Sence/04SingleplayerScene.unity`
- 保留但未加入 Build Settings 的参考场景：
  `Assets/A_MRNet/Sence/02SingleplayerScene.unity`

直接证据：

- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/A_MRNet/Sence/01StartUp.unity` 中的 `MainPage.soloScene`
- `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_LoadScene.cs`

`01StartUp` 中的 `soloScene` 与 `TexCore_LoadScene.BusinessSceneName` 均为
`03SingleplayerScene`。`04SingleplayerScene` 虽处于 Build Settings，
但不是当前首页单人按钮的默认目标。

## 2. Android 与 XR 路线

当前 Android XR Plug-in Management 的 loader 列表为：

1. ARCore Loader
2. PXR Loader

Standalone 和 WSA 使用 OpenXR Loader。工程同时包含 PICO XR 3.4.0、Unity
OpenXR 配置资产和若干 PICO OpenXR feature 记录；这些资产的存在不能反推
Android 当前正在使用 OpenXR Loader。

因此，当前 Android/PICO 主线应描述为：

- Android 设备运行由 PXR 路线承担；
- PICO OpenXR feature 配置属于共存配置，不是 Android active loader 的证明；
- `PassthroughFeature Android` 当前为禁用，不能写成“透视功能已启用”；
- `AndroidManifestPatcher` 只声明支持 `OpenXRLoader`，不能证明其 PICO
  meta-data 或 CAMERA 权限会进入当前 ARCore＋PXR 构建；最终 Manifest
  以实际 Gradle 导出物为准；
- Quest/Meta Android feature 当前保持禁用；
- PICO 4 与 PICO 4 Ultra 控制器 profile 当前已启用。

直接证据：

- `Assets/XR/XRGeneralSettings.asset`
- `Assets/XR/Settings/OpenXR Package Settings.asset`
- `Packages/manifest.json`
- `Packages/PICO/package.json`

## 3. Android 构建基线

- Unity：`2021.3.29f1c1`
- Scripting Backend：IL2CPP
- Target Architecture：ARM64
- 最低 Android API：29
- 当前共享仓库默认不保存开发者本地签名；正式签名另行注入

默认使用批工程：

```text
D:\gitea_obj\_unity projection\gitea_BPF__batchbuild
```

该批工程通过 Junction 共享当前仓库的 `Assets`、`Packages` 和
`ProjectSettings`，并保留自己的 `Library`、`Builds`、`Logs` 和
`UserSettings`。执行构建前必须重新检查三个 Junction 的目标，不能只依据
文档中的历史状态。

标准脚本：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1
```

当前脚本生成的 Gradle 工程位于：

```text
D:\gitea_obj\_unity projection\gitea_BPF__batchbuild\Builds\Android\BpfPico_Gradle
```

APK 只有在 Unity 导出和 Gradle 构建成功后才会出现在：

```text
...\launcher\build\outputs\apk\release\launcher-release.apk
```

不要把 `Library\Bee\...` 下的临时产物写成标准交付路径。

## 4. 业务资源与遗留目录

当前业务开发优先使用：

- `Assets/A_MRNet`
- `Assets/Resources/BusinessPrefabs`
- `Assets/Resources/TopicPrefabs`
- `Assets/APP/BPF/Compatibility/Fonts`

`Assets/APP/MRTKSamples` 集中存放上游 MRTK 示例目录，包括动画、音频、
示例脚本、Prefab、场景和贴图。移动时保留了 Unity GUID，当前业务场景仍可
继续引用这些依赖，但它们不属于 BPF 自有业务资产。
`Assets/APP/Modules/LegacyMrtkSync` 是 `04SingleplayerScene` 仍有实际
引用的遗留联机模块。

`Assets/APP/BPF` 是当前 BPF 兼容资产区。目前仅承载按用途分组的兼容字体；
不要将它与同级的遗留归档目录混为一类，也不要据此恢复历史
`Assets/App/Modules` 或 `Assets/App/Shared` 结构。

以下目录只在明确的遗留联机、迁移或历史对照任务中使用：

- `Assets/APP/OldMRTK`
- `Assets/APP/ScenesNGOsync`
- `Assets/APP/Modules/LegacyMrtkSync`

当前工作树不存在小写的 `Assets/App/Modules`、`Assets/App/Shared` 或
`Assets/_syc_resource`。提到这些路径的旧迁移文档只记录历史快照，不能直接
作为当前文件位置或可执行工作流；它们也不是当前 `Assets/APP/BPF` 的别名。

## 5. Git 与 Unity MCP

每轮仓库工作开始前：

1. 执行 `git fetch --all --prune`；
2. 检查 `origin/main` 是否包含本地 `main` 没有的提交；
3. 如有新提交，先停止仓库操作并征求是否拉取；
4. 不自动把当前功能分支合并或变基到 `origin/main`。

项目锁定的 Unity MCP 配置以根目录 `.mcp.json` 为准，当前端点是：

```text
http://127.0.0.1:8080/mcp
```

只有实际 Unity MCP 调用成功，才说明本轮已连接。

## 6. 文档状态规则

- “现行操作依据”：可用于当前工程操作，但执行前仍应检查直接证据。
- “静态核对快照”：结论绑定核对日期、分支与提交，不代表后续工程状态。
- “历史迁移资料”：仅用于理解旧目录和迁移理由，不代表当前路径。
- “需求/设计资料”：描述目标行为，不代表功能已经实现。
- “上游归档”：保存 MRTK/PICO 上游信息，不定义 BPF 当前业务主线。

文档分类索引见 [README.md](./README.md)。
