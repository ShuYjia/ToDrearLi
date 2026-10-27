# BPF Git Delivery Runbook

## Preflight

1. Run git fetch --all --prune.
2. Inspect git status --short.
3. Compare local main and origin/main according to AGENTS.md.
4. Identify task-owned files before staging.

## Staging

Stage only files touched for the task.

Usually include:

- .cs
- .prefab
- .unity
- .meta
- focused .md
- focused tooling scripts

Usually exclude unless requested:

- Library/
- Logs/
- UserSettings/
- Word lock files
- unrelated Unity auto-saves

## Verification

Run git diff --cached --check before commit.

Run git lfs fsck when Unity scenes, Prefabs, models, media, or binary docs are staged.

## Commit

Use conventional commit format:

type(optional-scope): imperative summary

Push or PR only after explicit user acceptance.
