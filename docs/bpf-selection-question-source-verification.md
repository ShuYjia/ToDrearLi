# BPF 选择题配置源码核对结论

## 1. 文档目的

本文记录当前 BPF 主线中选择题的源码核对结果，用于回答以下问题：

- 连续多道选择题应该如何组织；
- 单选、多选和不定项选择如何配置；
- 题目数据、选项模板、正确答案和下一题之间如何关联；
- `docs/bpf-frontend-collaboration-guide.md` 中相关说明是否与源码一致；
- 哪些结论已经由静态源码证实，哪些仍需 Unity 或 PICO 运行验证。

本文不是新的业务规范，源码和实际运行结果仍是最终依据。

## 2. 核对范围与版本限制

- 核对日期：2026-07-20
- 当前分支：`03`
- 当前本地提交：`ff6103f`
- 当前业务场景入口：
  `Assets/A_MRNet/Sence/03SingleplayerScene.unity`
- Unity MCP：本轮没有可调用的 `unityMCP` 工具，未进行编辑器内验证。
- 远端状态：执行 `git fetch --all --prune` 时无法连接 GitHub 443
  端口，因此本文只能确认当前本地源码，不能保证包含远端尚未获取的提交。

本次仅进行源码、Prefab 和配置链路的静态核对，没有修改业务资源，也没有
导出、安装或启动 PICO APK。

> 历史边界：本节记录的是该次核对现场。Unity MCP、远端状态和本地提交信息
> 不代表后续会话的当前状态；使用本文前仍需重新执行仓库与编辑器检查。

## 3. 核对过的主要依据

### 3.1 核心源码

- `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/Topic/Panel_Select.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/Topic/PanelTopicBase.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/BusinessLogic/NodeScripts/NodePanelBase.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/ObjState/TexCore_StepObj.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_ConfigManager.cs`
- `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/PlaneNodeScripts/TexCore_PlaneManager.cs`

### 3.2 真实业务 Prefab

- 父节点：
  `Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统/01服务器网络搭建.prefab`
- 选择题示例：
  `Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统/01服务器网络搭建/03Web服务器类型选择.prefab`
- 通用模板：
  `Assets/Resources/TopicPrefabs/选择题.prefab`

## 4. 已由源码确认的结论

### 4.1 连续多道选择题由父节点 `childData` 排序

`NodePanelBase.Start()` 会加载 `datas.childData[0]`。用户完成当前题并点击
“下一步”后，`NodePanelBase.NextTopic()` 会根据当前题在 `childData` 中的索引，
加载下一项的名称。

因此，连续题目的顺序由父节点 Prefab 中的 `childData` 顺序决定。真实的
“01服务器网络搭建”节点也按以下顺序保存了题目：

1. `01服务器网络搭建练习题`
2. `02服务器硬件组件选择`
3. `03Web服务器类型选择`
4. `02硬件连接操作`

`NodePanelBase.LoadChild()` 最终把该名称传给 `TexCore_PlaneManager.LoadPlane()`。
因此父节点中的名称、题目 Prefab 文件名和题目数据名称必须一致。

### 4.2 选择题使用两组固定顺序的数据

`Panel_Select.Start()` 直接读取：

```csharp
topicData = (DataClass_Topic)datas.selfDatas[0];
selfData = (DataClass_Option)datas.selfDatas[1];
```

运行时或生成配置后的固定顺序是：

1. `selfDatas[0]`：题目基础数据 `DataClass_Topic`
2. `selfDatas[1]`：选择规则数据 `DataClass_Option`

需要注意：在题目 Prefab 的 `Panel_Select` Inspector 中，主要编辑入口是
`topicData` 和 `selfData` 字段。`Panel_Select.GetData()` 会清空并重新组装
`datas.selfDatas`，按 `topicData`、`selfData` 的顺序写入。不要把“顺序固定”
误解为必须直接编辑 Prefab YAML 中的 `datas.selfDatas`。

### 4.3 文本选项来自 `topicData.tasks`

当 `topicType` 为“文本选择”时，`Panel_Select` 会遍历 `tasks`：

- 第一个选项复用 `itemParent.GetChild(0)`；
- 后续选项复制第一个子物体；
- 每个选项 GameObject 的名称被设置为完整选项文本；
- 显示文本也使用完整选项文本；
- 文本中的 `\n` 会在运行时转换为真正换行。

所以 `Item Parent` 必须存在，并且至少有一个可复制的选项模板。

### 4.4 正确答案匹配完整选项名称

用户点击选项后，`Panel_Select` 把被点击 GameObject 的名称加入 `steps`。
判分时使用 `rightTasks.Split('_')` 得到正确答案列表，再与 `steps` 中的名称
逐项比较。

因此：

- `rightTasks` 必须与 `tasks` 中的完整选项字符串完全一致；
- 不能只写 `A`、`B`、`C`，除非选项本身就只叫这个名称；
- 空格、标点、大小写或其他字符不同，都会导致答案不匹配；
- 多选答案必须使用半角下划线 `_` 连接。

例如：

```text
tasks:
- A.CPU
- B.内存
- C.硬盘

rightTasks:
A.CPU_B.内存
```

### 4.5 单选、多选和不定项选择的当前行为

`SelecionCount` 枚举包含：

- 单选
- 多选
- 不定项选择

单选时，每次选择新选项都会重新创建 `steps` 列表，所以只保留最后一个
选项。多选和不定项选择不会主动清空列表，可以保留多个选项。

当前判分代码对“多选”和“不定项选择”使用相同逻辑：只有所选答案全部正确，
并且选择数量与正确答案数量一致，才会判为正确。源码中暂未看到两者在判分
规则上的额外差异。

### 4.6 “确定”和“下一步”的流程

