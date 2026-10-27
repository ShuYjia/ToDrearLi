# BPF PICO Build Runbook

Use the batchbuild project for Android/PICO delivery unless the user explicitly says otherwise.

## Preconditions

- Unity project content is verified.
- User explicitly requested APK export/install.
- PICO device is connected and visible through adb; `adb devices` must report state `device`.

## Flow

1. Use the batchbuild project.
2. Export Android/Gradle.
3. Build release APK.
4. Verify adb devices -l.
5. Install APK with adb install -r.
6. Launch package.
7. Ask user for device-side acceptance.

## Commands

Run from `D:\gitea_obj\_github_obj\github_gitea_bpf_714`.

| Goal | Command |
| --- | --- |
| Full export, Gradle, install, launch | `powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -CleanExport` |
| Reuse the existing Gradle project (Unity export already succeeded) | `powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -SkipUnityExport` |
| Install and launch only, reuse the existing APK | `powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -SkipUnityExport -SkipGradleBuild` |
| Manual install and launch | `adb -s <serial> install -r <apk>`, then `adb -s <serial> shell monkey -p com.mrtksync -c android.intent.category.LAUNCHER 1` |

`-CleanExport` deletes `BpfPico_Gradle` and forces a full IL2CPP recompile (about 17 minutes per the
wrapper comment) plus a cold Gradle build (about 24 minutes). Omit it for normal iteration so the
IL2CPP and Gradle caches survive.

## Success Criteria

- Unity export: `Build Finished, Result: Success.` in
  `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild\Builds\Android\build-android-codex-export.log`.
- Gradle: `BUILD SUCCESSFUL` in `Builds\Android\manual-gradle-jdk11-codex.log`.
- APK: `Builds\Android\BpfPico_Gradle\launcher\build\outputs\apk\release\launcher-release.apk`
  exists with a timestamp from the current run.
- Install: `adb install -r` prints `Success`.
- Launch: `adb shell pidof com.mrtksync` returns a pid.
- Runtime: logcat shows `PXR_Loader`, `CreateLayer`, and `XR_SESSION_STATE_FOCUSED`, and no
  `loading error 0x0602`.
- After the export, recheck `git status --short` and restore only confirmed build drift
  (`Assets/Resources/PXR_ProjectSetting.asset` `portalInited`,
  `ProjectSettings/ProjectSettings.asset` `preloadedAssets`). Never revert unrelated user changes.

## Verified Run: 2026-09-15

End-to-end success on branch `main`, starting at commit `42514093` with uncommitted user changes
present.

- Unity re-export succeeded: `Build Finished, Result: Success.`
- Gradle `assembleRelease` reused its caches: `BUILD SUCCESSFUL in 3m 48s` (84 tasks, 81
  up-to-date).
- APK written 22:01:16, 1,442,589,842 bytes (about 1.4 GB, so a USB install takes minutes).
- Installed 22:05:38; the device `base.apk` size matched the local APK byte for byte, which is how
  the install was confirmed without pushing the file a second time.
- The first two launch attempts were refused by the system because the old bundle id
  `com.DefaultCompany.NETcode` still held the top-app slot. After `am force-stop` of that package,
  `com.mrtksync` started and the user confirmed on the device that scene `03` loads, the white
  loading circle clears, and interaction works.
- A later `-CleanExport` run the same evening spent about 16 minutes in Unity, then failed at
  Gradle signing because that export wrote empty keystore passwords into the generated
  `launcher\build.gradle` (see pitfall 2). Patching the generated file and rerunning with
  `-SkipUnityExport` produced a signed 1.44 GB APK in 6m51s and installed it in 17.5s, so the
  expensive Unity export only had to run once.

## Known Pitfalls

1. **The wrapper deletes its Gradle logs at the start of every Gradle step.** It removes
   `manual-gradle-jdk11-codex.log` and `manual-gradle-jdk11-codex.stderr.log`, and Unity overwrites
   `build-android-codex-export.log` on every export. Copy any log you still need before rerunning,
   otherwise the original failure evidence is gone.
