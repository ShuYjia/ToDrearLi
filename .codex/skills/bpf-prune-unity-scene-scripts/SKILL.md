---
name: bpf-prune-unity-scene-scripts
description: Safely remove selected MonoBehaviour scripts from a named GameObject subtree in BPF Unity scenes while preserving MRTK/XR/UI dependency closure and pre-existing dirty scene edits. Use for bulk scene-script cleanup, removing non-MR, legacy, networking, or ExportRuntime components, keeping only MR interaction code, cleaning hierarchies such as "无交互物体", validating Missing Script or dangling references, or committing task-only changes from a Git LFS-tracked .unity scene.
---

# BPF Unity Scene Script Pruning

Remove scripts from one scene subtree without changing its objects, breaking MR controls, or absorbing unrelated local scene edits into the task commit.

## Establish the Contract

Resolve these inputs before editing:

1. Identify the exact `.unity` scene.
2. Identify the target root by full hierarchy path, not name alone.
3. Translate terms such as "MR code only" into an explicit retain/remove policy.
4. Record whether the scene already contains uncommitted user changes.
5. Confirm whether prefab assets are in scope. Default to scene-instance changes only.

Treat "MR interaction code" as a functional dependency closure. Preserve the MRTK or XR components plus the UI, TMP, Graphics Tools, event, visual, and adapter components required for those controls to run and render.

## Follow the Safe Workflow

### 1. Capture the Baselines

Follow the repository's `AGENTS.md` preflight first. Then record the branch, `HEAD`, `git status --short`, LFS attributes, scene size, and scene hash.

If the target scene is already dirty:

- Copy its real contents to an out-of-tree backup and hash the backup.
- Preserve both a clean `HEAD` input and the dirty working-tree input.
- Never rely on an LFS pointer diff as proof that local scene changes are safe.
- Never use a destructive checkout or reset as the backup mechanism.

Use the clean input to build the task-only staged result. Use the dirty input to rebuild the user's working scene with the same cleanup applied.

### 2. Inventory the Target Subtree

Prefer Unity Editor or Unity MCP hierarchy and component APIs when connected. Otherwise, inspect the serialized YAML graph using [scene-yaml-lfs-safety.md](references/scene-yaml-lfs-safety.md).

Before removal, capture:

- the resolved root path and descendant GameObject count;
- direct scene objects versus prefab instances;
- every `MonoBehaviour` script GUID, asset path, namespace, class, and occurrence count;
- existing Missing Script entries and unresolved object references;
- references from retained components to candidate components.

Disambiguate duplicate object names through parent Transform relationships. Do not treat a text match for `m_Name` as a hierarchy boundary.

### 3. Classify Before Deleting

Create an evidence table with these columns:

`script GUID | asset path | assembly/namespace | class | count | dependencies | keep/remove | reason`

Apply these rules:

1. Resolve `m_Script` GUIDs through `.meta` files; do not classify by component display name alone.
2. Keep the MR dependency closure, including required Unity UI, TMP, XR, Graphics Tools, event, and visual helpers.
3. Remove only explicitly evidenced legacy namespaces, script paths, or classes.
4. Hold unknown scripts for inspection instead of deleting them by default.
5. Stop and inspect any candidate referenced by a retained component.
6. Keep object identity, hierarchy, transforms, renderer state, and prefab source assets unchanged unless separately requested.

For the proven `03SingleplayerSceneSYC/无交互物体` precedent, read [03singleplayer-scenesyc-case.md](references/03singleplayer-scenesyc-case.md). Treat its namespace decisions as precedent, not a universal allowlist.

### 4. Apply One Deterministic Transform

When Unity is connected, remove components through Unity serialization with Undo support, save the scene, and inspect the Console. Avoid Play mode for structural editing.

When Unity is unavailable, edit only complete YAML records and maintain all ownership/reference invariants described in the safety reference. Never use broad regex or line-range deletion.

For a dirty scene, apply the identical component-selection transform independently to:

- `T = clean HEAD scene + requested cleanup`;
- `C = original dirty scene + requested cleanup`.

Require the transform to be idempotent: a second run must report zero additional removals.

### 5. Validate Structurally and Semantically

Require all relevant checks to pass:

- target root resolves uniquely;
- GameObject count and hierarchy are unchanged;
- every approved removal class has zero instances under the target;
- retained MR/UI/TMP/XR component counts match the inventory;
- Missing Script count is zero, or unchanged only when explicitly accepted as pre-existing;
- dangling references to removed file IDs are zero;
- broken prefab instances and invalid overrides are zero;
- scene opens, saves, and reloads without Console errors;
- non-target serialized records are semantically unchanged; allow only proven serializer-only formatting or ordering changes;
- original dirty edits remain semantically identical after subtracting the cleanup;
- a second cleanup pass removes nothing.

Run the repository's normal APK/PICO verification when required. Report the exact build or device blocker when it cannot run.

### 6. Commit Only the Task Delta

If the scene started clean, stage the verified result normally.

If the same LFS scene started dirty:

1. Keep the hashed dirty backup until delivery is complete.
2. Stage `T` through the normal Git/LFS clean filter.
3. Restore `C` as the working-tree version immediately after staging.
4. Materialize the indexed LFS object into a temporary file and verify it matches `T`.
5. Verify the index contains only the cleanup and the remaining worktree delta represents only the user's original edits.
6. Commit only the target scene; never stage unrelated files.
7. Recheck the backup hash, branch, commit contents, and remaining `git status` after the commit.

Do not use `git add -p` on an LFS pointer as a substitute for this separation.

## Report Evidence

Report the exact scene and root, removed and retained component groups and counts, object count, Missing Script/dangling/prefab results, commit hash, and whether user edits remain uncommitted. Name any skipped Unity, build, or PICO validation and its concrete blocker.
