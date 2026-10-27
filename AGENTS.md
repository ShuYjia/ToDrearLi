# AGENTS.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with
project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial
tasks, use judgment.

## Project Entrypoints

- The current BPF business mainline defaults to
  `Assets/A_MRNet/Sence/01StartUp.unity` and
  `Assets/A_MRNet/Sence/03SingleplayerScene.unity`.
- `Assets/A_MRNet/Sence/04SingleplayerScene.unity` must not be part of Build
  Settings; the entry currently present in `ProjectSettings/EditorBuildSettings.asset`
  is disabled (`enabled: 0`). Treat
  `Assets/A_MRNet/Sence/02SingleplayerScene.unity` as a retained
  reference scene unless the user explicitly targets it.
- For Android or PICO export work, default to the batch project
  `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild` instead of exporting
  from the main project. Keep the main project open for Unity MCP work unless
  the user explicitly asks to change that workflow.
- After every completed code, prefab, scene, XR, Android, or runtime asset
  change that is intended to affect PICO behavior, default to testing it in the
  currently connected Unity Editor.
- After Unity testing, ask the user whether an APK export is needed. Do not
  export, install, or launch an APK on PICO unless the user explicitly confirms
  it for the current change.
- When the user confirms export, use the batch project, install the APK to every
  connected PICO device with `adb install -r`, and launch it for user testing.
  If a concrete blocker exists, state the exact blocker and the next command or
  action needed.
- Treat the batch project as a shared-content clone through junctions. Assume
  `Assets`, `Packages`, and `ProjectSettings` are shared with the main
  project, while `Library`, `Builds`, `Logs`, and `UserSettings` remain local
  to the batch project.
- Treat `Assets/APP/Modules/LegacyMrtkSync/*`,
  `Assets/APP/ScenesNGOsync/*`, `Assets/APP/OldMRTK/*`, and legacy networking
  scene names such as
  `Init`, `Lobby`, `UDPLobby`, and `MRTKsync` as legacy-only unless the user
  explicitly asks about legacy networking or migration work.
- Reuse `Assets/APP/OldMRTK/*` only as a historical reference path, but do not
  infer the current business scene flow from its archived module snapshots or
  generated legacy scripts. `Assets/APP/BPF/*` is the current BPF compatibility
  asset area and is separate from those legacy siblings. Paths under the
  differently cased `Assets/App/*` and under `Assets/_syc_resource/*` found in
  old documents are migration snapshots and do not exist in the current
  worktree.
- `Assets/...` is the active project layout for current business work. Do not
  treat the broad `Assets/APP` root as a single BPF ownership area:
  `Assets/APP/BPF` contains current BPF compatibility assets,
  `Assets/APP/MRTKSamples` contains upstream MRTK examples and dependencies,
  and `Assets/APP/Modules/LegacyMrtkSync` is legacy-only. Its `OldMRTK` and
  `ScenesNGOsync` siblings remain historical/legacy areas. Do not use
  historical `Assets/App` targets unless a concrete migration task explicitly
  recreates them.
- For current business work, prefer `Assets/A_MRNet`,
  `Assets/APP/BPF/Compatibility/Fonts`,
  `Assets/Resources/BusinessPrefabs`, `Assets/Resources/TopicPrefabs`, and
  the current business asset roots that exist directly under `Assets/`.
  Treat `Assets/APP/MRTKSamples` as shared upstream content; modify it only
  when the task explicitly targets MRTK sample content or its compatibility
  dependencies.
- For the current framework-level PICO compatibility judgment of
  `Assets/GameFramework` and `Assets/A_MRNet`, use
  `docs/framework-pico-mainline-notes.md` as the local source of truth unless
  live code/config changes supersede it.
- For TexCore module scoring work, use
  `docs/texcore-module-score-data.xlsx` (sheet `积分明细`) as the source of
  truth for per-module score configuration.

## Project-Local Skills

- The canonical skill collection for this repository is
  `.codex/skills/`; use its [skill index](.codex/skills/README.md) when
  selecting a workflow.
- Every skill created for this repository in the future must be stored as a
  complete directory under `.codex/skills/<skill-name>/`, with its `SKILL.md`
  and any required `agents/`, `references/`, `scripts/`, or `assets/` files.
- Update `.codex/skills/README.md` whenever a skill is added, removed, renamed,
  or its repository scope changes. Do not make the user-level
  `C:\Users\admin\.codex\skills` directory the source of truth for BPF work.

## Unity MCP Status

