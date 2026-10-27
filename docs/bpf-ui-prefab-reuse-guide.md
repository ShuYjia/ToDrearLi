# BPF UI 预制体复用规范

> 状态：现行操作依据
> 更新日期：2026-08-04
> 适用范围：BPF / TexCore 业务题目 UI、步骤 UI、视频题 UI、提示 UI、按钮与面板样式

## 核心结论

以后新增或修改业务 UI，默认采用“复用已验证模板 / 预制体”的模式。
不要重新手工拼面板、按钮、颜色、材质和层级，除非已经确认项目内没有可复用模板。

优先级如下：

1. 复用当前项目中已经显示正确的业务 prefab 或其子物体。
2. 复用 Assets/Resources/TopicPrefabs 下的共享题型 prefab。
3. 复用 MRTK 官方 package prefab，但只在项目内没有更贴近业务风格的模板时使用。
4. 最后才考虑新增 UI 结构或代码，并且要先说明为什么不能复用。

## 标准复用方式

### 1. 先找正确样板

修改 UI 前先在项目内找到一个实际显示正确、交互正常的同类 UI。

当前可作为业务 UI 样板的路径：

- 选择题 / 练习题样板：
  Assets/Resources/BusinessPrefabs/MR混合现实多人协作平台/01货到人拣选电商仓储实施安装与调试实训系统/01服务器网络搭建/01服务器网络搭建练习题.prefab
- 视频题共享样板：
  Assets/Resources/TopicPrefabs/视频题.prefab
- 题型共享 prefab 目录：
  Assets/Resources/TopicPrefabs
- 业务节点 prefab 目录：
  Assets/Resources/BusinessPrefabs

判断样板是否可复用时，不只看名字，要确认：

- 层级是否已经在当前业务流程里运行；
- 面板、按钮、文字是否在 Unity 中显示正确；
- 按钮是否已有有效的 MRTK / TexCore 交互组件；
- 是否已经能跟随玩家、保持世界空间显示或被 TexCore 正确切换；
- 是否没有缺失脚本、缺失材质和重复事件。

### 2. 复制结构，不复制想象

复用 UI 时，应直接复制或嵌套已有 prefab / 子物体结构。

推荐做法：

- 从正确 prefab 中复制 BackPlate、按钮、标题、选项容器等子物体；
- 保留这些子物体原有的材质、字体、动画、MRTK 组件和布局组件；
- 只修改必要的文本、尺寸、位置、回调目标和业务脚本引用；
- 共享题型应改共享 prefab，让所有变体继承同一套样式；
- 业务节点特殊 UI 才在对应业务 prefab 内做局部 override。

禁止做法：

- 新建一个普通 Image 当面板，再手工调成“差不多的颜色”；
- 手写 FrameworkPanel、Backglow、Frontplate 等视觉层；
- 单独复制颜色值或材质名，而不是复制完整可工作的子物体；
- 给同一个按钮重复添加 Button、PressableButton 或 StatefulInteractable；
- 为了逻辑归属把可正常显示的 Canvas UI 强行拖到 3D 物体下面；
- 修改 Packages/org.mixedrealitytoolkit.* 上游资源本体。

## 视频题固定模式

视频题已经按此模式修正，后续同类需求应沿用同一结构。

共享 prefab：

- Assets/Resources/TopicPrefabs/视频题.prefab

当前固定层级：

    视频题
    ├─ BackPlate
    ├─ 视频画面
    │  └─ VideoSurface
    ├─ Title
    └─ 跳过

实现要求：

- BackPlate 从正确练习题 prefab 的 题目/BackPlate 复用；
- 跳过 从正确练习题 prefab 的按钮模板复用；
- VideoSurface 保留 RawImage + AspectRatioFitter，只负责显示视频纹理；
- TexCore_VideoTopic.videoSurface 必须指向 VideoSurface；
- TexCore_VideoTopic.btn_Skip 必须指向 跳过；
- 跳过按钮只保留一个点击回调：TexCore_VideoTopic.Skip；
- VideoPlayer、自然播放结束、跳过推进、无视频自动跳过等逻辑不应因 UI 样式调整被改写。

