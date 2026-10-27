# BPF Batchbuild Project Workflow

> Status: current operational guide
> Updated: 2026-09-11

Keep the main Unity editor session available for MCP work and export Android
builds from a separate batch project.

## Paths

- Current source repository:
  `D:\gitea_obj\_github_obj\github_gitea_bpf_714`
- Batch project:
  `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild`
- Unity:
  `D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Unity.exe`
- JDK 11:
  `D:\gitea_obj\_unity projection\tools\OpenJDK11\jdk-11.0.31+11`

The batch project must share these source folders through junctions:

- `Assets`
- `Packages`
- `ProjectSettings`

It keeps local cache and output folders:

- `Library`
- `Builds`
- `Logs`
- `UserSettings`

## Verify Or Repair Junctions

From the source repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\setup-batchbuild-project.ps1 `
  -SourceProjectPath 'D:\gitea_obj\_github_obj\github_gitea_bpf_714' `
  -BatchProjectPath 'D:\gitea_obj\_unity projection\gitea_BPF__batchbuild'
```

If a junction points elsewhere, rerun with `-ForceRepair`. The script refuses
to delete a normal directory in place of a junction.

Always inspect the three resolved junction targets before a release build.
Their state may change independently of this document.

## Build, Install And Launch

The standard command is:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1
```

The script defaults to:

- Unity batch export through
  `MixedReality.Toolkit.Examples.Build.BuildApp.StartBpfCommandLineBuild`;
- Gradle project at
  `Builds\Android\BpfPico_Gradle` inside the batch project;
- JDK 11 `assembleRelease`;
- APK at
  `launcher\build\outputs\apk\release\launcher-release.apk` inside that
  Gradle project;
- `adb install -r` for every connected device;
- launch package `com.mrtksync`.

The full expected APK path after a successful build is:

```text
D:\gitea_obj\_unity projection\gitea_BPF__batchbuild\Builds\Android\BpfPico_Gradle\launcher\build\outputs\apk\release\launcher-release.apk
```

Do not treat a `Library\Bee\...` APK as the standard delivery path.

## Optional Script Switches

- `-CleanExport`: delete the previous Gradle export first and rebuild from
  scratch. This also deletes
  `BpfPico_Gradle\unityLibrary\build\il2cpp_arm64-v8a_Release\il2cpp_cache`
  (about 2.66 GB, 763 files), which forces every IL2CPP unit to recompile.
- `-SkipCleanExport`: safety override so a clean can be suppressed explicitly.
- `-SkipUnityExport`: reuse an existing exported Gradle project.
- `-SkipGradleBuild`: reuse an existing release APK.
- `-SkipInstall`: build only; do not access devices.

Reuse switches are safe only when the existing artifact is known to contain the
current source and settings.

## Cost Of A Build

Measured on this workstation for the current project:

| Scenario | Unity export | Gradle | Total |
| --- | --- | --- | --- |
| `-CleanExport` | 17m 20s | 23m 54s | about 41m |
| default (incremental) | about 15m | 7m 59s | about 23m |

Use the default incremental path for normal iteration. Keep the IL2CPP and
Gradle caches alive by omitting `-CleanExport`; the wrapper logs which mode it
chose. Reserve `-CleanExport` for suspected Gradle-project corruption or a
deliberate from-scratch release build.

Two properties to preserve when editing this script:

- the Gradle invocation must not pass `--no-daemon`, because the daemon is what
  keeps the configuration and task-graph caches warm between runs;
- the Gradle launch must wait on the immediate `gradlew` child only. Do not use
  `Start-Process -Wait` there: PowerShell's `-Wait` also waits for every
  descendant, and the daemon outlives `gradlew` by design, so the wrapper would
  block forever after an already-successful build.

## Post-Change Verification And Export Gate

For a completed code, Prefab, Scene, XR, Android or runtime-asset change intended
to affect PICO behavior:

1. verify source/batch junctions;
2. test the change in the currently connected Unity Editor;
3. ask the user whether an APK export is needed for this change;
4. only after the user explicitly confirms, export and build from the batch
   project, install to every connected PICO device and launch the app;
5. ask the user to verify the exact changed behavior on the device.

Do not export, install or launch an APK before that confirmation. Use
`-SkipInstall` only when the user requests a build without device access.
Docs-only and analysis-only turns require neither Unity runtime testing nor an
APK build.

## Failure Reporting

Record the first concrete blocker and its log path:

- Unity compilation or export failure:
  `Builds\Android\build-android-codex-export.log`
- Gradle failure:
  `Builds\Android\manual-gradle-jdk11-codex.log`
- no `device` entries from adb
- adb install or launch failure
- missing Unity, JDK, SDK or batch-project path

Do not report the build as successful merely because an older APK exists.