**The Unity MCP checks below apply only to turns that involve Unity construction,
scene or prefab work, Unity Editor operations, runtime asset changes, Unity
testing, Android/PICO builds, or exports.**

- For conversations unrelated to Unity construction or builds, do not call
  Unity MCP tools, run MCP telemetry or connectivity checks, query MCP
  resources, or use MCP-backed operations. This includes ordinary Git,
  documentation, code, repository, and general question-and-answer work.
- For Unity-related turns, follow the MCP checks and workflow below.

- 本仓库每次仅处理当前已检出的分支。所有检查、修改、验证、提交，
  以及用户明确要求时的推送或 PR，都只能针对当前分支。
- 除非用户明确修改此范围，不得切换、创建、更新、合并、rebase、reset、
  比较、检查或处理任何非当前的本地或远端分支；其他分支状态一律忽略。
- 不执行自动分支同步。若当前分支工作区有未提交改动，不得 stash、覆盖、
  回退或自动纳入与当前任务无关的文件。
- Before doing anything else in a Unity-related turn, detect whether Unity MCP
  is connected.
- The first line of every user-visible final reply must be exactly one status
  line. Use only one of these two forms: `Unity MCP：已连接，本轮继续。` or
  `Unity MCP：未连接，本轮继续。`
- Do not output an extra pre-check line such as
  "先检测连接状态，再决定是否继续". Do not repeat the status line later in the
  same reply.
- For this repository, treat Unity MCP as connected only when an actual
  `unityMCP` tool call returns a successful response. Tool availability by
  itself does not count as connected.
- Prefer `manage_editor.telemetry_ping` for the initial probe. If that result
  is inconclusive, use one additional lightweight read-only `unityMCP` call,
  such as `manage_scene.get_active`, to confirm connectivity.
- Treat any `unityMCP` response with `success=true` as connected, even if the
  message text says `queued`.
- Continue with the normal task flow whether Unity MCP is connected or not.
- When Unity MCP is not connected, avoid Unity-MCP-only operations and use
  repo files, command-line Unity, adb, or other available local tools as
  appropriate for the user's request.
- On the next user message, detect Unity MCP connection again instead of
  reusing the previous result.
- For this repository, treat the locked Unity MCP as the open-source
  `com.coplaydev.unity-mcp` / `MCPForUnity` package, not a Unity Technologies
  first-party official Unity MCP.
- Treat `.mcp.json` at the repo root as the project-locked MCP endpoint source
  of truth. The expected server is `unityMCP` at
  `http://127.0.0.1:8080/mcp`.
- Do not re-check or re-debate whether the current MCP is "official Unity
  MCP" on every turn. Re-verify its origin only if `Packages/manifest.json`,
  the root `.mcp.json`, or the live MCP connection evidence changes.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them. Don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.
- If the answer can be verified from the repo, configuration, Unity MCP, or
  local tests, verify it first and only ask the user after those checks are
  exhausted.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes,
simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it. Don't delete it.

When your changes create orphans:
- Remove imports, variables, or functions that your changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" -> "Write tests for invalid inputs, then make them pass"
- "Fix the bug" -> "Write a test that reproduces it, then make it pass"
- "Refactor X" -> "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] -> verify: [check]
2. [Step] -> verify: [check]
3. [Step] -> verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it
work") require constant clarification.

### Early Stop And Handoff

- During any modification task, if validation shows the result is poor,
  confidence stays low after a reasonable attempt, or the work is taking too
  long without converging, stop the editing loop in that turn.
- Do not keep stacking speculative changes just to avoid interrupting the
  conversation.
- When stopping early, tell the user explicitly:
  1. what you changed or tried
  2. why you stopped
  3. what exact user action, check, or command should happen next
  4. what result, screenshot, log, or decision the user should send back
- Prefer a concrete handoff over a long apology or another blind attempt.

### End-Of-Turn User Action

- After every user-visible result, always tell the user what they should do
  next, even when the turn is successful.
- Do not end with only a change summary or a generic completion statement.
- The next user action must be concrete and task-specific, such as:
  1. what to verify in Unity or on PICO
  2. what command to run
  3. what screenshot, log, or behavior to send back
  4. whether the user should continue with the next accepted requirement
- If the task is fully complete and no validation is needed, say that the
  user can proceed to the next requirement and name the expected direction.

## Automatic Task Delegation

