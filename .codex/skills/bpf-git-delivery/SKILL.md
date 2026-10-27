---
name: bpf-git-delivery
description: Deliver BPF repository changes safely using the current branch, Git LFS checks, dirty-worktree protection, local commit, optional push, and PR creation only after user acceptance. Use when tasks mention commit, push, PR, branch, delivery, staging, all files, accepted changes, LFS, or remote verification.
---

# BPF Git Delivery

## Preflight

1. Run git fetch --all --prune.
2. Inspect current branch.
3. Inspect worktree and index.
4. Inspect nested gitlinks if present.
5. Identify task-owned changed files.

## Commit Rules

- Reuse current branch.
- Do not create a new branch unless explicitly requested.
- Stage only task-owned files.
- Run git diff --cached --check.
- Run git lfs fsck when Unity assets or LFS files are touched.
- After any authorized file change, commit task-owned files locally before
  reporting completion, requesting final acceptance, or starting unrelated
  work.
- Never leave agent-created changes uncommitted when a safe scoped commit is
  possible.
- If a scoped commit is blocked, report the exact blocker and affected paths;
  do not include unrelated user changes as a workaround.

## Push And PR Rules

- Push only after explicit user acceptance.
- Confirm remote delivery with remote refs.
- Report transport failure as not delivered.

## References

- references/dirty-worktree-policy.md
- references/lfs-checklist.md
- references/commit-pr-policy.md
