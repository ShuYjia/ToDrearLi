# BPF Validation Gates

## Read-Only Audit

Pass criteria:

- Target path identified.
- Evidence source stated.
- No file modifications.
- Static evidence and runtime evidence separated.
- Uncertainty explicitly listed.

## Unity Scene Or Prefab Change

Pass criteria:

- Unity MCP checked when applicable.
- Scene or Prefab saved only when intended.
- Missing scripts checked.
- Relevant Renderer, Collider, UI callback, and hierarchy checks run.
- Console checked for errors.

## TexCore Flow Change

Pass criteria:

- Aa_Node.childData matches business order.
- Topic Prefabs exist.
- TexCore_StepObj has node/topic/step hierarchy.
- stepIndex is continuous.
- Config is regenerated or verified; generated config is not hand-edited.

## Material Repair

Pass criteria:

- Pipeline identified.
- shader == null checked.
- Hidden/InternalErrorShader checked.
- shader.isSupported checked.
- Active drawable null slots checked.
- Prefab preview or scene verification performed when possible.

## PICO Build

Pass criteria:

- Batch project used.
- APK built.
- Device detected by adb devices -l.
- APK installed.
- App launched.
- User acceptance requested.

## Git Delivery

Pass criteria:

- Dirty worktree reviewed.
- Only task-owned files staged.
- git diff --cached --check passes.
- git lfs fsck passes when LFS assets are touched.
- Local commit created.
- Push or PR only after explicit acceptance.
