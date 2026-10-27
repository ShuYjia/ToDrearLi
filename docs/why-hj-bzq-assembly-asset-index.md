# 06 货架安装与 07 播种墙安装资产及步骤索引

> 核对日期：2026-07-27
> 状态：旧工程只读取证索引，尚未迁入当前 BPF 场景
> 适用模块：`WHY_HJ`（06 货架安装）、`WHY_BZQ`（07 播种墙安装）

## 1. 索引用途

本文档记录旧 MRTKsync 工程中 06、07 两个组装关卡的完整资产来源和步骤关系，供后续迁移到当前 BPF `TexCore` 步骤框架时使用。

索引覆盖：

- 背包中生成或取出的可搬运物品；
- 场景中的安装目标、蓝色待装件、绿色预览件和安装后实物；
- 步骤完成条件；
- UI 提示文案和对应语音；
- 来源 FBX、总关卡 Prefab、工具及辅助资源；
- 旧配置中必须修正的异常。

本文档不表示这些资产已经迁入当前工程。当前工程中没有 `Assets/WHY_HJ` 和 `Assets/WHY_BZQ` 目录。

## 2. 取证范围

旧工程根目录：

```text
D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4
```

旧场景：

```text
Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity
```

旧场景对象：

```text
SHIXUN/WHY_HJ
SHIXUN/WHY_BZQ
```

自包含总关卡 Prefab：

```text
Assets/WHY_HJ/Prefabs/WHY_HJ.prefab
Assets/WHY_BZQ/Prefabs/WHY_BZQ.prefab
```

## 3. 资产完整性结论

| 模块 | 总 Prefab | 大小（约） | 来源 FBX 数 | 关键网格缺失 | 当前工程状态 |
|---|---|---:|---:|---|---|
| 06 货架安装 | `Assets/WHY_HJ/Prefabs/WHY_HJ.prefab` | 4.35 MB | 4 | 未发现 `m_Mesh` 丢失 | 尚未迁入 |
| 07 播种墙安装 | `Assets/WHY_BZQ/Prefabs/WHY_BZQ.prefab` | 9.17 MB | 4 | 未发现 `m_Mesh` 丢失 | 尚未迁入 |

结论：两关的实体模型、场景装配状态和语音基本可以找到，但多数零件并不是独立 Prefab，而是来源 FBX 的子节点以及总关卡 Prefab 中的实例。后续不能只复制 FBX；还必须恢复每件物品的状态对应关系。

每个可安装物品应在当前框架中形成以下映射：

```text
背包可搬运物品
  -> 安装目标/触发区
  -> 绿色定位预览
  -> 安装后最终实物
  -> 本步骤完成检测
```

## 4. 06 货架安装资产索引

### 4.1 来源模型

| 类型 | 旧工程相对路径 | 用途 |
|---|---|---|
| 货架主体及零件 | `Assets/WHY_HJ/Models/GameMain/Models/WHY/HuoJia.fbx` | 底座、立柱、横梁、层板、螺丝、库位盒等 |
| 内六角扳手 | `Assets/WHY_HJ/Models/GameMain/Models/JYJ/NeiLiuJiaoBanShou.fbx` | 紧固辅助工具 |
| 胶水 | `Assets/WHY_HJ/Models/GameMain/Res/Models/JiaoShui/Model/JiaoShui.FBX` | 螺丝固定步骤 |
| 橡胶锤 | `Assets/WHY_HJ/Models/GameMain/Entity/橡胶锤-11.FBX` | 横梁敲入卡扣 |
| 指引箭头 | `Assets/WHY_HJ/Support/GuidingArrow.prefab` | 场景指引 |

### 4.2 步骤、物品、UI 与语音

