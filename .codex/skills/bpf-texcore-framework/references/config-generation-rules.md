# Config Generation Rules

Do not hand-edit Assets/StreamingAssets/Config.xml.

When config is stale:

1. Fix source Prefab or scene data.
2. Regenerate config through the project mechanism.
3. Verify generated output.

If generation code is broken, fix the generator instead of patching generated XML.
