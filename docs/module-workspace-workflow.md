# BPF 单模块分支镜像工作流

> 状态：暂停使用。当前 `Tooling/ModuleWorkspace/module-workspaces.json`
> 仍指向不存在的 `Assets/App/Modules/*`，运行
> `Test-ModuleWorkspaceIsolation.ps1` 会因模块根目录缺失而失败。必须先按当前
> `Assets/...` 布局重建清单并让隔离测试通过，才能重新启用本工作流。本文以下
> 内容仅保留历史设计。见
> [current-project-baseline.md](./current-project-baseline.md)。

本工作流把 `gitea_BPF` 保持为集成主仓，同时新增一个“模块分支镜像仓”。

这个镜像仓不承担集成，不作为主开发主线，只承担两件事：

- 给单模块开发者发放隔离工程
- 把该模块分支上的结果回灌到 `gitea_BPF`

当前实现落在：

- `Tooling/ModuleWorkspace/module-workspaces.json`
- `Tooling/ModuleWorkspace/ModuleWorkspace.Common.ps1`
- `Tooling/ModuleWorkspace/New-ModuleWorkspace.ps1`
- `Tooling/ModuleWorkspace/Update-ModuleWorkspaceBaseline.ps1`
- `Tooling/ModuleWorkspace/Sync-ModuleWorkspaceBack.ps1`
- `Tooling/ModuleWorkspace/Publish-ModuleBranchMirror.ps1`
- `Tooling/ModuleWorkspace/Import-ModuleBranchMirrorBack.ps1`
- `Tooling/ModuleWorkspace/Test-ModuleWorkspaceIsolation.ps1`

## 目标

- `gitea_BPF` 继续承担集成、合并、总体验证和最终提交。
- 新远端模块仓只保留模块分支，不保留集成开发主线。
- 每个模块分支都是一个可以独立打开 Unity 的工程快照。
- 模块开发者只拿到自己的模块分支，不看主仓，也不看其他模块内容。
- 主仓可以重新发布模块分支基线。
- 模块分支的改动可以白名单回灌回主仓。

## 仓库角色

### 1. 集成主仓

就是当前仓：

- `D:\gitea_obj\_unity projection\gitea_BPF`

职责：

- 管理真实业务代码和资产
- 跑完整集成验证
- 处理跨模块资源迁移
- 维护模块清单、导出脚本、回灌脚本

### 2. 模块分支镜像仓

建议新建一个单独远端仓，例如：

- `git_BPF_modules.git`

职责：

- 只存模块快照分支
- 不做集成主线开发
- 不接受跨白名单改动

建议分支命名固定为：

- `module/NetworkCabinetWiring`
- `module/ServerAssembly`
- `module/RmsInstall`
- `module/RmsImport`
- `module/LayoutMarking`
- `module/RackAssembly`
- `module/DisplayWallAssembly`
- `module/ChargingStationAssembly`
- `module/AgvCommissioning`
- `module/RackPlacement`

## 关于“删掉主分支”

Git 远端仓不能处于“完全没有任何默认分支”的稳定状态。实际可执行做法是：

1. 先创建一个临时初始化分支，例如 `bootstrap/default`
2. 把远端默认分支切到这个临时分支
3. 发布 10 个 `module/*` 分支
4. 停止在临时分支上开发
5. 通过仓库权限或约定，把开发入口固定到 `module/*`

也就是说，业务上可以做到“没有主开发分支”，但 Git 托管侧通常仍需要一个默认分支占位。不要把这个占位分支当成开发主线。

## 当前清单模型

清单文件：

- `Tooling/ModuleWorkspace/module-workspaces.json`

里面固定表达四类信息：

- `editablePaths`
  - 模块开发者允许改动的路径
- `readonlyPaths`
  - 为了让 Unity 能打开、模块能运行而保留的只读基线
- `excludedPaths`
  - 不会进入模块工程、也不允许从模块工程回灌到主仓的路径
- `branchMirror`
  - 模块分支镜像仓的远端名、分支前缀、本地镜像目录后缀

脚本额外会自动排除其他 9 个 `Assets/App/Modules/<ModuleName>` 目录。

## 状态含义

清单里的 `status` 当前分为三类：

- `isolated`
  - 该模块当前只发现共享只读基线依赖，没有发现对其他模块目录的直接资源引用
- `cross-module-readonly`
  - 该模块虽然可以发布成独立工程分支，但静态依赖审计已经发现它直接引用了其他模块目录里的资源
- `shared-scene-readonly`
  - 该模块仍依赖共享场景；共享场景只读带入，不开放场景级编辑

截至 2026-06-22 的真实状态是：

- `isolated`: 0
- `cross-module-readonly`: 8
- `shared-scene-readonly`: 2

审计结果文件：

- `docs/module-workspace-isolation-audit.json`

## 工作流 A：主仓发布模块分支

以 `ServerAssembly` 为例，在主仓执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\ModuleWorkspace\Publish-ModuleBranchMirror.ps1 `
  -ModuleName ServerAssembly `
  -MirrorRemoteUrl http://192.168.11.14:3000/admin/git_BPF_modules.git `
  -Push
