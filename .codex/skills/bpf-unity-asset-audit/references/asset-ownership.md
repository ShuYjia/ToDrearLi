# Asset Ownership

Current business work should prefer:

- Assets/A_MRNet
- Assets/Resources/BusinessPrefabs
- Assets/Resources/TopicPrefabs
- current module roots that exist directly under Assets

Treat these as legacy or historical unless explicitly targeted:

- Assets/APP/OldMRTK
- Assets/APP/Modules/LegacyMrtkSync
- Assets/APP/ScenesNGOsync
- legacy networking scenes such as Init, Lobby, UDPLobby, MRTKsync

Do not infer current runtime behavior from legacy folders.
