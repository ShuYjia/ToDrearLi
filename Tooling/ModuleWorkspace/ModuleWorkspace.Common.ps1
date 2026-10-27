[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [string]$BasePath = (Get-Location).Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath(
        [System.IO.Path]::Combine($BasePath, $Path)
    )
}

function Normalize-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $normalized = $Path.Replace("/", "\").Trim()
    while ($normalized.StartsWith(".\")) {
        $normalized = $normalized.Substring(2)
    }

    return $normalized.TrimStart("\")
}

function ConvertTo-GitPathSpec {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Normalize-RelativePath -Path $Path).Replace("\", "/")
}

function ConvertTo-UniqueStringArray {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Values
    )

    $seen = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    $result = New-Object "System.Collections.Generic.List[string]"

    foreach ($value in $Values) {
        if ($null -eq $value) {
            continue
        }

        $text = [string]$value
        if ([string]::IsNullOrWhiteSpace($text)) {
            continue
        }

        $normalized = Normalize-RelativePath -Path $text
        if ($seen.Add($normalized)) {
            [void]$result.Add($normalized)
        }
    }

    return @($result)
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
        return
    }

    $item = Get-Item -LiteralPath $Path -Force
    if (-not $item.PSIsContainer) {
        throw "Path exists but is not a directory: $Path"
    }
}

function Assert-PathWithinRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,

        [Parameter(Mandatory = $true)]
        [string]$CandidatePath,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    $resolvedRoot = Resolve-AbsolutePath -Path $RootPath
    $resolvedCandidate = Resolve-AbsolutePath -Path $CandidatePath

    if ($resolvedCandidate.Equals($resolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        return
    }

    $rootPrefix = $resolvedRoot.TrimEnd("\") + "\"
    if (-not $resolvedCandidate.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Description is outside the expected root.`nRoot: $resolvedRoot`nPath: $resolvedCandidate"
    }
}

function Get-AbsolutePathUnderRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $normalized = Normalize-RelativePath -Path $RelativePath
    return Resolve-AbsolutePath -Path (Join-Path $RootPath $normalized)
}

function Load-ModuleWorkspaceManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath
    )

    $manifestPath = Join-Path $SourceProjectPath "Tooling\ModuleWorkspace\module-workspaces.json"
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "Module workspace manifest does not exist: $manifestPath"
    }

    return (Get-Content -Raw -Encoding UTF8 -LiteralPath $manifestPath | ConvertFrom-Json)
}

function Get-ModuleWorkspaceDefinition {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Manifest,

        [Parameter(Mandatory = $true)]
        [string]$ModuleName
    )

    $moduleEntry = $Manifest.modules | Where-Object { $_.name -eq $ModuleName } | Select-Object -First 1
    if ($null -eq $moduleEntry) {
        throw "Module '$ModuleName' is not defined in Tooling/ModuleWorkspace/module-workspaces.json."
    }

    $otherModuleRoots = @()
    if ($moduleEntry.excludeOtherModuleRoots) {
        $otherModuleRoots = @(
            $Manifest.modules |
            Where-Object { $_.name -ne $ModuleName } |
            ForEach-Object { "Assets/App/Modules/$($_.name)" }
        )
    }

    return [PSCustomObject]@{
        SchemaVersion            = [int]$Manifest.schemaVersion
        WorkspaceRootSuffix      = [string]$Manifest.workspaceRootSuffix
        LocalWorkspaceDirectories = @($Manifest.localWorkspaceDirectories)
        GeneratedFiles           = @($Manifest.generatedFiles)
        GlobalReadonlyPaths      = ConvertTo-UniqueStringArray -Values @($Manifest.globalReadonlyPaths)
        GlobalExcludedPaths      = ConvertTo-UniqueStringArray -Values @($Manifest.globalExcludedPaths)
        Name                     = [string]$moduleEntry.name
        Status                   = [string]$moduleEntry.status
        EntryScenePath           = [string]$moduleEntry.entryScenePath
        EditablePaths            = ConvertTo-UniqueStringArray -Values @($moduleEntry.editablePaths)
        ReadonlyPaths            = ConvertTo-UniqueStringArray -Values (@($Manifest.globalReadonlyPaths) + @($moduleEntry.readonlyPaths))
        ExcludedPaths            = ConvertTo-UniqueStringArray -Values (@($Manifest.globalExcludedPaths) + @($moduleEntry.excludedPaths) + $otherModuleRoots)
        Notes                    = @($moduleEntry.notes)
    }
}

function Get-DefaultModuleWorkspaceRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkspaceRootSuffix
    )

    $sourceParent = Split-Path -Parent $SourceProjectPath
    $sourceName = Split-Path -Leaf $SourceProjectPath
    return Join-Path $sourceParent ($sourceName + $WorkspaceRootSuffix)
}

function Get-ModuleBranchMirrorSettings {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Manifest
    )

    $settings = $Manifest.branchMirror
    if ($null -eq $settings) {
        return [PSCustomObject]@{
            RemoteName          = "module-origin"
            BranchPrefix        = "module/"
            WorkspaceRootSuffix = "__modulebranchmirror"
            LegacyDefaultBranches = @("main", "master")
        }
    }

    return [PSCustomObject]@{
        RemoteName            = if ([string]::IsNullOrWhiteSpace([string]$settings.remoteName)) { "module-origin" } else { [string]$settings.remoteName }
        BranchPrefix          = if ([string]::IsNullOrWhiteSpace([string]$settings.branchPrefix)) { "module/" } else { [string]$settings.branchPrefix }
        WorkspaceRootSuffix   = if ([string]::IsNullOrWhiteSpace([string]$settings.workspaceRootSuffix)) { "__modulebranchmirror" } else { [string]$settings.workspaceRootSuffix }
        LegacyDefaultBranches = ConvertTo-UniqueStringArray -Values @($settings.legacyDefaultBranches)
    }
}

function Get-ModuleBranchName {
    param(
        [Parameter(Mandatory = $true)]
        [object]$BranchMirrorSettings,

        [Parameter(Mandatory = $true)]
        [string]$ModuleName
    )

    return ([string]$BranchMirrorSettings.BranchPrefix) + $ModuleName
}

function Get-DefaultModuleBranchMirrorRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$ScratchRootSuffix
    )

    $sourceParent = Split-Path -Parent $SourceProjectPath
    $sourceName = Split-Path -Leaf $SourceProjectPath
    return Join-Path $sourceParent ($sourceName + $ScratchRootSuffix)
}

