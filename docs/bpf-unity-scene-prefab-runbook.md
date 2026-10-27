# BPF Unity Scene And Prefab Runbook

## Preflight

1. Identify exact scene or Prefab path.
2. Probe Unity MCP for Unity-related work.
3. Inspect current worktree before saving or editing.
4. Decide whether the task is read-only or edit-authorized.

## Inspection

For scenes:

- active scene and dirty state
- relevant hierarchy roots
- missing scripts
- key components
- console errors

For Prefabs:

- Renderer count
- material slot count
- missing scripts
- null references
- UI callbacks when relevant

## Save Rules

Save only changed scenes or Prefabs that are in scope.

Do not save unrelated Unity auto-changes.

## Reporting

Report:

- exact paths inspected
- evidence method
- changed files
- validation performed
- remaining risks
