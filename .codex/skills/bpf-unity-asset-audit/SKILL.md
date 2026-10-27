---
name: bpf-unity-asset-audit
description: Audit BPF Unity assets, scenes, Prefabs, missing scripts, missing GUIDs, material slots, Renderer state, legacy asset ownership, UI buttons, and current-vs-historical asset usage. Use for requests asking to inspect assets, all Prefabs, all buttons, purple objects, missing references, or OldMRTK and legacy folders.
---

# BPF Unity Asset Audit

## Scope

Audit only the requested path unless the user asks for a broader sweep.

## Checklist

1. Resolve exact target paths.
2. Count scenes, Prefabs, and assets in scope.
3. Check missing scripts.
4. Check missing GUIDs.
5. Check Renderer and material slots.
6. Check button hierarchy, text or icon, and callbacks when UI is in scope.
7. Separate active, hidden, disabled, and no-geometry objects.
8. Report current-business versus legacy ownership.

## Classification

Classify each finding as:

- confirmed current runtime issue
- serialized structure issue
- hidden or inactive risk
- legacy-only issue
- unresolved product decision

## References

- references/asset-ownership.md
- references/oldmrtk-boundary.md
- references/missing-reference-rules.md
