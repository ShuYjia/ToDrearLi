---
name: bpf-pico-export-install
description: Export, build, install, launch, and verify the BPF Unity Android/PICO APK for the github_gitea_bpf_714 project. Use when the user asks to 导出, 重新导出, 安装, 重新安装, launch, test on PICO, fix a PICO white loading circle after install, run ADB installation, or work with Tooling/build-and-install-pico.ps1, -CleanExport, -SkipUnityExport, com.mrtksync, PICO A9210, launcher-release.apk, a Gradle release build that fails at APK signing with a keystore or password error, or a Unity incremental player build that fails with an unparsable Bee backend log.
---

# BPF PICO Export Install

## Scope

Use this skill only for the BPF repository at `D:\gitea_obj\_github_obj\github_gitea_bpf_714`.
Follow the repository `AGENTS.md` first, especially the Unity MCP status line rule, the
current-branch-only rule, and the requirement to use the batch project for Android/PICO export.
The flow always targets the currently checked-out branch; uncommitted changes in that branch
do not block an export.

## Standard Paths

- Repository: `D:\gitea_obj\_github_obj\github_gitea_bpf_714`
- Batch project: `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild`
- Unity: `D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Unity.exe`
- JDK 11: `D:\gitea_obj\_unity projection\tools\OpenJDK11\jdk-11.0.31+11`
- adb: `D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`
- Build script: `Tooling\build-and-install-pico.ps1`
- APK: `D:\gitea_obj\_unity projection\gitea_BPF__batchbuild\Builds\Android\BpfPico_Gradle\launcher\build\outputs\apk\release\launcher-release.apk`
- Package: `com.mrtksync`

## Fast Decision Tree

1. Reinstall-only testing: the APK exists and no tracked asset is newer than it. Verify the APK
   and use the manual install step. Do not rebuild. The wrapper can also do install and launch
   only: `powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -SkipUnityExport -SkipGradleBuild`.
2. Unity export already succeeded and only the wrapper was interrupted: confirm the export log
   says `Build Finished, Result: Success`, then rerun with `-SkipUnityExport`.
3. Project assets, scenes, scripts, packages, or build settings changed: run a full
   `-CleanExport`. Compare the APK timestamp with the newest changed asset; if the asset is
   newer than the APK, the APK is stale and must be rebuilt.
4. ADB is not in `PATH`: use the Unity Android SDK `platform-tools\adb.exe` path above.

## Preflight

1. Run `git status --short` and confirm the checked-out branch. Do not switch branches, fetch,
   merge, rebase, or sync any other branch, and do not overwrite uncommitted user changes.
2. Confirm the batch project, Unity executable, JDK home, and build script exist.
3. Confirm a device, and record its serial:

   ```powershell
   $adb = 'D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
   & $adb devices -l
   ```

   The device state must be `device`. An empty list, `offline`, or `unauthorized` means a cable,
   PICO USB debugging authorization, or driver problem: stop instead of starting a long export
   that will fail at the install stage.

## Execution Workflow

4. Full export, Gradle release build, install, and launch, from the repository root:

   ```powershell
   cd D:\gitea_obj\_github_obj\github_gitea_bpf_714
   powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -CleanExport
   ```

   The wrapper exports through Unity batch mode into the batch project, runs Gradle
   `assembleRelease` with JDK 11, runs `adb install -r` on every connected device, and launches
   `com.mrtksync`.

5. Unity export succeeded but the wrapper stopped: verify `Build Finished, Result: Success` in
   `Builds\Android\build-android-codex-export.log`, then reuse the existing export:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -SkipUnityExport
   ```

6. Manual APK install only:

   ```powershell
   $serial = '<serial from adb devices -l>'
   $apk = 'D:\gitea_obj\_unity projection\gitea_BPF__batchbuild\Builds\Android\BpfPico_Gradle\launcher\build\outputs\apk\release\launcher-release.apk'
   & $adb -s $serial install -r $apk
   & $adb -s $serial shell monkey -p com.mrtksync -c android.intent.category.LAUNCHER 1
   ```

7. Install and launch only, reusing the existing export and APK (no Unity, no Gradle):

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\Tooling\build-and-install-pico.ps1 -SkipUnityExport -SkipGradleBuild
   ```

   Use this when an earlier run already produced a fresh `launcher-release.apk`. Confirm first
   that no other Unity export, Gradle build, or `adb install` is still running.

## Verification

- Gradle success requires `BUILD SUCCESSFUL` and a freshly written `launcher-release.apk`.
- Install success requires `adb install -r` output `Success`.
- Confirm the process and the foreground activity:

  ```powershell
  & $adb -s $serial shell pidof com.mrtksync
  & $adb -s $serial shell dumpsys activity activities |
    Select-String 'ResumedActivity|mResumedActivity|mFocusedApp|UnityPlayerActivity'
  ```

- After launch, collect a short logcat window and check for `PXR_Loader`, `CreateLayer`, and
  `XR_SESSION_STATE_FOCUSED`; check that previous failure text such as `loading error 0x0602`
  is absent.
