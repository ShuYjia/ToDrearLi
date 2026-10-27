# BPF Validation Matrix

Use this matrix to decide when a task is complete.

| Task Type | Required Checks | Completion Signal |
| --- | --- | --- |
| Read-only audit | target path, evidence source, no file changes | findings classified with uncertainty |
| Unity scene change | Unity MCP when applicable, hierarchy, console, relevant components | scene saved intentionally and re-read |
| Prefab change | Prefab Mode or AssetDatabase check, missing scripts, references | Prefab saved intentionally and re-scanned |
| TexCore flow | Aa_Node childData, topic Prefabs, StepObj tree, stepIndex | generated config/source alignment verified |
| Score work | score workbook, scene-action mapping, question vs module points | no invented stepIndex or score semantics |
| Material repair | pipeline, shader status, null material slots, active drawable slots | no bad shader and no unhandled active drawable null slots |
| PICO build | batch project, Gradle/APK, adb device, install, launch | user can test launched package |
| Git delivery | dirty worktree, staged diff, diff check, LFS check | local commit created; push waits for acceptance |

## Evidence Labels

Use these labels in reports:

- confirmed runtime issue
- serialized structure issue
- deterministic script finding
- hidden or inactive risk
- legacy-only issue
- unverified hypothesis
