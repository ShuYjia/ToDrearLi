# Asset Migration Location Map

> Historical migration snapshot. “Now” below means the destination recorded by
> the migration pass that produced this document, not the current filesystem.
> The current worktree has no lowercase `Assets/App` root. Its current
> `Assets/APP/BPF` compatibility tree is unrelated to the historical
> `Modules/Shared` destinations below. Do not use this map for direct edits
> without rechecking the current asset path and GUID. See
> [current-project-baseline.md](./current-project-baseline.md).

This file is a focused lookup table for the migrated legacy assets that matter
most during day-to-day review.

Primary source:

- `docs/asset-compare/old-to-new-compare.txt`

Supporting boundary docs:

- `docs/legacy-app-migration-inventory.md`
- `docs/asset-consolidation-next-steps.md`

## Current compatibility-font locations (2026-07-22)

These mappings describe the current uppercase `Assets/APP/BPF` tree and take
precedence over historical font paths elsewhere in this document:

- `Assets/ArtResources/Font/*` ->
  `Assets/APP/BPF/Compatibility/Fonts/TopicUI/*`
- `Assets/GameMain/Res/Fonts/*` ->
  `Assets/APP/BPF/Compatibility/Fonts/LegacyUGUI/*`
- `Assets/StandarAssets/Fonts/*` ->
  `Assets/APP/BPF/Compatibility/Fonts/LegacyMrtkSync/*`

Only the locations changed; filenames and Unity GUIDs were preserved.

## Current MRTK sample and legacy-sync locations (2026-07-22)

The following uppercase `Assets/APP` areas are current filesystem locations,
but they do not change BPF business ownership:

- former top-level MRTK sample roots such as `Assets/Animations`,
  `Assets/Audio`, `Assets/Prefabs`, `Assets/Scenes`, and `Assets/Scripts`
  -> `Assets/APP/MRTKSamples/<same folder>`
- `Assets/Modules/LegacyMrtkSync`
  -> `Assets/APP/Modules/LegacyMrtkSync`

The move preserved filenames, `.meta` files, and Unity GUIDs. Some sample
assets remain real dependencies of the current 01/03/04 scenes, while the
legacy sync module remains referenced by `04SingleplayerScene`.

## How to read this file

- `Old` means the source location in the legacy import or old project layout.
- `Now` means the current location in this repository.
- When a whole category moved as a bucket, the mapping is shown as a directory
  rule first, then a few concrete examples.

## 1. UI assets

### RMS install media/UI

- Old: `Assets/_Materials/LSL/RMS_Picture/*`
- Now: `Assets/App/Modules/RmsInstall/Media/LSL/RMS_Picture/*`

Examples:

- `Assets/_Materials/LSL/RMS_Picture/RenderTex.renderTexture`
  -> `Assets/App/Modules/RmsInstall/Media/LSL/RMS_Picture/RenderTex.renderTexture`
- `Assets/_Materials/LSL/RMS_Picture/video_9_low.mp4`
  -> `Assets/App/Modules/RmsInstall/Media/LSL/RMS_Picture/video_9_low.mp4`

- Old: `Assets/_Materials/LSL/UI/*`
- Now: `Assets/App/Modules/RmsInstall/Media/LSL/UI/*`

Example:

- `Assets/_Materials/LSL/UI/CloseButton.png`
  -> `Assets/App/Modules/RmsInstall/Media/LSL/UI/CloseButton.png`

### Top-level UI buckets

- Old: `Assets/UI/JYJ_UI/*`
- Now: `Assets/App/Shared/UI/JYJ_UI/*`

- Old: `Assets/UI/PQ/*`
- Now: `Assets/App/Shared/UI/PQ/*`

- Old: `Assets/UI/TuBiao/*`
- Now: `Assets/App/Shared/UI/TuBiao/*`

- Old: `Assets/UI/WHY/*`
- Now: `Assets/App/Shared/UI/WHY/*`

- Old: `Assets/UI/ZDP/*`
- Now: `Assets/App/Shared/UI/ZDP/*`

- Old: `Assets/NewUI/*`
- Now: `Assets/App/Shared/UI/NewUI/*`

Concrete examples:

- `Assets/UI/WHY/Spirit_问答界面/下一题.png`
  -> `Assets/App/Shared/UI/WHY/Spirit_问答界面/下一题.png`
- `Assets/UI/WHY/UI_镜像安装系统/Video/Render Texture.renderTexture`
  -> `Assets/App/Shared/UI/WHY/UI_镜像安装系统/Video/Render Texture.renderTexture`
- `Assets/UI/ZDP/指引/下一步提示/确定按钮.png`
  -> `Assets/App/Shared/UI/ZDP/指引/下一步提示/确定按钮.png`