点击名为“确定”的对象时会调用 `CalculateScore()`。提交后源码会：

1. 禁用所有选项下的 Collider；
2. 广播当前节点、题目和 `stepIndex = 0` 的触发事件；
3. 隐藏“确定”按钮；
4. 显示“下一步”按钮。

父节点收到“下一步”点击后，根据 `childData` 加载下一道题。

`btn_Confirm` 没有绑定时，源码会尝试执行 `transform.Find("确定")`。这只会
查找当前 Transform 的直接子物体；名称或层级不正确时可能出现空引用。因此
显式绑定当前题目的确定按钮更稳妥。

### 4.7 普通选择题固定触发步骤 0

`Panel_Select.Submit()` 广播触发物体事件时，第三个参数固定为 `0`。因此普通
选择题在 `TexCore_StepObj` 中通常只对应 `stepIndex = 0`。

真正的多步 3D 操作题使用以下结构：

```text
NodeObjs
└── TopicObjs
    └── StepObjs
        ├── stepIndex
        └── stepObjs
```

“连续几道选择题”和“一道题包含多个 3D 操作步骤”是两种不同配置：前者改
父节点 `childData`，后者改场景中的 `TexCore_StepObj`。

### 4.8 Prefab 是配置来源之一，`Config.xml` 受运行开关控制

`TexCore_ConfigManager` 能通过 `Resources.Load`/`Resources.LoadAll` 读取
`BusinessPrefabs`，调用 Prefab 上的 `IGetData.GetData()`，再组装或保存配置。

但运行时是否保存或加载 `Config.xml`，取决于组件上的 `isSaveFile` 和
`isLoadFile` 开关。因此更准确的说法是：

- 业务题目应优先修改对应 Prefab，而不是把手改 `Config.xml` 当成长期配置
  来源；
- 当前具体运行到底是现场生成、加载已有 XML，还是两者结合，必须继续核对
  场景中 `TexCore_ConfigManager` 的实际 Inspector 开关和运行日志。

## 5. 对现有协作指南的判断

`docs/bpf-frontend-collaboration-guide.md` 中选择题章节的核心规则与当前本地
源码一致，包括：

- 复制现有选择题或通用模板；
- 保持两组题目数据的固定顺序；
- `tasks` 填完整选项文本；
- `rightTasks` 填完整答案名称；
- 多选答案使用 `_`；
- `Item Parent` 使用第一个子物体作为模板；
- `Btn Confirm` 应绑定当前题目的确定按钮；
- 普通选择题通常对应 `stepIndex = 0`；
- 连续题目名称和父节点 `childData` 必须一致。

需要补充或更严谨表达的部分：

1. Prefab 编辑阶段主要填写 `topicData` 和 `selfData`，`GetData()` 才会将其
   组装为 `selfDatas[0]` 和 `[1]`。
2. “Item Parent 下只保留一个模板”是安全配置约束，不是源码中的显式数量
   校验。源码不会自动删除多余的预置子物体。
3. `Config.xml` 的实际使用方式受场景中 `isSaveFile`、`isLoadFile` 开关控制，
   需要结合场景 Inspector 和运行日志确认。
4. 文档目前属于静态配置指南，不能替代 Unity 和 PICO 上的完整运行验证。

## 6. 已知配置风险

当前源码对错误配置缺少完整保护，至少存在以下风险：

- `datas.selfDatas` 少于两个元素时，`Panel_Select.Start()` 会越界；
- `topicData` 或 `selfData` 类型、顺序错误时会转换失败；
- `itemParent` 为空或没有子物体时，`GetChild(0)` 会报错；
- 没有显式绑定 `btn_Confirm`，且直接子物体中不存在“确定”时会空引用；
- `rightTasks` 与选项名称不完全一致时会判错；
- Prefab 中预先保存多个选项子物体时，运行时继续复制可能出现重复选项；
- 示例题目的 `score` 可能为 `0`，需要计分时必须填写实际分值；
- 练习模式和考核模式的错误提示、重试与计分行为不同，需要分别验证。

## 7. 推荐配置流程

1. 在对应业务节点目录中复制最接近的现有选择题 Prefab。
2. 将 Prefab 文件名、根对象/数据名称和父节点 `childData` 名称保持一致。
3. 在 `Panel_Select.topicData` 中设置题型、题干、选项、正确答案和分值。
4. 在 `Panel_Select.selfData` 中设置单选、多选或不定项选择。
5. 确认 `itemParent` 已绑定，且其下只保留一个有效选项模板。
6. 显式绑定 `btn_Confirm`。
7. 在父节点 `childData` 中按需要的出题顺序排列题目名称。
8. 普通选择题如需触发场景物体，在 `TexCore_StepObj` 中核对题目对应的
   `stepIndex = 0`。
9. 保存所有修改过的 Prefab；若修改步骤树，同时保存场景。
10. 在 Unity 中从当前业务入口完整走题，再按项目规则导出、安装并在 PICO
    上验证。

## 8. 尚未完成的运行验证

以下内容不能仅凭静态源码最终确认：

- 当前场景中 `TexCore_ConfigManager.isSaveFile` 和 `isLoadFile` 的实际值；
- Prefab 修改后 `Config.xml` 在当前启动流程中的生成和加载时机；
- 单选按钮的视觉互斥状态是否与数据列表完全同步；
- 多选取消选择、连续重进题目时是否存在重复项或残留状态；
- 练习模式连续答错三次后的完整交互；
- 考核模式的计分数据是否正确写入考试记录；
- “确定”“下一步”在 PICO 射线和近场交互下是否都可正常点击；
- 题目完成后对应的 3D 物体状态是否按 `stepIndex = 0` 正确切换。

下一次实际制作选择题后，应把以上项目作为 Unity 与 PICO 验收清单。