function Copy-ParentDirectoryMetas {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [string]$RelativeDirectory
    )

    if ([string]::IsNullOrWhiteSpace($RelativeDirectory)) {
        return
    }

    $normalized = Normalize-RelativePath -Path $RelativeDirectory
    $segments = $normalized.Split("\", [System.StringSplitOptions]::RemoveEmptyEntries)
    $builder = New-Object "System.Collections.Generic.List[string]"

    foreach ($segment in $segments) {
        [void]$builder.Add($segment)
        $current = ($builder -join "\")
        $currentDirectory = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath $current
        Ensure-Directory -Path $currentDirectory

        $sourceMeta = Get-AbsolutePathUnderRoot -RootPath $SourceProjectPath -RelativePath ($current + ".meta")
        if (Test-Path -LiteralPath $sourceMeta) {
            $targetMeta = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath ($current + ".meta")
            Ensure-Directory -Path (Split-Path -Parent $targetMeta)
            Copy-Item -LiteralPath $sourceMeta -Destination $targetMeta -Force
        }
    }
}

function Copy-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $normalized = Normalize-RelativePath -Path $RelativePath
    $sourcePath = Get-AbsolutePathUnderRoot -RootPath $SourceProjectPath -RelativePath $normalized
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Configured path does not exist in source project: $normalized"
    }

    $parentRelative = Split-Path -Parent $normalized
    if (-not [string]::IsNullOrWhiteSpace($parentRelative)) {
        Copy-ParentDirectoryMetas -SourceProjectPath $SourceProjectPath -WorkspacePath $WorkspacePath -RelativeDirectory $parentRelative
    }

    $sourceItem = Get-Item -LiteralPath $sourcePath -Force
    if ($sourceItem.PSIsContainer) {
        $targetParent = if ([string]::IsNullOrWhiteSpace($parentRelative)) {
            $WorkspacePath
        }
        else {
            Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath $parentRelative
        }

        Ensure-Directory -Path $targetParent
        Copy-Item -LiteralPath $sourcePath -Destination $targetParent -Recurse -Force

        $sourceMeta = $sourcePath + ".meta"
        if (Test-Path -LiteralPath $sourceMeta) {
            $targetMeta = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath ($normalized + ".meta")
            Copy-Item -LiteralPath $sourceMeta -Destination $targetMeta -Force
        }

        return
    }

    $targetPath = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath $normalized
    Ensure-Directory -Path (Split-Path -Parent $targetPath)
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    if (-not $normalized.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
        $sourceMeta = $sourcePath + ".meta"
        if (Test-Path -LiteralPath $sourceMeta) {
            $targetMeta = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath ($normalized + ".meta")
            Copy-Item -LiteralPath $sourceMeta -Destination $targetMeta -Force
        }
    }
}

function Set-ReadOnlyStateForRoots {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [string[]]$RelativeRoots,

        [Parameter(Mandatory = $true)]
        [bool]$ReadOnly
    )

    foreach ($root in (ConvertTo-UniqueStringArray -Values $RelativeRoots)) {
        $targetPath = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath $root
        if (-not (Test-Path -LiteralPath $targetPath)) {
            continue
        }

        $item = Get-Item -LiteralPath $targetPath -Force
        if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $targetPath -File -Recurse -Force | ForEach-Object {
                $_.IsReadOnly = $ReadOnly
            }
        }
        else {
            $item.IsReadOnly = $ReadOnly
        }

        $metaPath = $targetPath + ".meta"
        if (Test-Path -LiteralPath $metaPath) {
            (Get-Item -LiteralPath $metaPath -Force).IsReadOnly = $ReadOnly
        }
    }
}

function Remove-RelativePathIfExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $targetPath = Get-AbsolutePathUnderRoot -RootPath $WorkspacePath -RelativePath $RelativePath
    if (Test-Path -LiteralPath $targetPath) {
        Assert-PathWithinRoot -RootPath $WorkspacePath -CandidatePath $targetPath -Description "Delete target"
        Remove-Item -LiteralPath $targetPath -Recurse -Force
    }

    $metaPath = $targetPath + ".meta"
    if (Test-Path -LiteralPath $metaPath) {
        Assert-PathWithinRoot -RootPath $WorkspacePath -CandidatePath $metaPath -Description "Delete target meta"
        Remove-Item -LiteralPath $metaPath -Force
    }
}

function Get-FileMapForRoots {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,

        [Parameter(Mandatory = $true)]
        [string[]]$RelativeRoots
    )

    $map = @{}
    foreach ($root in (ConvertTo-UniqueStringArray -Values $RelativeRoots)) {
        $absoluteRoot = Get-AbsolutePathUnderRoot -RootPath $RootPath -RelativePath $root
        if (-not (Test-Path -LiteralPath $absoluteRoot)) {
            continue
        }

        $item = Get-Item -LiteralPath $absoluteRoot -Force
        if ($item.PSIsContainer) {
            Get-ChildItem -LiteralPath $absoluteRoot -File -Recurse -Force | ForEach-Object {
                $relative = $_.FullName.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                $map[(Normalize-RelativePath -Path $relative)] = $_.FullName
            }

            $metaPath = $absoluteRoot + ".meta"
            if (Test-Path -LiteralPath $metaPath) {
                $relative = $metaPath.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                $map[(Normalize-RelativePath -Path $relative)] = $metaPath
            }
            continue
        }

        $relativeItem = $item.FullName.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
        $map[(Normalize-RelativePath -Path $relativeItem)] = $item.FullName

        if (-not $item.Name.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
            $metaPath = $item.FullName + ".meta"
            if (Test-Path -LiteralPath $metaPath) {
                $relativeMeta = $metaPath.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                $map[(Normalize-RelativePath -Path $relativeMeta)] = $metaPath
            }
        }
    }

    return $map
}