2. **An APK signing failure is not automatically a wrong password.** Messages such as
   `Failed to read key 714714714 from store`, `Keystore was tampered with, or password was
   incorrect`, or `Password verification failed` mean APK signing, not an ADB or PICO fault. Check
   `ProjectSettings/ProjectSettings.asset` (`androidUseCustomKeystore: 1`,
   `AndroidKeystoreName: C:/Users/admin/Desktop/Regedit/user.keystore`,
   `AndroidKeyaliasName: 714714714`) and the exported `launcher\build.gradle`
   `signingConfigs.release` block, then verify the exported password non-destructively with JDK 11:

   ```powershell
   $keytool = 'D:\gitea_obj\_unity projection\tools\OpenJDK11\jdk-11.0.31+11\bin\keytool.exe'
   & $keytool -list -keystore 'C:\Users\admin\Desktop\Regedit\user.keystore' -storepass '<storePassword from launcher\build.gradle>'
   ```

   Exit code `0` with the alias listed as `PrivateKeyEntry` proves the keystore file and the store
   password are fine. The key password is a separate check, because Gradle reads it through
   `KeyStore.getKey`, not through the store password; a small Java probe that calls
   `getKey(alias, keyPassword)` is the only non-destructive way to prove it.

   **Root cause confirmed on 2026-09-15.** The 22:31 `-CleanExport` run exported successfully
   (Unity 22:31-22:47) but failed at `:launcher:packageRelease` because the freshly generated
   `BpfPico_Gradle\launcher\build.gradle` contained `storePassword ''` and `keyPassword ''`. The
   batch project's Unity holds no keystore passwords, so every clean re-export writes empty
   passwords and breaks signing until the file is fixed. Fixes, in order of preference:

   1. Enter the keystore passwords once in the batch project's Unity (Build Settings > Player
      Settings > Publishing Settings) so later exports embed them.
   2. Patch the generated `BpfPico_Gradle\launcher\build.gradle` with the correct passwords from
      the credential holder, then rerun with `-SkipUnityExport`. That skips Unity and costs only
      the Gradle time (6m51s on 2026-09-15).

   Do not guess passwords, do not commit them anywhere in this repository, and do not replace the
   keystore; a debug signature or a new keystore changes the APK signature and may require
   uninstalling `com.mrtksync`, which clears app data.
3. **Corrupt Bee cache.** `Incremental Player build failed` or
   `Backend produced unparsable log entry` means the batch project Bee cache is bad. Quit the batch
   Unity process, move the cache aside instead of deleting it, then rerun the export:

   ```powershell
   $batch = 'D:\gitea_obj\_unity projection\gitea_BPF__batchbuild'
   $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
   Move-Item -LiteralPath (Join-Path $batch 'Library\Bee') -Destination (Join-Path $batch "Library\Bee.failed-$stamp")
   ```

   Never delete files in the main project to work around this.
4. **Launch blocked by another app.** When another package holds the top-app slot, logcat shows
   `VDM disallowStartActivity` and `am start` prints `Activity not started because the current
   activity is being kept for the user`, while `pidof com.mrtksync` stays empty. Fix:
   `adb shell am force-stop <occupying package>`, then launch again. Observed on 2026-09-15 with the
   legacy bundle id `com.DefaultCompany.NETcode` running in the foreground.
5. **Headset asleep.** When `dumpsys power` reports `mWakefulness=Asleep` (headset not worn), the
   app process starts but its activity stays paused, the system dock
   `com.pvr.shortcut/.dock.activity.TunnelActivity` keeps the resume slot, and no XR session logs
   appear. `adb shell input keyevent KEYCODE_WAKEUP` does not substitute for wearing the headset.
   XR focus can only be verified while it is worn, and the final acceptance is the user's.
6. **Do not re-push the APK blindly.** Confirm the installed copy first:
   `adb shell pm path com.mrtksync`, then compare `adb shell ls -l <path>` size with the local APK
   size and check `lastUpdateTime` from `dumpsys package com.mrtksync`.
7. **No concurrent runs.** The batch project shares `Assets`, `Packages`, and `ProjectSettings` with
   the main project through junctions. Check
   `Get-CimInstance Win32_Process -Filter "Name='Unity.exe' or Name='java.exe' or Name='adb.exe'"`
   before starting, and never run a second Unity export or a second install at the same time.
8. **No device at the install stage.** The wrapper throws `No connected adb devices found.` after
   Unity and Gradle already finished. Attach the device and rerun with `-SkipUnityExport`.

## Boundary

Do not export, install, or launch APK unless the user explicitly confirms it for the current change.
