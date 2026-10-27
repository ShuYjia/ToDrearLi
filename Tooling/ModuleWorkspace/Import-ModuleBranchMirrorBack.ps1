[CmdletBinding()]
param(
    [string]$MirrorRepositoryPath = "",

    [string]$SourceProjectPath = "",

    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "ModuleWorkspace.Common.ps1")

if ([string]::IsNullOrWhiteSpace($MirrorRepositoryPath)) {
    $MirrorRepositoryPath = (Get-Location).Path
}

$resolvedMirrorRepositoryPath = Resolve-AbsolutePath -Path $MirrorRepositoryPath
$markerPath = Get-WorkspaceMarkerPath -WorkspacePath $resolvedMirrorRepositoryPath
if (-not (Test-Path -LiteralPath $markerPath)) {
    throw "Module branch mirror marker not found: $markerPath"
}

$marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json
if (-not [string]::IsNullOrWhiteSpace([string]$marker.distributionMode)) {
    if ([string]$marker.distributionMode -ne "module-branch-mirror") {
        throw "This workspace was not marked as a module branch mirror: $resolvedMirrorRepositoryPath"
    }
}

if ([string]::IsNullOrWhiteSpace($SourceProjectPath)) {
    $SourceProjectPath = $marker.sourceProjectPath
}

$resolvedSourceProjectPath = Resolve-AbsolutePath -Path $SourceProjectPath
$manifest = Load-ModuleWorkspaceManifest -SourceProjectPath $resolvedSourceProjectPath
$definition = Get-ModuleWorkspaceDefinition -Manifest $manifest -ModuleName $marker.moduleName

Validate-UnityMetaPairs -RootPath $resolvedMirrorRepositoryPath -EditableRoots $definition.EditablePaths

$mirrorAllFiles = Get-WorkspaceContentFileMap -WorkspacePath $resolvedMirrorRepositoryPath
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

$generatedFiles = ConvertTo-UniqueStringArray -Values @($manifest.generatedFiles)
$unexpectedPaths = @(
    $mirrorAllFiles.Keys |
    Where-Object {
        $relativePath = $_
        $isGeneratedFile = $false
        foreach ($generatedFile in $generatedFiles) {
            if ($relativePath.Equals((Normalize-RelativePath -Path $generatedFile), [System.StringComparison]::OrdinalIgnoreCase)) {
                $isGeneratedFile = $true
                break
            }
        }

        if ($isGeneratedFile) {
            return $false
        }

        (-not $allowedMetaPaths.Contains($relativePath)) -and
        (-not (Test-RelativePathMatchesAnyRoot -RelativePath $relativePath -Roots $allowedRoots))
    }
)

if ($unexpectedPaths.Count -gt 0) {
    $preview = $unexpectedPaths | Select-Object -First 10
    throw "Module branch mirror contains files outside the configured allow-list.`n$($preview -join [Environment]::NewLine)"
}

$readonlyMirrorFiles = Get-FileMapForRoots -RootPath $resolvedMirrorRepositoryPath -RelativeRoots $definition.ReadonlyPaths
$readonlySourceFiles = Get-FileMapForRoots -RootPath $resolvedSourceProjectPath -RelativeRoots $definition.ReadonlyPaths
$readonlyDiffs = New-Object "System.Collections.Generic.List[string]"

foreach ($relativePath in (($readonlyMirrorFiles.Keys + $readonlySourceFiles.Keys) | Sort-Object -Unique)) {
    $inMirror = $readonlyMirrorFiles.ContainsKey($relativePath)
    $inSource = $readonlySourceFiles.ContainsKey($relativePath)

    if ($inMirror -ne $inSource) {
        [void]$readonlyDiffs.Add($relativePath)
        continue
    }

    if ($inMirror -and -not (Test-FileContentEquals -PathA $readonlyMirrorFiles[$relativePath] -PathB $readonlySourceFiles[$relativePath])) {
        [void]$readonlyDiffs.Add($relativePath)
    }
}

if ($readonlyDiffs.Count -gt 0) {
    $preview = $readonlyDiffs | Select-Object -First 10
    throw "Readonly baseline changed inside the module branch mirror. Republish from the main repo before importing back.`n$($preview -join [Environment]::NewLine)"
}

$editableMirrorFiles = Get-FileMapForRoots -RootPath $resolvedMirrorRepositoryPath -RelativeRoots $definition.EditablePaths
$editableSourceFiles = Get-FileMapForRoots -RootPath $resolvedSourceProjectPath -RelativeRoots $definition.EditablePaths

$filesToCopy = New-Object "System.Collections.Generic.List[string]"
$filesToDelete = New-Object "System.Collections.Generic.List[string]"

foreach ($relativePath in (($editableMirrorFiles.Keys + $editableSourceFiles.Keys) | Sort-Object -Unique)) {
    $inMirror = $editableMirrorFiles.ContainsKey($relativePath)
    $inSource = $editableSourceFiles.ContainsKey($relativePath)

    if ($inMirror -and -not $inSource) {
        [void]$filesToCopy.Add($relativePath)
        continue
    }

    if (-not $inMirror -and $inSource) {
        [void]$filesToDelete.Add($relativePath)
        continue
    }

    if (-not (Test-FileContentEquals -PathA $editableMirrorFiles[$relativePath] -PathB $editableSourceFiles[$relativePath])) {
        [void]$filesToCopy.Add($relativePath)
    }
}

if ($DryRun) {
    Write-Host ""
    Write-Host "Dry run only. No files were changed."
    Write-Host "Mirror repo : $resolvedMirrorRepositoryPath"
    Write-Host "Source      : $resolvedSourceProjectPath"
    Write-Host "Module      : $($definition.Name)"
    Write-Host "Copy        : $($filesToCopy.Count)"
    Write-Host "Delete      : $($filesToDelete.Count)"

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
    $sourcePath = $editableMirrorFiles[$relativePath]
    $targetPath = Get-AbsolutePathUnderRoot -RootPath $resolvedSourceProjectPath -RelativePath $relativePath
    Ensure-Directory -Path (Split-Path -Parent $targetPath)
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force
}

Write-Host ""
Write-Host "Module branch mirror changes imported back to the main repo."
Write-Host "Mirror repo : $resolvedMirrorRepositoryPath"
Write-Host "Source      : $resolvedSourceProjectPath"
Write-Host "Module      : $($definition.Name)"
Write-Host "Copied      : $($filesToCopy.Count)"
Write-Host "Deleted     : $($filesToDelete.Count)"
