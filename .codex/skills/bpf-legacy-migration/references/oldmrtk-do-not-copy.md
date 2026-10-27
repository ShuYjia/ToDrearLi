# OldMRTK Do Not Copy

Do not migrate these by default:

- Mirror networking components
- LoadSceneObj
- SycObj
- old lobby or UDP flows
- multiplayer scene managers
- legacy-only input managers

Prefer extracting model assets, targets, prompts, and audio while rebuilding current TexCore bindings.
