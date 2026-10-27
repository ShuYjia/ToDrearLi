[CmdletBinding()]
param(
    [string]$WorkspacePath = "",

    [string]$SourceProjectPath = "",

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "ModuleWorkspace.Common.ps1")

if ([string]::IsNullOrWhiteSpace($WorkspacePath)) {
    $WorkspacePath = (Get-Location).Path
}

$resolvedWorkspacePath = Resolve-AbsolutePath -Path $WorkspacePath
$markerPath = Get-WorkspaceMarkerPath -WorkspacePath $resolvedWorkspacePath
if (-not (Test-Path -LiteralPath $markerPath)) {
    throw "Workspace marker not found: $markerPath"
}

$marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($SourceProjectPath)) {
    $SourceProjectPath = $marker.sourceProjectPath
}

$resolvedSourceProjectPath = Resolve-AbsolutePath -Path $SourceProjectPath
$manifest = Load-ModuleWorkspaceManifest -SourceProjectPath $resolvedSourceProjectPath
$definition = Get-ModuleWorkspaceDefinition -Manifest $manifest -ModuleName $marker.moduleName

Validate-UnityMetaPairs -RootPath $resolvedWorkspacePath -EditableRoots $definition.EditablePaths

$workspaceAllFiles = Get-WorkspaceContentFileMap -WorkspacePath $resolvedWorkspacePath
$allowedRoots = ConvertTo-UniqueStringArray -Values (@($definition.ReadonlyPaths) + @($definition.EditablePaths))
$allowedMetaPaths = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)

foreach ($root in $allowedRoots) {
    $normalizedRoot = Normalize-RelativePath -Path $root
    $segments = $normalizedRoot.Split("\", [System.StringSplitOptions]::RemoveEmptyEntries)
    $builder = New-Object "System.Collections.Generic.List[string]"
    foreach ($segment in $segments) {
        [void]$builder.Add($segment)
        [void]$allowedMetaPaths.Add((($builder -join "\") + ".meta"))
    }
}

$unexpectedPaths = @(
    $workspaceAllFiles.Keys |
    Where-Object {
        (-not $allowedMetaPaths.Contains($_)) -and
        (-not (Test-RelativePathMatchesAnyRoot -RelativePath $_ -Roots $allowedRoots))
    }
)

if ($unexpectedPaths.Count -gt 0) {
    $preview = $unexpectedPaths | Select-Object -First 10
    throw "Workspace contains files outside the configured allow-list.`n$($preview -join [Environment]::NewLine)"
}

$readonlyWorkspaceFiles = Get-FileMapForRoots -RootPath $resolvedWorkspacePath -RelativeRoots $definition.ReadonlyPaths
$readonlySourceFiles = Get-FileMapForRoots -RootPath $resolvedSourceProjectPath -RelativeRoots $definition.ReadonlyPaths
$readonlyDiffs = New-Object "System.Collections.Generic.List[string]"

foreach ($relativePath in (($readonlyWorkspaceFiles.Keys + $readonlySourceFiles.Keys) | Sort-Object -Unique)) {
    $inWorkspace = $readonlyWorkspaceFiles.ContainsKey($relativePath)
    $inSource = $readonlySourceFiles.ContainsKey($relativePath)

    if ($inWorkspace -ne $inSource) {
        [void]$readonlyDiffs.Add($relativePath)
        continue
    }

    if ($inWorkspace -and -not (Test-FileContentEquals -PathA $readonlyWorkspaceFiles[$relativePath] -PathB $readonlySourceFiles[$relativePath])) {
        [void]$readonlyDiffs.Add($relativePath)
    }
}

if ($readonlyDiffs.Count -gt 0) {
    $preview = $readonlyDiffs | Select-Object -First 10
    throw "Readonly baseline changed inside the workspace. Refresh or revert these files before syncing back.`n$($preview -join [Environment]::NewLine)"
}

$editableWorkspaceFiles = Get-FileMapForRoots -RootPath $resolvedWorkspacePath -RelativeRoots $definition.EditablePaths
$editableSourceFiles = Get-FileMapForRoots -RootPath $resolvedSourceProjectPath -RelativeRoots $definition.EditablePaths

$filesToCopy = New-Object "System.Collections.Generic.List[string]"
$filesToDelete = New-Object "System.Collections.Generic.List[string]"

foreach ($relativePath in (($editableWorkspaceFiles.Keys + $editableSourceFiles.Keys) | Sort-Object -Unique)) {
    $inWorkspace = $editableWorkspaceFiles.ContainsKey($relativePath)
    $inSource = $editableSourceFiles.ContainsKey($relativePath)

    if ($inWorkspace -and -not $inSource) {
        [void]$filesToCopy.Add($relativePath)
        continue
    }

    if (-not $inWorkspace -and $inSource) {
        [void]$filesToDelete.Add($relativePath)
        continue
    }

    if (-not (Test-FileContentEquals -PathA $editableWorkspaceFiles[$relativePath] -PathB $editableSourceFiles[$relativePath])) {
        [void]$filesToCopy.Add($relativePath)
    }
}

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry run only. No files were changed."
    Write-Host "Workspace : $resolvedWorkspacePath"
    Write-Host "Source    : $resolvedSourceProjectPath"
    Write-Host "Module    : $($definition.Name)"
    Write-Host "Copy      : $($filesToCopy.Count)"
    Write-Host "Delete    : $($filesToDelete.Count)"

    if ($filesToCopy.Count -gt 0) {
        Write-Host ""
        Write-Host "Files that would be copied:"
        $filesToCopy | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
    }

    if ($filesToDelete.Count -gt 0) {
        Write-Host ""
        Write-Host "Files that would be deleted:"
        $filesToDelete | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" }
    }

    return
}

foreach ($relativePath in $filesToDelete) {
    $targetPath = Get-AbsolutePathUnderRoot -RootPath $resolvedSourceProjectPath -RelativePath $relativePath
    if (Test-Path -LiteralPath $targetPath) {
        Assert-PathWithinRoot -RootPath $resolvedSourceProjectPath -CandidatePath $targetPath -Description "Editable delete target"
        Remove-Item -LiteralPath $targetPath -Force
    }
}

foreach ($relativePath in $filesToCopy) {
    $sourcePath = $editableWorkspaceFiles[$relativePath]
    $targetPath = Get-AbsolutePathUnderRoot -RootPath $resolvedSourceProjectPath -RelativePath $relativePath
    Ensure-Directory -Path (Split-Path -Parent $targetPath)
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
}

Write-Host ""
Write-Host "Workspace changes synced back to the main repo."
Write-Host "Workspace : $resolvedWorkspacePath"
Write-Host "Source    : $resolvedSourceProjectPath"
Write-Host "Module    : $($definition.Name)"
Write-Host "Copied    : $($filesToCopy.Count)"
Write-Host "Deleted   : $($filesToDelete.Count)"
