# 06 货架安装与 07 播种墙安装资产索引

## 文档目的

本文档记录旧实训场景中 `WHY_HJ` 和 `WHY_BZQ` 两个组装关卡的步骤、物品、模型、提示 UI 与语音来源，用于后续在当前 BPF 框架中构建：

- 06 货架安装（`WHY_HJ` / `RackAssembly`）
- 07 播种墙安装（`WHY_BZQ` / `DisplayWallAssembly`）

本文档是资产定位和迁移索引，不表示两关已在当前 03 场景中配置完成。

## 结论

两关所需业务物品均能在现有资源中找到对应模型或场景子物体，没有发现必须重新建模的核心部件。

但当前资源组织有一个重要限制：

- 两关各自只有一个总装业务 Prefab：`WHY_HJ.prefab` 和 `WHY_BZQ.prefab`。
- 立柱、横梁、层板、螺丝、端子、标签等多数物品是总装 Prefab 或总装 FBX 内的子物体。
- 它们不是“每种背包物品一个独立 Prefab”的现成结构。
- 在接入当前背包前，需要从总装 Prefab 中选取相应搬运件，制作独立的当前框架物品 Prefab，或使用框架支持的子物体模板。
- 旧背包按钮引用的通用载体和旧多人同步脚本不能直接作为当前背包物品来源。

因此，“模型能找到”的答案是肯定的；“所有物品已有可直接放进背包的独立 Prefab”的答案是否定的。

## 取证来源

### 原始旧场景

`D:/gitea_obj/_unity projection/gitea_mrtk_sync_old_4/Assets/_syc_resource/_my_scenes/sync/MRTKsync.unity`

层级来源：

- `SHIXUN/WHY_HJ`
- `SHIXUN/WHY_BZQ`

### 当前仓库中的迁入副本

06：

- 总装 Prefab：`Assets/APP/OldMRTK/WHY_HJ/Prefabs/WHY_HJ.prefab`
- 货架模型：`Assets/APP/OldMRTK/WHY_HJ/Models/GameMain/Models/WHY/HuoJia.fbx`
- 胶水模型：`Assets/APP/OldMRTK/WHY_HJ/Models/GameMain/Res/Models/JiaoShui/Model/JiaoShui.FBX`
- 橡皮锤模型：`Assets/APP/OldMRTK/WHY_HJ/Models/GameMain/Entity/橡胶锤-11.FBX`
- 内六角扳手模型：`Assets/APP/OldMRTK/WHY_HJ/Models/GameMain/Models/JYJ/NeiLiuJiaoBanShou.fbx`
- 指引箭头：`Assets/APP/OldMRTK/WHY_HJ/Support/GuidingArrow.prefab`
- 语音目录：`Assets/APP/OldMRTK/WHY_HJ/Support/GameMain/Res/Scenes/Music/货架安装`

07：

- 总装 Prefab：`Assets/APP/OldMRTK/WHY_BZQ/Prefabs/WHY_BZQ.prefab`
- 播种墙模型：`Assets/APP/OldMRTK/WHY_BZQ/Models/GameMain/Models/WHY/BoZhongQiang 1.fbx`
- 手电钻模型：`Assets/APP/OldMRTK/WHY_BZQ/Models/GameMain/Models/JYJ/ShouDianZuan.fbx`
- 指引箭头：`Assets/APP/OldMRTK/WHY_BZQ/Support/GuidingArrow.prefab`
- 语音目录：`Assets/APP/OldMRTK/WHY_BZQ/Support/GameMain/Res/Scenes/Music/播种墙安装`

## 资产状态说明

| 状态 | 含义 | 后续动作 |
| --- | --- | --- |
| 独立模型 | 有单独 FBX，可直接作为制作 Prefab 的模型源 | 补交互组件、碰撞体和框架物品定义 |
| 总装子物体 | 能在总装 Prefab/FBX 内定位，但没有单独业务 Prefab | 从总装层级提取或制作 Prefab Variant |
| 工具模型 | 工具模型存在，但旧交互依赖历史脚本 | 保留模型，重新绑定当前交互事件 |
| 目标状态 | `blue/green/shi` 等目标或完成态对象 | 保留目标位置和外观，不放入背包 |
| 待核对 | 旧配置引用或枚举存在异常 | 构建时修正，不能照抄旧序列化值 |

## 06 货架安装物品索引

06 的主模型组织在 `WHY_HJ.prefab` 的以下四套状态中：

- `HuoJia_cube`：可搬运/背包取出的零件来源
- `HuoJia_blue`：待安装目标提示
- `HuoJia_green`：正确位置绿色预览
- `HuoJia_shi`：安装完成后的最终实物

