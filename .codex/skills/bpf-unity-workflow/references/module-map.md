# BPF Module Map

Use this file when a request names a business module, a legacy scene, or an old contributor folder.

## Historical Migration Targets

The `Assets/App/Modules/*` and `Assets/App/Shared/*` paths below are historical
migration targets. They do not exist in the current worktree and must not be
used for current placement unless a concrete migration task recreates them.
Search the live `Assets/...` tree before choosing any destination.

## Legacy-To-Business Mapping

| SHIXUN object | Business module | Legacy scene | Main legacy script area | Historical migration target |
| --- | --- | --- | --- | --- |
| `NetWork-03_2` | `NetworkCabinetWiring` | `PQ_NetworkBuild.unity` | `Assets/GameMain/Scripts/PQ` | `Assets/App/Modules/NetworkCabinetWiring` |
| `WHY_FuWuQi-6` | `ServerAssembly` | `WHY_FuWuQi.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/ServerAssembly` |
| `LSL_RMS_Install` | `RmsInstall` | `LSL_RMSInstall.unity` | `Assets/GameMain/Scripts/LSL` | `Assets/App/Modules/RmsInstall` |
| `LSL_RMS_Import` | `RmsImport` | `LSL_RMSImport.unity` | `Assets/GameMain/Scripts/LSL` | `Assets/App/Modules/RmsImport` |
| `HuaXian` | `LayoutMarking` | `ZDP_ZhuChangJing.unity` | `Assets/GameMain/Scripts/ZDP/HuaXian` and `Assets/GameMain/Scripts/PQ` | `Assets/App/Modules/LayoutMarking` |
| `WHY_HJ` | `RackAssembly` | `WHY_HJ.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/RackAssembly` |
| `WHY_BZQ` | `DisplayWallAssembly` | `WHY_BZQ.unity` | `Assets/GameMain/Scripts/WHY` | `Assets/App/Modules/DisplayWallAssembly` |
| `changjing-2` | `ChargingStationAssembly` | `JYJ_anzhuang.unity` | `Assets/GameMain/Scripts/JYJ` | `Assets/App/Modules/ChargingStationAssembly` |
| `ChangJing-8` | `AgvCommissioning` | `ZDP_ZhuChangJing.unity` | `Assets/GameMain/Scripts/ZDP/JiQiRen` and `Assets/GameMain/Scripts/ZDP/JiFen*` | `Assets/App/Modules/AgvCommissioning` |
| `changjing-4` | `RackPlacement` | `JYJ_HuoJiaRuChang.unity` | `Assets/GameMain/Scripts/JYJ/HJRC` | `Assets/App/Modules/RackPlacement` |

## Historical Placement Rules

Apply these rules only when an explicit migration task restores the proposed
`Modules` and `Shared` layout. For current work, prefer an existing business
root such as `Assets/A_MRNet`, `Assets/Resources/BusinessPrefabs`, or
`Assets/Resources/TopicPrefabs`.

Move into `Modules` when the asset is clearly owned by one module:

- module scenes
- module scripts
- module-only prefabs
- module-only models
- module-only audio
- module-specific UI or config

Keep in `Shared` when the asset is reused:

- common workshop environment assets
- shared `MR_ChangJing` content
- shared UI atlases and generic buttons
- shared materials and textures
- shared workflow scripts
- fonts and generic runtime resources

## Historical Shared Targets

- `Assets/App/Shared/Scripts/WorkflowCommon`
- `Assets/App/Shared/Scripts/LegacySceneFlow`
- `Assets/App/Shared/Scripts/TaskRewardFlow`
- `Assets/App/Shared/Prefabs/System`
- `Assets/App/Shared/Resources`

## Notes

- The repo still contains historical source folders such as `WHY`, `JYJ`, `PQ`, `ZDP`, `LSL` in older areas.
- Resolve current ownership from the live tree. Use this map only for provenance or an explicitly requested migration destination.
