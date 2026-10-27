# FBX Material Slot Restore

When Prefab or scene Renderer.sharedMaterials slots are null:

1. Locate the source FBX or Model Prefab.
2. Match by Mesh identity, renderer path, and material slot index.
3. Use only unique unambiguous candidates.
4. Leave no-geometry Renderers unfilled unless they draw at runtime.
5. Reopen or rescan after saving to confirm slot persistence.

Never assign arbitrary Standard material as a cosmetic patch.
