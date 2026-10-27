# Unity Scene YAML and Git LFS Safety

Read this reference when Unity Editor APIs are unavailable or when a dirty LFS-tracked scene must produce a task-only commit.

## Serialized Graph Model

Treat a text-serialized Unity scene as records separated by headers such as:

```text
--- !u!1 &100       GameObject
--- !u!4 &101       Transform
--- !u!224 &102     RectTransform
--- !u!114 &103     MonoBehaviour
--- !u!1001 &104    PrefabInstance
```

Build indexes before changing anything:

- `GameObject fileID -> component fileIDs`
- `Transform/RectTransform fileID -> GameObject fileID + parent Transform fileID`
- `MonoBehaviour fileID -> GameObject fileID + script GUID`
- `script GUID -> .meta path -> class/namespace/assembly`
- `removed fileID -> every reference location in the scene`

Resolve the subtree by finding the exact root GameObject and recursively following parent Transform links. Require one root match.

## Direct Component Removal Invariants

For each direct scene `MonoBehaviour` removal:

1. Remove the complete `!u!114` record.
2. Remove its fileID from the owning GameObject's `m_Component` list.
3. Search the entire scene for every remaining reference to that fileID.
4. Clear or update a reference only when its serialized meaning is known.
5. Fail validation if any unexplained reference remains.

Preserve record delimiters, line endings, encoding, unrelated field order, and all non-target records. Reject a transform that changes GameObject, Transform, or RectTransform counts.

## Prefab Boundaries

Do not delete a component record as though a prefab-origin component were a direct scene component. A prefab instance may encode removal through `m_RemovedComponents`, added components, or property modifications.

Prefer Unity serialization for prefab-origin components. If offline handling cannot prove the correct override representation, stop instead of changing the source prefab or emitting a malformed instance.

## LFS Dual-Baseline Model

Use these symbols:

- `B`: clean scene materialized from `HEAD` through the configured LFS object/filter;
- `U`: original dirty working scene, copied outside the repository and hashed;
- `F(x)`: deterministic requested cleanup;
- `T = F(B)`: task-only scene to stage;
- `C = F(U)`: combined scene to leave in the working tree.

Run the same selector and transform for `B` and `U`. Compare their removal manifests by stable object hierarchy, component fileID, and script GUID. Investigate any mismatch.

Stage `T` using normal Git so `.gitattributes` and the LFS clean filter produce the correct index object. Restore `C` to the worktree afterward. An LFS pointer diff shows only object ID and size, so validate the materialized scene contents and manifests as well as Git metadata.

After staging, read the scene from the index, pass its LFS pointer through the configured smudge/materialization path into a temporary file, and compare that file's hash and manifest with `T`. Also require `git diff --cached --name-only` to contain only the intended task files.

After committing `T`, confirm that the worktree still contains the semantic equivalent of the original `U - B` user delta. Keep `U` until this check passes.

## Minimum Manifests

Persist or print these machine-checkable summaries during the run:

- root hierarchy path and descendant IDs;
- before/after object and component counts;
- removed component fileID, GameObject path, GUID, class, and reason;
- retained component counts by GUID/class;
- references found and how each was resolved;
- hashes for `B`, `U`, `T`, and `C`;
- first-pass and second-pass removal counts.

Treat zero removals on the second pass as the idempotence gate.
