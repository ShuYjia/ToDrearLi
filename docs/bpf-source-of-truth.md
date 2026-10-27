# BPF Source Of Truth

## Repository Rules

- Root operating rules: AGENTS.md
- MCP endpoint: .mcp.json
- Current project baseline: docs/current-project-baseline.md
- PICO framework notes: docs/framework-pico-mainline-notes.md
- Unified logging and diagnostics: docs/bpf-logging-architecture.md, docs/bpf-logging-runbook.md

## Unity Entrypoints

- Startup scene: Assets/A_MRNet/Sence/01StartUp.unity
- Current business scenes are resolved from AGENTS.md, build settings, and live Unity evidence.
- Treat Assets/A_MRNet/Sence/02SingleplayerScene.unity as a retained reference unless explicitly targeted.

## Business Data

- Business node Prefabs: Assets/Resources/BusinessPrefabs
- Topic Prefabs: Assets/Resources/TopicPrefabs
- TexCore score authority: docs/texcore-module-score-data.xlsx

## Legacy Boundaries

- Assets/APP/OldMRTK is a historical or explicitly targeted legacy area.
- Assets/APP/Modules/LegacyMrtkSync is legacy-only unless a migration task targets it.
- Do not infer current business behavior from archived networking scenes.

## Generated Data

- Assets/StreamingAssets/Config.xml is generated output.
- Fix source Prefabs, scene data, or generator logic before regenerating config.
