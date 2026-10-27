# bpf-legacy-scene-analysis

解析旧工程超大 Unity 场景（如 MRTKsync.unity，100MB+ 级 YAML）的层级、脚本与文本，
并与主工程场景 / 积分表交叉核对。适用于"理解旧 changjing-N 关卡设计""核对新旧步骤一致性"类任务。

## 触发条件

- 用户要求理解旧工程（gitea_mrtk_sync_old_4 等）某场景内某物体（如 SHIXUN/changjing-N）及其子物体。
- 需要把旧关卡步骤与主工程 03 场景模块、`docs/texcore-module-score-data.xlsx` 积分明细核对。

## 工作流

1. **不要用文本编辑器直接读巨型场景**。用 `scripts/scene_tools.py` 一次流式扫描建缓存：
   ```
   python scene_tools.py build  <scene.unity>  <cache.json>     # 首次约 20s
   ```
   它逐行解析：`--- !u!1`（GameObject：m_Name / m_TagString / m_IsActive / m_Component）、
   `--- !u!4|224`（Transform/RectTransform：m_GameObject / m_Father / m_Children）、
   `--- !u!114`（MonoBehaviour：m_Script guid）。
   **必须同时解析 224（RectTransform）**，否则 UI 子树会断裂（KeyError）。
   产物为 JSON 缓存：`go_names/go_tags/go_active/go_comps/comp_class/comp_script/trans_owner/trans_children/trans_father`。
   （历史遗留的 `scripts/parse_scene.py` 用 pickle，字段语义相同，仅作参考。）
2. **全部查询走缓存，秒级返回**（不必重扫场景）：
   ```
   python scene_tools.py who   <cache.json> <name>          # 同名物体 + 父路径 + active
   python scene_tools.py kids  <cache.json> <name|goID>     # 直接子物体（带同级下标）
   python scene_tools.py tree  <cache.json> <name|goID> 4 --filtered
   ```
   `--under=<路径子串>` 可把同名命中限定到目标副本（场景常有 3 份同名物体）。
3. **GUID 反查脚本**：在旧工程 `Assets/` 下 `grep -rl --include="*.cs.meta" "guid: <g>"`。
   注意：Windows 下 Python 文本模式写入的文件带 `\r`，shell 循环读入必须 `tr -d '\r'`，否则 grep 全部落空。
   找不到的 guid 多为 MRTK/包脚本（查 `Library/PackageCache`）。
4. **提取序列化数据**：`scene_tools.py mb` 定位目标 MonoBehaviour 块并打印序列化字段
   （`mb <cache> <goID> skipScript1,skipScript2` 可跳过噪声）；`text` 提取 UI 文本，
   两者内部已用 `unyaml()` 还原 `\uXXXX` 转义（中文名/中文文本必需，Unity 两种写法都要兼容：
   `\u4EFB` 直译 与 `\u00E8\u00B4\u00A6` 的 UTF-8 逐字节转义）。fileID 需解析回物体路径。
5. **主工程核对用 Unity MCP**（http://127.0.0.1:8080/mcp，与仓库根 `.mcp.json` 锁定端点一致）。
   若会话未注册 unityMCP 工具，可用 `scripts/umcp_client.py` 裸 JSON-RPC：
   initialize（记 Mcp-Session-Id）→ notifications/initialized → tools/list / resources/read / tools/call。
   实例 ID 在 domain reload 后失效，需重新 find_gameobjects。
6. **积分表**：`docs/texcore-module-score-data.xlsx` sheet `积分明细`，按 module_id（如 M10）过滤。
   sheetagent 对 Windows 盘符路径解析有 bug，用 venv openpyxl 直读即可。
7. **交叉核对输出**：旧枚举流（LoadSceneObj.Tesks 段）↔ 旧 UI 文本 ↔ 主工程 TexCore_StepObj
   interacterObjs（nodePrefab/nodeObjs/stepObjs）↔ Panel_Select/Panel_Operate 分值 ↔ 积分明细行。
8. **核对现行框架是否真的接好**（问「现在 X 关卡是什么情况」时用）：
   - 框架对象默认 `m_IsActive: 0`，`find_gameobjects` 必须传 `include_inactive: true`，否则一律 0 命中。
   - 用 `find_gameobjects(by_component)` 数组件个数（如门控在 M02/08/09/10 共 4 个）快速判断范式复用范围。
   - 校验步骤树绑定：取 L1/L2 `*.prefab.meta` 的 guid → 在场景 `TexCore_StepObj` 块里按
     `nodePrefab`/`topicPrefab` 的 guid 定位节点 → 读 `topicObjs[*].stepObjs[*].fileID`
     → 回场景查该 `&fileID` 的 `m_Name`，确认指向哪个 GameObject。
   - 门控依赖看 `requiredSteps: - {fileID: ...}`，再把该 fileID 的 `m_Script` guid 反查脚本名。

## 环境陷阱（Git Bash / Windows）

- 本会话 Bash 默认 PATH 缺核心命令，每条命令前必须
  `export PATH="/usr/bin:/bin:/c/Windows/System32:$PATH"`，否则 `ls/dirname` 全部 not found。
- 打印中文必须 `export PYTHONIOENCODING=utf-8`，否则 gbk 控制台抛 UnicodeEncodeError。
- Python 文本模式写入的文件带 `\r`，shell 循环读入需 `tr -d '\r'`，否则 grep 全部落空。
- `Edit` 工具偶发「报成功但未落盘」，改完关键行务必 `grep -n` 复核再跑。
- **场景 YAML 里中文是 `\uXXXX` 转义**：直接 `grep 中文名` 永远是 0。中文名只靠 MCP（返回已解码）
  或 `unyaml()` 解码后匹配；`grep` 只对 ASCII 名字（如 `TexCore_10Steps`）有效。
- MCP 端点固定为 `http://127.0.0.1:8080/mcp`（与 `.mcp.json`、`AGENTS.md` 一致）。请求必须带
  `Accept: application/json, text/event-stream`，否则 8080 会返回 406。

## 已知映射结论（截至 2026-09）

- 旧 `SHIXUN/changjing-4` = 主工程 03 场景 M10「货架入场」。
- 旧驱链：instantiation 生成 HuoJia-4(tag=instant) → AttachToParent 吸附 AGV1(tag=jiqiren)
  → 落位 huojia 目标点 → collision.cs 生成下一个货架 → Panding 全部目标点隐藏 → Completeentry。
- 主工程等价物：TexCore_RackEntryWorkflowStep（50 目标点、0.24m 容差、背包复用单个
  TexCore_M10RackCarrier）+ TexCore_ModuleCompletionGateStep，由 TexCore_10SceneBuilder 生成。
- 积分：knowledge 30 + operation 10 + completion 10。

## 注意

- 旧场景 158MB、4 万物体，MCP 逐物体查询不可行；YAML 解析一次全量拿层级更可靠。
- MCP 端口以探测结果为准（本例为 8080）；连接成功判定以 tools/call 返回 success=true 为准。