```

这个脚本会做：

1. 准备一个本地镜像 clone
2. 切到 `module/ServerAssembly`
3. 用主仓清单白名单内容重建该分支工作树
4. 写入 `.module-workspace.json`、`README.md`、`.gitignore`
5. 提交当前基线
6. 可选推送到远端

默认本地镜像目录：

- `D:\gitea_obj\_unity projection\gitea_BPF__modulebranchmirror\module-branches`

## 工作流 B：模块开发者更新分支

模块开发者只拿模块镜像仓里的自己分支，例如：

- `module/ServerAssembly`

开发约束：

- 只允许修改清单里的 `editablePaths`
- `readonlyPaths` 不允许带回主仓差异
- 不能自行补入其他模块目录

如果主仓维护者重新发布了该模块基线，模块开发者只需要：

```powershell
git pull
```

然后继续在同一模块分支开发。

## 工作流 C：从模块分支回灌主仓

在模块镜像仓根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\ModuleWorkspace\Import-ModuleBranchMirrorBack.ps1 `
  -SourceProjectPath 'D:\gitea_obj\_unity projection\gitea_BPF' `
  -DryRun
```

确认预览正确后，再执行真实回灌：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\ModuleWorkspace\Import-ModuleBranchMirrorBack.ps1 `
  -SourceProjectPath 'D:\gitea_obj\_unity projection\gitea_BPF'
```

回灌脚本会做三类校验：

- `editablePaths` 下的 Unity 资产必须带上配套 `.meta`
- `readonlyPaths` 只要和主仓不一致，就直接拒绝回灌
- 模块分支工程里如果出现清单外文件，也直接拒绝回灌

## 本地模块工作区脚本仍然保留

以下脚本不会废弃：

- `New-ModuleWorkspace.ps1`
- `Update-ModuleWorkspaceBaseline.ps1`
- `Sync-ModuleWorkspaceBack.ps1`

原因很简单：

- 它们仍然适合主仓维护者本机做快速试验
- 分支镜像脚本内部也复用了同一套白名单构建逻辑

所以现在有两层模式：

1. 本地维护模式
   - 主仓维护者用 `New-ModuleWorkspace.ps1`
2. 发放模式
   - 对模块开发者用 `Publish-ModuleBranchMirror.ps1`

## 当前已知限制

### 1. 现在还没有任何模块达到真正完全隔离

静态 GUID 审计已经证明：当前 10 个模块里没有一个能诚实地标成 `isolated`。

这意味着：

- 现在可以做“仓库级可见性隔离”
- 还不能承诺“运行时完全不依赖其他模块资产”

### 2. `LayoutMarking` 和 `AgvCommissioning` 仍共用场景

这两个模块当前仍依赖：

- `Assets/App/Shared/Scenes/ZDP_ZhuChangJing.unity`

所以即便放进模块分支仓，场景也只能只读带入。

### 3. 多个模块仍直接引用其他模块目录资源

当前已确认的代表性跨模块引用：

- `RmsImport` -> `RmsInstall`（2 条）
- `RmsInstall` -> `ServerAssembly`（3 条）
- `NetworkCabinetWiring` -> `ServerAssembly`、`ChargingStationAssembly`
- `ServerAssembly` -> `NetworkCabinetWiring`、`ChargingStationAssembly`
- `RackAssembly` -> `ChargingStationAssembly`、`ServerAssembly`
- `DisplayWallAssembly` -> `ChargingStationAssembly`、`RackAssembly`、`ServerAssembly`
- `ChargingStationAssembly` -> `NetworkCabinetWiring`、`AgvCommissioning`、`RackAssembly`、`LayoutMarking`、`ServerAssembly`
- `RackPlacement` -> `ChargingStationAssembly`、`NetworkCabinetWiring`、`ServerAssembly`
- `LayoutMarking` -> `NetworkCabinetWiring`、`ServerAssembly`、`ChargingStationAssembly`
- `AgvCommissioning` -> `NetworkCabinetWiring`、`RackAssembly`、`ChargingStationAssembly`、`RackPlacement`

因此，模块分支镜像仓当前解决的是：

- 权限级隔离
- 目录级隔离
- 白名单回灌

它还没有解决的是：

- 彻底运行时去耦

## 推荐推进顺序

1. 先创建模块镜像远端仓，固定只发 `module/*` 分支
2. 用 `ServerAssembly` 或 `RmsImport` 做首批分支镜像试点
3. 优先清理最小跨模块依赖
4. 把真正公共资源迁到 `Assets/App/Shared`
5. 把真正私有资源迁回各自模块目录
6. 最后再拆共享场景

## 当前最适合先拆的依赖

按当前审计结果，建议先做：

1. `RmsImport -> RmsInstall`
2. `RmsInstall -> ServerAssembly`
3. `NetworkCabinetWiring <-> ServerAssembly`

原因是这几组依赖数量最小，先拆掉后，模块分支镜像的实际独立性会明显提升。
