# Copyright (c) Mixed Reality Toolkit Contributors
# Licensed under the BSD 3-Clause

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$SourceProjectPath = "",

    [Parameter(Mandatory = $false)]
    [string]$BatchProjectPath = "",

    [switch]$ForceRepair
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceProjectPath)) {
    $scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
    $SourceProjectPath = Join-Path $scriptDirectory ".."
}

function Resolve-FullPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine((Get-Location).Path, $Path)
    )
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
        Write-Host "Created directory: $Path"
        return
    }

    $item = Get-Item -LiteralPath $Path -Force
    if (-not $item.PSIsContainer) {
        throw "Path exists but is not a directory: $Path"
    }
}

function Get-LinkTargetPath {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileSystemInfo]$Item
    )

    if (-not ($Item.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
        return $null
    }

    $targets = @($Item.Target)
    if ($targets.Count -eq 0 -or [string]::IsNullOrWhiteSpace($targets[0])) {
        return $null
    }

    return [System.IO.Path]::GetFullPath($targets[0])
}

function Ensure-Junction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LinkPath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    if (-not (Test-Path -LiteralPath $TargetPath)) {
        throw "Junction target does not exist: $TargetPath"
    }

    if (Test-Path -LiteralPath $LinkPath) {
        $existingItem = Get-Item -LiteralPath $LinkPath -Force
        $existingTarget = Get-LinkTargetPath -Item $existingItem
        if ($existingTarget -eq $TargetPath) {
            Write-Host "Junction already correct: $LinkPath -> $TargetPath"
            return
        }

        if (-not $ForceRepair) {
            throw "Existing path does not match expected junction target: $LinkPath"
        }

        if (-not ($existingItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing to repair a non-junction path automatically: $LinkPath"
        }

        Remove-Item -LiteralPath $LinkPath -Force
        Write-Host "Removed junction: $LinkPath"
    }

    New-Item -ItemType Junction -Path $LinkPath -Target $TargetPath | Out-Null
    Write-Host "Created junction: $LinkPath -> $TargetPath"
}

$resolvedSourceProjectPath = Resolve-FullPath -Path $SourceProjectPath
if (-not (Test-Path -LiteralPath $resolvedSourceProjectPath)) {
    throw "Source project path does not exist: $resolvedSourceProjectPath"
}

if ([string]::IsNullOrWhiteSpace($BatchProjectPath)) {
    $sourceParent = Split-Path -Parent $resolvedSourceProjectPath
    $sourceName = Split-Path -Leaf $resolvedSourceProjectPath
    $BatchProjectPath = Join-Path $sourceParent ($sourceName + "__batchbuild")
}

$resolvedBatchProjectPath = Resolve-FullPath -Path $BatchProjectPath
$batchParentPath = Split-Path -Parent $resolvedBatchProjectPath
if (-not (Test-Path -LiteralPath $batchParentPath)) {
    throw "Batch project parent does not exist: $batchParentPath"
}

Ensure-Directory -Path $resolvedBatchProjectPath

$junctionMap = @{
    "Assets" = (Join-Path $resolvedSourceProjectPath "Assets")
    "Packages" = (Join-Path $resolvedSourceProjectPath "Packages")
    "ProjectSettings" = (Join-Path $resolvedSourceProjectPath "ProjectSettings")
}

foreach ($entry in $junctionMap.GetEnumerator()) {
    Ensure-Junction `
        -LinkPath (Join-Path $resolvedBatchProjectPath $entry.Key) `
        -TargetPath $entry.Value
}

$localDirectories = @(
    "Builds",
    "Builds\\Android",
    "Library",
    "Logs",
    "UserSettings"
)

foreach ($relativeDirectory in $localDirectories) {
    Ensure-Directory -Path (Join-Path $resolvedBatchProjectPath $relativeDirectory)
}

Write-Host ""
Write-Host "Batch project is ready."
Write-Host "Source : $resolvedSourceProjectPath"
Write-Host "Batch  : $resolvedBatchProjectPath"