- Recheck `git status --short` after export. Restore only confirmed build drift, especially
  `Assets/Resources/PXR_ProjectSetting.asset` `portalInited` and
  `ProjectSettings/ProjectSettings.asset` `preloadedAssets`. Never commit or revert unrelated
  user changes.

## Troubleshooting

- `adb devices` shows no device or a state other than `device`: fix the USB cable, the PICO USB
  debugging authorization, or the driver.
- `INSTALL_FAILED_UPDATE_INCOMPATIBLE`: the APK already on the device has a different signature.
  Uninstalling clears app data:

  ```powershell
  & $adb -s $serial uninstall com.mrtksync
  & $adb -s $serial install $apk
  ```

- No device at the install stage: the wrapper throws `No connected adb devices found.` after
  Unity and Gradle already finished. Attach a device and rerun with `-SkipUnityExport`.
- Unity export fails with `Incremental Player build failed` or
  `Backend produced unparsable log entry`: the batch project Bee cache is corrupt. Quit the batch
  Unity process, move the cache aside instead of deleting it, then rerun the export:

  ```powershell
  $batch = 'D:\gitea_obj\_unity projection\gitea_BPF__batchbuild'
  $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
  Move-Item -LiteralPath (Join-Path $batch 'Library\Bee') -Destination (Join-Path $batch "Library\Bee.failed-$stamp")
  ```

  Never delete files in the main project to work around this.
- Gradle fails with `Failed to read key <alias> from store`, `Keystore was tampered with, or
  password was incorrect`, or `Password verification failed`: this is APK signing, not an ADB or
  PICO fault. Do not guess passwords and do not overwrite the keystore. Check
  `ProjectSettings/ProjectSettings.asset` (`androidUseCustomKeystore`, `AndroidKeystoreName`,
  `AndroidKeyaliasName`) and the exported `BpfPico_Gradle\launcher\build.gradle`
  `signingConfigs.release` block (`storeFile`, `storePassword`, `keyAlias`, `keyPassword`).
  Verify the exported password non-destructively with JDK 11 before blaming the keystore:

  ```powershell
  $keytool = 'D:\gitea_obj\_unity projection\tools\OpenJDK11\jdk-11.0.31+11\bin\keytool.exe'
  & $keytool -list -keystore 'C:\Users\admin\Desktop\Regedit\user.keystore' -storepass '<storePassword from launcher\build.gradle>'
  ```

  Exit code `0` with the alias listed as `PrivateKeyEntry` proves the file and store password are
  fine, so the failure is elsewhere. Check the generated `BpfPico_Gradle\launcher\build.gradle`
  for empty `storePassword ''` / `keyPassword ''`: the batch project's Unity stores no keystore
  passwords, so a clean re-export writes empty values and breaks signing. Either re-enter the
  passwords in the batch project's Unity, or patch the generated file with the passwords from the
  credential holder and rerun with `-SkipUnityExport` (Gradle only, about 7 minutes). Never commit
  the password. Switching to a debug signature or a new keystore changes the APK signature and may
  require uninstalling `com.mrtksync`, which clears app data.
- Preserve failure evidence before rerunning: the wrapper deletes
  `Builds\Android\manual-gradle-jdk11-codex.log` and `.stderr.log` at the start of its Gradle
  step, and Unity overwrites `build-android-codex-export.log` on every export. Copy any log you
  still need before starting the next run.
- Before starting any step, check for concurrent work with
  `Get-CimInstance Win32_Process -Filter "Name='Unity.exe' or Name='java.exe' or Name='adb.exe'"`.
  The batch project shares `Assets`, `Packages`, and `ProjectSettings` with the main project, so a
  second Unity build or a second `adb install` to the same device can corrupt the result. Report a
  concurrent run instead of starting a competing one.

- Launch is refused while another app holds the top-app slot: logcat shows
  `VDM disallowStartActivity` and `am start` prints `Activity not started because the current
  activity is being kept for the user`, so `pidof com.mrtksync` stays empty. Force-stop the
  occupying package (`adb shell am force-stop <package>`) and launch again. The legacy bundle id
  `com.DefaultCompany.NETcode` has occupied the slot this way.
- A non-empty `pidof` is not proof of a running XR session: with the headset not worn,
  `dumpsys power` reports `mWakefulness=Asleep`, the activity stays paused, the system dock keeps
  the resume slot, and no `PXR_Loader` / `XR_SESSION_STATE_FOCUSED` lines appear.
  `input keyevent KEYCODE_WAKEUP` is no substitute for wearing the device, so ask the user to wear
  it and treat their on-device acceptance as the final evidence.
- Before reinstalling, confirm the installed copy: `adb shell pm path com.mrtksync`, compare
  `adb shell ls -l <path>` size with the local APK size, and check `lastUpdateTime`. A release APK
  here is about 1.4 GB, so a redundant `adb install -r` costs minutes.
- Full command list, timings, success criteria, and the verified 2026-09-15 run:
  `docs/bpf-pico-build-runbook.md`.

## Reporting

Report the APK path, device serial, install result, launch result, `pidof` output, foreground
activity, and whether XR/PXR logs reached a focused session.
End with the concrete user action: wear the PICO and reply whether the `03` scene loads, the
white loading circle disappears, and interaction works; on failure, ask for the PowerShell
output or the relevant `logcat` excerpt.