| 顺序 | 物品/工具 | 数量或操作 | 模型来源 | 当前资产形态 | 目标/完成条件 | UI 提示摘要 | 语音 |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 1 | 货架零件集合 | 打开背包 | `WHY_HJ.prefab` | 总装 Prefab | 进入货架安装流程 | 从背包选择货架零件 | `6—1.MP3` |
| 2 | 底座 | 1 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 底座放到指定安装位置 | 选择底座并摆放 | `6—2.MP3` |
| 3 | 货架二维码 | 1 | `WHY_HJ.prefab` 内对应子物体/贴图 | 总装子物体 | 底座翻转后贴到正下方 | 翻倒底座并粘贴二维码 | `6—3.MP3` |
| 4 | 立柱 | 4 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 四根立柱全部进入底座 | 将四根立柱插入底座 | `6—4.MP3` |
| 5 | 螺丝 | 每根立柱4颗，合计16颗 | `HuoJia.fbx` / Prefab 中 `LuoSi*` | 总装子物体 | 相应最终螺丝全部激活 | 每根立柱安装四颗螺丝 | `6—5.MP3` |
| 5 | 胶水 | 1件工具，可重复使用 | `JiaoShui.FBX` | 独立模型 | 对螺丝执行涂胶动作 | 与螺丝固定合并提示 | `6—5.MP3` |
| 5 | 电钻/紧固工具 | 1件工具，可重复使用 | 旧关卡层级中的工具对象 | 场景/总装子物体 | 完成螺丝紧固 | 与螺丝固定合并提示 | `6—5.MP3` |
| 6 | 底层层板 | 1 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 放到货架底座 | 放下层板至底座 | `6—6.MP3` |
| 7 | 第一层横梁 | 一组 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 横梁进入对应卡扣 | 安装第一层横梁 | `6—7.MP3` |
| 7 | 橡皮锤 | 1件工具，可重复使用 | `橡胶锤-11.FBX` | 独立模型 | 对横梁完成锤击固定 | 使用橡皮锤锤入卡扣 | `6—7.MP3` |
| 8 | 第一块层板 | 1 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 放到第一层横梁 | 层板放置横梁上 | `6—8.MP3` |
| 9 | 第二层横梁 | 一组 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 横梁进入对应卡扣并完成锤击 | 安装下一层横梁 | `6—9.MP3` |
| 10 | 第二块层板 | 1 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 放到第二层横梁 | 将层板放置横梁上 | `6—10.MP3` |
| 11 | 第三层横梁 | 一组 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 横梁进入对应卡扣并完成锤击 | 安装下一层横梁 | `6—11.MP3` |
| 12 | 第三块层板 | 1 | `HuoJia.fbx` / `HuoJia_cube` 子物体 | 总装子物体 | 放到第三层横梁 | 放下层板至横梁 | `6—12.MP3` |
| 13 | 库位盒 | 旧关卡中的一组 | `HuoJia.fbx` / `WHY_HJ.prefab` 子物体 | 总装子物体 | 全部摆放到指定层板 | 将库位盒摆放至层板 | `6—13.MP3` |
| 14 | 已组装货架 | 整体移动 | `HuoJia_shi` | 最终状态对象 | 整体货架进入暂存区 | 放置到暂存区 | `6—14.MP3` |

### 06 内部状态顺序

`InspectHJ → Placebase → Tiltingbase → Installupright → Putscrew → Dropglue → Setscrew → Downlaminate → Installbeam1 → Hammerboard1 → Putlaminate1 → Installbeam2 → Hammerboard2 → Putlaminate2 → Installbeam3 → Hammerboard3 → Putlaminate3 → Putbox → InstallhJ`

## 07 播种墙安装物品索引

07 的主模型组织在 `WHY_BZQ.prefab` 的以下四套状态中：

- `BoZhongQiang_cube`：可搬运/背包取出的零件来源
- `BoZhongQiang_blue`：待安装目标提示
- `BoZhongQiang_green`：正确位置绿色预览
- `BoZhongQiang_shi`：安装完成后的最终实物

