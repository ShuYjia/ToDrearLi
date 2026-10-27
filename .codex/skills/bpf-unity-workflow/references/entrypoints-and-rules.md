# BPF Entry Points And Rules

## Current Entry Points

- Startup scene: `Assets/A_MRNet/Sence/01StartUp.unity`
- Default business scene: `Assets/A_MRNet/Sence/03SingleplayerSceneSYC.unity`
- Additional enabled scene: `Assets/A_MRNet/Sence/04SingleplayerScene.unity`
- Retained reference scene, not enabled by default: `Assets/A_MRNet/Sence/02SingleplayerScene.unity`
- Business node prefabs: `Assets/Resources/BusinessPrefabs`
- Topic prefab templates: `Assets/Resources/TopicPrefabs`
- Current business ownership: the active roots that exist directly under `Assets/`, led by `Assets/A_MRNet`, `Assets/Resources/BusinessPrefabs`, and `Assets/Resources/TopicPrefabs`
- Legacy-only roots by default: `Assets/APP`, `Assets/App`, `Assets/Modules/LegacyMrtkSync`, and archived networking scenes

Before editing, verify these scene facts against
`docs/current-project-baseline.md`, `ProjectSettings/EditorBuildSettings.asset`,
`Assets/A_MRNet/Sence/01StartUp.unity`, and
`TexCore_LoadScene.BusinessSceneName`. Newer project files override this
reference when the project changes.

## Critical Runtime Classes

- `Aa_Node`
  - file: `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/NodeScripts/Node/Aa_Node.cs`
  - role: top-level business node flow data

- `Panel_Select`
  - file: `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/Topic/Panel_Select.cs`
  - role: selection-topic UI and answer parsing

- `TexCore_StepObj`
  - file: `Assets/A_MRNet/ProgramResources/ProgramResources/ObjState/TexCore_StepObj.cs`
  - role: scene step tree

- `TexCore_InteracterFunction`
  - file: `Assets/A_MRNet/ProgramResources/ProgramResources/ObjState/TexCore_InteracterFunction.cs`
  - role: per-object interaction state machine

## Hard Rules

1. Do not hand-edit `Assets/StreamingAssets/Config.xml`.
2. Keep prefab file names, data names, and `childData` names aligned.
3. Save prefab changes in Prefab Mode.
4. Save scene changes after step-tree or interaction edits.
5. Prefer copying the nearest working prefab or scene object.

## Common Failure Modes

- `childData` contains names that do not match actual topic prefab file names.
- `Panel_Select` data arrays are reordered.
- `rightTasks` uses labels like `A/B/C` instead of the real option names.
- `Item Parent` stores multiple generated option children.
- `stepIndex` does not match business order.
- Interaction object is listed in `stepObjs` but lacks a `TexCore_InteracterBase` subclass.
- An animation flag is enabled but the object has no valid animator setup.
- Node/topic list positions are guessed from names instead of generated-config and prefab sibling order.