- `Assets/UI/PQ/Sprites/是否退出_slices/确定.png`
  -> `Assets/App/Shared/UI/PQ/Sprites/是否退出_slices/确定.png`
- `Assets/UI/TuBiao/工具/手电钻.png`
  -> `Assets/App/Shared/UI/TuBiao/工具/手电钻.png`

## 2. Prefabs

### Legacy player prefab

- Old: `Assets/_syc_resource/_my_perfabs/Player/Player.prefab`
- Now: `Assets/App/Shared/Resources/Players/Player/Player.prefab`

### Business prefabs moved out of `GameMain/Prefabs`

- Old: `Assets/GameMain/Prefabs/changjing-2.prefab`
- Now: `Assets/App/Modules/ChargingStationAssembly/Prefabs/changjing-2.prefab`

- Old: `Assets/GameMain/Prefabs/changjing-4.prefab`
- Now: `Assets/App/Modules/RackPlacement/Prefabs/changjing-4.prefab`

- Old: `Assets/GameMain/Prefabs/ChangJing-8.prefab`
- Now: `Assets/App/Modules/AgvCommissioning/Prefabs/ChangJing-8.prefab`

- Old: `Assets/GameMain/Prefabs/WHY_FuWuQi.prefab`
- Now: `Assets/App/Modules/ServerAssembly/Prefabs/WHY_FuWuQi.prefab`

- Old: `Assets/GameMain/Prefabs/UI_XiTongJingXiangAnZhuang.prefab`
- Now: `Assets/App/Modules/ServerAssembly/Prefabs/UI_XiTongJingXiangAnZhuang.prefab`

- Old: `Assets/GameMain/Prefabs/UPan.prefab`
- Now: `Assets/App/Modules/ServerAssembly/Prefabs/UPan.prefab`

- Old: `Assets/GameMain/Prefabs/WHY_BZQ.prefab`
- Now: `Assets/App/Modules/DisplayWallAssembly/Prefabs/WHY_BZQ.prefab`

- Old: `Assets/GameMain/Prefabs/WHY_HJ.prefab`
- Now: `Assets/App/Modules/RackAssembly/Prefabs/WHY_HJ.prefab`

### Shared/system prefabs

- Old: `Assets/GameMain/Prefabs/ChangJing-3.prefab`
- Now: `Assets/App/Shared/Prefabs/System/ChangJing-3.prefab`

- Old: `Assets/GameMain/Prefabs/openxr_left_hand.prefab`
- Now: `Assets/App/Shared/Prefabs/System/openxr_left_hand.prefab`

- Old: `Assets/GameMain/Prefabs/openxr_right_hand.prefab`
- Now: `Assets/App/Shared/Prefabs/System/openxr_right_hand.prefab`

- Old: `Assets/GameMain/Prefabs/pui.prefab`
- Now: `Assets/App/Shared/Prefabs/System/pui.prefab`

### Historical prefab archive

- Old: `Assets/GameMain/Prefabs/old/*`
- Now: `Assets/App/Shared/LegacyArchive/PrefabsOld/old/*`

Examples:

- `Assets/GameMain/Prefabs/old/WHY_BZQ.prefab`
  -> `Assets/App/Shared/LegacyArchive/PrefabsOld/old/WHY_BZQ.prefab`
- `Assets/GameMain/Prefabs/old/WHY_HJ.prefab`
  -> `Assets/App/Shared/LegacyArchive/PrefabsOld/old/WHY_HJ.prefab`

### NetSync prefab bucket

- Old: `Assets/NetSync_Prefabs_2/*`
- Now: `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2/*`

Examples:

- `Assets/NetSync_Prefabs_2/AC.prefab`
  -> `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2/AC.prefab`
- `Assets/NetSync_Prefabs_2/AGV1.prefab`
  -> `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2/AGV1.prefab`
- `Assets/NetSync_Prefabs_2/Global_NetworkPrefabsList.asset`
  -> `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2/Global_NetworkPrefabsList.asset`

## 3. Fonts

### `_syc_resource` font resources

- Old: `Assets/_syc_resource/_my_resources/font/*`
- Now: `Assets/App/Shared/Resources/Fonts/font/*`

Examples:

- `Assets/_syc_resource/_my_resources/font/ark-pixel-font-10px-proportional-ttf-v2026.01.04/ark-pixel-10px-proportional-zh_cn.ttf`
  -> `Assets/App/Shared/Resources/Fonts/font/ark-pixel-font-10px-proportional-ttf-v2026.01.04/ark-pixel-10px-proportional-zh_cn.ttf`