function Test-FileContentEquals {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathA,

        [Parameter(Mandatory = $true)]
        [string]$PathB
    )

    $itemA = Get-Item -LiteralPath $PathA -Force
    $itemB = Get-Item -LiteralPath $PathB -Force

    if ($itemA.Length -ne $itemB.Length) {
        return $false
    }

    if ($itemA.LastWriteTimeUtc.Ticks -eq $itemB.LastWriteTimeUtc.Ticks) {
        return $true
    }

    if ($itemA.Length -eq 0) {
        return $true
    }

    $hashA = (Get-FileHash -LiteralPath $PathA -Algorithm SHA256).Hash
    $hashB = (Get-FileHash -LiteralPath $PathB -Algorithm SHA256).Hash
    return $hashA -eq $hashB
}

function Test-RelativePathMatchesAnyRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Roots
    )

    $candidate = Normalize-RelativePath -Path $RelativePath
    foreach ($root in (ConvertTo-UniqueStringArray -Values $Roots)) {
        $normalizedRoot = Normalize-RelativePath -Path $root
        if ($candidate.Equals($normalizedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }

        if ($candidate.Equals($normalizedRoot + ".meta", [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }

        $prefix = $normalizedRoot.TrimEnd("\") + "\"
        if ($candidate.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Get-WorkspaceMarkerPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath
    )

    return Join-Path $WorkspacePath ".module-workspace.json"
}

function Write-WorkspaceMarker {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [object]$Definition,

        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath
    )

    $marker = [PSCustomObject]@{
        schemaVersion   = $Definition.SchemaVersion
        moduleName      = $Definition.Name
        moduleStatus    = $Definition.Status
        sourceProjectPath = $SourceProjectPath
        generatedUtc    = (Get-Date).ToUniversalTime().ToString("o")
        entryScenePath  = $Definition.EntryScenePath
        editablePaths   = @($Definition.EditablePaths)
        readonlyPaths   = @($Definition.ReadonlyPaths)
        excludedPaths   = @($Definition.ExcludedPaths)
        notes           = @($Definition.Notes)
    }

    $markerPath = Get-WorkspaceMarkerPath -WorkspacePath $WorkspacePath
    $marker | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $markerPath -Encoding UTF8
}

function Write-WorkspaceReadme {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [object]$Definition,

        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath
    )

    $notesText = if ($Definition.Notes.Count -gt 0) {
        ($Definition.Notes | ForEach-Object { "- $_" }) -join [Environment]::NewLine
    }
    else {
        "- No extra notes."
    }

    $content = @"
# $($Definition.Name) module workspace

This workspace was generated from:

- Source project: $SourceProjectPath
- Module: $($Definition.Name)
- Status: $($Definition.Status)
- Entry scene: $($Definition.EntryScenePath)

Editable paths:
$(
($Definition.EditablePaths | ForEach-Object { "- $_" }) -join [Environment]::NewLine
)

Readonly baseline:
$(
($Definition.ReadonlyPaths | ForEach-Object { "- $_" }) -join [Environment]::NewLine
)

Blocked paths:
$(
($Definition.ExcludedPaths | ForEach-Object { "- $_" }) -join [Environment]::NewLine
)

Notes:
$notesText

Recommended workflow:

1. Open this workspace in Unity.
2. Keep edits inside the editable paths only.
3. If this workspace was generated locally from the main repo, refresh the readonly baseline with `Tooling/ModuleWorkspace/Update-ModuleWorkspaceBaseline.ps1`.
4. If this workspace came from the module branch mirror repo, pull the same branch after the main repo maintainer republishes the baseline.
5. Sync approved edits back to the main repo with `Tooling/ModuleWorkspace/Sync-ModuleWorkspaceBack.ps1`, or let the main repo maintainer import this branch back.
"@

    Set-Content -LiteralPath (Join-Path $WorkspacePath "README.md") -Value $content -Encoding UTF8
}

function Write-WorkspaceGitIgnore {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath
    )

    $content = @"
/Builds/
/Library/
/Logs/
/Temp/
/UserSettings/
/obj/
/.vs/
/*.csproj
/*.sln
"@

    Set-Content -LiteralPath (Join-Path $WorkspacePath ".gitignore") -Value $content -Encoding UTF8
}

function Initialize-ModuleWorkspaceContent {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [object]$Definition,

        [switch]$PreserveGitDirectory
    )

    Ensure-Directory -Path $WorkspacePath

    if ($PreserveGitDirectory) {
        Clear-DirectoryContentsExceptGit -DirectoryPath $WorkspacePath
    }
    else {
        Get-ChildItem -LiteralPath $WorkspacePath -Force | ForEach-Object {
            Assert-PathWithinRoot -RootPath $WorkspacePath -CandidatePath $_.FullName -Description "Workspace reset target"
            Remove-Item -LiteralPath $_.FullName -Recurse -Force
        }
    }

    $copyRoots = ConvertTo-UniqueStringArray -Values (@($Definition.ReadonlyPaths) + @($Definition.EditablePaths))
    foreach ($copyRoot in $copyRoots) {
        Copy-RelativePath `
            -SourceProjectPath $SourceProjectPath `
            -WorkspacePath $WorkspacePath `
            -RelativePath $copyRoot
    }

    foreach ($localDirectory in $Definition.LocalWorkspaceDirectories) {
        Ensure-Directory -Path (Join-Path $WorkspacePath $localDirectory)
    }

    Write-WorkspaceMarker `
        -WorkspacePath $WorkspacePath `
        -Definition $Definition `
        -SourceProjectPath $SourceProjectPath

    Write-WorkspaceReadme `
        -WorkspacePath $WorkspacePath `
        -Definition $Definition `
        -SourceProjectPath $SourceProjectPath

    Write-WorkspaceGitIgnore -WorkspacePath $WorkspacePath

    Set-ReadOnlyStateForRoots `
        -WorkspacePath $WorkspacePath `
        -RelativeRoots $Definition.ReadonlyPaths `
        -ReadOnly $true

    Set-ReadOnlyStateForRoots `
        -WorkspacePath $WorkspacePath `
        -RelativeRoots $Definition.EditablePaths `
        -ReadOnly $false
}

function Refresh-ReadonlyBaselineInWorkspace {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProjectPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [Parameter(Mandatory = $true)]
        [object]$Definition
    )

    Set-ReadOnlyStateForRoots `
        -WorkspacePath $WorkspacePath `
        -RelativeRoots $Definition.ReadonlyPaths `
        -ReadOnly $false

    foreach ($readonlyRoot in $Definition.ReadonlyPaths) {
        Remove-RelativePathIfExists -WorkspacePath $WorkspacePath -RelativePath $readonlyRoot
        Copy-RelativePath `
            -SourceProjectPath $SourceProjectPath `
            -WorkspacePath $WorkspacePath `
            -RelativePath $readonlyRoot
    }

    Write-WorkspaceMarker `
        -WorkspacePath $WorkspacePath `
        -Definition $Definition `
        -SourceProjectPath $SourceProjectPath

    Write-WorkspaceReadme `
        -WorkspacePath $WorkspacePath `
        -Definition $Definition `
        -SourceProjectPath $SourceProjectPath

    Write-WorkspaceGitIgnore -WorkspacePath $WorkspacePath

    Set-ReadOnlyStateForRoots `
        -WorkspacePath $WorkspacePath `
        -RelativeRoots $Definition.ReadonlyPaths `
        -ReadOnly $true

    Set-ReadOnlyStateForRoots `
        -WorkspacePath $WorkspacePath `
        -RelativeRoots $Definition.EditablePaths `
        -ReadOnly $false
}

function Validate-UnityMetaPairs {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,

        [Parameter(Mandatory = $true)]
        [string[]]$EditableRoots
    )

    $missing = New-Object "System.Collections.Generic.List[string]"

    foreach ($editableRoot in (ConvertTo-UniqueStringArray -Values $EditableRoots)) {
        $absoluteRoot = Get-AbsolutePathUnderRoot -RootPath $RootPath -RelativePath $editableRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot)) {
            continue
        }

        Get-ChildItem -LiteralPath $absoluteRoot -File -Recurse -Force | Where-Object {
            -not $_.Name.EndsWith(".meta", [System.StringComparison]::OrdinalIgnoreCase)
        } | ForEach-Object {
            $metaPath = $_.FullName + ".meta"
            if (-not (Test-Path -LiteralPath $metaPath)) {
                $relative = $_.FullName.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                [void]$missing.Add($relative)
            }
        }

        $directoryList = @()
        $rootItem = Get-Item -LiteralPath $absoluteRoot -Force
        if ($rootItem.PSIsContainer) {
            $directoryList += $rootItem
            $directoryList += Get-ChildItem -LiteralPath $absoluteRoot -Directory -Recurse -Force
        }

        foreach ($directory in $directoryList) {
            $metaPath = $directory.FullName + ".meta"
            if (-not (Test-Path -LiteralPath $metaPath)) {
                $relative = $directory.FullName.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                [void]$missing.Add($relative + " (folder)")
            }
        }
    }

    if ($missing.Count -gt 0) {
        $preview = $missing | Select-Object -First 10
        throw "Editable Unity assets are missing .meta pairs.`n$($preview -join [Environment]::NewLine)"
    }
}

function Get-WorkspaceContentFileMap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath
    )

    $map = @{}
    $scanRoots = @("Assets", "Packages", "ProjectSettings", "Tooling")
    foreach ($scanRoot in $scanRoots) {
        $absoluteRoot = Join-Path $WorkspacePath $scanRoot
        if (-not (Test-Path -LiteralPath $absoluteRoot)) {
            continue
        }

        Get-ChildItem -LiteralPath $absoluteRoot -File -Recurse -Force | ForEach-Object {
            $relative = $_.FullName.Substring($WorkspacePath.TrimEnd("\").Length).TrimStart("\")
            $map[(Normalize-RelativePath -Path $relative)] = $_.FullName
        }
    }

    $rootFiles = @(".mcp.json")
    foreach ($rootFile in $rootFiles) {
        $absoluteFile = Join-Path $WorkspacePath $rootFile
        if (Test-Path -LiteralPath $absoluteFile) {
            $map[(Normalize-RelativePath -Path $rootFile)] = $absoluteFile
        }
    }

    return $map
}

function Invoke-ExternalProcess {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [string]$WorkingDirectory = ""
    )

    function Format-WindowsProcessArgument {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Value
        )

        if ($Value.Length -eq 0) {
            return '""'
        }

        if ($Value -notmatch '[\s"]') {
            return $Value
        }

        $builder = New-Object System.Text.StringBuilder
        [void]$builder.Append('"')
        $backslashCount = 0

        foreach ($character in $Value.ToCharArray()) {
            if ($character -eq '\') {
                $backslashCount++
                continue
            }

            if ($character -eq '"') {
                [void]$builder.Append(('\' * (($backslashCount * 2) + 1)))
                [void]$builder.Append('"')
                $backslashCount = 0
                continue
            }

            if ($backslashCount -gt 0) {
                [void]$builder.Append(('\' * $backslashCount))
                $backslashCount = 0
            }

            [void]$builder.Append($character)
        }

        if ($backslashCount -gt 0) {
            [void]$builder.Append(('\' * ($backslashCount * 2)))
        }

        [void]$builder.Append('"')
        return $builder.ToString()
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true

    if (-not [string]::IsNullOrWhiteSpace($WorkingDirectory)) {
        $psi.WorkingDirectory = $WorkingDirectory
    }

    $psi.Arguments = (($Arguments | ForEach-Object {
        Format-WindowsProcessArgument -Value ([string]$_)
    }) -join " ")

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $psi
    [void]$process.Start()
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    $output = New-Object "System.Collections.Generic.List[string]"
    foreach ($text in @($stdout, $stderr)) {
        if ([string]::IsNullOrWhiteSpace($text)) {
            continue
        }

        foreach ($line in ($text -split "(`r`n|`n|`r)")) {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                [void]$output.Add($line)
            }
        }
    }

    return [PSCustomObject]@{
        ExitCode = $process.ExitCode
        Output   = @($output)
    }
}

function Set-WorkspaceMarkerOverrides {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkspacePath,

        [string]$SourceProjectPath = "",

        [string]$DistributionMode = "",

        [string]$BranchName = ""
    )

    $markerPath = Get-WorkspaceMarkerPath -WorkspacePath $WorkspacePath
    if (-not (Test-Path -LiteralPath $markerPath)) {
        throw "Workspace marker not found: $markerPath"
    }

    $marker = Get-Content -Raw -Encoding UTF8 -LiteralPath $markerPath | ConvertFrom-Json

    function Set-MarkerProperty {
        param(
            [Parameter(Mandatory = $true)]
            [object]$Target,

            [Parameter(Mandatory = $true)]
            [string]$Name,

            $Value
        )

        $existing = $Target.PSObject.Properties[$Name]
        if ($null -ne $existing) {
            $existing.Value = $Value
            return
        }

        $Target | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }

    if ($PSBoundParameters.ContainsKey("SourceProjectPath")) {
        Set-MarkerProperty -Target $marker -Name "sourceProjectPath" -Value $SourceProjectPath
    }

    if (-not [string]::IsNullOrWhiteSpace($DistributionMode)) {
        Set-MarkerProperty -Target $marker -Name "distributionMode" -Value $DistributionMode
    }

    if (-not [string]::IsNullOrWhiteSpace($BranchName)) {
        Set-MarkerProperty -Target $marker -Name "branchName" -Value $BranchName
    }

    Set-MarkerProperty -Target $marker -Name "generatedUtc" -Value ((Get-Date).ToUniversalTime().ToString("o"))
    $marker | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $markerPath -Encoding UTF8
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [switch]$AllowFailure
    )

    $result = Invoke-ExternalProcess -FilePath "git" -Arguments (@("-C", $WorkingDirectory) + $Arguments)
    $output = $result.Output
    $exitCode = $result.ExitCode

    if (($exitCode -ne 0) -and (-not $AllowFailure)) {
        $joinedArguments = $Arguments -join " "
        $joinedOutput = ($output | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
        throw "git $joinedArguments failed in $WorkingDirectory`n$joinedOutput"
    }

    return [PSCustomObject]@{
        ExitCode = $exitCode
        Output   = @($output | ForEach-Object { [string]$_ })
    }
}

function Resolve-GitRemoteUrl {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,

        [Parameter(Mandatory = $true)]
        [string]$RemoteName
    )

    if (-not (Test-Path -LiteralPath (Join-Path $RepositoryPath ".git"))) {
        return ""
    }

    $result = Invoke-Git -WorkingDirectory $RepositoryPath -Arguments @("remote", "get-url", $RemoteName) -AllowFailure
    if ($result.ExitCode -ne 0) {
        return ""
    }

    return (($result.Output | Select-Object -First 1) -as [string]).Trim()
}

function Ensure-GitRemoteConfigured {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,

        [Parameter(Mandatory = $true)]
        [string]$RemoteName,

        [Parameter(Mandatory = $true)]
        [string]$RemoteUrl
    )

    $existingUrl = Resolve-GitRemoteUrl -RepositoryPath $RepositoryPath -RemoteName $RemoteName
    if ([string]::IsNullOrWhiteSpace($existingUrl)) {
        Invoke-Git -WorkingDirectory $RepositoryPath -Arguments @("remote", "add", $RemoteName, $RemoteUrl) | Out-Null
        return
    }

    if (-not $existingUrl.Equals($RemoteUrl, [System.StringComparison]::OrdinalIgnoreCase)) {
        Invoke-Git -WorkingDirectory $RepositoryPath -Arguments @("remote", "set-url", $RemoteName, $RemoteUrl) | Out-Null
    }
}

function Ensure-GitMirrorClone {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,

        [string]$RemoteUrl = ""
    )

    if (Test-Path -LiteralPath (Join-Path $RepositoryPath ".git")) {
        if (-not [string]::IsNullOrWhiteSpace($RemoteUrl)) {
            Ensure-GitRemoteConfigured -RepositoryPath $RepositoryPath -RemoteName "origin" -RemoteUrl $RemoteUrl
        }

        return
    }

    if (Test-Path -LiteralPath $RepositoryPath) {
        $existingItems = @(Get-ChildItem -LiteralPath $RepositoryPath -Force)
        if ($existingItems.Count -gt 0) {
            throw "Mirror repository path exists but is not a git clone: $RepositoryPath"
        }
    }
    else {
        Ensure-Directory -Path (Split-Path -Parent $RepositoryPath)
    }

    if ([string]::IsNullOrWhiteSpace($RemoteUrl)) {
        throw "Mirror repository path is not initialized, and no remote URL was provided: $RepositoryPath"
    }

    $cloneParent = Split-Path -Parent $RepositoryPath
    Ensure-Directory -Path $cloneParent
    $cloneResult = Invoke-ExternalProcess `
        -FilePath "git" `
        -Arguments @("clone", $RemoteUrl, $RepositoryPath) `
        -WorkingDirectory $cloneParent
    $cloneOutput = $cloneResult.Output
    $cloneExitCode = $cloneResult.ExitCode

    if ($cloneExitCode -ne 0) {
        $joinedOutput = ($cloneOutput | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
        throw "git clone failed for $RemoteUrl`n$joinedOutput"
    }
}

function Clear-ReadOnlyAttributes {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Get-ChildItem -LiteralPath $Path -Force -Recurse -File | ForEach-Object {
        $_.IsReadOnly = $false
    }
}

function Clear-DirectoryContentsExceptGit {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DirectoryPath
    )

    if (-not (Test-Path -LiteralPath $DirectoryPath)) {
        return
    }

    Clear-ReadOnlyAttributes -Path $DirectoryPath

    Get-ChildItem -LiteralPath $DirectoryPath -Force | Where-Object {
        $_.Name -ne ".git"
    } | ForEach-Object {
        Assert-PathWithinRoot -RootPath $DirectoryPath -CandidatePath $_.FullName -Description "Mirror worktree delete target"
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
    }
}

function Copy-DirectoryContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,

        [Parameter(Mandatory = $true)]
        [string]$DestinationDirectory
    )

    Ensure-Directory -Path $DestinationDirectory
    Get-ChildItem -LiteralPath $SourceDirectory -Force | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $DestinationDirectory -Recurse -Force
    }
}
