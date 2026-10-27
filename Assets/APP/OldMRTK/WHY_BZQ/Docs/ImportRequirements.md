# WHY_BZQ Import Requirements

## Package Baseline
- Unity 2021.3.29f1c1
- com.unity.netcode.gameobjects 1.5.2
- org.mixedrealitytoolkit.core / input / spatialmanipulation / uxcore matching the source project
- com.unity.xr.interaction.toolkit 2.4.3

## Exported Assets
- Prefab: `Assets/WHY_BZQ/Prefabs/WHY_BZQ.prefab`
- Support prefab: `Assets/WHY_BZQ/Support/GuidingArrow.prefab`
- Support prefab: `Assets/WHY_BZQ/Support/GlobalSyncManager.prefab`
- Support prefab: `Assets/WHY_BZQ/Support/MultController.prefab`

## Scene-Level Expectations
- `WHY_BZQSceneBindings` accepts explicit scene override slots for `PlayerController`, `GuidingArrow`, `GlobalSyncManager`, `MultController`, and related scene objects.
- `PersonMove` will bind `PlayerController` from the explicit override first, then by object name, then by tag `PlayerController`.
- `WHY_BZQSceneBindings` will reuse an explicit or existing `GuidingArrow`, or spawn the exported support prefab under `Camera.main`.
- `GlobalSyncManager` is not auto-created. Add the exported support prefab if SceneItemSync network behavior is required.
- `MultController` is not auto-created. Add the exported support prefab if multiplayer score/reporting behavior is required.
- `SceneItemSync.uniqueID` values are preserved; this prefab package is intended for one instance per scene.

## Residual External Asset Dependencies
- None detected.
