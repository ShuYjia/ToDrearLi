# BPF 工程内 Codex Skills

这是本工程 skill 的唯一索引和维护入口。所有面向
`github_gitea_bpf_714` 的新 skill、修改和配套资源都必须直接放在
`.codex/skills/<skill-name>/`；每个目录至少包含 `SKILL.md`，并随需保留
`agents/`、`references/`、`scripts/`、`assets/` 等依赖文件。

用户级 `C:\Users\admin\.codex\skills` 不是本工程的来源真相。需要兼容其他
工程时可以存在外部副本，但 BPF 工作流应优先读取本目录；新增、删除、重命名
或改变适用范围时必须同步更新本索引。

## Skill 索引

| Skill | 工程内路径 | 适用范围 |
| --- | --- | --- |
| `bpf-engineering-agent` | [`bpf-engineering-agent/`](./bpf-engineering-agent/) | BPF 工程总路由、证据标准、任务分流与验证门禁 |
| `bpf-unity-workflow` | [`bpf-unity-workflow/`](./bpf-unity-workflow/) | 当前业务节点、Topic、步骤树与资产归属 |
| `bpf-texcore-framework` | [`bpf-texcore-framework/`](./bpf-texcore-framework/) | TexCore 节点/步骤/评分/Config 与框架合规 |
| `bpf-unity-asset-audit` | [`bpf-unity-asset-audit/`](./bpf-unity-asset-audit/) | 场景、Prefab、GUID、材质、按钮和新旧资产审计 |
| `bpf-unity-material-repair` | [`bpf-unity-material-repair/`](./bpf-unity-material-repair/) | 紫色材质、Shader、FBX 材质槽诊断与修复 |
| `bpf-pico-export-install` | [`bpf-pico-export-install/`](./bpf-pico-export-install/) | Android/PICO 导出、安装、启动和 ADB 验证 |
| `bpf-git-delivery` | [`bpf-git-delivery/`](./bpf-git-delivery/) | `03` 分支、dirty worktree、LFS、提交与 PR 交付 |
| `bpf-docs-and-config` | [`bpf-docs-and-config/`](./bpf-docs-and-config/) | 文档、源配置、评分数据和工程运行说明维护 |
| `bpf-prune-unity-scene-scripts` | [`bpf-prune-unity-scene-scripts/`](./bpf-prune-unity-scene-scripts/) | 场景子树脚本清理与 YAML/LFS 安全 |
| `bpf-legacy-migration` | [`bpf-legacy-migration/`](./bpf-legacy-migration/) | OldMRTK、LegacyMrtkSync 与历史资产迁移边界 |
| `unity-mcp-orchestrator` | [`unity-mcp-skill/`](./unity-mcp-skill/) | 本工程锁定的 Unity MCP 操作与资源优先工作流 |
| `unity-intro-video-gate` | [`unity-intro-video-gate/`](./unity-intro-video-gate/) | Unity 介绍视频播放、跳过和首步门控 |
| `bpf-legacy-scene-analysis` | [`bpf-legacy-scene-analysis/`](./bpf-legacy-scene-analysis/) | 超大旧场景 YAML 解析、Unity MCP 裸客户端与新旧关卡步骤核对 |

## 维护约定

- 复制或迁移 skill 时保留完整目录树，不得只复制 `SKILL.md`。
- `unity-mcp-skill` 的目录名保持与 Unity MCP 安装约定一致；其
  `SKILL.md` 的规范名称是 `unity-mcp-orchestrator`。
- `netcode-ngo-project` 当前没有 `SKILL.md`，不属于本工程索引；其他通用
  skill 只有在明确成为 BPF 工作流依赖后才加入，并记录适用范围。
