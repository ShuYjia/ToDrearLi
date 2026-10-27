---
name: bpf-engineering-agent
description: General engineering agent workflow for the BPF Unity/TexCore/PICO repository. Use for BPF tasks involving Unity scenes, Prefabs, TexCore business flow, scoring, PICO builds, Git delivery, legacy migration, documentation, diagnostics, validation, or repository-wide engineering coordination.
---

# BPF Engineering Agent

## Mission

Handle BPF engineering work with evidence-first, narrow-scope, reversible changes.

Separate each request into:

1. read-only audit
2. implementation
3. Unity validation
4. PICO/APK validation
5. Git delivery
6. PR/release handoff

## Preflight

Before repository work:

1. Inspect the current branch and dirty worktree.
2. Follow root `AGENTS.md` synchronization rules before changing repository files.
3. Identify whether the task is read-only or edit-authorized.
4. If Unity construction, scenes, Prefabs, runtime assets, PICO, builds, or exports are involved, probe Unity MCP and report the required status line.
5. Preserve existing user changes; never overwrite unrelated dirty files.

## Task Router

Choose one primary task type:

- Unity scene or Prefab workflow -> use `bpf-unity-workflow`.
- TexCore framework, business flow, step tree, config, or scoring -> use `bpf-texcore-framework`.
- Asset audit, missing scripts, missing GUIDs, buttons, materials, or legacy asset inspection -> use `bpf-unity-asset-audit`.
- Purple material, shader, pipeline, or FBX material slot restoration -> use `bpf-unity-material-repair`.
- PICO export, install, launch, or APK verification -> use `bpf-pico-export-install`.
- Git staging, commit, push, PR, or delivery -> use `bpf-git-delivery`.
- Legacy migration or OldMRTK provenance -> use `bpf-unity-workflow` plus `references/source-of-truth-map.md`.

If multiple types apply, handle them in dependency order:

1. source-of-truth check
2. asset or scene diagnosis
3. implementation
4. Unity validation
5. PICO validation
6. Git delivery

## Evidence Rules

Do not infer from names alone.

For Unity:

- Prefer live Unity MCP evidence when connected.
- If MCP is unavailable, use serialized YAML, `.meta` GUIDs, project files, deterministic scripts, and Unity logs.
- Distinguish static structure from runtime behavior.

For TexCore:

- Verify `Aa_Node.childData`.
- Verify the `TexCore_StepObj` three-level tree.
- Verify `stepIndex` continuity.
- Do not hand-edit `Assets/StreamingAssets/Config.xml`.

For scoring:

- Use `docs/texcore-module-score-data.xlsx` as the score source of truth.
- Do not confuse question marks, module points, coins, and completion rewards.

For Git:

- Stage only task-owned files.
- Run diff and LFS checks before commit.
- After any authorized file change, validate and create a local scoped commit
  before reporting completion, asking for acceptance, or starting unrelated
  work.
- Never leave agent-created changes uncommitted when a safe scoped commit is
  possible.
- Do not push or create PR without explicit user acceptance.

## References

Read only what the task needs:

- `references/task-router.md` for trigger routing.
- `references/evidence-standards.md` for audit language.
- `references/validation-gates.md` for completion gates.
- `references/git-delivery-policy.md` for commit handoff.
- `references/unity-mcp-policy.md` for Unity MCP boundaries.
- `references/source-of-truth-map.md` for project facts.

## Completion

A task that changes files is not complete until the matching validation gate
passes and the task-owned changes are committed locally. If a safe commit is
impossible, report the concrete blocker and exact affected paths.