| 顺序 | 物品/工具 | 数量或对象 | 模型来源 | 当前资产形态 | 目标/完成条件 | UI 提示摘要 | 语音 |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 1 | 播种墙零件集合 | 打开背包 | `WHY_BZQ.prefab` | 总装 Prefab | 进入播种墙安装流程 | 取出播种墙零件 | `7—1.MP3` |
| 2 | 立柱 | 4根：`LiZhu9/8/6/2` | `BoZhongQiang 1.fbx` / `BoZhongQiang_cube` | 总装子物体 | 四根最终立柱全部激活 | 小孔朝下插入底座 | `7—2.MP3` |
| 3 | 斜撑及固定螺丝 | 10个 `XieCheng*`、12颗 `Jia_LuoSiOn/LuoSi*` | 播种墙总装模型 | 总装子物体 | 22个最终对象全部激活 | 安装横梁并用螺丝紧固 | `7—3.MP3` |
| 4 | 横梁组1 | `HengLiang3`、`HengLiang5` | 播种墙总装模型 | 总装子物体 | 两根横梁激活 | 安装横梁并紧固 | `7—3.MP3` |
| 5 | 层板1 | `CengBan8` | 播种墙总装模型 | 总装子物体 | 对应最终层板激活 | 取出层板进行安装 | `7—4 1.MP3` |
| 6 | 横梁组2 | `HengLiang6`、`HengLiang2` | 播种墙总装模型 | 总装子物体 | 两根横梁激活 | 安装横梁并紧固 | `7—3.MP3` |
| 7 | 层板2 | `CengBan7` | 播种墙总装模型 | 总装子物体 | 对应最终层板激活 | 取出层板进行安装 | `7—4 1.MP3` |
| 8 | 横梁组3 | `HengLiang1`、`HengLiang7` | 播种墙总装模型 | 总装子物体 | 两根横梁激活 | 安装横梁并紧固 | `7—3.MP3` |
| 9 | 层板3 | `CengBan6` | 播种墙总装模型 | 总装子物体，旧任务值异常 | 对应最终层板激活 | 取出层板进行安装 | `7—4 1.MP3` |
| 10 | 横梁组4 | `HengLiang`、`HengLiang4` | 播种墙总装模型 | 总装子物体 | 两根横梁激活 | 安装横梁并紧固 | `7—3.MP3` |
| 11 | 层板4 | `CengBan5` | 播种墙总装模型 | 总装子物体 | 对应最终层板激活 | 取出层板进行安装 | `7—4 1.MP3` |
| 12 | 扫描枪 | 1 | `SaoMaQiang` 子物体 | 总装子物体 | `SaoMaQiang (3)` 激活 | 安装到指定位置 | `7—5.MP3` |
| 13 | 电脑主机 | 1 | `zhuji` 子物体 | 总装子物体 | `zhuji (3)` 激活 | 安装到指定位置 | `7—6.MP3` |
| 14 | 显示器支架 | 1 | `XianShiQiZhiJia` 子物体 | 总装子物体 | `XianShiQiZhiJia (3)` 激活 | 安装显示器支架 | `7—7.MP3` |
| 15 | 支架固定螺丝 | 2颗 | `DNLuoSi` 子物体 | 总装子物体，旧引用异常 | 两颗不同螺丝均激活 | 用螺丝固定支架 | `7—8.MP3` |
| 16 | 键盘和键盘支架 | `JianPan1 (3)`、`JianPanZhiJia (3)` | 播种墙总装模型 | 总装子物体 | 两个最终对象激活 | 取出键盘连接主机 | `7—9.MP3` |
| 17 | 显示器 | 1 | `XianShiQi` 子物体 | 总装子物体 | `XianShiQi (3)` 激活 | 安装到显示器支架 | `7—10 1.MP3` |
| 18 | 卡槽底/底槽 | 3段：`DiCao/DiCao1/DiCao2` | 播种墙总装模型 | 总装子物体 | 三段底槽全部激活 | 取出卡槽底并固定 | `7—11.MP3` |
| 18 | 底槽固定螺丝 | 18颗：`LuoSi`、`LuoSi10`～`LuoSi26` | 播种墙总装模型 | 总装子物体 | 18颗最终螺丝全部激活 | 与卡槽底共用提示 | `7—11.MP3` |
| 19 | 排线端子 | 12个：`BZQ_DuanZi`～`BZQ_DuanZi011` | 播种墙总装模型 | 总装子物体 | 12个端子全部激活 | 端子用于固定排线 | `7—12.MP3` |
| 20 | 电子标签 | 12个：`BiaoQian1`～`BiaoQian12` | 播种墙总装模型 | 总装子物体 | 12个标签全部激活 | 安装到固定位置 | `7—13.MP3` |
| 21 | 完整播种墙 | 最终状态 | `BoZhongQiang_shi` | 最终状态对象 | 所有最终安装对象满足 | 播种墙安装完成 | 无 |

可重复使用工具：

