# PICO MR Passthrough Chain Compare - Historical Diagnostic Snapshot

> Status: superseded diagnostic note
> Rechecked: 2026-07-20
> Current baseline: [current-project-baseline.md](./current-project-baseline.md)

This document previously described a PICO OpenXR passthrough chain as active in
the BPF mainline. That conclusion no longer matches the current repository and
must not be used as proof that passthrough is enabled.

## Current Evidence

The repository still contains official PICO implementation code under
`Packages/PICO`, including:

- `Runtime/BuildingBlocks/PXR_CameraEffectBlock.cs`
- `Runtime/Scripts/OpenXRFeatures/Features/PassthroughFeature.cs`
- `Runtime/Subsystem/PXR_CameraSubsystem.cs`

However, current project configuration shows:

- Android XR Plug-in Management uses ARCore Loader and PXR Loader;
- `PassthroughFeature Android` has `m_enabled: 0`;
- `PassthroughFeature.isCameraSubsystem` has value `0`;
- the formerly documented
  `Assets/App/Scripts/Runtime/Pico/PicoMixedRealityBootstrap.cs` does not exist;
- the formerly documented
  `Assets/App/Editor/Pico/BpfPicoMixedRealityConfigurator.cs` does not exist;
- `Assets/link.xml` still contains a preservation entry for the removed bootstrap,
  which is historical residue rather than proof of a compiled type;
- `01StartUp.unity` still contains a GameObject named
  `PicoMixedRealityBootstrap`, but no current script implementation was found.

## Correct Interpretation

- Embedded package code proves capability is available in the package, not that
  the project enabled or reached it.
- A Scene GameObject name is not proof that the expected component or runtime
  bootstrap exists.
- Both feature-list membership and the serialized enabled fields must be checked.
- PXR loader behavior and PICO OpenXR feature behavior must not be treated as
  interchangeable.

## What Must Happen Before Passthrough Is Claimed

1. Confirm the product requirement and target PICO models.
2. Select the intended PXR or PICO OpenXR implementation route.
3. Enable only the required feature and subsystem settings.
4. Remove or replace stale bootstrap/linker references.
5. Compile successfully in Unity.
6. Build and install an APK from the current source.
7. Capture device logs showing the selected passthrough path initialized.
8. Verify the camera background and lifecycle behavior on device.

Until these checks pass, the accurate statement is: the repository contains
PICO passthrough implementation code, but the current project does not show an
enabled, validated passthrough chain.
