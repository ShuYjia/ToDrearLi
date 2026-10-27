# Copyright (c) Mixed Reality Toolkit Contributors
# Licensed under the BSD 3-Clause

[CmdletBinding()]
param(
    [string]$UnityExePath = "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Unity.exe",
    [string]$BatchProjectPath = "D:\gitea_obj\_unity projection\gitea_BPF__batchbuild",
    [string]$JdkHome = "D:\gitea_obj\_unity projection\tools\OpenJDK11\jdk-11.0.31+11",
    [string]$BuildName = "BpfPico",
    # Opt in to a from-scratch export. It deletes the whole Gradle project,
    # including unityLibrary\build\il2cpp_*_cache, which forces a full IL2CPP
    # recompile (about 17 min) plus a cold Gradle build (about 24 min).
    # Omit it for normal iteration so the IL2CPP and Gradle caches are reused.
    [switch]$CleanExport,
    # Force a clean export even if a future default changes to cleaning.
    [switch]$SkipCleanExport,
    [switch]$SkipUnityExport,
    [switch]$SkipGradleBuild,
    [switch]$SkipInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-NativeExitCode {
    if (Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue) {
        return [int]$global:LASTEXITCODE
    }

    return 0
}

function Quote-ProcessArgument {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ($Value -notmatch '[\s"]') {
        return $Value
    }

    return '"' + ($Value -replace '"', '\"') + '"'
}

function Assert-PathExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Label does not exist: $Path"
    }
}

Assert-PathExists -Path $UnityExePath -Label "Unity editor"
Assert-PathExists -Path $BatchProjectPath -Label "Batch project"
Assert-PathExists -Path $JdkHome -Label "JDK home"

$totalStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$unityExportDuration = [TimeSpan]::Zero
$gradleBuildDuration = [TimeSpan]::Zero
$installDuration = [TimeSpan]::Zero

$logsDir = Join-Path $BatchProjectPath "Builds\Android"
$gradleExportDir = Join-Path $logsDir ($BuildName + "_Gradle")
$unityLogPath = Join-Path $logsDir "build-android-codex-export.log"
$gradleLogPath = Join-Path $logsDir "manual-gradle-jdk11-codex.log"
$gradleStdErrLogPath = Join-Path $logsDir "manual-gradle-jdk11-codex.stderr.log"
$apkPath = Join-Path $gradleExportDir "launcher\build\outputs\apk\release\launcher-release.apk"
$gradleProjectPath = $gradleExportDir
$androidSdkRoot = "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK"
$fallbackGradleLauncherJar = "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\Tools\gradle\lib\gradle-launcher-6.1.1.jar"
$fallbackGradleLibWildcard = "D:\Program Files\UnityEditor\2021.3.29f1c1\Editor\Data\PlaybackEngines\AndroidPlayer\Tools\gradle\lib\*"

New-Item -ItemType Directory -Force -Path $logsDir | Out-Null

if (-not $SkipUnityExport) {
    $cleanExport = $CleanExport -and -not $SkipCleanExport
    if ($cleanExport -and (Test-Path -LiteralPath $gradleExportDir)) {
        Write-Host "Clean export requested: removing $gradleExportDir (full IL2CPP rebuild follows)."
        Remove-Item -LiteralPath $gradleExportDir -Recurse -Force
    }
    elseif (Test-Path -LiteralPath $gradleExportDir) {
        Write-Host "Incremental export: reusing $gradleExportDir so the IL2CPP and Gradle caches survive."
    }

    $unityStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $unityArgs = @(
        "-batchmode",
        "-quit",
        "-projectPath", $BatchProjectPath,
        "-buildTarget", "Android",
        "-executeMethod", "MixedReality.Toolkit.Examples.Build.BuildApp.StartBpfCommandLineBuild",
        "-androidExportProject",
        "-buildOutput", $gradleExportDir,
        "-logFile", $unityLogPath
    )

    $unityArgumentLine = ($unityArgs | ForEach-Object { Quote-ProcessArgument $_ }) -join ' '
    $unityProcess = Start-Process -FilePath $UnityExePath -ArgumentList $unityArgumentLine -Wait -PassThru -NoNewWindow
    $unityStopwatch.Stop()
    $unityExportDuration = $unityStopwatch.Elapsed
    $unityExitCode = $unityProcess.ExitCode
    if ($unityExitCode -ne 0) {
        throw "Unity export failed. See log: $unityLogPath"
    }
}

Assert-PathExists -Path $gradleProjectPath -Label "Gradle project"