| 序号 | 内部状态 | 背包/场景物品 | 操作与完成条件 | UI 提示原文 | 语音 |
|---:|---|---|---|---|---|
| 1 | `InspectHJ` | 货架零件入口 | 打开背包，进入组装流程 | 打开UI面板从背包中选择货架零件开始安装 | `6—1.MP3` |
| 2 | `Placebase` | 底座 | 从背包取出底座并放到安装区 | 从背包中选择底座零件，摆放至合适位置开始安装。 | `6—2.MP3` |
| 3 | `Tiltingbase` | 底座、货架二维码 | 翻转底座，将二维码粘贴到货架正下方 | 将底座翻倒，从背包中取出货架二维码，粘贴至货架正下方。 | `6—3.MP3` |
| 4 | `Installupright` | 4 根立柱 | 四根立柱全部插入底座并固定 | 将四根立柱插入底座固定好 | `6—4.MP3` |
| 5 | `Putscrew` / `Dropglue` / `Setscrew` | 16 颗螺丝、胶水、电钻/扳手 | 每根立柱 4 颗螺丝，涂胶后紧固 | 固定螺丝到每个立柱，一个立柱四个螺丝，将螺丝使用胶水之后在用电钻进行紧固。 | `6—5.MP3` |
| 6 | `Downlaminate` | 底层层板 | 将底层层板放到底座 | 放下层板至底座。 | `6—6.MP3` |
| 7 | `Installbeam1` / `Hammerboard1` | 第一层横梁、橡胶锤 | 横梁安装后敲入卡扣 | 安装第一层横梁，并且使用橡皮锤锤击让横梁进入至卡扣从而更加紧固，橡皮锤可以从背包中工具选项中拿取。 | `6—7.MP3` |
| 8 | `Putlaminate1` | 第一块层板 | 将层板放到横梁上 | 放下层板至横梁。 | `6—8.MP3` |
| 9 | `Installbeam2` / `Hammerboard2` | 第二层横梁、橡胶锤 | 横梁安装后敲入卡扣 | 安装下一层横梁，并且使用橡皮锤锤击让横梁进入至卡扣从而更加紧固，橡皮锤可以从背包中工具选项中拿取。 | `6—9.MP3` |
| 10 | `Putlaminate2` | 第二块层板 | 将层板放到横梁上 | 同样我们安装层板，将层板放置横梁上。 | `6—10.MP3` |
| 11 | `Installbeam3` / `Hammerboard3` | 第三层横梁、橡胶锤 | 横梁安装后敲入卡扣 | 安装下一层横梁，并且使用橡皮锤锤击让横梁进入至卡扣从而更加紧固，橡皮锤可以从背包中工具选项中拿取。 | `6—11.MP3` |
| 12 | `Putlaminate3` | 第三块层板 | 将层板放到横梁上 | 放下层板至横梁。 | `6—12.MP3` |
| 13 | `Putbox` | 库位盒 | 将库位盒放到层板上 | 将库位盒摆放至层板。 | `6—13.MP3` |
| 14 | `InstallhJ` | 完整货架 | 将组装完成的货架移动到暂存区 | 将安装好的货架放置暂存区。 | `6—14.MP3` |

06 语音目录：

```text
Assets/WHY_HJ/Support/GameMain/Res/Scenes/Music/货架安装
```

说明：旧状态机将螺丝、涂胶、紧固和三次锤击拆成独立状态，但 UI/语音将相邻动作合并提示。迁移时可以保留独立交互检测，同时复用合并提示。

## 5. 07 播种墙安装资产索引

### 5.1 来源模型

| 类型 | 旧工程相对路径 | 用途 |
|---|---|---|
| 播种墙主体及零件 | `Assets/WHY_BZQ/Models/GameMain/Models/WHY/BoZhongQiang 1.fbx` | 立柱、斜撑、横梁、层板及主体组成件 |
| 补充整体模型 | `Assets/WHY_BZQ/Models/GameMain/Entity/整体.fbx` | 电脑、扫描枪或整体设备相关组成件 |
| 场景补充模型 | `Assets/WHY_BZQ/Models/GameMain/Models/JYJ/MR_ChangJing-2.fbx` | 场景和设备补充节点 |
| 手电钻 | `Assets/WHY_BZQ/Models/GameMain/Models/JYJ/ShouDianZuan.fbx` | 螺丝紧固工具 |
| 指引箭头 | `Assets/WHY_BZQ/Support/GuidingArrow.prefab` | 场景指引 |

### 5.2 步骤、物品、UI 与语音

