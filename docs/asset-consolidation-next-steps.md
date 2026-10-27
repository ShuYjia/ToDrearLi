# Asset Consolidation Next Steps

> Historical migration snapshot. The lowercase `Assets/App` target layout
> described below is not present in the current worktree and this plan must not
> be executed as-is. The current `Assets/APP/BPF` tree only hosts compatibility
> assets and is not the historical `Modules/Shared` layout. Re-inventory the
> current `Assets/APP`, `Assets/Modules`, and active business roots before any
> migration. See
> [current-project-baseline.md](./current-project-baseline.md).

This document defines the post-migration consolidation boundary after the first
pass business-module reorganization under `Assets/App`.

It is intentionally operational. The goal is to decide what should be
consolidated next, what should stay temporarily frozen, and what should not be
reorganized yet.

## Current compatibility-font consolidation (2026-07-22)

The current worktree completed a separate, narrowly scoped consolidation into
the uppercase `Assets/APP/BPF/Compatibility/Fonts` tree:

- former `Assets/ArtResources/Font` -> `TopicUI`
- former `Assets/GameMain/Res/Fonts` -> `LegacyUGUI`
- former `Assets/StandarAssets/Fonts` -> `LegacyMrtkSync`

The assets kept their existing GUIDs and filenames. This does not revive the
historical lowercase `Assets/App/Modules|Shared` layout and does not authorize
moving `A_MRNet`, runtime `Resources`, third-party packages, or Unity special
directories.

## Current MRTK sample consolidation (2026-07-22)

The top-level MRTK example roots were grouped without changing their internal
structure:

- `Animations`, `Audio`, `Data Binding Example`, `Example Assets 1`,
  `Models`, `Prefabs`, `Scenes`, `Scripts`, `Textures`, and
  `UX Theming Example` now live under `Assets/APP/MRTKSamples`.
- the legacy networking module moved from `Assets/Modules/LegacyMrtkSync` to
  `Assets/APP/Modules/LegacyMrtkSync`.

All source `.meta` files and GUIDs were preserved. `Assets/APP/MRTKSamples`
is an upstream sample/dependency area rather than BPF-owned business content;
`04SingleplayerScene` still has actual references to the legacy sync module.

## Current baseline

At the current checkpoint:

- `Assets/GameMain` has already been drained out of the working tree.
- `Assets/App` is now the main asset host for migrated business content.
- compile validation currently passes:
  - `dotnet build Assembly-CSharp.csproj`
  - `dotnet build Assembly-CSharp-Editor.csproj`
- the active build entry scenes still point to:
  - `Assets/_syc_resource/_my_scenes/00Init/Init.unity`
  - `Assets/_syc_resource/_my_scenes/02Lobby/Lobby.unity`
  - `Assets/_syc_resource/_my_scenes/T04UDPSprate/UDPLobby.unity`
  - `Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`

This means the project is already restructured enough for ongoing development,
but it is not fully consolidated yet.

## Remaining legacy-side directories

The main directories still outside `Assets/App` are:

- `Assets/_syc_resource`
- `Assets/GameFramework`
- `Assets/UI`
- `Assets/NewUI`
- `Assets/NetSync_Prefabs_2`
- `Assets/Materials`
- `Assets/_Materials`
- `Assets/StreamingAssets`
- `Assets/StandarAssets`
- `Assets/VolumetricLines`

## Priority tiers

### Tier 1: consolidate next

These directories still create ownership ambiguity and should be reviewed next.

#### `Assets/UI`

Why it is next:

- It still contains `JYJ_UI`, `LSL`, `PQ`, `WHY`, `ZDP` source-person buckets.
- The folder naming conflicts with the business-module structure already
  established under `Assets/App/Modules`.
- Most files are static images and should be straightforward to re-home based on
  actual module usage.

Suggested target:

- module-specific UI assets -> `Assets/App/Modules/<Module>/UI`
- cross-module/common prompt art -> `Assets/App/Shared/UI`

Suggested action:

- treat `Assets/UI` as a content-mapping task, not as a blind move
- move only files whose scene or prefab usage can be confirmed
- if ownership is unclear, place them in `Assets/App/Shared/UI/LegacyPending`

#### `Assets/NetSync_Prefabs_2`

Why it is next:

- It contains many runtime prefabs spanning multiple business modules.
- The folder is still flat and mixes networking, task props, tools, rack parts,
  AGV pieces, and QR assets.
- It is one of the remaining large ambiguous roots after `GameMain` removal.

Suggested target:

- module-tied prefabs -> `Assets/App/Modules/<Module>/Prefabs/...`
- shared network/system prefabs -> `Assets/App/Shared/Prefabs/System/...`

Suggested action:

- split by actual scene ownership and network registration usage
- keep `Global_NetworkPrefabsList.asset` stable until all referenced prefabs are
  remapped and verified
- do not bulk-rename assets during this pass

#### `Assets/NewUI`

Why it is next:

- Small folder, low risk, likely easy to merge into module UI or shared UI.
- Current naming is temporary and not ownership-safe.

Suggested target:

- module-specific visual assets -> matching module UI folder
- generic dialog/button/background art -> `Assets/App/Shared/UI`

### Tier 2: consolidate carefully

These directories matter, but should be moved only with stronger validation.

#### `Assets/GameFramework`

Why it is not a blind-move candidate:

- It contains framework runtime code, editor tooling, and one framework prefab.
- It is not simply legacy business content; it acts as a foundational
  dependency.
- Moving it prematurely would create a wide blast radius across scenes, scripts,
  and editor tooling.

Observed structure:

- `Libraries`
- `Scripts`
- `Prefabs/GameFramework.prefab`

Suggested action:

- keep path stable for now
- first audit whether the project intends to:
  - preserve `Assets/GameFramework` as a third-party/framework root, or
  - fold selected wrappers into `Assets/App/Shared`
- only after that decision, consider limited consolidation of wrappers or
  project-specific extensions

Default recommendation:

- freeze location for now

#### `Assets/StreamingAssets`

Why it needs care:

- It contains runtime data files:
  - `Config.xml`
  - `GameFrameworkVersion.dat`
  - `UserLogin.json`
  - `Scenes/*.dat`
  - `Spaces/cover_image_small.png`
- These files may be loaded by path-sensitive runtime code.

Suggested action:

- keep location stable
- audit who reads each file before any move
- if a future cleanup is needed, prefer reducing unused files over relocating
  active ones

Default recommendation:

- freeze location for now

#### `Assets/Materials` and `Assets/_Materials`

Why they need care:

- small size, but likely referenced directly by scenes or prefabs
- mixed contents:
  - shader/material utility assets
  - RMS-specific media and UI assets

Suggested action:

- move only after reference-driven inspection
- likely split into:
  - `Assets/App/Shared/Materials`
  - `Assets/App/Modules/RmsInstall/...` or `Assets/App/Modules/RmsImport/...`

Default recommendation:

- do not bulk-move yet

### Tier 3: keep stable unless a direct need appears

These are low-volume or path-sensitive enough that they should not drive the
next consolidation pass.

#### `Assets/_syc_resource`

Current role:

- reduced to the legacy mainline scene entry only
- still referenced by `ProjectSettings/EditorBuildSettings.asset`
- also referenced by `Assets/App/Editor/LegacyMainlineValidator.cs`

Suggested action:

- do not move or rename the remaining scene paths yet
- only change this area as part of an explicit "mainline scene cutover" task

#### `Assets/StandarAssets`

Current role:

- effectively a font bucket

Suggested action:

- low urgency
- merge later if font ownership cleanup becomes necessary

#### `Assets/VolumetricLines`

Current role:

- tiny built-in render pipeline support bucket

Suggested action:

- leave in place unless the package or render-path strategy changes

## Recommended order of work

Use the following sequence for the next consolidation round:

1. `Assets/UI`
2. `Assets/NetSync_Prefabs_2`
3. `Assets/NewUI`
4. reference audit for `Assets/Materials` and `Assets/_Materials`
5. explicit decision on whether `Assets/GameFramework` remains a stable root
6. separate task later: mainline cutover away from `_syc_resource`

## First executed consolidation slice

The first low-risk execution slice should be:

- move `Assets/UI/LSL/RMS_Picture`
- target: `Assets/App/Modules/RmsInstall/UI/RMS_Picture`

Reason:

- the assets are already referenced by `RmsInstall` prefabs and scenes
- no code path audit currently shows a hardcoded `Assets/UI/LSL` string
- the folder is self-contained and module-specific

The other `Assets/UI` buckets should remain pending for now:

- `JYJ_UI`
- `PQ`
- `WHY`
- `ZDP`
- `TuBiao`

Those buckets still show cross-module or historical-scene usage and should be
sorted with a second pass instead of being moved by contributor-name folder.

## Consolidation boundary reached in this pass

This pass completed the broad top-level asset fold-in that can be done without
changing the active runtime entry roots.

### Moved into `Assets/App`

- `Assets/UI/JYJ_UI` -> `Assets/App/Shared/UI/JYJ_UI`
- `Assets/UI/PQ` -> `Assets/App/Shared/UI/PQ`
- `Assets/UI/TuBiao` -> `Assets/App/Shared/UI/TuBiao`
- `Assets/UI/WHY` -> `Assets/App/Shared/UI/WHY`
- `Assets/UI/ZDP` -> `Assets/App/Shared/UI/ZDP`
- `Assets/UI/LSL/RMS_Picture` -> `Assets/App/Modules/RmsInstall/UI/RMS_Picture`
- `Assets/NewUI` -> `Assets/App/Shared/UI/NewUI`
- `Assets/Materials` ->
  `Assets/App/Shared/Materials/LegacyTopLevelMaterials`
- `Assets/_Materials/LSL` -> `Assets/App/Modules/RmsInstall/Media/LSL`
- `Assets/NetSync_Prefabs_2` ->
  `Assets/App/Shared/Prefabs/System/NetSync_Prefabs_2`
- `Assets/StandarAssets/Fonts` -> `Assets/App/Shared/Fonts/StandarAssets`
- `Assets/VolumetricLines/BuiltInRenderPipeline` ->
  `Assets/App/Shared/Materials/VolumetricLines/BuiltInRenderPipeline`

### Intentionally kept stable

The following roots were intentionally not moved in this pass:

- `Assets/GameFramework`
- `Assets/StreamingAssets`
- `Assets/_syc_resource`

Reason:

- they still behave as framework/runtime-entry/path-sensitive roots
- moving them is a separate task with a much larger blast radius

## What remains after this pass

After this pass, the remaining top-level legacy roots are reduced to:

- `Assets/GameFramework`
- `Assets/StreamingAssets`
- `Assets/_syc_resource`

That is the practical stopping point for the current asset consolidation phase.

## What not to do next

- do not start by moving `Assets/GameFramework`
- do not rename the remaining `_syc_resource` mainline scene paths yet
- do not bulk-move `StreamingAssets`
- do not do another whole-tree "organize everything" pass
- do not rename assets just to make names prettier without ownership evidence

## Definition of done for the next pass

The next consolidation pass should be considered complete only if:

- `Assets/UI`, `Assets/NewUI`, and the clearly owned part of
  `Assets/NetSync_Prefabs_2` are either migrated or explicitly parked
- every moved asset has a documented module/shared target
- compile validation still passes
- no new ambiguous top-level legacy folder is created as a temporary dump zone