if (-not $SkipGradleBuild) {
    $gradlew = Join-Path $gradleProjectPath "gradlew.bat"
    $gradleCommand = $gradlew
    $useWrapper = $true
    if (-not (Test-Path -LiteralPath $gradlew)) {
        Assert-PathExists -Path $fallbackGradleLauncherJar -Label "Fallback Gradle launcher"
        Assert-PathExists -Path $fallbackGradleLibWildcard.Replace("\*", "") -Label "Fallback Gradle lib directory"
        $useWrapper = $false
    }

    $env:JAVA_HOME = $JdkHome
    $env:GRADLE_USER_HOME = Join-Path $BatchProjectPath ".gradle-jdk11"
    $env:ANDROID_SDK_ROOT = $androidSdkRoot
    $env:ANDROID_HOME = $env:ANDROID_SDK_ROOT

    $gradleFilePath = if ($useWrapper) { $gradleCommand } else { Join-Path $JdkHome "bin\java.exe" }
    # No --no-daemon: the daemon keeps the configuration and task-graph caches
    # warm across runs, which is the bulk of the win on repeat builds.
    $gradleArgumentList = if ($useWrapper) {
        @("assembleRelease", "--stacktrace")
    }
    else {
        (
            '-cp "{0}" org.gradle.launcher.GradleMain -p "{1}" assembleRelease --stacktrace' -f
            $fallbackGradleLibWildcard,
            $gradleProjectPath
        )
    }

    if (Test-Path -LiteralPath $gradleLogPath) {
        Remove-Item -LiteralPath $gradleLogPath -Force
    }

    if (Test-Path -LiteralPath $gradleStdErrLogPath) {
        Remove-Item -LiteralPath $gradleStdErrLogPath -Force
    }

    $gradleStopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $gradleProcess = Start-Process `
        -FilePath $gradleFilePath `
        -ArgumentList $gradleArgumentList `
        -WorkingDirectory $gradleProjectPath `
        -RedirectStandardOutput $gradleLogPath `
        -RedirectStandardError $gradleStdErrLogPath `
        -PassThru `
        -NoNewWindow
    # Do NOT add -Wait here. PowerShell's -Wait also waits for every descendant
    # of the child, and the Gradle daemon (started because --no-daemon is gone)
    # outlives gradlew by design, so -Wait would block forever even though the
    # build already finished. Wait on the immediate child only.
    if (-not $gradleProcess.WaitForExit(3600000)) {
        $gradleProcess.Kill()
        $gradleStopwatch.Stop()
        throw "Gradle build timed out after 60 minutes. See log: $gradleLogPath"
    }
    $gradleProcess.WaitForExit()
    $gradleStopwatch.Stop()
    $gradleBuildDuration = $gradleStopwatch.Elapsed

    if (Test-Path -LiteralPath $gradleStdErrLogPath) {
        Add-Content -LiteralPath $gradleLogPath -Value ""
        Get-Content -LiteralPath $gradleStdErrLogPath | Add-Content -LiteralPath $gradleLogPath
    }

    # Do not rely on the exit code alone. When gradlew.bat is absent the script
    # falls back to org.gradle.launcher.GradleMain, and Process.ExitCode comes
    # back null for that invocation on this host, which would misreport a
    # successful build as a failure. Treat the Gradle banner in the log as the
    # authoritative result and use the exit code only as a secondary signal.
    $gradleExitCode = $gradleProcess.ExitCode
    $gradleLogText = if (Test-Path -LiteralPath $gradleLogPath) {
        Get-Content -LiteralPath $gradleLogPath -Raw
    }
    else {
        ""
    }

    $gradleSucceeded = $gradleLogText -match '(?m)^BUILD SUCCESSFUL'
    $gradleFailed = $gradleLogText -match '(?m)^BUILD FAILED'

    if ($gradleFailed -or ($null -ne $gradleExitCode -and $gradleExitCode -ne 0)) {
        throw "Gradle build failed (exit code: $gradleExitCode). See log: $gradleLogPath"
    }

    if (-not $gradleSucceeded) {
        throw "Gradle build did not report BUILD SUCCESSFUL (exit code: $gradleExitCode). See log: $gradleLogPath"
    }
}

Assert-PathExists -Path $apkPath -Label "Release APK"

if (-not $SkipInstall) {
    # Use the SDK path resolved at startup. Reading $env:ANDROID_SDK_ROOT here
    # breaks -SkipGradleBuild runs: that env var is only set inside the Gradle
    # branch, so the adb path would be null and Assert-PathExists would throw.
    $env:ANDROID_SDK_ROOT = $androidSdkRoot
    $adbPath = Join-Path $androidSdkRoot "platform-tools\adb.exe"
    Assert-PathExists -Path $adbPath -Label "adb"
    $installStopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    $deviceLines = & $adbPath devices
    $deviceIds = @()
    foreach ($line in $deviceLines) {
        if ($line -match "^([^\s]+)\s+device$") {
            $deviceIds += $Matches[1]
        }
    }

    if ($deviceIds.Count -eq 0) {
        throw "No connected adb devices found."
    }

    foreach ($deviceId in $deviceIds) {
        & $adbPath -s $deviceId install -r $apkPath
        $installExitCode = Get-NativeExitCode
        if ($installExitCode -ne 0) {
            throw "adb install failed for $deviceId"
        }

        & $adbPath -s $deviceId shell monkey -p com.mrtksync -c android.intent.category.LAUNCHER 1
        $launchExitCode = Get-NativeExitCode
        if ($launchExitCode -ne 0) {
            throw "Failed to launch app on $deviceId"
        }
    }

    $installStopwatch.Stop()
    $installDuration = $installStopwatch.Elapsed
}

$totalStopwatch.Stop()
Write-Host ""
Write-Host "APK ready: $apkPath"
Write-Host ("Timing: UnityExport={0:c} Gradle={1:c} Install={2:c} Total={3:c}" -f $unityExportDuration, $gradleBuildDuration, $installDuration, $totalStopwatch.Elapsed)
