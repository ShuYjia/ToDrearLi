# BPF PICO 4 业务发布执行说明

> 状态：现行操作依据
> 更新日期：2026-07-21
> 目标仓库：`D:/gitea_obj/_github_obj/github_gitea_bpf_714`
> 目标设备：PICO 4 系列

本文记录发布前应检查的当前静态基线，不表示 APK 已在本轮成功构建或通过
PICO 真机验收。易变化的事实也汇总在
[current-project-baseline.md](./current-project-baseline.md)。

## 当前业务场景

Build Settings 当前启用：

1. `Assets/A_MRNet/Sence/01StartUp.unity`
2. `Assets/A_MRNet/Sence/03SingleplayerScene.unity`
3. `Assets/A_MRNet/Sence/04SingleplayerScene.unity`

首页默认单人业务流为：

```text
01StartUp -> 03SingleplayerScene
```

`02SingleplayerScene.unity` 仍保留，但未加入当前 Build Settings。

## XR 与 Android 基线

- Unity：`2021.3.29f1c1`
- Android Scripting Backend：IL2CPP
- Android Architecture：ARM64
- Android Min SDK：29
- Android XR loader：ARCore Loader、PXR Loader
- Standalone / WSA loader：OpenXR Loader
- PICO XR 包：`com.unity.xr.picoxr` 3.4.0
- Quest/Meta Android features：禁用
- PICO 4、PICO 4 Ultra 控制器 profiles：启用
- PICO Passthrough feature：当前禁用

不要把 `OpenXR Package Settings.asset` 的存在理解为 Android 正使用
OpenXR Loader，也不要把禁用的 Passthrough feature 写成已经启用。

## 场景加载链

`Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_LoadScene.cs`
当前定义：

- `StartupSceneName = "01StartUp"`
- `BusinessSceneName = "03SingleplayerScene"`

`01StartUp.unity` 中 `MainPage.soloScene` 同样保存为
`03SingleplayerScene`，并已挂载 `TexCore_LoadScene`。场景名调整方法见
[bpf-scene-name-configuration-guide.md](./bpf-scene-name-configuration-guide.md)。

## 业务配置与资源

运行时资源入口：

- `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_RuntimeAssetService.cs`

当前 `03SingleplayerScene` 中 `TexCore_ConfigManager` 的序列化开关为：

- `isSaveFile: 1`
- `isLoadFile: 1`

因此既不能把 Prefab 描述成唯一运行时来源，也不能把手改 `Config.xml` 当成
长期配置流程。业务配置应优先修改 Prefab，并结合场景开关、生成结果和运行日志
确认最终数据来源。

当前 `Assets/StreamingAssets` 至少包含：

- `Config.xml`
- `Spaces/cover_image_small.png`

`Interface.xml` 当前未见实体；需要它的业务必须在发布前单独确认。

## Android 输入

输入桥接位于：

- `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/InteractionScript/TexCore_InputFieldBridge.cs`

当前实现通过 Unity Android `TouchScreenKeyboard` 服务 `TMP_InputField`。
若后续切换 PICO 官方键盘，应在桥接层替换，不应让业务输入代码分别适配。

## Manifest 与签名

- `Assets/BuildAssets/AndroidManifestPatcher.cs` 不再注入 Quest 专用
  `oculus.software.overlay_keyboard`。
- 该 patcher 的 `SupportedXRLoaders` 仅列出 `OpenXRLoader`，当前 Android
  loader 为 ARCore＋PXR；不能据此认定其 `pvr.app.type`、`enable_vst` 或
  CAMERA 权限会在当前 PXR 构建中生效。PXR 包还会按
  `PXR_ProjectSetting.videoSeeThrough` 处理对应项，而当前值为 0；最终
  Manifest 必须从实际 Gradle 导出物核对。
- 共享仓库默认 `androidUseCustomKeystore: 0`。
- `applicationIdentifier` 当前仍为 `com.mrtksync`；正式发布前应由发布负责人
  确认最终包名、版本号和签名。

## 构建与安装

先在当前 Unity Editor 中验证本次改动，再询问用户是否需要 APK。只有用户对本次
改动明确确认导出后，才运行完整构建、安装和启动流程。完整流程使用：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1
```

脚本会：

1. 从批工程导出 Android Gradle 工程；
2. 使用 JDK 11 构建 release APK；
3. 将 APK 安装到每台状态为 `device` 的 adb 设备；
4. 启动包名 `com.mrtksync`。

如果用户只确认构建、不允许访问设备，使用 `-SkipInstall`；未获得确认时不得
导出、安装或启动 APK。

完整路径与 Junction 检查见
[batchbuild-project-workflow.md](./batchbuild-project-workflow.md)。

## 发布前验收清单

1. Build Settings 的 01/03/04 场景均存在且启用。
2. `01StartUp` 首页按钮可稳定进入 03。
3. Unity Console 无编译错误。
4. Android 为 IL2CPP＋ARM64，Min SDK 29。
5. Android loader 顺序与目标设备计划一致。
6. Quest overlay keyboard feature 未重新注入。
7. 冷安装首启可读取业务配置和资源。
8. 控制器射线、近场交互和手追逐项真机验证。
9. 图片、音频、视频和系统键盘逐项真机验证。
10. 最终 applicationId、版本号、keystore 和证书已由发布负责人确认。
11. APK 安装后记录设备序列号、构建日志和验收结果。

## 尚未由静态文件证明的内容

- 当前工作树是否能完整编译
- Gradle release 构建是否成功
- PICO 4 与 PICO 4 Ultra 的实际控制器/手追表现
- Passthrough 或其他 MR feature 的产品需求与最终配置
- 冷启动、重进、后台恢复和重复安装行为

这些项目必须通过 Unity 构建日志和 PICO 真机验证完成。
