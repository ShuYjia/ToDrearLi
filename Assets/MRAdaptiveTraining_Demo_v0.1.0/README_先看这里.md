# MR 教学演示包交付

给有原 Unity 工程的同学：先导入包并跑通独立演示，再接一个真实单人任务。

目标 Unity：2021.3.29f1c1。设备背景：PICO 4U；本版不依赖 PICO SDK，也未做 PICO 真机验证。多人流程暂不接入。

## 快速开始

1. 导入 `Release/MRAdaptiveTraining_Demo_v0.1.0.unitypackage`。
2. 等待编译，点击 Unity 菜单 `Tools > MR 教学 > 打开独立演示场景`。
3. 点击 Play，在 Game 窗口按中文提示操作。
4. 接入真实任务前，阅读包内 `Documentation/接入原项目.md`。
5. 需要 AI 帮助时，把 `Documentation/给接入同学的AI提示词.md` 连同真实任务脚本交给 AI。

演示可展示：无错误提前结束、一般表现保持训练量、错误较多增加练习、交互异常待核实、必要步骤不可跳过、教师记录及 CSV 导出。默认训练次数对应 2 / 3 / 最多 5 次。

## 内容

- `Release`：可导入的 Unity 包和验证摘要。
- `SourceProject`：独立源码工程，保留 Assets、Packages、ProjectSettings，不含 Library 缓存。
- `Validation/CoreTests.cs` 与 `VerifyCore.ps1`：可复现的纯 C# 核心测试。
- `验证与文件清单.md`：本次完成的验证、边界和新增文件。

导入演示无需自己接 Inspector。接入真实项目必须连接规则资产、任务标识、重练、继续及教师处理事件；PICO UI 复用原项目已有组件。

本版是可供接入的独立原型，不是已经接好真实关卡的 PICO 成品。核心规则默认值需要由教师按任务确认；联网教师端和多人共享流程不在此包范围内。

没有改动原 Study 1 / Study 2 工程。
