---
name: bpf-legacy-migration
description: Investigate, plan, and execute BPF legacy asset migration from OldMRTK, LegacyMrtkSync, historical scenes, and old module roots into the current TexCore/BPF project. Use when tasks mention OldMRTK, legacy scripts, migration, historical modules, archived networking, source assets, or current-vs-legacy ownership.
---

# BPF Legacy Migration

## Scope

Use this skill to separate legacy provenance from current TexCore runtime.

## Rules

- Treat OldMRTK as historical unless explicitly targeted.
- Reuse models, prompt text, audio, and targets only after verifying current ownership.
- Do not copy multiplayer carriers, Mirror networking, SycObj wrappers, or legacy scene managers into current flow.
- Do not infer current behavior from archived scenes.
- When migrating, create or update current Prefabs/scenes under current ownership paths.

## References

- references/legacy-to-current-map.md
- references/oldmrtk-do-not-copy.md
- references/module-migration-checklist.md
