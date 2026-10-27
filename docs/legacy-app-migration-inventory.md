# Legacy App Migration Inventory

> Historical migration snapshot only. This file records a previous `Assets/App`
> reorganization and does not define current paths. The active business route is
> `Assets/A_MRNet/Sence/01StartUp.unity` ->
> `Assets/A_MRNet/Sence/03SingleplayerScene.unity`; Build Settings also
> enables `04SingleplayerScene.unity`. The current worktree has no `Assets/App`
> root. See [current-project-baseline.md](./current-project-baseline.md).

This file freezes the phase-1 migration boundary for rebuilding the legacy app
inside `gitea_BPF`.

## First-pass migration

- `Assets/_syc_resource/_my_scenes/00Init/Init.unity`
- `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`
- `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`
- `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`
- `Assets/_syc_resource/_my_scenes/sync/MRTKsyncsuccess.unity`
- `Assets/_syc_resource/_my_perfabs`
- `Assets/_syc_resource/_my_resources`
- `Assets/GameFramework`
- `Assets/StreamingAssets/UserLogin.json`
- `Assets/StreamingAssets/GameFrameworkVersion.dat`
- `Assets/StreamingAssets/Scenes`
- `Assets/GameMain/Animations`
- `Assets/GameMain/Configs`
- `Assets/GameMain/DataTables`
- `Assets/GameMain/Entity`
- `Assets/GameMain/Localization`
- `Assets/GameMain/Models`
- `Assets/GameMain/Prefabs`
- `Assets/GameMain/Res`
- `Assets/GameMain/Scenes`
- `Assets/GameMain/Scripts`
- `Assets/GameMain/Sounds`
- `Assets/GameMain/UI`
- `Assets/GameMain/tietu`
- `Assets/UI`
- `Assets/NewUI`
- `Assets/Materials`
- `Assets/_Materials`
- `Assets/NetSync_Prefabs_2`
- `Assets/StandarAssets/Fonts`
- `Assets/VolumetricLines/BuiltInRenderPipeline/Materials`
- `Assets/VolumetricLines/BuiltInRenderPipeline/Prefabs`
- `Assets/TextMesh Pro/Fonts`
- `Assets/TextMesh Pro/Resources/Fonts & Materials`

## Second-pass migration

- `Assets/_syc_resource/_my_scripts`
  - Keep as a source-of-truth reference only for now.
  - Do not copy directly into the compile path yet, because `Assets/App/Scripts/SycResource`
    already provides local-compatible replacements for the same classes.
- `Assets/GameMain/Item`
  - Currently empty in the legacy project, but remains part of the review boundary.

## Explicitly excluded

- `Packages/MixedReality/*.tgz`
- `com.microsoft.mixedreality.webrtc*.tgz`
- `Assets/GameMain/Plugins/Mirror`
- `Assets/GameMain/Plugins/QuickOutline`
- `Assets/Demigiant`
- `Assets/Samples`
- `Assets/Plugins/Android/gradleTemplate.properties.backup`
- `buildfailed.txt`

## Pending manual review

- `Assets/GameMain/Scripts/JYJ`
- `Assets/GameMain/Scripts/WHY`
- `Assets/GameMain/Scripts/PQ`
- Legacy scene-only unknown script GUID `e233b1e6870b30044bf89102a3f86053` on `Lobby.unity` `Cell`

## Notes

- `gitea_BPF` keeps ownership of package, XR, and Android/PICO project settings.
- Legacy networking is replaced in phase 1 with local compatibility shims under
  `Assets/App/ThirdPartyAdapters`.
- The phase-1 legacy networking build entry remains the `_syc_resource` mainline:
  `Init -> Lobby/UDPLobby -> MRTKsync`.

## Current reorganization status

The migration is no longer only a raw copy. A first-pass business-module
reorganization is now in progress under `Assets/App`.

## Current compile baseline

At the current checkpoint:

- `dotnet build Assembly-CSharp.csproj` passes
- `dotnet build Assembly-CSharp-Editor.csproj` passes
- the previous `DOTweenModuleUI.cs` `Outline` alias collision has been resolved
  by switching the alias to `UIOutline`
- legacy editor tooling has been moved under `Assets/App/Editor/LegacyEditor`
  and its default paths now point at `Assets/App/Shared/...`

### Reorganized into `Assets/App/Modules`

Visible source-person directory names have also started to be replaced inside
module internals:

- `ChargingStationAssembly/Animations/LegacyAnimator/AssemblyControllers`
- `NetworkCabinetWiring/Animations/LegacyAnimator/WiringControllers`
- `RackAssembly/Prefabs/LegacyEntity/RackVariants`