- 手电钻：`ShouDianZuan.fbx`，是独立模型，但需要重新绑定当前框架交互。

### 07 内部状态顺序

`Setupright → Installbeam → Installbeam4 → Installlaminate1 → Installbeam5 → Installlaminate2 → Installbeam6 → Installlaminate3 → Installbeam7 → Installlaminate4 → Putgun → PutComputer → Putsupport → Putscrew1 → PutKeyboard → Putdisplay → Installslot → Slotscrew → Terminal → Installtag → InstallBZQ`

## UI 与语音复用规则

- 旧关卡同时存在简单 Text 提示和任务提示 Canvas 两套 UI。
- 当前实现只复用提示文案与语音资源，不直接搬旧 Canvas。
- 当前 BPF 统一提示 UI 每次只切换文案、音频和指引目标。
- 进入步骤时播放对应音频；步骤无音频时不播放。
- 切换或回退步骤时，应停止上一段语音后再按新步骤播放。
- 指引箭头可以参考旧 `GuidingArrow.prefab` 的位置，但应接入当前框架统一指引模块。

## 已知旧配置异常

1. 07 的 `Putscrew1` 完成数组重复引用 `DNLuoSiOn/LuoSi5_1`，应改为两颗不同螺丝。
2. 07 的 `CengBan6` 旧任务值为 `89`，落入 06 枚举区间，迁移时必须重新绑定为第三块层板步骤。
3. 07 的 `Installtag` 步骤检测标签3～12，但最终控制器检测标签1～12；当前实现应统一为12个标签。
4. 旧背包按钮引用通用同步载体，真正取件依赖 `SycObj`、Mirror 和旧 `LoadSceneObj`；不能直接照搬。
5. 旧脚本依赖 `MultController`、`ShiXun`、`PersonMove` 等历史系统；迁移时仅复用模型、目标位置、提示与步骤意图。

## 建议的当前框架资产结构

每种可从背包取出的物品制作一个明确的物品 Prefab；同类重复件可共用一个 Prefab：

```text
Assets/Resources/BusinessPrefabs/Assembly/
├── 06RackAssembly/
│   ├── Items/
│   │   ├── RackBase.prefab
│   │   ├── RackQrLabel.prefab
│   │   ├── RackUpright.prefab
│   │   ├── RackScrew.prefab
│   │   ├── RackBottomShelf.prefab
│   │   ├── RackBeam.prefab
│   │   ├── RackShelf.prefab
│   │   └── RackBin.prefab
│   └── Tools/
│       ├── GlueBottle.prefab
│       ├── ElectricDrill.prefab
│       └── RubberMallet.prefab
└── 07DisplayWallAssembly/
    ├── Items/
    │   ├── WallUpright.prefab
    │   ├── WallBrace.prefab
    │   ├── WallBeam.prefab
    │   ├── WallShelf.prefab
    │   ├── WallScrew.prefab
    │   ├── Scanner.prefab
    │   ├── ComputerHost.prefab
    │   ├── DisplayBracket.prefab
    │   ├── Keyboard.prefab
    │   ├── KeyboardBracket.prefab
    │   ├── Display.prefab
    │   ├── BottomSlot.prefab
    │   ├── CableTerminal.prefab
    │   └── ElectronicLabel.prefab
    └── Tools/
        └── ElectricDrill.prefab
```

以上是建议目标结构，当前尚未创建。创建时应保留原始 FBX、材质和迁入 Prefab 的 GUID，不移动 `Assets/APP/OldMRTK` 历史源资源；新建业务 Prefab 引用这些模型即可。

## 构建前核对清单

- [ ] 在 Unity 中展开 `WHY_HJ.prefab`，逐项确认底座、二维码、立柱、横梁、层板和库位盒子物体。
- [ ] 在 Unity 中展开 `WHY_BZQ.prefab`，逐项确认立柱、斜撑、横梁、层板、主机、支架、键盘、显示器、底槽、端子和标签。
- [ ] 为背包物品制作独立 Prefab 或框架支持的可生成模板。
- [ ] 确保同类重复件共用模型 Prefab，但每次生成独立实例。
- [ ] 为搬运件配置抓取、碰撞和目标匹配组件。
- [ ] 为目标位置配置待安装提示、绿色预览与最终状态。
- [ ] 修正本文“已知旧配置异常”中的三项业务引用。
- [ ] 使用当前统一 UI 绑定每一步提示和语音。
- [ ] 验证前进、回退和随时切换关卡时的状态恢复。
- [ ] 在 Unity Play Mode 中完整走通 06 后，再复制结构构建 07。