11 个模块的 00介绍视频.prefab 应继续继承共享 视频题.prefab。
除非某个模块确实有独立视觉要求，否则不要逐个修改 11 个变体。

## 选择题 / 操作题 UI 模式

选择题、图片题、文本题、操作题等普通题型，应优先复用现有题型 prefab 或现有业务题目。

推荐复用点：

- 面板背景：现有题目中的 BackPlate；
- 题干文字：现有题目中的 题干 或对应标题文本；
- 选项按钮：现有 选项 按钮；
- 确认 / 下一步按钮：现有 确定、下一步 按钮；
- 横向或纵向排布：现有 root、HorizontalLayoutGroup、LayoutElement。

只允许改：

- 显示文字；
- 选项数量；
- 绑定的业务数据；
- 必要的 RectTransform 尺寸和位置；
- 必要的 TexCore 回调或字段引用。

不要改：

- 按钮核心交互组件；
- MRTK 材质；
- 动画状态机；
- 已验证的框架按钮内部层级；
- 题型脚本的基本数据顺序。

## 实施检查表

每次 UI 修改按以下步骤执行：

1. 明确 UI 类型：视频、选择题、提示、操作步骤、按钮或菜单。
2. 找到一个当前工程内已经显示正确的同类模板。
3. 确认模板所在 prefab、子物体路径和关键组件。
4. 复制或嵌套模板结构，避免手工重建视觉层。
5. 只改业务必需字段：文本、引用、尺寸、位置、回调。
6. 保留 TexCore / MRTK 原有运行路径，不新建平行流程。
7. 改共享题型时，确认业务变体继续继承共享 prefab。
8. 导入 Unity 后检查 Console：不能新增缺失引用、缺失脚本或重复交互警告。
9. 用 git diff --check 检查序列化文件格式。
10. 只提交本次任务拥有的 prefab、scene、脚本或 docs 文件。

## 常见问题与处理

### 面板颜色不统一

原因通常是只复制了颜色或材质，没有复制完整业务 UI 模板。

处理：

- 回到一个显示正确的业务 prefab；
- 复制完整 BackPlate 或完整按钮模板；
- 不要只调 Image.color。

### 视频只有声音没有画面

原因通常是视频纹理层被背景遮挡、RawImage 未绑定、Canvas 层级或深度不对。

处理：

- 保持 VideoSurface 在 BackPlate 前方；
- 确认 TexCore_VideoTopic.videoSurface 指向 VideoSurface；
- 确认 VideoPlayer.targetTexture 仍由 TexCore_VideoTopic 配置；
- 不要让背景 Image 覆盖视频 RawImage。

### 按钮看起来对但点不动

原因通常是重复添加交互组件或事件绑定在错误子物体上。

处理：

- 找到原按钮中真正拥有 PressableButton / StatefulInteractable 的物体；
- 复用该物体；
- 清理多余持久化事件；
- 只绑定一个业务回调。

### UI 跟随或世界空间显示异常

原因通常是把 Canvas UI 拖到错误层级，或破坏了原来的世界空间 Canvas 配置。

处理：

- UI 视觉层保留在可正常渲染的 Canvas / prefab 层级；
- 逻辑归属通过 TexCore 数据、节点控制或脚本引用表达；
- 不为了“看起来属于某个物体”而破坏 Canvas 层级。

## 交付标准

UI 修改完成后，应能说明：

- 复用了哪个现有 prefab 或子物体；
- 改了哪些业务字段；
- 哪些视觉和交互组件保持原样；
- 哪些变体继承共享 prefab；
- Unity 导入后是否有新增 Console 错误；
- 本次提交包含哪些文件，排除了哪些无关改动。

如果无法找到可复用模板，应先停下来说明原因，再决定是否新增 UI。
