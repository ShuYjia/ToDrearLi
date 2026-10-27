# AI 与 MR/VR 实训产品案例扩展

> 本文补充 `docs/ai-product-case-studies.md` 未列出的用户侧项目。来源限定为厂商官方产品/案例页、政府、大学或研究机构一手页面。每条的“对 BPF 的启发”是产品设计建议，不表示 BPF 已经实现该能力。

## A. AI 虚拟人、对话教练与情景学习

| 项目 | 组织 | 用户如何使用 | AI/XR 能力 | 对 BPF 的具体启发 | URL | 成熟度/可信度 |
|---|---|---|---|---|---|---|
| Digital People | Soul Machines | 学员与企业服务、销售或健康场景中的数字人进行自然语言对话。 | 官方产品页描述具备语音、表情、记忆和实时对话的数字人。 | 为服务器安装或仓储安全关卡加入“现场主管”角色；把可问范围限制在当前步骤知识。 | [Soul Machines Digital People](https://www.soulmachines.com/digital-people/) | 商业产品；厂商一手页 |
| UneeQ Digital Humans | UneeQ | 企业用浏览器或沉浸式终端与数字人问答、导览和培训。 | 云端数字人支持语音/文本对话、知识库接入和多语言表达。 | 将步骤说明、术语和安全禁忌接入同一角色，保留 TexCore 的门禁与评分。 | [UneeQ](https://www.digitalhumans.com/) | 商业产品；厂商一手页 |
| Body Interact | Body Interact | 医学生在虚拟病人场景中问诊、选择检查和处置，结束后复盘。 | 交互式虚拟病人根据决策改变状态，并记录临床推理过程。 | 把“问设备—检查—修复—复测”做成可分支的服务器/机器人故障情景。 | [Body Interact](https://bodyinteract.com/) | 商业产品；厂商一手页 |
| Shadow Health Digital Clinical Experiences | Elsevier | 护理学生与虚拟病人进行文字或语音问诊并提交临床记录。 | 数字病人对问题作出情境化回答，平台提供结构化评估和反馈。 | 参考“关键问题覆盖率”指标，评估学员是否完成安全确认，而不只看最终按钮。 | [Shadow Health](https://www.elsevier.com/solutions/shadow-health) | 商业教育产品；厂商一手页 |
| Embodied Labs | Embodied Labs | 护理、社工和照护人员以第一人称 VR 体验老年、残障或临终照护情景。 | 360°沉浸式叙事、角色视角切换和课程评估。 | 在仓储/装配中加入“新手视角”和安全后果叙事，帮助学员理解违规的实际影响。 | [Embodied Labs](https://www.embodiedlabs.com/) | 商业产品；厂商一手页 |

## B. 动作、姿态与视觉评估

| 项目 | 组织 | 用户如何使用 | AI/XR 能力 | 对 BPF 的具体启发 | URL | 成熟度/可信度 |
|---|---|---|---|---|---|---|
| XRHealth | XRHealth | 患者在 VR 治疗任务中按提示完成动作，治疗师查看训练结果。 | 官方说明使用摄像头/传感器跟踪动作、范围和重复次数，并提供远程数据。 | 对装配姿势、伸手角度、工具停留时间做“过程指标”；评分需显示置信度并可人工复核。 | [XRHealth](https://www.xr.health/) | 商业医疗产品；厂商一手页 |
| SWORD Health Digital Therapist | SWORD Health | 用户在家中跟随数字治疗师训练，传感器反馈动作并由临床团队远程跟进。 | 计算机视觉/运动传感、个性化计划和远程教练。 | 将“自动提示 + 教师复核”用于划线贴码的手腕轨迹和安全距离，不直接替代完成判定。 | [SWORD Health](https://swordhealth.com/) | 商业医疗产品；厂商一手页 |
| Kemtai | Kemtai | 用户通过摄像头进行健身或康复训练，系统即时纠正姿势。 | 计算机视觉姿态估计、实时反馈和训练分析。 | 参考轻量摄像头方案，为货架入场、搬运路线提供可选的姿态安全提示。 | [Kemtai](https://kemtai.com/) | 商业产品；厂商一手页 |
| Vicon Valkyrie / Shōgun | Vicon | 运动科学、影视和训练机构用光学动捕记录动作并回放分析。 | 多摄像头 3D 身体追踪、骨骼重建和动作质量分析。 | 若未来需要精确装配动作数据，可把动作轨迹作为旁路证据，不改变 TexCore 事件流。 | [Vicon Motion Capture](https://www.vicon.com/) | 商业专业系统；厂商一手页 |
| Microsoft Azure Kinect Body Tracking | Microsoft | 开发者用深度摄像头把人体骨骼数据接入训练、机器人或交互应用。 | 官方 SDK 提供实时骨骼关节跟踪和遮挡处理。 | 为 PICO 外部摄像头/实验室采集预留标准化姿态事件格式，先做离线分析。 | [Azure Kinect Body Tracking](https://learn.microsoft.com/en-us/azure/kinect-dk/body-sdk-download) | 官方 SDK；厂商一手文档 |

## C. 工业、机器人与数字孪生

| 项目 | 组织 | 用户如何使用 | AI/XR 能力 | 对 BPF 的具体启发 | URL | 成熟度/可信度 |
|---|---|---|---|---|---|---|
| RobotStudio | ABB | 工程师在虚拟产线中编程、验证机器人路径，再将程序部署到真实工作站。 | 数字孪生、离线编程、碰撞/节拍仿真和虚拟调试。 | 机器人入场培训可先在数字孪生中演练路径、禁区和急停，再进入 PICO 现场关卡。 | [ABB RobotStudio](https://new.abb.com/products/robotics/robotstudio) | 商业成熟产品；厂商一手页 |
| ROBOGUIDE | FANUC | 工厂用虚拟机器人单元验证布局、抓取、节拍和程序。 | 3D 仿真、可达性/碰撞检查和离线编程。 | 用统一场景参数生成不同货架、机器人和障碍物变体，扩大训练覆盖。 | [FANUC ROBOGUIDE](https://www.fanucamerica.com/products/robots/robot-simulation-software/roboguide) | 商业成熟产品；厂商一手页 |
| KUKA.Sim | KUKA | 操作员和工程师在仿真环境中验证机器人应用并培训动作流程。 | 机器人数字孪生、路径规划、节拍分析和虚拟调试。 | 将“先规划后操作”的检查点加入装配模块，记录规划错误与实际错误。 | [KUKA.Sim](https://www.kuka.com/en-us/products/robotics-systems/software/simulation/kuka-sim) | 商业成熟产品；厂商一手页 |
| Emulate3D | Rockwell Automation | 自动化团队在虚拟工厂中测试 PLC、输送线和机器人逻辑。 | 设备级数字孪生、虚拟调试和控制器联调。 | 服务器安装可增加网络拓扑/PLC 故障变体；培训环境与真实控制逻辑保持可追溯映射。 | [Emulate3D](https://www.rockwellautomation.com/en-us/products/software/factorytalk/designsuite/emulate3d.html) | 商业成熟产品；厂商一手页 |
| Process Simulate | Siemens Tecnomatix | 制造工程师在 3D 工厂模型中验证装配可行性、人体工学和机器人流程。 | 数字人体、装配仿真、碰撞与工位节拍分析。 | 对装配关卡加入工具可达性、人员安全区和节拍指标，而不只判断零件是否放对。 | [Tecnomatix Process Simulate](https://www.sw.siemens.com/en-US/digital-manufacturing/manufacturing-engineering/process-simulate-software/) | 商业成熟产品；厂商一手页 |
| 3DEXPERIENCE Virtual Twin | Dassault Systèmes | 企业在虚拟孪生中设计、运行和优化工厂/产品，再将结果反馈真实运营。 | 云端协作、物理仿真、数据连接和 AI 辅助优化。 | 为 BPF 建立“场景版本—步骤脚本—设备配置”关联，支持同一课程的多产线变体。 | [3DEXPERIENCE Virtual Twin](https://www.3ds.com/virtual-twin) | 商业平台；厂商一手页 |
| Isaac Sim | NVIDIA | 机器人开发者在 Omniverse 中训练、测试和验证感知与操作算法。 | 物理仿真、合成数据、传感器模拟和强化学习接口。 | 机器人/货架课程可用合成数据覆盖光照、遮挡和摆放差异，再把安全规则映射回实训。 | [NVIDIA Isaac Sim](https://developer.nvidia.com/isaac/sim) | 商业/开发者平台；厂商一手页 |
| Azure Digital Twins | Microsoft | 运维团队把建筑、设备和传感器建成关系模型，查询状态并驱动应用。 | 图谱化数字孪生、实时 IoT 数据和规则/AI 应用接口。 | 教师端可按“设备—步骤—错误”关系查询班级风险，而不是只看单次分数。 | [Azure Digital Twins](https://azure.microsoft.com/products/digital-twins) | 云服务；厂商一手页 |

## D. 远程专家与现场协作

| 项目 | 组织 | 用户如何使用 | AI/XR 能力 | 对 BPF 的具体启发 | URL | 成熟度/可信度 |
|---|---|---|---|---|---|---|
| Dynamics 365 Remote Assist | Microsoft | 一线人员用 HoloLens/移动端与远程专家通话，在现场对象上共享和标注视图。 | 空间锚定标注、视频协作、工单和资产上下文。 | 教师可远程标注当前货架/端口；标注事件要写入学员复盘，但不自动改变步骤完成。 | [Remote Assist](https://www.microsoft.com/dynamics-365/products/remote-assist) | 商业成熟产品；厂商一手页 |
| WorkLink | Scope AR | 企业把 3D 作业指导发布到 AR 设备，现场人员按步骤执行并可呼叫专家。 | 空间识别、分步 3D 指引、远程协作和作业记录。 | 参考把 TexCore StepObj 映射为可版本化的 3D 指令，并保存每一步证据。 | [Scope AR WorkLink](https://www.scopear.com/worklink/) | 商业产品；厂商一手页 |
| Manifest | Taqtile | 现场员工在平板或 AR 设备中查看 SOP、提交照片/签名并请求远程支持。 | 数字作业指导、资格认证、审计记录和协作。 | 为服务器安装增加“照片/二维码/端口”证据附件，教师可抽查而不依赖口头描述。 | [Taqtile Manifest](https://taqtile.com/manifest/) | 商业产品；厂商一手页 |
| Help Lightning | P2P Medical / Help Lightning | 远程专家把自己的手势叠加到现场视频，指导医疗或工业操作。 | 视频融合、空间叠加和双向实时指导。 | 适合困难装配的教师演示；系统应标记“远程指导”并区分自助完成与辅助完成。 | [Help Lightning](https://www.helplightning.com/) | 商业产品；厂商一手页 |
| XMReality Remote Guidance | XMReality | 技术员通过手机/智能眼镜呼叫专家，专家在共享视野中画线、指点和发送文档。 | 免手视频、实时标注、知识库与会话记录。 | 在 PICO 断网时退回预录示范；联网时保存专家标注作为可复用提示素材。 | [XMReality](https://xmreality.com/remote-guidance/) | 商业产品；厂商一手页 |

## E. 职业教育与高风险训练

| 项目 | 组织 | 用户如何使用 | AI/XR 能力 | 对 BPF 的具体启发 | URL | 成熟度/可信度 |
|---|---|---|---|---|---|---|
| FLAIM Trainer | FLAIM Systems | 消防员在 VR 火场中穿戴热感/呼吸器反馈，反复练习搜索、灭火和撤离。 | VR 火场、环境变化、生理/装备反馈和训练记录。 | 服务器断电、网络中断、烟雾遮挡可成为安全故障脚本；保留“撤离优先”硬门禁。 | [FLAIM Trainer](https://flaimsystems.com/flaim-trainer/) | 商业高风险训练产品；厂商一手页 |
| VirTra | VirTra | 警察、军队和公共安全人员在 180/300 度情景屏中练习决策、沟通和用力升级。 | 高沉浸多屏情景、分支脚本、教练控制和表现复盘。 | 为机器人/货架培训增加“口头确认—风险升级—急停”分支，并让教练现场注入事件。 | [VirTra Training Systems](https://www.virtra.com/) | 商业高风险训练产品；厂商一手页 |
| RiVR Link | RiVR | 消防、警察和企业安全团队用 360°真实场景进行调查、疏散和应急决策练习。 | 实景 VR、分支任务、多人/教练模式和评估。 | 用真实仓库照片/视频制作“现场勘查”前置任务，再进入 3D 装配步骤。 | [RiVR](https://www.rivr.com/) | 商业训练产品；厂商一手页 |
| SimSpray | 3M / SimSpray | 涂装学员在 VR 中练习喷涂距离、速度、覆盖率和安全流程，减少材料浪费。 | VR 喷枪追踪、实时视觉反馈、覆盖率指标和课程报告。 | 划线贴码可借鉴轨迹、速度、覆盖率的过程评分；结果与二维码是否贴对分开统计。 | [3M SimSpray](https://www.3m.com/3M/en_US/metalworking-us/simulated-spray-painting/) | 商业职业教育产品；厂商一手页 |
| 3D Organon | 3D Organon | 医学生在桌面或 VR 中探索可拆解的人体模型并完成测验。 | 交互式 3D 解剖、语音搜索、测验和学习分析。 | 为仓储设备做可拆解 3D 模型与部件检索，支持“先认识对象、再执行步骤”。 | [3D Organon](https://www.3dorganon.com/) | 商业教育产品；厂商一手页 |
| U.S. Department of Labor HAZWOPER VR research | U.S. Department of Labor / OSHA | 高危作业人员在研究型 VR 场景中练习危险识别、个人防护和应急响应。 | 官方研究/培训资源记录 VR 对风险识别和程序练习的应用。 | 将 PPE、断电、禁入区设为进入装配关卡前的硬性检查，并记录漏检原因。 | [OSHA Safety and Health Topics](https://www.osha.gov/emergency-preparedness) | 政府资源；一手公共部门页面 |
| University of Maryland Fire and Rescue Institute VR training | University of Maryland | 消防学员使用沉浸式模拟进行火场搜索、团队协作和决策训练。 | 大学研究/教学项目使用 VR 情景与教练回放。 | 参考“团队角色 + 教练回放”模式，为服务器安装设计双人协作和交接班评分。 | [UMD FRSI](https://frs.umd.edu/) | 大学项目；机构一手页 |

## 对 BPF 的共同边界

- AI/视觉/动作模型负责提示、解释、分支情景、推荐和复盘；TexCore 继续独立决定步骤完成、门禁、积分和考核结果。
- 远程专家、自动评分和生成式教练都应记录来源、时间、设备/模型版本和置信度；教师可以覆盖自动结论。
- 优先以“服务器系统安装”做 MVP：当前步骤问答、故障分支、过程数据和三条复盘建议均可离线退化为预置内容。