- `Assets/App/Modules/NetworkCabinetWiring`
  - scenes: `PQ_NetworkBuild.unity`
  - scripts: network-cabinet subset from legacy `PQ`
  - prefabs: `WHY_FuWuQi`-adjacent wiring scene prefabs and `changjing` assets
  - models: legacy network-build kit
  - audio: `服务器网络搭建`
- `Assets/App/Modules/ServerAssembly`
  - scenes: `WHY_FuWuQi.unity`
  - scripts: server-install subset from legacy `WHY`
  - task scripts: `TaskUPan`, `TaskXiTong`, `TaskJieMian`, etc.
  - prefabs: `WHY_FuWuQi`, `UPan`, `UI_XiTongJingXiangAnZhuang`
  - resource models: `JiGui`, `UPan`
  - media: `UI_镜像安装系统`
  - audio: `服务器系统搭建`
- `Assets/App/Modules/RmsInstall`
  - scenes: `LSL_RMSInstall.unity`
  - voice bootstrap: `Scripts/VoiceInput/DictationHandler.cs`
  - audio: `3_RMS系统安装`
- `Assets/App/Modules/RmsImport`
  - scenes: `LSL_RMSImport.unity`
  - audio: `4_RMS上传地图`
- `Assets/App/Modules/LayoutMarking`
  - scripts: `ZDP/HuaXian`
  - scripts: QR/line-marking subset split from `PQ`
  - prefabs: `pen 1.prefab`
  - models: measurement kit split from shared workshop assets
  - audio: `5画线贴码`
- `Assets/App/Modules/RackAssembly`
  - scenes: `WHY_HJ.unity`
  - scripts: rack-install subset split from `WHY`
  - prefabs: `WHY_HJ.prefab`
  - models: rack assembly kit
  - resource models: `JiaoShui`
  - audio: `货架安装`
- `Assets/App/Modules/DisplayWallAssembly`
  - scenes: `WHY_BZQ.unity`
  - scripts: BZQ subset split from `WHY`
  - prefabs: `WHY_BZQ.prefab`
  - models: display-wall kit
  - audio: `播种墙安装`
- `Assets/App/Modules/ChargingStationAssembly`
  - scenes: `JYJ_anzhuang.unity`
  - scripts: charging-station script set from `JYJ`
  - prefabs: `changjing-2.prefab`
  - models: legacy `Models/JYJ`
  - audio: `充电桩安装`
- `Assets/App/Modules/AgvCommissioning`
  - scripts: `ZDP/JiQiRen`
  - task scripts: `TaskAGVButton`, `TaskAGVClaim`, `TaskCDZ_peiwang`,
    `TaskRCS`
  - reward flow:
    `Scripts/TaskRewardFlow/{BuyNextStep,CheckRobotLeft,CheckRobotRight,ShowBuyPage}`
  - reward buttons:
    `Scripts/TaskRewardFlow/TaskButtons/{TaskJianChaButton,TaskKaiJiButton,TaskXiFuButton}`
  - prefabs: `ChangJing-8.prefab`
  - migrated drill prefab: `ShouDianZuan-9.prefab`
  - models: AGV kit
  - audio: `9机器人入场`
- `Assets/App/Modules/RackPlacement`
  - scenes: `JYJ_HuoJiaRuChang.unity`
  - scripts: `JYJ/HJRC`
  - prefabs: `changjing-4.prefab`
  - audio: `货架入场`

### Reorganized into `Assets/App/Shared`

- scenes:
  - `LSL_RMS.unity`
  - `ZDP_ZhuChangJing.unity`
  - `LegacyMainline/*`
  - `Composite/*`
  - `Tests/*`
  - `History/*`
- scripts:
  - `RmsWorkflow`
  - `TaskRewardFlow`
  - common workflow scripts
  - `LegacyFramework/*`
  - `LegacyGameplay/*`
  - `LegacyNetworkingSceneFlow/*`
  - `LegacyService/*`
  - `LegacyUI/*`
  - editor-only tools moved to `Assets/App/Editor/LegacyEditor/*`
- prefabs:
  - generic system prefabs
  - player prefab
- resources:
  - fonts from `_my_resources/font`
  - generic UI atlases and button/image assets
  - shared UI slices
  - legacy entity resources
- config/data/localization:
  - `Assets/App/Shared/Config/GameMain`
  - `Assets/App/Shared/DataTables/GameMain`
  - `Assets/App/Shared/Localization/GameMain`
- legacy plugin binaries:
  - `Assets/App/ThirdPartyAdapters/LegacyDlls/LitJson.dll`
  - `Assets/App/ThirdPartyAdapters/LegacyDlls/BouncyCastle/*`
