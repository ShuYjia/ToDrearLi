# SHIXUN Module Mapping

> Historical mapping snapshot. It records an earlier `Assets/App/Modules`
> migration model, not the current worktree. Use the module names as domain
> vocabulary only; re-locate assets under the current `Assets/...` layout before
> editing. See [current-project-baseline.md](./current-project-baseline.md).

This document freezes the business-module interpretation of
`Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`.

It is a historical module-mapping reference. Do not treat the suggested target
column in this file as the current on-disk source of truth without checking the
real `Assets/...` layout first.

The `SHIXUN` root in `MRTKsync.unity` is not a random mixed scene. It is a
container of workshop-style business modules. Future asset organization in
`gitea_BPF` should use these module names instead of source-person folders such
as `JYJ`, `WHY`, `PQ`, or `ZDP`.

## Current source organization

The migrated legacy assets are still mainly organized by historical source:

- `Assets/_syc_resource/...`
- `Assets/GameMain/...`
- `Assets/GameFramework/...`

Inside `Assets/GameMain`, assets are still mostly organized by asset type:

- `Models`
- `Prefabs`
- `Res`
- `UI`
- `Sounds`
- `Scenes`
- `Scripts`

This is why the current import is usable as a source baseline, but not yet a
clean business-facing project structure.

## SHIXUN modules

| SHIXUN child | Business module | Related legacy scene(s) | Main source script area | Suggested target |
| --- | --- | --- | --- | --- |
| `NetWork-03_2` | `NetworkCabinetWiring` | `PQ_NetworkBuild.unity` | `Assets/GameMain/Scripts/PQ` | `Assets/App/Modules/NetworkCabinetWiring` |
| `WHY_FuWuQi-6` | `ServerAssembly` | `WHY_FuWuQi.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/ServerAssembly` |
| `LSL_RMS_Install` | `RmsInstall` | `LSL_RMSInstall.unity` | `Assets/GameMain/Scripts/LSL` | `Assets/App/Modules/RmsInstall` |
| `LSL_RMS_Import` | `RmsImport` | `LSL_RMSImport.unity` | `Assets/GameMain/Scripts/LSL` | `Assets/App/Modules/RmsImport` |
| `HuaXian` | `LayoutMarking` | `ZDP_ZhuChangJing.unity` | `Assets/GameMain/Scripts/ZDP/HuaXian` + `Assets/GameMain/Scripts/PQ` | `Assets/App/Modules/LayoutMarking` |
| `WHY_HJ` | `RackAssembly` | `WHY_HJ.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/RackAssembly` |
| `WHY_BZQ` | `DisplayWallAssembly` | `WHY_BZQ.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/DisplayWallAssembly` |
| `changjing-2` | `ChargingStationAssembly` | `JYJ_anzhuang.unity` | `Assets/GameMain/Scripts/JYJ` | `Assets/App/Modules/ChargingStationAssembly` |
| `ChangJing-8` | `AgvCommissioning` | `ZDP_ZhuChangJing.unity` | `Assets/GameMain/Scripts/ZDP/JiQiRen` + `Assets/GameMain/Scripts/ZDP/JiFen*` | `Assets/App/Modules/AgvCommissioning` |
| `changjing-4` | `RackPlacement` | `JYJ_HuoJiaRuChang.unity` | `Assets/GameMain/Scripts/JYJ/HJRC` | `Assets/App/Modules/RackPlacement` |

## Evidence from SHIXUN first-level objects

### `NetWork-03_2` / `NetworkCabinetWiring`

Key first-level objects:

- `MR_ChangJing`
- `JiaoHuanJiGongDian`
- `ACKaiJi`
- `Usb-Chuanxiankou2`
- `Chuanxiankou2-3`
- `Chuanxiankou3-Shuijingtou`
- `ChaTou2-6`
- `DianYuan_FuwuqiChaTou`
- `ZHIYIN`

### `WHY_FuWuQi-6` / `ServerAssembly`

Key first-level objects:

- `UPanCollider`
- `UPanPosition`
- `UPan`
- `upPosition`
- `UI_XiTong`
- `DianNao`
- `GameObject`

### `LSL_RMS_Install` / `RmsInstall`

Key first-level object evidence:

- `ZHIYIN`

### `LSL_RMS_Import` / `RmsImport`

Key first-level object evidence:

- `ZHIYIN`

### `HuaXian` / `LayoutMarking`

Key first-level objects:

- `ShuiPingYi`
- `JuanChi`
- `JiHaoBi`
- `JiZhun`
- `QRCode`
- `Pasting`
- `DiMian`
- `ZHIYIN`

### `WHY_HJ` / `RackAssembly`

Key first-level objects:

- `JiaoShui`
- `BanShou`
- `HuoJia_blue`
- `HuoJia_green`
- `HuoJia_cube`
- `HuoJia_shi`
- `ZHIYIN`

