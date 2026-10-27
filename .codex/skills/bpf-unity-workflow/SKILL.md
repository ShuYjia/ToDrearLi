---
name: bpf-unity-workflow
description: Guide current BPF business workflow development in the active Unity repository, including business-node prefabs, topic prefabs, scene step trees, and current asset ownership under Assets/A_MRNet and the business roots that actually exist directly under Assets. Use for requests mentioning BPF workflow configuration, Aa_Node, Panel_Select, BusinessPrefabs, TopicPrefabs, TexCore_StepObj, or TexCore_InteracterBase.
---

# BPF Unity Workflow

## Overview

Use this skill to keep BPF work inside the real project ownership model instead of wandering through legacy MRTK content. Treat it as a repo navigation and execution guide for business workflow configuration, not as a generic Unity tutorial.

## Start Here

Confirm the task type before editing anything:

1. Business node or sub-topic flow configuration
2. Topic UI configuration
3. Scene step-tree or 3D interaction configuration
4. Module ownership or file placement under the current `Assets/...` layout
5. Shared framework or legacy runtime investigation

If the request is only about BPF business content, read:

- `docs/current-project-baseline.md`
- `docs/bpf-frontend-collaboration-guide.md`
- `docs/shixun-module-mapping.md`

Read `references/entrypoints-and-rules.md` for the current entrypoints and hard rules.
Read `references/module-map.md` only when the request involves a specific training module, historical provenance, or an explicit migration task.

## Core Rule Set

Follow these project rules first:

1. Modify prefabs and scenes, not handwritten `Assets/StreamingAssets/Config.xml`.
2. Keep names aligned across prefab file names, data references, and `childData` entries.
3. Save every changed prefab and every changed scene immediately.
4. Prefer copying an existing working node, topic, or scene object over building one from scratch.
5. Keep edits scoped to the requested business module. Do not refactor unrelated legacy MRTK content.

## Workflow Decision Tree

### Business Flow Prefab Work

Use this path when the task mentions node flow, course structure, topic order, or selection topics.

No dedicated level-step builder Skill is currently active. Inspect the target scene, generated config, and business prefabs directly before proposing scene-step changes.

Primary locations:

- `Assets/Resources/BusinessPrefabs`
- `Assets/Resources/TopicPrefabs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/NodeScripts/Node/Aa_Node.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/Topic/Panel_Select.cs`

Execution pattern:

1. Open `Assets/A_MRNet/Sence/03SingleplayerSceneSYC.unity` unless the request clearly targets another scene. Treat `02SingleplayerScene.unity` as a retained reference, not the default business scene.
2. Locate the existing business node prefab under `Assets/Resources/BusinessPrefabs`.
3. Copy the closest existing prefab rather than creating one from zero.
4. For `Aa_Node`, verify:
   - `selfDatas[0].sceneData`
   - `childData`
5. Fill `childData` with ordered child topic names, not dragged object references.
6. When adding a selection topic, start from the selection-topic prefab template under `Assets/Resources/TopicPrefabs` or from an existing sibling topic prefab.
7. For `Panel_Select`, preserve `selfDatas[0]` as base topic data and `selfDatas[1]` as selection-rule data.
8. For multi-select answers, join the real option names with ASCII underscore, for example `OptionA_OptionC`.

### Scene Step Tree And Interaction Work

Use this path when the task mentions steps, highlighting, click order, `stepIndex`, drill actions, target transforms, or scene interaction bugs.

Primary locations:

- `Assets/A_MRNet/ProgramResources/ProgramResources/ObjState/TexCore_StepObj.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/ObjState/TexCore_InteracterFunction.cs`
- current business scenes under `Assets/A_MRNet/Sence` or the concrete current asset root named by the task

Execution pattern:

1. Identify the owning business scene first.
2. Find the scene object with `TexCore_StepObj`.
3. Treat `interacterObjs` as a strict three-level tree:
   - `NodeObjs`
   - `TopicObjs`
   - `StepObjs`
4. Match each level to the actual business flow one by one.
5. Derive node/topic indices from generated config and prefab `childData`; require continuous step list indices equal to `stepIndex`.
6. Require a `TexCore_InteracterBase` subclass. Require a collider only when the interaction path uses click or physics input.
7. If `TexCore_InteracterFunction` uses extra flags, verify the paired dependency:
   - `isSetState` needs a compatible selection-state script
   - `isPlayAnimation` needs a valid `Animator` and animation name
   - `isTarget` needs a target `Transform`

### Module Ownership And File Placement

Use this path when the request is about where code or assets should live, or when historical folders such as `WHY`, `JYJ`, `PQ`, `ZDP`, `LSL` appear.

Inspect the current `Assets/...` tree before choosing ownership. Treat `Assets/App`, `Assets/APP`, `Assets/Modules/LegacyMrtkSync`, and archived networking scenes as legacy unless a concrete migration task targets them.

Use `references/module-map.md` to understand historical ownership or an explicitly requested migration target. Do not treat its `Assets/App/...` targets as current paths.

Default placement rule:

1. Prefer `Assets/A_MRNet`, `Assets/Scripts`, `Assets/Textures`, `Assets/Animations`, `Assets/Audio`, `Assets/Resources/BusinessPrefabs`, and `Assets/Resources/TopicPrefabs`.
2. Use a module-named root directly under `Assets/` only when it exists and owns the content.
3. Leave legacy and upstream MRTK areas alone unless explicitly targeted.

## Practical Search Order

When starting a task, search in this order:

1. `Assets/A_MRNet`
2. `Assets/Resources/BusinessPrefabs` and `Assets/Resources/TopicPrefabs`
3. current module-named roots directly under `Assets/`
4. `docs/`
5. legacy folders only when the current layout is insufficient or the task targets migration

Use `rg` to find business terms, legacy scene names, module names, and script class names before opening files broadly.

## Validation Checklist

Run a manual check before finishing:

1. Node names, topic names, and `childData` references match exactly.
2. Changed prefabs are saved.
3. Changed scenes are saved.
4. `Panel_Select` keeps the expected `selfDatas` order.
5. `Item Parent` keeps only one option template child.
6. Multi-select `rightTasks` uses ASCII underscore.
7. `stepIndex` matches the real step order.
8. Added or moved Unity assets include their `.meta` files.
9. Changes intended to affect PICO behavior are tested in the connected Unity Editor first. Ask whether an APK is needed; do not export, install, or launch one until the user explicitly confirms it for the current change.

## Git Scope

For routine BPF workflow tasks, the expected staged files are usually:

- `.prefab`
- `.unity`
- matching `.meta`
- focused docs updates

Usually exclude:

- `Library/`
- `Logs/`
- `UserSettings/`

Question unexpected changes to `Packages/packages-lock.json` before including them.

## Response Style For This Skill

When using this skill:

1. State which task type the request belongs to.
2. Name the exact directories and classes you will inspect.
3. Prefer the existing template or sibling module implementation.
4. Explain validation in terms of prefab names, scene state, step-tree alignment, and module ownership.
