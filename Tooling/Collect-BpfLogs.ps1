<#
.SYNOPSIS
    Collects BPF unified log files (JSONL) from connected Android/PICO devices.

.DESCRIPTION
    Pulls every file from <persistentDataPath>/BpfLogs on each target device,
    optionally captures the recent logcat tail, and writes a collection-info
    file per device that records the application identifier, device serial,
    in-app log path and collection time.

    Two access paths are supported:
      1. "run-as"  development (debuggable) players read their private folder
                   through "adb exec-out run-as <package> cat ...", which also
                   works on Android 11+ where /sdcard/Android/data is restricted.
      2. "sdcard"  release players fall back to
                   /sdcard/Android/data/<package>/files/<LogDirectoryName>
                   with a direct "adb pull".

    The access path actually used is recorded in collection-info.json.

.PARAMETER Serial
    One or more device serials. When omitted every device in "adb devices"
    with state "device" is collected.

.PARAMETER OutputDirectory
    Root folder for collected logs. Defaults to Tooling/out/bpf-logs.

.PARAMETER AppIdentifier
    Android application identifier of the BPF player.

.PARAMETER LogDirectoryName
    Folder below Application.persistentDataPath that holds the JSONL files.

.PARAMETER IncludeLogcat
    Also capture the recent logcat ring buffer for the application.

.PARAMETER LogcatLines
    Number of logcat lines captured when -IncludeLogcat is used.

.EXAMPLE
    powershell -File Tooling/Collect-BpfLogs.ps1

.EXAMPLE
    powershell -File Tooling/Collect-BpfLogs.ps1 -Serial A9210ABC123 -IncludeLogcat

.EXAMPLE
    powershell -File Tooling/Collect-BpfLogs.ps1 -Serial dev1,dev2 -OutputDirectory D:\bpf-logs
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string[]] $Serial,

    [Parameter(Mandatory = $false)]
    [string] $OutputDirectory = (Join-Path $PSScriptRoot 'out\bpf-logs'),

    [Parameter(Mandatory = $false)]
    [string] $AppIdentifier = 'com.mrtksync',

    [Parameter(Mandatory = $false)]
    [string] $LogDirectoryName = 'BpfLogs',

    [Parameter(Mandatory = $false)]
    [switch] $IncludeLogcat,

    [Parameter(Mandatory = $false)]
    [int] $LogcatLines = 400
)

$ErrorActionPreference = 'Stop'

function Get-AdbPath {
    $command = Get-Command adb -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
        'C:\Program Files\Android\Android Studio\platform-tools\adb.exe'
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw 'adb was not found. Install the Android platform tools or add adb to PATH.'
}

function Invoke-Adb {
    param(
        [Parameter(Mandatory = $true)] [string] $AdbPath,
        [Parameter(Mandatory = $true)] [string[]] $Arguments
    )

    $output = & $AdbPath @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw ("adb {0} failed: {1}" -f ($Arguments -join ' '), ($output -join ' '))
    }

    return $output
}

function Get-TargetSerials {
    param([Parameter(Mandatory = $true)] [string] $AdbPath)

    if ($Serial) {
        return $Serial
    }

    $lines = & $AdbPath devices
    $serials = @()
    foreach ($line in $lines) {
        if ($line -match '^(\S+)\s+device\s*$') {
            $serials += $Matches[1]
        } elseif ($line -match '^(\S+)\s+unauthorized\s*$') {
            Write-Warning ("device {0} is not authorized; accept the USB debugging prompt first." -f $Matches[1])
        }
    }

    if ($serials.Count -eq 0) {
        throw 'No authorized adb device found. Connect the PICO headset and confirm the debugging prompt.'
    }

    return $serials
}

function Select-JsonlNames {
    param([Parameter(Mandatory = $false)] $Listing)

    $files = @()
    foreach ($entry in @($Listing)) {
        $name = ($entry -replace "`r", '').Trim()
        if ($name -match '\.jsonl$') {
            $files += $name
        }
    }

    return $files
}

function Resolve-RemoteLogDirectory {
    param(
        [Parameter(Mandatory = $true)] [string] $AdbPath,
        [Parameter(Mandatory = $true)] [string] $DeviceSerial
    )

    $runAsFolder = 'files/{0}' -f $LogDirectoryName
    try {
        $listing = Invoke-Adb -AdbPath $AdbPath -Arguments @(
            '-s', $DeviceSerial, 'shell', 'run-as', $AppIdentifier, 'ls', '-1', $runAsFolder)
        return [pscustomobject]@{
            Mode  = 'run-as'
            Root  = $runAsFolder
            Files = @(Select-JsonlNames -Listing $listing)
        }
    } catch {
        Write-Verbose ("run-as access failed on {0}: {1}" -f $DeviceSerial, $_.Exception.Message)
    }

    $sdcardFolder = '/sdcard/Android/data/{0}/files/{1}' -f $AppIdentifier, $LogDirectoryName
    try {
        $listing = Invoke-Adb -AdbPath $AdbPath -Arguments @(
            '-s', $DeviceSerial, 'shell', 'ls', '-1', $sdcardFolder)
        return [pscustomobject]@{
            Mode  = 'sdcard'
            Root  = $sdcardFolder
            Files = @(Select-JsonlNames -Listing $listing)
        }
    } catch {
        Write-Warning ("no readable BPF log folder on {0} ({1})" -f $DeviceSerial, $_.Exception.Message)
    }

    return [pscustomobject]@{
        Mode  = 'none'
        Root  = $sdcardFolder
        Files = @()
    }
}