- At the start of each task, identify simple, self-contained, low-risk work.
  Automatically route an atomic simple task, or independent simple parts of a
  larger task, to a Terra or Luna model when those models are available. Do not
  ask the user for permission before routine delegation.
- Give each delegated task explicit scope, relevant context, constraints, and
  verifiable success criteria. Parallelize only work whose edits and decisions
  do not conflict.
- Keep ambiguous requirements, architecture decisions, security-sensitive or
  destructive operations, Unity scene/prefab serialization, build/deployment,
  and final git delivery under the main agent unless the user explicitly asks
  otherwise.
- The main agent remains responsible for reviewing delegated output, inspecting
  the resulting diff, running relevant verification, and integrating the final
  answer. If Terra and Luna are unavailable, execute the work with the available
  agent and do not claim that either model was used.

## 5. Git Delivery

### Branch Reuse

- 当前有效范围固定为已检出的当前分支：仅允许在当前分支上修改、验证、
  提交，以及在用户明确要求时推送或创建 PR。
- 除非用户明确解除或修改此范围，不得切换、新建、更新或处理其他分支或
  worktree；不要查询或报告其他分支状态。
- 用户要求“提交所有文件”或推送时，仅使用当前分支；不得为了交付新建分支。

**When the user accepts the changes, finish the delivery.**

- Use a two-step delivery flow for this repository:
  1. after the task changes are complete, stage and commit locally immediately
  2. after the user explicitly accepts the verified result, create a pull
     request (PR) from the current branch
- Analysis, review, exploration, and debugging do not by themselves count as
  acceptance for PR.
- Automatically stage only the files touched for that task.
- Write a clear git commit message that matches the actual change.
- Commit the local change before asking the user for final acceptance.
- After the local commit, tell the user what to verify and make it clear that
  PR creation is waiting on acceptance.
- After the user accepts, push the current branch and create a pull request
  only when the user explicitly requests that delivery.
- Never include unrelated user changes in the same commit.

### Mandatory Commit-First Rule

- Any file change made by the agent must be followed by validation and a local
  commit before the agent reports completion, asks for acceptance, or starts an
  unrelated task.
- This applies to code, scenes, Prefabs, assets, docs, tooling, configuration,
  and agent/Skill instruction files.
- Stage and commit only files owned by the current task. Existing user changes
  must remain untouched and uncommitted unless the user explicitly includes
  them in scope.
- If a safe scoped commit is impossible because of conflicts, missing
  identity, repository failure, or another concrete blocker, stop and report
  the exact blocker and paths instead of leaving the agent's changes silently
  uncommitted.

### Commit Message Standard

- Use this subject format by default:
  `<type>(optional-scope): <short summary>`
- Allowed `type` values: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`,
  `build`, `ci`, `chore`, `revert`.
- Keep the subject line in imperative mood, describe the result, and do not
  end it with a period.
- Keep the subject line at 72 characters or fewer.
- Use a narrow `scope` when it helps, such as `server-assembly`, `build`,
  `scene`, `docs`, or `rms-install`.
- Do not use vague subjects such as `update`, `misc fixes`, `temp`, or `WIP`.

### Commit Body Rules

- A one-line commit is acceptable only for a trivial single-purpose change.
- Add a commit body for any non-trivial change, especially when the task
  touches multiple files, changes behavior, updates scenes or assets, changes
  build or CI flow, or adds tests.
- Leave one blank line between the subject and the body.
- Wrap body lines at about 72 characters.
- In the body, explain why the change was needed and summarize the most
  important behavior or content changes.
- When useful, end with footers such as `Refs: #123` or
  `BREAKING CHANGE: ...`.

### Preferred Body Template

Use this template for non-trivial changes:

```
<type>(optional-scope): <short summary>

Why:
- reason 1
- reason 2

What:
- change 1
- change 2

Refs: #issue
```

`Refs` is optional and should be omitted when there is no issue or task ID.

### Examples

Good:

- `fix(build): use BPF mainline scenes for command line build`
- `docs(asset-map): document migrated legacy asset locations`

Good non-trivial commit:

```text
fix(scene): restore BPF singleplayer scene defaults

Why:
- the scene carried unintended editor-saved transform and prefab overrides
- the mainline smoke test needs stable scene defaults

What:
- restore the affected prefab override values
- keep the intentional object rename for DashHolsterContentRoot
- add editor coverage for enabled build scenes and missing scripts
```

---

**These guidelines are working if:** fewer unnecessary changes in diffs,
fewer rewrites due to overcomplication, and clarifying questions come before
implementation rather than after mistakes.
