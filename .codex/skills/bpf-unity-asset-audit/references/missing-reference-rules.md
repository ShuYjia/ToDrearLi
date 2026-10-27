# Missing Reference Rules

For missing GUIDs:

1. Resolve GUIDs through .meta files in Assets and Packages when possible.
2. Do not treat PackageCache misses as missing assets without checking package ownership.
3. Trace missing references from the component field where they occur.
4. Separate missing script references from missing material, animation, texture, audio, and prefab references.

For UI:

- inspect hierarchy
- inspect label or icon
- inspect callback/event binding
- do not infer button behavior from its label alone
