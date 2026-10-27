# GameFramework + TexCore PICO Mainline Notes

> Status: current framework-level judgment
> Updated: 2026-07-20

This note evaluates the current BPF business framework under:

- `Assets/GameFramework`
- `Assets/A_MRNet`

It does not use legacy networking snapshots to define the active business route.
For volatile scene, XR and build facts, see
[current-project-baseline.md](./current-project-baseline.md).

## Scope Baseline

The active single-player route is:

- `Assets/A_MRNet/Sence/01StartUp.unity`
- `Assets/A_MRNet/Sence/03SingleplayerScene.unity`

`04SingleplayerScene.unity` is also enabled in Build Settings. The retained
`02SingleplayerScene.unity` is not the current home-page target.

Primary conclusion:

- `GameFramework + TexCore` is not inherently Windows-only or HoloLens-only.
- It can continue to serve as the current PICO business framework baseline.
- Remaining risk is concentrated in XR runtime behavior, Android media/input,
  content configuration, and device validation rather than a mandatory framework
  rewrite.

## Current XR Interpretation

The Android loader configuration is not a pure OpenXR route:

- Android XR Plug-in Management lists ARCore Loader and PXR Loader.
- Standalone and WSA list OpenXR Loader.
- The project also embeds PICO XR 3.4.0 and contains PICO OpenXR feature assets.
- Package or feature-asset presence alone does not identify the active loader.

Therefore, current Android/PICO discussions must start from PXR loader behavior.
The older “pure OpenXR, no PICO SDK/PXR” baseline is archived in
[openxr-cross-platform-mainline.md](./openxr-cross-platform-mainline.md) and no
longer describes the current project.

## GameFramework Status

Why it is considered PICO-compatible at framework level:

- It includes Android file-system support in
  `Assets/GameFramework/Scripts/Runtime/FileSystem/AndroidFileSystemStream.cs`.
- Runtime roots use Unity mobile paths such as `Application.streamingAssetsPath`
  and `Application.persistentDataPath`.
- The deprecated `WWWWebRequestAgentHelper` implementation is behind
  `#if !UNITY_2018_3_OR_NEWER`, so it is not part of the Unity 2021 runtime path.

This does not prove every GameFramework service is used by the active BPF scene.
It only shows there is no framework-wide PICO blocker that requires replacement.

## TexCore Status

Current PICO-relevant integration points include:

- runtime asset access through
  `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/Managers/TexCore_RuntimeAssetService.cs`;
- Android input through
  `Assets/A_MRNet/ProgramResources/ProgramResources/CommonScripts/InteractionScript/TexCore_InputFieldBridge.cs`;
- MRTK/XRI interaction components such as `PressableButton` rather than direct
  Windows-only interaction APIs.

`TexCore_Interaction_Hololens` is a legacy type name, not proof of an active
Windows-only dependency. Avoid renaming it solely for terminology because that
can create unnecessary Prefab and Scene serialization churn.

## What Still Needs Device Validation

- Controller ray hover and click stability
- Hand tracking interaction stability
- TMP input behavior through Android system keyboard
- Image, audio and video loading on device
- Re-entry, background/foreground and repeated launch behavior
- PXR initialization and controller-profile behavior on each target PICO model

## Working Rule

When deciding current PICO compatibility:

1. Start from the actual active scenes, `Assets/GameFramework` and
   `Assets/A_MRNet`.
2. Read `Assets/XR/XRGeneralSettings.asset` before describing the active loader.
3. Separate framework compatibility from loader configuration and device proof.
4. Treat historical `Assets/App`, `_syc_resource` and legacy networking documents
   as references only.
