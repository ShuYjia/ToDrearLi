# `03SingleplayerSceneSYC` Cleanup Precedent

Use this precedent only for the same scene/root or to interpret the project's
established meaning of "keep MR interaction code only".

## Request and Scope

- Scene: `Assets/A_MRNet/Sence/03SingleplayerSceneSYC.unity`
- Target root: `无交互物体` (resolve by its full Hierarchy path before editing)
- Request: remove all non-MR scripts below the target while keeping MR
  interaction and every runtime/display dependency those controls require.
- Boundary: remove scene-component instances only. Do not delete `.cs` source
  files, GameObjects, prefab source assets, transforms, or unrelated scene edits.

## Proven Classification

Remove `NetWork03_2.ExportRuntime` components. The removed group covered legacy
synchronization, tasks, movement, outline, and scene-binding behavior.

Keep MRTK/Mixed Reality, XR, Graphics Tools, Unity UI, and TMP components that
are needed for interaction, events, visuals, text, or rendering. "MR only" is
therefore a dependency closure, not a filename or namespace text filter.

Hold any other script for reference analysis; do not delete it solely because it
does not contain `MR` in its name.

## Recorded Result

- Removed `1,481` `NetWork03_2.ExportRuntime` component instances.
- Retained `2,876` MRTK/XR/GraphicsTools/UI/TMP component instances.
- Preserved all `1,430` GameObjects.
- Found `0` Missing Script entries, `0` broken prefab instances, and `0`
  dangling references.
- Preserved `17` pre-existing scene parameter edits as uncommitted user work.
- User completed Unity acceptance.

Delivery commit: `2f382f3 fix(scene): remove non-MR scripts from static objects`.

## Required Reuse Checks

1. Inventory script GUIDs and retained-component references before removal.
2. Remove only the classified `NetWork03_2.ExportRuntime` instances.
3. Re-run the transform and require zero additional removals.
4. Verify the object count, retained dependency counts, Missing Script count,
   broken prefab count, and dangling-reference count.
5. Keep any unrelated dirty scene edits out of the commit.

## Historical APK/PICO Limitation

APK/PICO verification did not run because the batch project then targeted a
different legacy `Assets/APP` resource tree, its
`EditorBuildSettings.asset` had unresolved conflict markers, and no ADB device
was connected. Recheck these conditions for later work; they are not permanent
exemptions.
