# BPF Git Delivery Policy

Use the current branch by default.

Do not:

- create a new branch unless the user asks
- stash or overwrite user changes without permission
- stage unrelated Unity auto-saves
- push without explicit user acceptance
- claim remote delivery after a failed transport

Before committing:

1. Run git status --short.
2. Review staged diff.
3. Run git diff --cached --check.
4. Run git lfs fsck when scenes, Prefabs, models, textures, or other LFS files are touched.
5. Use a scoped conventional commit message.

Any authorized file change must end in a local scoped commit before completion
is reported. If the commit cannot be made safely, report the blocker and
affected paths instead of leaving the agent's changes silently uncommitted.
