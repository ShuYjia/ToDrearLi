# 11 个业务文档积分数据表（TexCore 对接基线）

本数据集从 `docs/文档/①...⑪...docx` 的积分表逐行整理，用于后续把业务模块、题目分值和实操步骤绑定到 TexCore。它是“原文积分基线”，不是已经写入场景或 `Config.xml` 的运行配置。

## 文件

- `texcore-module-score-summary.csv`：11 个模块的汇总，一模块一行。
- `texcore-module-score-items.csv`：65 个计分项的明细，一计分动作一行。
- `texcore-module-score-data.xlsx`：上述两张表加字段字典，供人工查看、筛选和交接。

## 汇总结论

| 状态 | 模块数 | 原文声明总分 |
|---|---:|---:|
| 已定义积分 | 9 | 1760 |
| 未定义积分 | 2 | — |

③“RMS 软件系统安装”和④“RMS 系统导入地图”没有积分表或显式积分规则，因此未擅自补分。⑩“货架入场”的“完成整个模块”为 10 分、总计 50 分，本数据集按原文保留并标为 `needs_confirmation`。

## 实现偏离记录（不改动原文基线）

| 模块 | 计分项 | 原文分值 | 实现状态 |
|---|---|---:|---|
| M09 | M09-OP-05 机器人加入系统 | 10 | `removed_in_scene`：按需求已从 09 机器人入场流程删除，题目与步骤不再存在 |

M09 因此的实现口径为：操作题 60、模块合计 90、总计 190（原文基线仍为操作 70、模块 100、总计 200）。本表与两张 CSV 的数值列保持原文转录，只通过 `mapping_status` 与备注记录实现偏离，避免把"原文积分基线"改成运行配置。

## TexCore 映射约定

1. `module_order` / `score_item_order` 是文档展示顺序；`texcore_child_order` 是拟用于 `TexCore_DataClass.childData` 的零基顺序。
2. `texcore_step_index` 只对应 `TexCore_StepObj.StepObjs.stepIndex`。在没有核对具体场景的 `NodeObjs → TopicObjs → StepObjs` 前全部留空，不能把文档步骤号直接当运行索引。
3. `DataClass_Topic.score` 是现有题目满分字段；明细表把它写在 `texcore_score_target` 中。选择题的 30 分如果由多题组成，必须先拆分到每个题目，不能给每题重复写 30 分。
4. `completion` 是原文模块完成奖励。现有代码核对只证明题目级 `DataClass_Topic.score` 和提交记录，尚未证明存在独立模块奖励字段，因此标为 `integration_required`；实现时应选择“独立完成题”或明确的模块完成事件，避免重复计分。
5. `module_id`、`score_item_id` 是新增的稳定机器标识，不依赖中文标题；中文 `module_name` / `score_name` 保留为 TexCore 展示名候选。

## 字段字典

### 模块汇总

| 字段 | 含义 |
|---|---|
| `module_id` | 稳定模块 ID，`M01`—`M11` |
| `score_status` | `defined` 或 `undefined` |
| `knowledge_score` | 知识点答题合计；⑧第 0 项不是答题，所以为 0 |
| `operation_score` | 原文所有非知识点、非模块完成计分项之和 |
| `completion_score` | 原文“完成整个模块”奖励 |
| `declared_total` | 原文声明总分 |
| `calculated_total` | 对计分项重新求和的结果 |
| `texcore_child_order` | 建议的模块零基顺序，供 `childData` 排序参考 |
| `mapping_status` | 当前对接状态或需要确认的事项 |

### 积分明细

| 字段 | 含义 |
|---|---|
| `score_item_order` | 模块内计分项零基顺序 |
| `source_step_label` | 原文步骤标签；完成奖励为 `完成` |
| `score_item_id` | 稳定计分项 ID |
| `score_type` | `knowledge`、`operation` 或 `completion` |
| `score_value` | 原文分值，统一为整数 |
| `score_unit` | 统一机器单位 `points` |
| `source_score_unit` | 原文写法：积分、金币或分 |
| `applies_to` | 原文说明适用练习与考核模式 |
| `texcore_child_order` | 建议的题目零基顺序 |
| `texcore_step_index` | 实际场景绑定索引；当前为空，等待场景核对 |
| `texcore_score_target` | 当前代码中的拟映射字段 `DataClass_Topic.score` |
| `mapping_status` | `proposed_topic`、`pending_scene_binding`、`integration_required`、`needs_confirmation` 或 `removed_in_scene` |

## 原文与运行数据边界

- 本表只抄录积分表中的计分项。业务流程中出现但积分表未给分的步骤不自动计分。
- ③、④不以其他模块的 100 分完成奖励类推。
- CSV 使用 UTF-8，字段名固定为 ASCII；人工查看优先打开同目录 XLSX，程序可直接读取 CSV。
- 与 TexCore 接入时，应先按 `module_id` 和 `score_item_id` 绑定，再补场景对象、Prefab、`topicType`、正确答案和 `stepIndex`。
