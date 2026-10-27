# BPF Agent Operating Model

This document defines the repository-level operating model for engineering agents working on the BPF Unity/TexCore/PICO project.

## Layers

The engineering agent system has four layers:

1. AGENTS.md stores repository hard rules.
2. Repository-local Codex skills under
   [`.codex/skills/`](../.codex/skills/README.md) store reusable agent
   workflows; that directory's README is the canonical index.
3. docs/ stores project facts, runbooks, and human-readable source-of-truth notes.
4. Tooling/agent/ stores deterministic checks that agents can run before editing or delivery.

## Default Flow

For every non-trivial task:

1. Classify the task.
2. Protect the current worktree.
3. Gather evidence from source files, Unity MCP, scripts, or logs.
4. Make only the narrow authorized change.
5. Run the matching validation gate.
6. Commit local repository changes when task delivery rules require it.
7. Push or create PR only after explicit user acceptance.

## Task Categories

The complete skill catalog and maintenance policy live in the
[工程内 skill index](../.codex/skills/README.md). The table below is a concise
routing summary and must not become a second source of truth.

| Category | Primary Skill | Typical Scope |
| --- | --- | --- |
| General routing | [`bpf-engineering-agent`](../.codex/skills/bpf-engineering-agent/) | classify task, choose workflow, enforce gates |
| Unity business flow | [`bpf-unity-workflow`](../.codex/skills/bpf-unity-workflow/) | scenes, Prefabs, nodes, topics, step trees |
| TexCore framework | [`bpf-texcore-framework`](../.codex/skills/bpf-texcore-framework/) | Aa_Node, childData, StepObj, scoring, Config.xml |
| Asset audit | [`bpf-unity-asset-audit`](../.codex/skills/bpf-unity-asset-audit/) | missing scripts, GUIDs, materials, UI callbacks |
| Material repair | [`bpf-unity-material-repair`](../.codex/skills/bpf-unity-material-repair/) | purple objects, shader/pipeline, FBX material slots |
| PICO delivery | [`bpf-pico-export-install`](../.codex/skills/bpf-pico-export-install/) | batchbuild, APK, adb install, launch |
| Git delivery | [`bpf-git-delivery`](../.codex/skills/bpf-git-delivery/) | stage, commit, LFS, push/PR handoff |
| Legacy migration | [`bpf-legacy-migration`](../.codex/skills/bpf-legacy-migration/) | OldMRTK provenance, current ownership boundary |
| Docs/config | [`bpf-docs-and-config`](../.codex/skills/bpf-docs-and-config/) | source-of-truth docs, generated config rules |
| Scene script pruning | [`bpf-prune-unity-scene-scripts`](../.codex/skills/bpf-prune-unity-scene-scripts/) | scoped MonoBehaviour cleanup with YAML/LFS safety |
| Unity MCP operations | [`unity-mcp-orchestrator`](../.codex/skills/unity-mcp-skill/) | Unity Editor MCP resources and operations |
| Introduction video gate | [`unity-intro-video-gate`](../.codex/skills/unity-intro-video-gate/) | skippable video gating before first content |

## Guardrails

- Do not infer runtime behavior from object names alone.
- Do not treat legacy folders as current runtime proof.
- Do not hand-edit generated Assets/StreamingAssets/Config.xml.
- Do not convert render pipelines unless pipeline mismatch is proven.
- Do not push or create a PR without explicit acceptance.
