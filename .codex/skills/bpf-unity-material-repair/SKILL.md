---
name: bpf-unity-material-repair
description: Diagnose and repair Unity BPF material loss or purple objects in Assets/A_MRNet, Assets/APP/OldMRTK, and related Prefab/model assets. Use when checking missing materials, pink or purple renderers, shader pipeline mismatch, FBX embedded material slots, Material errors, or Prefab material restoration.
---

# BPF Unity Material Repair

## Scope

Use this skill for Unity material investigations and repairs in the BPF repository.

Target paths:

- Assets/A_MRNet
- Assets/APP/OldMRTK
- Assets/Resources
- concrete Prefab or scene paths named by the user

Do not treat Assets/APP/OldMRTK as current business flow unless the user explicitly targets legacy assets.

## Diagnosis Order

1. Confirm Unity MCP connection when Unity validation is in scope.
2. Refresh Git refs and inspect dirty worktree when repository files may change.
3. Identify exact target scene or Prefab paths.
4. Scan Renderers:
   - total renderer count
   - material slot count
   - null material slots
   - shader == null
   - Hidden/InternalErrorShader
   - shader.isSupported == false
   - URP/HDRP shader names in Built-in pipeline
5. Separate causes:
   - shader or pipeline incompatibility
   - missing material reference
   - disabled or hidden renderer
   - no mesh or no drawable geometry
6. Verify with Unity preview or Prefab Mode when needed.

## Repair Rules

- If Shader is bad, repair shader dependency first.
- If material slot is null, restore original material reference from source FBX, Model Prefab, or known source project.
- Do not bulk-convert pipeline unless evidence proves pipeline mismatch.
- Do not assign random default materials just to hide purple.
- Preserve .meta GUIDs whenever restoring assets.

## References

- references/purple-object-diagnosis.md
- references/fbx-material-slot-restore.md
- references/built-in-pipeline-rules.md
