# Dirty Worktree Policy

Before editing or staging:

1. Run git status --short.
2. Identify user-owned existing changes.
3. If synchronization or merge would overwrite dirty files, stop and report paths.
4. Do not stash, restore, reset, or include unrelated changes without explicit instruction.

Unity auto-saves must be treated as real user changes until proven task-owned.