- animations:
  - `Assets/App/Shared/Animations/LegacyClips`
  - `Assets/App/Shared/Animations/DrillInteractionControllers`
  - `Assets/App/Shared/Animations/LegacyClips/SharedDrillInteractionClips`
  - `Assets/App/Modules/AgvCommissioning/Animations/LegacyAnimator/AgvTaskControllers`
  - `Assets/App/Modules/AgvCommissioning/Animations/LegacyClips/AgvTaskClips`
  - `Assets/App/Modules/ChargingStationAssembly/Animations/LegacyClips/AssemblyClips/SDZ_BanJi_State0.anim`
- audio:
  - `Assets/App/Shared/Audio/LegacySoundArchive`
- models:
  - mixed workshop environment assets
  - common scene shells
- audio:
  - shared SFX bucket
  - legacy music archive
- legacy archive:
  - `GameMain/Prefabs/old`

### Not fully reorganized yet

- `_syc_resource` scene source tree remains as legacy mainline reference
- `_syc_resource` generic runtime resources beyond fonts/player
- `ZiGongSi12` and a few AGV reward/drill assets are still grouped by current
  usage evidence, not by a fully normalized final taxonomy

### Limitation of the current state

This is a first-pass business-module organization based on `SHIXUN` ownership.
It is already no longer organized only by source-person folders, but it is not
yet a fully normalized final asset taxonomy. Shared environment assets and
cross-module resources are intentionally kept in `Assets/App/Shared` until their
ownership is split more safely.

### Old-source shrink result

At this point:

- `Assets/GameMain/Prefabs` has been drained and only historical backup content
  was preserved under `Assets/App/Shared/LegacyArchive`
- `Assets/GameMain/UI` has been drained
- `Assets/GameMain/tietu` has been drained into module materials and shared
  legacy material buckets
- `Assets/GameMain/Configs`, `DataTables`, and `Localization` have been moved
  under `Assets/App/Shared`
- `Assets/GameMain/Sounds` has been archived under `Assets/App/Shared/Audio`
- `Assets/GameMain/Scenes` has been split into:
  - `Assets/App/Shared/Scenes/LegacyMainline`
  - `Assets/App/Shared/Scenes/Composite`
  - `Assets/App/Shared/Scenes/Tests`
  - `Assets/App/Shared/Scenes/History`
- `Assets/GameMain/Scenes` runtime scene files have been fully drained
- `Assets/GameMain/Scripts` has been split into:
  - `Assets/App/Shared/Scripts/LegacyFramework`
  - `Assets/App/Shared/Scripts/LegacyGameplay`
  - `Assets/App/Shared/Scripts/LegacySceneFlow`
  - `Assets/App/Shared/Scripts/LegacyService`
  - `Assets/App/Shared/Scripts/LegacyUI`
  - `Assets/App/Shared/Scripts/RmsWorkflow`
  - `Assets/App/Shared/Scripts/TaskRewardFlow`
  - `Assets/App/Modules/RmsInstall/Scripts/VoiceInput`
  - `Assets/App/Modules/AgvCommissioning/Scripts/TaskRewardFlow`
  - `Assets/App/Editor/LegacyEditor`
- the old `ZDP` animation buckets have been drained and replaced by:
  - `Assets/App/Modules/AgvCommissioning/Animations/LegacyAnimator/AgvTaskControllers`
  - `Assets/App/Modules/AgvCommissioning/Animations/LegacyClips/AgvTaskClips`
  - `Assets/App/Shared/Animations/DrillInteractionControllers`
  - `Assets/App/Shared/Animations/LegacyClips/SharedDrillInteractionClips`
  - `Assets/App/Modules/ChargingStationAssembly/Animations/LegacyClips/AssemblyClips/SDZ_BanJi_State0.anim`
- empty shell directories and orphan `.meta` files under `Assets/GameMain` have
  been recursively cleaned
- `Assets/GameMain/Plugins` runtime DLLs have been moved into
  `Assets/App/ThirdPartyAdapters/LegacyDlls`
- `Assets/GameMain/Res`, `Scenes`, `Entity`, `Plugins`, `Models`, `Prefabs`,
  `UI`, and `tietu` have been drained and their empty shell directories removed
- `Assets/GameMain/Res` has been reduced to the remaining top-level shells that
  still need final archival or cleanup handling
- module-owned legacy `Entity` and `Animator` content has been further split
  into `Assets/App/Modules/<ModuleName>/Prefabs|Animations|Materials`
- the temporary `Assets/App/Modules/PendingUnknown` bucket has been fully
  drained in the current checkpoint