| 序号 | 内部状态 | 背包/场景物品 | 操作与完成条件 | UI 提示原文 | 语音 |
|---:|---|---|---|---|---|
| 1 | 准备阶段 | 播种墙零件入口 | 打开背包并准备零件 | 从背包中取出播种墙零件。 | `7—1.MP3` |
| 2 | `Setupright` | 4 根立柱 | 小孔朝下插入底座；4 根全部激活 | 依次从零件中取出立柱，按照小孔朝下的方向将立柱插入底座的孔内，检查四根立柱是否都固定好。 | `7—2.MP3` |
| 3 | `Installbeam` | 10 个斜撑、12 颗固定螺丝 | 斜撑与对应螺丝全部安装 | 从背包中取出横梁并且进行安装，之后在使用螺丝进行紧固。 | `7—3.MP3` |
| 4 | `Installbeam4` | `HengLiang3`、`HengLiang5` | 两根横梁全部安装 | 横梁提示同上 | `7—3.MP3` |
| 5 | `Installlaminate1` | 第一块层板 | 层板安装到位 | 从背包中取出层板并且进行安装。 | `7—4 1.MP3` |
| 6 | `Installbeam5` | `HengLiang6`、`HengLiang2` | 两根横梁全部安装 | 横梁提示同上 | `7—3.MP3` |
| 7 | `Installlaminate2` | 第二块层板 | 层板安装到位 | 层板提示同上 | `7—4 1.MP3` |
| 8 | `Installbeam6` | `HengLiang1`、`HengLiang7` | 两根横梁全部安装 | 横梁提示同上 | `7—3.MP3` |
| 9 | `Installlaminate3` | 第三块层板 | 层板安装到位 | 层板提示同上 | `7—4 1.MP3` |
| 10 | `Installbeam7` | `HengLiang`、`HengLiang4` | 两根横梁全部安装 | 横梁提示同上 | `7—3.MP3` |
| 11 | `Installlaminate4` | 第四块层板 | 层板安装到位 | 层板提示同上 | `7—4 1.MP3` |
| 12 | `Putgun` | 扫描枪 | 安装到指定位置 | 从背包中取出扫描枪安装至指定位置。 | `7—5.MP3` |
| 13 | `PutComputer` | 电脑主机 | 安装到指定位置 | 从背包中取出电脑主机安装至指定位置。 | `7—6.MP3` |
| 14 | `Putsupport` | 显示器支架 | 安装到指定位置 | 从背包中取出电脑显示器支架，安装至指定位置。 | `7—7.MP3` |
| 15 | `Putscrew1` | 支架固定螺丝 | 两颗支架螺丝安装到位 | 安装好显示器支架之后，在用螺丝固定支架。 | `7—8.MP3` |
| 16 | `PutKeyboard` | 键盘、键盘支架 | 两个对象全部安装并连接主机 | 从背包中拿出键盘连接电脑主机。 | `7—9.MP3` |
| 17 | `Putdisplay` | 显示器 | 显示器安装到支架 | 从背包中拿出显示器安装至显示器支架进行固定。 | `7—10 1.MP3` |
| 18 | `Installslot` / `Slotscrew` | 3 段底槽、18 颗螺丝 | 底槽与固定螺丝全部安装 | 从背包中拿出卡槽底，使用螺丝进行固定。 | `7—11.MP3` |
| 19 | `Terminal` | 12 个端子 | `BZQ_DuanZi` 至 `BZQ_DuanZi011` 全部安装 | 从背包中拿出端子，端子的功能为固定排线。 | `7—12.MP3` |
| 20 | `Installtag` | 12 个电子标签 | 标签全部安装；迁移后统一检测 1 至 12 | 从背包中拿出电子标签，安装固定位置。 | `7—13.MP3` |
| 21 | `InstallBZQ` | 完整播种墙 | 所有最终装配状态满足 | 播种墙安装完成 | 无 |

07 语音目录：

```text
Assets/WHY_BZQ/Support/GameMain/Res/Scenes/Music/播种墙安装
```

## 6. 场景对象状态对应表

旧总 Prefab 采用以下分组。迁移时应按物品逐一建立对应，不要只复制可搬运模型。