### `WHY_BZQ` / `DisplayWallAssembly`

Key first-level objects:

- `ShouDianZuan`
- `BoZhongQiang_blue`
- `BoZhongQiang_green`
- `BoZhongQiang_cube`
- `BoZhongQiang_shi`
- `GameObject`
- `ZHIYIN`

### `changjing-2` / `ChargingStationAssembly`

Key first-level objects:

- `ShouDianZuan-CDZ`
- `ErWeiMa`
- `ChongDianZhuang`
- `ChongDianZhuang_fuben`
- `XiangZi`
- `NeiLiuJiaoBanShou`
- `MR_ChangJing`
- `YangJiaoChui`
- `BiJiBen-ChuanKouXian`
- `ZHIYIN`

### `ChangJing-8` / `AgvCommissioning`

Key first-level objects:

- `MR_ChangJing`
- `EventSystem`
- `AGV`
- `ShouDianZuan-JQR`
- `BiJiBen-ChuanKouXian`
- `ZHIYIN`

### `changjing-4` / `RackPlacement`

Key first-level objects:

- `Directional Light`
- `MR_ChangJing`
- `huojia collision`
- `colliderxs`
- `ZHIYIN`

## Evidence from task enums

`Assets/GameMain/Scripts/Scene/LoadSceneObj.cs` contains workflow names that are
closer to the real business semantics than the person folders:

- `RMSIntro`
- `RMSTest`
- `ImportPackage`
- `QRCodePasting`
- `InstallhJ`
- `InstallBZQ`
- `Installchargingpile`
- `AGVnetwork`
- `AGVnetWork`

This confirms that the formal business structure should be built around workflow
modules, not around contributor-name folders.

## Practical reorganization rule

Use a two-layer business structure in `Assets/App`:

- `Assets/App/Modules/<ModuleName>/...`
- `Assets/App/Shared/...`

### Move into `Modules`

Assets that can be clearly tied to one module by scene usage, script semantics,
or document naming:

- module scenes
- module scripts
- module audio
- module-only models
- module-only prefabs
- module-specific config or UI

### Keep in `Shared`

Assets reused by multiple SHIXUN modules:

- common `MR_ChangJing` environment assets
- shared UI atlases and generic button images
- common materials and textures reused by multiple modules
- common fonts
- `_syc_resource` generic runtime resources

## Phase status

At the time of writing:

- the old assets have been copied into `gitea_BPF`
- the business-module mapping is now frozen
- the actual asset tree is reorganized by business ownership for the main
  SHIXUN modules
- `Assets/App` now contains:
  - `Modules`
  - `Shared`
  - `Scripts`
  - `ThirdPartyAdapters`
- a first batch has already been moved into module folders:
  - module scenes
  - module scripts
  - module-specific audio
  - module-specific prefabs
  - part of module-specific models and media
- the AGV reward/task-claim flow has been split out of the old
  `ZDP/JiFen` source tree into:
  - `Assets/App/Modules/AgvCommissioning/Scripts/TaskRewardFlow`
  - `Assets/App/Shared/Scripts/TaskRewardFlow`
- RMS voice input bootstrap now lives under:
  - `Assets/App/Modules/RmsInstall/Scripts/VoiceInput`
- ZDP animation ownership has been split by business use:
  - AGV task controllers now live under
    `Assets/App/Modules/AgvCommissioning/Animations/LegacyAnimator/AgvTaskControllers`
  - AGV task clips now live under
    `Assets/App/Modules/AgvCommissioning/Animations/LegacyClips/AgvTaskClips`
  - shared drill interaction controllers now live under
    `Assets/App/Shared/Animations/DrillInteractionControllers`
  - shared drill interaction clips now live under
    `Assets/App/Shared/Animations/LegacyClips/SharedDrillInteractionClips`
  - the charging-station-specific duplicate `SDZ_BanJi` clip pair is kept in
    `Assets/App/Modules/ChargingStationAssembly/Animations/LegacyClips/AssemblyClips`
    as `SDZ_BanJi.anim` and `SDZ_BanJi_State0.anim`
- several visible person-name folders have already been replaced with
  business-facing directory names:
  - `Animations/LegacyAnimator/WiringControllers`
  - `Animations/LegacyAnimator/AssemblyControllers`
  - `Prefabs/LegacyEntity/RackVariants`
- shared assets have started to move into `Assets/App/Shared`:
  - fonts
  - player prefab
  - generic UI atlases
  - common workflow scripts
  - mixed workshop environment assets
- the remaining work is mainly:
  - `GameMain/Res` leftovers
  - `GameMain/Prefabs/old`
  - shared scene-support media
  - prefabs/resources whose ownership is still cross-module
