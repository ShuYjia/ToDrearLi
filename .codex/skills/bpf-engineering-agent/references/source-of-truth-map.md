# BPF Source Of Truth Map

## Project Rules

- Root policy: AGENTS.md
- MCP endpoint: .mcp.json
- Current baseline: docs/current-project-baseline.md
- PICO framework notes: docs/framework-pico-mainline-notes.md

## Unity Mainline

- Startup scene: Assets/A_MRNet/Sence/01StartUp.unity
- Main business scene candidates:
  - Assets/A_MRNet/Sence/03SingleplayerScene.unity
  - Assets/A_MRNet/Sence/03SingleplayerSceneSYC.unity
- Additional enabled scene: Assets/A_MRNet/Sence/04SingleplayerScene.unity

## Business Prefabs

- Business nodes: Assets/Resources/BusinessPrefabs
- Topic Prefabs: Assets/Resources/TopicPrefabs

## TexCore

- Node flow: Aa_Node
- Topic selection: Panel_Select
- Step tree: TexCore_StepObj
- Interaction function: TexCore_InteracterFunction
- Score source: docs/texcore-module-score-data.xlsx

## Legacy Boundaries

- Assets/APP/OldMRTK is historical reference unless explicitly targeted.
- Assets/APP/Modules/LegacyMrtkSync is legacy-only.
- Do not infer current business runtime from archived networking scenes.

## PICO Build

Use the batchbuild project for APK export unless the user explicitly asks otherwise.