| 旧对象分组 | 作用 | 当前 BPF 迁移建议 |
|---|---|---|
| `*_cube` | 背包生成或玩家搬运的物品 | 制作为明确命名的可交互零件 Prefab |
| `*_blue` | 未完成时显示安装位置 | 作为目标提示或步骤高亮对象 |
| `*_green` | 进入正确区域时的绿色预览 | 作为目标匹配预览；默认隐藏 |
| `*_shi` | 安装成功后的最终实物 | 作为完成态对象；默认隐藏 |
| `TriggerBase` 所在对象 | 检查物品与目标匹配 | 改用当前 `TexCore_InteracterBase` 体系或当前项目已有放置组件 |
| `StepALLTeskController` | 检查一组最终实物全部激活 | 映射到当前步骤完成条件 |
| `HjBzqTaskController` | 检查整关完成 | 映射到当前 Topic/关卡完成状态 |
| `ZhiYin` | 旧 UI、文字、语音、箭头 | 仅复用文字、音频和指引意图；UI 使用当前统一框架 |

## 7. 旧配置异常与迁移约束

| 问题 | 旧配置表现 | 迁移处理 |
|---|---|---|
| 支架螺丝重复引用 | `Putscrew1` 两次引用 `DNLuoSiOn/LuoSi5_1` | 改为两个不同螺丝对象，并现场核对另一对象是否为 `LuoSi3_1` |
| 第四块层板任务值错误 | WHY_BZQ 的 `CengBan6` 使用任务值 `89`，落在 WHY_HJ 枚举区间 | 按 `Installlaminate4` 重新绑定 |
| 标签完成条件不统一 | 步骤检测标签 3 至 12，整关检测标签 1 至 12 | 统一为 12 个标签全部完成 |
| 背包按钮不是零件 Prefab | 旧 `ObjofBagDefine` 指向通用 `Tank.prefab` | 当前框架中每个按钮明确引用实际零件 Prefab |
| 旧 UI 重复 | 简单 Text 版和任务提示 UI 版同时存在 | 只迁移文字与音频，统一使用当前 BPF UI |
| 旧框架依赖 | `LoadSceneObj`、`MultController`、`ShiXun`、`PersonMove`、Mirror/SycObj | 不直接搬旧业务脚本，改为当前 BPF 状态与记录体系 |
| 外部 GUID 不完整 | 部分旧 MRTK 脚本、按钮材质、字体、动画和 UI 引用无法在旧工程 Assets/Packages 中解析 | 不影响已确认的关键 Mesh；迁移时用当前框架依赖替代 |

## 8. 当前工程建议落位

迁移前必须先检查当前 `Assets/...` 实际目录。不要照抄旧文档里的历史 `Assets/App/...` 路径。建议按当前项目所有权放置：

| 内容 | 建议位置 |
|---|---|
| 06、07 业务入口 | `Assets/Resources/BusinessPrefabs` |
| 06、07 Topic 数据/UI | `Assets/Resources/TopicPrefabs` |
| 当前交互与步骤场景对象 | `Assets/A_MRNet/Sence/03SingleplayerScene.unity` 中对应 06、07 层级 |
| 迁入的模型、材质、音频 | 先确认当前模块资产根；不存在时使用明确的当前业务资产目录，不复建历史 `Assets/App` |

## 9. 建议的迁移验收顺序

1. 先迁入 06 的来源 FBX、材质和 14 段语音。
2. 从总 Prefab 中拆出第一个“底座”步骤的四态对象。
3. 建立背包按钮到实际底座 Prefab 的引用。
4. 验证底座可取出、可搬运、目标可见、绿色预览正确、放置后最终实物显示。
5. 验证步骤状态、UI 文案、语音、返回/跳转和数据记录。
6. 用相同结构扩展 06 剩余步骤。
7. 06 完整通过后，再处理对象更多、完成条件更复杂的 07。

## 10. 核对边界

- 已确认：旧场景层级、两个总 Prefab、来源 FBX、步骤枚举、主要完成对象、UI 文案和步骤语音。
- 已确认：总 Prefab 未出现关键 `m_Mesh` GUID 丢失。
- 尚未确认：迁入当前工程后的材质外观、碰撞尺寸、手势抓取手感和 PICO 运行效果。
- 尚未实施：资产复制、Prefab 拆分、当前场景配置、运行测试、APK 导出。
