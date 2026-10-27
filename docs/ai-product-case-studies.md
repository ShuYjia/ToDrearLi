# AI 与 MR/VR 实训产品案例（用户侧）

> 调研范围：面向最终用户的 AI+MR/VR 培训、工业指导、数字孪生、动作/语音评估和学习分析产品。链接均指向厂商、学校、政府或研究机构的一手页面。
> “已核验事实”只描述来源明确写出的能力；“对 BPF 的启发”是基于本项目（MR 操作培训、TexCore 步骤/积分、PICO 设备）的产品解释，不代表案例已经在 BPF 中实现。

## 执行摘要：最值得 BPF 优先借鉴的 10 个

1. **Talespin CoPilot**：生成式 AI 角色扮演教练，适合把固定步骤扩展成可追问的情景训练。
2. **Bodyswaps**：VR 对话练习 + AI 语音/行为分析，适合“看我”式软技能评价。
3. **Virti**：AI 虚拟人、分支情景和表现分析，适合教练、考核、复盘一体化。
4. **Oxford Medical Simulation**：AI 虚拟病人和动态临床情景，适合把错误变成安全的故障演练。
5. **Labster AI Tutor**：实验过程中随时问答，适合 BPF 的步骤知识助手。
6. **Interplay Learning**：技能视频/VR 训练与 AI 个性化学习路径，适合弱项推荐。
7. **TeamViewer Frontline xPick**：计算机视觉辅助拣选，适合仓储、货架、二维码等现场作业。
8. **Augmentir Connected Worker**：AI 根据作业数据识别技能缺口并推荐培训，适合教师端学员画像。
9. **Tulip Frontline Copilot / Vision**：自然语言查询现场数据和视觉质检，适合把 BPF 操作记录转为可解释报告。
10. **NVIDIA Omniverse + ACE**：数字孪生场景中的 AI 角色/语音交互，适合未来的智能设备和虚拟教练。

## A. AI 教练、虚拟人和对话式实训

