# BPF 文档索引与状态

> 状态：现行操作依据  
> 更新日期：2026-09-15

使用任何文档前先看状态。工程文件、场景序列化内容和构建配置始终是最终证据。

## 当前工程操作依据

- [项目 Code Wiki](../CODE_WIKI.md)
- [工程内 Codex skill 索引](../.codex/skills/README.md)
- [当前工程基线](./current-project-baseline.md)
- [BPF 场景名配置指南](./bpf-scene-name-configuration-guide.md)
- [BPF 前端协作开发流程](./bpf-frontend-collaboration-guide.md)
- [BPF UI 预制体复用规范](./bpf-ui-prefab-reuse-guide.md)
- [九步有序放置操作题构建指南](./bpf-nine-step-ordered-placement-build-guide.md)
- [有序放置视觉指引指南](./bpf-ordered-placement-guidance-guide.md)
- [BPF AI 协作开发流程](./bpf-ai-collaboration-workflow.md)
- [批工程构建流程](./batchbuild-project-workflow.md)
- [PICO 发布执行说明](./pico-readiness-audit.md)
- [PICO 构建运行手册](./bpf-pico-build-runbook.md)
- [GameFramework + TexCore PICO 说明](./framework-pico-mainline-notes.md)
- [BPF 统一日志与诊断系统架构](./bpf-logging-architecture.md)
- [BPF 日志与诊断运行手册](./bpf-logging-runbook.md)

## 静态核对快照

下列文档保留核对日期和当时提交。它们可解释源码行为，但其中的分支、提交、
连接状态和场景入口可能已经变化：

- [选择题配置源码核对结论](./bpf-selection-question-source-verification.md)

## 历史迁移与遗留联机资料

下列文档不定义当前业务路径：

- [资源整合后续计划](./asset-consolidation-next-steps.md)
- [资源迁移位置映射](./asset-migration-location-map.md)
- [旧 App 迁移清单](./legacy-app-migration-inventory.md)
- [Legacy MRTK Sync 迁移计划](./legacy-mrtk-sync-migration-plan.md)
- [SHIXUN 模块映射](./shixun-module-mapping.md)
- [`_syc_resource` 旧联机流程](./syc-resource-networking-flow.md)
- [单模块分支镜像工作流](./module-workspace-workflow.md)，当前暂停使用
- [PICO MR 透视链对照](./pico-mr-passthrough-chain-compare.md)，历史诊断快照
- [旧纯 OpenXR 基线](./openxr-cross-platform-mainline.md)，已被当前 PXR 基线取代

`docs/asset-compare/*.txt` 是生成式比较快照，只能与其生成时的源树一起解释。

## 上游 MRTK 归档

- [MRTK 上游 README 归档](./mrtk-upstream-readme-archive.md)
- `docs/mrtk3-overview/**`

这些资料用于框架参考，不定义当前 BPF 场景、目录或 PICO loader。

## Word/PDF 资料

分类见 [`docs/文档/README.md`](./文档/README.md)。其中业务模块文档和项目要求
主要描述需求或设计目标，不等同于已实现、已接入 Build Settings 或已通过 PICO
真机验收。
