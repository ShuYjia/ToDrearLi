---
name: bpf-texcore-framework
description: Work with BPF TexCore business flow, node/topic Prefabs, scene step trees, scoring, generated config, and framework compliance. Use when tasks mention TexCore, Aa_Node, childData, StepObj, stepIndex, Panel_Select, score table, Config.xml, framework steps, or level validation.
---

# BPF TexCore Framework

## Scope

Use this skill for TexCore structure, level flow, step tree, scoring, and framework compliance.

## Required Checks

1. Locate the business node Prefab.
2. Verify Aa_Node.selfDatas.
3. Verify Aa_Node.childData.
4. Locate topic Prefabs.
5. Verify Panel_Select or operation topic data.
6. Locate scene TexCore_StepObj.
7. Verify node/topic/step nesting.
8. Verify stepIndex continuity.
9. Compare against generated config.
10. For score work, read the score workbook first.

## Hard Rules

- Do not hand-edit Assets/StreamingAssets/Config.xml.
- Do not invent step mapping from document numbering.
- Do not equate exam score with module points or coin balance.
- Keep intro videos and non-scored prefaces separate from scored checkpoints.

## References

- references/texcore-step-tree.md
- references/texcore-business-flow.md
- references/texcore-score-rules.md
- references/config-generation-rules.md