function Copy-RemoteFile {
    param(
        [Parameter(Mandatory = $true)] [string] $AdbPath,
        [Parameter(Mandatory = $true)] [string] $DeviceSerial,
        [Parameter(Mandatory = $true)] [string] $Mode,
        [Parameter(Mandatory = $true)] [string] $RemoteRoot,
        [Parameter(Mandatory = $true)] [string] $FileName,
        [Parameter(Mandatory = $true)] [string] $LocalPath
    )

    $directory = Split-Path -Parent $LocalPath
    if (-not (Test-Path $directory)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }

    $remote = '{0}/{1}' -f $RemoteRoot, $FileName
    if ($Mode -eq 'run-as') {
        # exec-out with cmd redirection keeps the JSONL bytes untouched.
        $command = '"{0}" -s {1} exec-out run-as {2} cat {3} > "{4}"' -f `
            $AdbPath, $DeviceSerial, $AppIdentifier, $remote, $LocalPath
        & cmd.exe /c $command | Out-Null
    } elseif ($Mode -eq 'sdcard') {
        & $AdbPath -s $DeviceSerial pull $remote $LocalPath 2>&1 | Out-Null
    } else {
        return $false
    }

    return (Test-Path $LocalPath)
}

$adb = Get-AdbPath
$targets = Get-TargetSerials -AdbPath $adb
$collectedAt = (Get-Date).ToUniversalTime()
$results = @()

foreach ($deviceSerial in $targets) {
    Write-Host ("Collecting BPF logs from {0} ..." -f $deviceSerial)

    $deviceDirectory = Join-Path $OutputDirectory $deviceSerial
    if (-not (Test-Path $deviceDirectory)) {
        New-Item -ItemType Directory -Path $deviceDirectory -Force | Out-Null
    }

    $remote = Resolve-RemoteLogDirectory -AdbPath $adb -DeviceSerial $deviceSerial
    $copied = @()
    foreach ($file in $remote.Files) {
        $localPath = Join-Path $deviceDirectory $file
        if (Copy-RemoteFile -AdbPath $adb -DeviceSerial $deviceSerial -Mode $remote.Mode `
                -RemoteRoot $remote.Root -FileName $file -LocalPath $localPath) {
            $copied += [pscustomobject]@{
                File      = $file
                Bytes     = (Get-Item $localPath).Length
                LocalPath = $localPath
            }
        } else {
            Write-Warning ("failed to copy {0}/{1} from {2}" -f $remote.Root, $file, $deviceSerial)
        }
    }

    $logcatPath = $null
    if ($IncludeLogcat) {
        $logcatPath = Join-Path $deviceDirectory 'logcat.txt'
        $command = '"{0}" -s {1} logcat -d -v threadtime -t {2} > "{3}"' -f `
            $adb, $deviceSerial, $LogcatLines, $logcatPath
        & cmd.exe /c $command | Out-Null
        if (-not (Test-Path $logcatPath)) {
            $logcatPath = $null
            Write-Warning ("logcat capture failed on {0}" -f $deviceSerial)
        }
    }

    $model = (& $adb -s $deviceSerial shell getprop ro.product.model) -join ' '
    $androidVersion = (& $adb -s $deviceSerial shell getprop ro.build.version.release) -join ' '
    $serialInfo = [pscustomobject]@{
        collectedAtUtc  = $collectedAt.ToString('yyyy-MM-ddTHH:mm:ssZ')
        deviceSerial    = $deviceSerial
        deviceModel     = $model.Trim()
        androidVersion  = $androidVersion.Trim()
        appIdentifier   = $AppIdentifier
        inAppLogPath    = ('/sdcard/Android/data/{0}/files/{1}' -f $AppIdentifier, $LogDirectoryName)
        remoteLogFolder = $remote.Root
        accessMode      = $remote.Mode
        collectedFiles  = $copied
        logcatFile      = $logcatPath
    }

    $infoPath = Join-Path $deviceDirectory 'collection-info.json'
    $serialInfo | ConvertTo-Json -Depth 5 | Set-Content -Path $infoPath -Encoding UTF8
    $results += $serialInfo

    Write-Host ("  access mode: {0}" -f $remote.Mode)
    Write-Host ("  files copied: {0}" -f $copied.Count)
    Write-Host ("  output: {0}" -f $deviceDirectory)
}

Write-Host ''
Write-Host 'BPF log collection summary'
foreach ($result in $results) {
    Write-Host ("  {0} ({1}): {2} file(s)" -f $result.deviceSerial, $result.deviceModel, $result.collectedFiles.Count)
}

if (($results | ForEach-Object { $_.collectedFiles.Count } | Measure-Object -Sum).Sum -eq 0) {
    Write-Warning 'No JSONL file was collected. Confirm the player was launched at least once with the Bpf diagnostics system enabled.'
    exit 2
}

exit 0