- `Assets/_syc_resource/_my_resources/font/ark-pixel-font-12px-monospaced-ttf-v2026.01.04/ark-pixel-12px-monospaced-zh_cn SDF.asset`
  -> `Assets/App/Shared/Resources/Fonts/font/ark-pixel-font-12px-monospaced-ttf-v2026.01.04/ark-pixel-12px-monospaced-zh_cn SDF.asset`

### `StandarAssets` font bucket

- Old: `Assets/StandarAssets/Fonts/*`
- Now: `Assets/App/Shared/Fonts/StandarAssets/*`

## 4. Scenes

### Legacy mainline scenes kept in place

These were intentionally not moved:

- `Assets/_syc_resource/_my_scenes/00Init/Init.unity`
- `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`
- `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`
- `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`
- `Assets/_syc_resource/_my_scenes/sync/MRTKsyncsuccess.unity`

### Module scenes moved from `GameMain/Scenes/Scenes`

- `Assets/GameMain/Scenes/Scenes/JYJ_anzhuang.unity`
  -> `Assets/App/Modules/ChargingStationAssembly/Scenes/JYJ_anzhuang.unity`
- `Assets/GameMain/Scenes/Scenes/JYJ_HuoJiaRuChang.unity`
  -> `Assets/App/Modules/RackPlacement/Scenes/JYJ_HuoJiaRuChang.unity`
- `Assets/GameMain/Scenes/Scenes/LSL_RMSImport.unity`
  -> `Assets/App/Modules/RmsImport/Scenes/LSL_RMSImport.unity`
- `Assets/GameMain/Scenes/Scenes/LSL_RMSInstall.unity`
  -> `Assets/App/Modules/RmsInstall/Scenes/LSL_RMSInstall.unity`
- `Assets/GameMain/Scenes/Scenes/PQ_NetworkBuild.unity`
  -> `Assets/App/Modules/NetworkCabinetWiring/Scenes/PQ_NetworkBuild.unity`
- `Assets/GameMain/Scenes/Scenes/WHY_BZQ.unity`
  -> `Assets/App/Modules/DisplayWallAssembly/Scenes/WHY_BZQ.unity`
- `Assets/GameMain/Scenes/Scenes/WHY_FuWuQi.unity`
  -> `Assets/App/Modules/ServerAssembly/Scenes/WHY_FuWuQi.unity`
- `Assets/GameMain/Scenes/Scenes/WHY_HJ.unity`
  -> `Assets/App/Modules/RackAssembly/Scenes/WHY_HJ.unity`

### Shared scene buckets

- Old: `Assets/GameMain/Scenes/Main.unity`
  -> `Assets/App/Shared/Scenes/LegacyMainline/Main.unity`
- Old: `Assets/GameMain/Scenes/Main 1.unity`
  -> `Assets/App/Shared/Scenes/LegacyMainline/Main 1.unity`
- Old: `Assets/GameMain/Scenes/MRTK.unity`
  -> `Assets/App/Shared/Scenes/Composite/MRTK.unity`
- Old: `Assets/GameMain/Scenes/Scenes/DuoRenXieTong.unity`
  -> `Assets/App/Shared/Scenes/Composite/Scenes/DuoRenXieTong.unity`
- Old: `Assets/GameMain/Scenes/202412/*`
  -> `Assets/App/Shared/Scenes/History/202412/*`
- Old: `Assets/GameMain/Scenes/Conformity1.1AndTest/*`
  -> `Assets/App/Shared/Scenes/Tests/Conformity1.1AndTest/*`
- Old: `Assets/GameMain/Scenes/Conformity2.1/*`
  -> `Assets/App/Shared/Scenes/Tests/Conformity2.1/*`

## 5. NetSync assets

There are two important NetSync-related situations:

### Bucket that was migrated

- Old: `Assets/NetSync_Prefabs_2/*`
- Now: `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2/*`

This is the current kept NetSync prefab bucket in the new tree.

### Buckets that show as missing in the compare report

These old buckets appear in the compare report as missing, not relocated:

- `Assets/NetSync_Prefabs/*`
- `Assets/NetSync_Prefabs_1/*`

Those are listed in:

- `docs/asset-compare/old-assets-missing-unexpected.txt`

That means they are not currently represented as moved matches in the new tree.
They require separate review if they are still expected to exist.

## 6. High-level answer

If you just want the short version of "where did the migrated assets go?", the
answer is:

- business-module-owned assets -> `Assets/App/Modules/*`
- shared legacy assets -> `Assets/App/Shared/*`
- `_syc_resource` mainline scenes -> still under `Assets/_syc_resource/_my_scenes/*`
- framework/runtime roots kept stable -> `Assets/GameFramework` and `Assets/StreamingAssets`
