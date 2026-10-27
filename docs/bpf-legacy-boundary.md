# BPF Legacy Boundary

## Legacy Areas

Treat these as legacy unless explicitly targeted:

- Assets/APP/OldMRTK
- Assets/APP/Modules/LegacyMrtkSync
- Assets/APP/ScenesNGOsync
- old networking scenes such as Init, Lobby, UDPLobby, and MRTKsync

## Reusable From Legacy

Potentially reusable:

- models
- meshes
- materials
- textures
- audio
- target transforms
- prompt text

Not reusable by default:

- multiplayer carriers
- Mirror networking
- legacy scene flow managers
- SycObj
- LoadSceneObj

## Migration Rule

Extract assets and rebuild current TexCore bindings under current ownership paths. Do not copy legacy runtime mechanics into the mainline without a concrete migration decision.