| 案例（组织） | 形态 | 已核验事实（官方来源） | 对 BPF 的启发 | 来源与可信度 |
|---|---|---|---|---|
| Talespin CoPilot（Talespin） | VR/桌面；虚拟人 | CoPilot 用生成式 AI 驱动可对话的虚拟角色，支持实时角色扮演和反馈。 | 将“下一步/提示”升级为能理解上下文的安全教练；仍由 TexCore 决定步骤和得分。 | [Talespin CoPilot](https://www.talespin.com/copilot)；商业产品 |
| Bodyswaps（Bodyswaps） | VR；语音交互 | 平台让学习者与虚拟人练习对话，并用 AI 分析语音、语言和行为后给反馈。 | 可复用“看我—评分—复盘”模式，先从沟通/安全 briefing 模块试点。 | [Bodyswaps](https://www.bodyswaps.co/)；商业产品 |
| Virti（Virti） | VR/AR/移动端；AI 虚拟人 | Virti 提供 AI 驱动的虚拟人、分支情景、语音对话和学习表现分析。 | 为每个 BPF 模块增加情景 NPC、错误追问和训练报告。 | [Virti AI](https://virti.com/ai/)；商业产品 |
| Mursion（Mursion） | VR/桌面；对话模拟 | Mursion 的模拟训练结合 AI 与真人模拟师，提供可重复的困难对话练习和反馈。 | 适合仓储主管、客户沟通、事故上报等“非纯装配”任务。 | [Mursion](https://www.mursion.com/)；商业产品/混合服务 |
| EON-XR AI Assistant（EON Reality） | XR；对话式助手 | EON-XR 宣传 AI Creator、AI Assistant，可在 3D/XR 内容中问答并生成讲解。 | BPF 的知识问答应绑定当前模块、步骤和来源，而不是开放式闲聊。 | [EON-XR](https://eonreality.com/eon-xr/)；商业产品 |
| 3spin Learning（3spin） | VR/AR；AI 内容与教练 | 3spin 提供沉浸式课程创作、AI 生成内容和学习分析能力。 | 参考“教练提示模板”和课程作者工具，降低新增模块成本。 | [3spin Learning](https://www.3spin-learning.com/)；商业产品 |
| Talespin Virtual Human（Talespin） | VR；情景角色 | Talespin 的虚拟人可在训练中根据学习者语言作出不同反应。 | 可做“设备故障客户/现场主管”角色，增加压力下的决策训练。 | [Virtual Human](https://www.talespin.com/virtual-human)；商业产品 |
| Mursion AI Coach 研究项目（Mursion/高校合作） | VR；研究型对话 | 官方研究页面记录 AI 角色和反馈在职业沟通训练中的试验。 | 先以 pilot 形式验证对话评分与学习增益，再纳入正式考核。 | [Mursion research](https://www.mursion.com/research)；试点/研究 |

## B. 医疗、实验和高风险行业的 AI 模拟病人/情景

| 案例（组织） | 形态 | 已核验事实（官方来源） | 对 BPF 的启发 | 来源与可信度 |
|---|---|---|---|---|
| Oxford Medical Simulation（OMS） | VR/桌面；AI 虚拟病人 | OMS 使用 AI 驱动的虚拟病人，根据学习者决策实时改变病情和情景，并提供评分。 | 可把“服务器故障、网络错误、装配漏件”做成动态故障树。 | [OMS](https://www.oxfordmedicalsimulation.com/)；商业产品 |
| SimX VR Medical Simulation（SimX） | VR；多人协作 | SimX 提供多人 VR 临床情景，虚拟病人和环境状态随操作变化。 | BPF 可增加双人/多人协作安装与交接班演练。 | [SimX](https://www.simxvr.com/)；商业产品 |
| FundamentalVR（FundamentalVR） | VR；触觉与技能评估 | 平台提供手术 VR、触觉反馈、客观技能指标和训练记录。 | 参考其“过程质量”指标，不只记录是否完成。 | [FundamentalVR](https://www.fundamentalvr.com/)；商业产品 |
| PrecisionOS（PrecisionOS） | VR；手术模拟 | PrecisionOS 提供沉浸式手术训练、任务评分、动作和时间数据。 | 为货架/机器人安装建立动作、时间、顺序的综合评分。 | [PrecisionOS](https://www.precisionostech.com/)；商业产品 |
| CAE Healthcare（CAE） | VR/模拟人；AI 数据 | CAE 的临床模拟器和学习平台记录实时生理状态、决策及 debrief 数据。 | 设计“操作事件流 + 教练复盘”数据结构。 | [CAE Healthcare](https://www.caehealthcare.com/)；商业产品 |
| Labster AI Tutor（Labster） | Web/VR 实验；AI Tutor | Labster 官方产品页介绍 AI Tutor，可在实验中回答问题、解释概念并提供引导。 | 将 PanelTopicBase 的题干、提示、资料接成带来源的步骤问答。 | [Labster AI Tutor](https://www.labster.com/ai-tutor)；商业产品 |
| Oxford Brookes Virtual Patient（Oxford Brookes University） | VR/桌面；研究 | 学校官方项目以虚拟病人研究自适应反馈和沉浸式临床决策。 | 适合借鉴“错误后改变后续情景”的教学设计。 | [项目页](https://www.brookes.ac.uk/research/virtual-reality)；研究 |
| HoloPatient（Bodyswaps/医疗教育合作） | AR/VR；虚拟患者 | HoloPatient 以 360°视频和互动虚拟病人为护理和医学培训提供情境练习。 | 将 BPF 的安全规范、设备说明做成可重复的现场情景。 | [HoloPatient](https://www.bodyswaps.co/holopatient)；商业/合作项目 |

## C. 工业、仓储和现场作业的 AI 指导/视觉辅助

| 案例（组织） | 形态 | 已核验事实（官方来源） | 对 BPF 的启发 | 来源与可信度 |
|---|---|---|---|---|
| TeamViewer Frontline xPick（TeamViewer） | AR 眼镜；计算机视觉 | xPick 用视觉识别、条码和 AR 指引拣选、复核和免手操作。 | 适合 M05 划线贴码、货架入场、二维码定位等任务。 | [xPick](https://www.teamviewer.com/en/solutions/frontline/xpick/)；商业产品 |
| TeamViewer Frontline xMake（TeamViewer） | AR 眼镜；装配指导 | xMake 提供逐步装配指导、工位确认和作业记录。 | 与 TexCore StepObj 对齐，增加“确认物体/顺序/工具”检查。 | [xMake](https://www.teamviewer.com/en/solutions/frontline/xmake/)；商业产品 |
| PTC Vuforia Expert Capture（PTC） | AR；空间/视觉捕捉 | Expert Capture 记录专家操作并生成 AR 工作指引，支持现场人员按步骤执行。 | 将优秀学员的操作录制为新手可复用的示范。 | [Vuforia Expert Capture](https://www.ptc.com/en/products/vuforia/vuforia-expert-capture)；商业产品 |
| PTC Vuforia Chalk（PTC） | AR；远程协作 | Chalk 让远程专家在实时视频上标注现场对象，支持 AR 协作排障。 | 可做教师远程指导和“标注当前物体”的辅助模式。 | [Vuforia Chalk](https://www.ptc.com/en/products/vuforia/vuforia-chalk)；商业产品 |
| Augmentir Connected Worker（Augmentir） | AR/移动端；AI 技能分析 | Augmentir 用 AI 分析作业数据、识别技能缺口并推荐个性化指导。 | 将 BPF 的错误次数、耗时、提示请求转为个性化训练计划。 | [Augmentir AI](https://www.augmentir.com/ai-powered-connected-worker)；商业产品 |
| Tulip Frontline Copilot（Tulip） | 平板/可穿戴；生成式 AI | Frontline Copilot 通过自然语言查询现场数据、生成工作流辅助和知识回答。 | 教师可直接问“谁在 M02 的 IP 配置最容易错？”并得到依据。 | [Tulip Copilot](https://tulip.co/platform/frontline-copilot/)；商业产品 |
| Tulip Vision（Tulip） | 摄像头；视觉检测 | Tulip Vision 用计算机视觉检查装配、存在性和质量条件，并把结果接入工作流。 | 对货架漏装、二维码位置、工具摆放做视觉辅助判定。 | [Tulip Vision](https://tulip.co/platform/vision/)；商业产品 |
| Poka AI（Poka） | Web/移动；工人知识助手 | Poka 提供面向一线员工的数字化作业指导、知识搜索和 AI 辅助。 | 建立“当前步骤知识卡 + 问答 + 专家内容”闭环。 | [Poka](https://www.poka.io/)；商业产品 |
| Dozuki AI（Dozuki） | Web/AR；知识与作业指导 | Dozuki 提供数字作业指导、搜索和 AI 辅助创建/检索标准作业。 | 把培训资料和步骤对象统一成可检索的 SOP。 | [Dozuki](https://www.dozuki.com/)；商业产品 |
| Honeywell Immersive Field Simulator（Honeywell） | VR；工业操作模拟 | Honeywell 官方方案使用沉浸式模拟训练过程控制和现场操作。 | 参考其高风险、低成本重复训练模式，构建设备故障关卡。 | [Honeywell immersive training](https://www.honeywell.com/us/en/news/2022/03/immersive-training)；商业方案 |
| Siemens Industrial Copilot（Siemens/Microsoft） | 工业软件；生成式 AI | Industrial Copilot 为工程、维护和操作人员提供自然语言技术问答、代码/工单辅助。 | 可作为 BPF 教师端/维护端的知识助手，而非直接控制场景。 | [Siemens Industrial Copilot](https://www.siemens.com/global/en/company/stories/industry/2023/industrial-copilot.html)；商业产品 |
| ABB Ability Genix Copilot（ABB） | 工业数字孪生；生成式 AI | Genix Copilot 用自然语言访问工业数据并辅助运维决策。 | 将训练数据转为“错误趋势、设备风险、班组对比”摘要。 | [ABB Genix Copilot](https://new.abb.com/news/detail/107099/abb-launches-genix-copilot)；商业产品 |
| NVIDIA Omniverse + ACE（NVIDIA） | 数字孪生；AI 角色/语音 | Omniverse 用于工业数字孪生；ACE 提供语音、对话和动画 AI，可驱动虚拟角色。 | 为 BPF 的仓库/机器人场景加入可问答的虚拟主管或设备助手。 | [Omniverse](https://www.nvidia.com/en-us/omniverse/)、[ACE](https://www.nvidia.com/en-us/ai-data-science/products/ace/)；商业平台 |
| Microsoft Dynamics 365 Guides + Copilot | HoloLens；混合现实指导 | Guides 在视野中显示分步 3D 指引；Copilot 可帮助创建和查询业务知识。 | 适合把 BPF 步骤说明、3D 标注与 AI 问答结合。 | [Dynamics 365 Guides](https://www.microsoft.com/en-us/dynamics-365/products/guides)、[Copilot](https://learn.microsoft.com/en-us/dynamics365/mixed-reality/guides/)；商业产品 |
| TeamViewer Frontline xInspect（TeamViewer） | AR；远程/AI 检查 | xInspect 提供现场检查清单、远程专家和视觉标注。 | 适合设备巡检考核和教师远程复核。 | [xInspect](https://www.teamviewer.com/en/solutions/frontline/xinspect/)；商业产品 |

## D. 自适应学习、技能分析和大规模培训

| 案例（组织） | 形态 | 已核验事实（官方来源） | 对 BPF 的启发 | 来源与可信度 |
|---|---|---|---|---|
| Interplay Learning（Interplay） | VR/移动；技能学习 | 平台提供模拟练习、技能评估和基于表现的学习路径，覆盖 HVAC、电气、制造等技能。 | 为每位学员按错误类型推荐 M02/M05 等后续模块。 | [Interplay Learning](https://www.interplaylearning.com/)；商业产品 |
| Strivr（Strivr） | VR；企业学习分析 | Strivr 提供 VR 课程、行为练习和学习数据分析，服务安全、沟通和操作培训。 | 参考其企业级部署、课程分发和效果仪表板。 | [Strivr](https://www.strivr.com/)；商业产品 |
| Transfr VR（Transfr） | VR；职业技能模拟 | Transfr 提供职业技能 VR 模拟、教练提示和学习者进度/技能数据。 | 与 BPF 的职业教育定位接近，可借鉴技能徽章和就业准备报告。 | [Transfr](https://transfrinc.com/)；商业产品 |
| zSpace Career & Technical Education（zSpace） | AR/VR 桌面；交互式课程 | zSpace 用 3D/AR 课程教授制造、医疗和 STEM 技能，并提供教师管理工具。 | 适合把 BPF 模块拆成可重复的课堂任务与教师观察面板。 | [zSpace CTE](https://zspace.com/education/career-technical-education)；商业产品 |
| EON Spatial AI / Creator（EON Reality） | XR；空间 AI | EON 官方介绍 Spatial AI 可从资料生成 3D/XR 学习内容和问答。 | 用于快速生成设备说明、空间标注和多语言提示。 | [EON Spatial AI](https://eonreality.com/spatial-ai/)；商业产品 |
| Area9 Rhapsode（Area9 Lyceum） | Web/移动；自适应学习 AI | Rhapsode 根据答题和掌握度动态调整内容、难度和复习间隔。 | 将固定 TexCore 关卡外包给自适应的理论复习层。 | [Rhapsode](https://area9lyceum.com/rhapsode/)；商业产品 |
| Sana Learn（Sana Labs） | Web/移动；AI 学习助手 | Sana 提供企业知识问答、课程生成、个性化学习和学习分析。 | 适合作为 BPF 教师端知识库和跨模块搜索参考。 | [Sana Learn](https://sanalabs.com/learn)；商业产品 |
| Docebo AI（Docebo） | Web/移动；学习平台 AI | Docebo AI 支持内容生成、技能标注、推荐和企业学习分析。 | 借鉴课程推荐、技能图谱和组织级报表。 | [Docebo AI](https://www.docebo.com/ai/)；商业产品 |
| Area9 VR Firefighter Training（消防培训项目） | VR；自适应情景 | 官方项目页面描述 VR 火灾情景和按学员表现变化的训练。 | 适合 BPF 做安全事故、设备断电、网络中断等变体考核。 | [Area9 case studies](https://area9lyceum.com/case-studies/)；项目/商业 |

## E. 研究与公共部门示范项目

| 案例（组织） | 形态 | 已核验事实（官方来源） | 对 BPF 的启发 | 来源与可信度 |
|---|---|---|---|---|
| U.S. Army Synthetic Training Environment（美国陆军） | VR/AR；大规模仿真 | 美军将 AI、云和数字孪生用于可扩展的合成训练环境和任务复盘。 | 参考“同一场景多种任务脚本 + 统一数据记录”的架构。 | [Army STE](https://www.army.mil/article/259911/synthetic_training_environment)；政府项目 |
| U.S. Air Force Pilot Training Next（美国空军） | VR；数据驱动教练 | 官方项目使用 VR、传感器和学习数据加速飞行员训练，并持续调整训练内容。 | 证明“短时高频练习 + 数据化复盘”适合技能培训。 | [Pilot Training Next](https://www.af.mil/News/Article-Display/Article/1807774/pilot-training-next/)；试点 |
| NASA Virtual Reality Laboratory Training（NASA） | VR；高风险作业模拟 | NASA 官方项目使用 VR 模拟航天器维护、舱外活动和应急流程。 | 对 BPF 的设备安装/故障处置关卡提供高风险训练范式。 | [NASA VR](https://www.nasa.gov/virtual-reality/)；政府项目 |
| NHS/Health Education England VR Training（英国 NHS） | VR；临床情景 | NHS 官方项目采用 VR 情景训练护理和临床沟通，并收集学习反馈。 | 适合 BPF 的安全沟通、交接、事故上报等软技能模块。 | [NHS VR training](https://www.england.nhs.uk/learning-disability/improving-health/virtual-reality/)；公共项目 |
| Boeing Virtual Maintenance Training（Boeing） | VR/AR；维护训练 | Boeing 官方介绍使用沉浸式技术训练飞机维护和装配流程。 | 借鉴“按工序、工具和安全检查点”组织关卡。 | [Boeing immersive training](https://www.boeing.com/innovation/virtual-reality)；企业项目 |
| Walmart VR Associate Training（Walmart） | VR；情景训练 | Walmart 官方宣布用 VR 模拟客户服务、设备和高峰运营场景，并通过数据评估学习。 | 可参考大规模门店/仓储培训的短课程和结果指标。 | [Walmart VR training](https://corporate.walmart.com/news/2017/06/08/walmart-to-train-1-million-associates-with-vr)；企业项目 |
| UPS VR Driver Training（UPS） | VR；安全驾驶模拟 | UPS 官方介绍 VR 用于驾驶员安全和配送情景训练。 | 适合将机器人入场、搬运路线、安全距离做成可重复考核。 | [UPS VR](https://about.ups.com/us/en/our-company/innovation/vr-training.html)；企业项目 |

## 对 BPF 的产品化结论

### 可以直接移植的交互模式

- **问当前步骤**：答案只检索当前 Node/Topic/Step 及其资料，并显示来源。
- **分级提示**：第一次只给方向，第二次给关键条件，第三次显示示范；考核模式关闭答案。
- **观察与复盘**：记录步骤耗时、重试、提示次数、工具/对象选择和错误类型。
- **动态故障**：由规则引擎选择故障变量，AI 只负责解释和追问。
- **教师 Copilot**：按班级、模块、步骤和错误聚类生成报告。
- **多语言/无障碍**：语音输入、文字字幕、简明语言和术语解释并行提供。

### 推荐的边界

AI 可以生成提示、解释、追问、个性化推荐和复盘摘要；TexCore 仍应独立决定步骤完成、门禁、积分和考核结果。所有视觉/语音评分都应显示置信度，并允许教师复核。

### 首个可验证 MVP

选择“服务器系统安装”模块：用户说“为什么要这样设置？”或“给我提示”，系统返回带模块/步骤来源的回答；记录耗时和重试；训练结束生成三条改进建议。离线时退回预置提示，原有控制器流程不受影响。
