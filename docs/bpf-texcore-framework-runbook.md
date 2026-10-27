# BPF TexCore Framework Runbook

## Business Flow

1. Locate business node Prefab.
2. Verify Aa_Node.selfDatas.
3. Verify ordered Aa_Node.childData.
4. Verify topic Prefabs exist.
5. Verify topic data, including Panel_Select order when applicable.

## Scene Step Tree

1. Locate scene TexCore_StepObj.
2. Verify node/topic/step tree.
3. Verify each step object has appropriate TexCore interaction component.
4. Verify continuous stepIndex.
5. Verify colliders, targets, and animation dependencies when flags require them.

## Score Work

Use docs/texcore-module-score-data.xlsx before making score assumptions.

Separate:

- question score
- module score
- coin or reward account
- completion reward

Do not infer score mapping from row number alone.
