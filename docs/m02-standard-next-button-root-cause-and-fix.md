# M02 普通“下一步”按钮问题：根因与修复经验

## 现象

在 03 场景的 M02“服务器系统安装”流程中，放置 U 盘到 `UPanCollider`
后会出现普通 UI 的“下一步”按钮。IP 设置完成和模块完成主题也可能出现
同一个按钮。该按钮不是 `UI_XiTongJingXiangAnZhuang` 内部的屏幕操作按钮。

## 根因

M02 主题复用了通用 `Panel_Operate`。任务完成后，调用链为：

1. 物体或屏幕交互触发 `Panel_Operate.OnClick`。
2. 当前主题所有任务完成后进入 `CalculateScore()`。
3. `CalculateScore()` 调用 `Submit()`。
4. `Submit()` 调用 `SubmitConfig()`，随后根据 `isNextShow` 分支：
   - `true`：执行 `ShowNext()`，显示 `btn_Next`；
   - `false`：直接调用 `btn_Next` 的 `TexCore_Interaction_UGUI.OnClick()`，
     自动推进到下一个主题。

M02 原始主题 Prefab 的配置曾为：

| 主题 | `isNextShow` | 分值 |
| --- | ---: | ---: |
| `02U盘启动盘插入` | 1 | 10 |
| `03系统安装操作` | 0 | 30 |
| `04设置正确IP` | 1 | 30 |
| `05完成整个模块` | 1 | 100 |

因此 U 盘、IP、完成主题会显示普通按钮；系统安装主题本身不会显示同类
按钮，但其内部屏幕页面仍由 `UI_XiTongJingXiangAnZhuang` 的步骤控制。

## 修复

在 `TexCore_M02ServerInstallBootstrap.ApplyRuntimeTopicTasks` 中对四个 M02
主题统一执行 `SuppressM02StandardNext`：

- 将运行时 `Panel_Operate.isNextShow` 设为 `false`；
- 隐藏通用 `btn_Next`；
- 保留 `Panel_Operate.Submit()` 的自动推进路径；
- 不修改通用 `Panel_Operate`、共享按钮 Prefab 或生成的
  `Assets/StreamingAssets/Config.xml`。

这样，玩家完成 `UPanCollider` 放置、屏幕页面操作、IP 设置或完成动作后，
仍会提交积分和完成事件，并自动进入下一个主题，不再暴露普通“下一步”。

## 关键边界

- U 盘放置目标是 `UPanCollider`，不是背包按钮或其他位置锚点。
- `UI_XiTongJingXiangAnZhuang` 的直接子物体是系统安装页面；页面顺序由
  M02 桥接中的 `InstallPages` 映射到连续的 `stepIndex`。
- 普通主题“下一步”与屏幕页面内部按钮是两条不同的交互链，不能混为一谈。
- 关闭 `isNextShow` 不等于跳过步骤；它只改变主题完成后的推进方式。

## 验证清单

1. 进入 03 场景并打开 M02。
2. 从背包取出 U 盘，将其放入 `UPanCollider`。
3. 确认没有普通“下一步”，并自动进入系统安装主题。
4. 按 `UI_XiTongJingXiangAnZhuang` 的页面顺序操作，确认每一步只推进一次。
5. 完成 IP 设置和模块完成动作，确认同样自动推进且不出现普通按钮。
6. 检查 Console 无新的 M02 脚本编译错误。

## 经验总结

- 诊断 UI 问题时，先区分“通用主题级按钮”和“业务页面内部按钮”。
- 先查 `Panel_Operate.Submit()` 的分支，再查具体 Prefab 的
  `isNextShow`，不要仅凭运行截图判断交互对象错误。
- 业务专用行为应优先在业务桥接层覆盖，避免修改共享框架导致其他模块回归。
- 静态 Prefab 配置、运行时实例状态和真实 Play Mode 行为必须分别验证。
