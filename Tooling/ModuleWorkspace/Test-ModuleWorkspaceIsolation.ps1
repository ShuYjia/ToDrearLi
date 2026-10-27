[CmdletBinding()]
param(
    [string]$SourceProjectPath = "",

    [string]$ModuleName = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "ModuleWorkspace.Common.ps1")

if ([string]::IsNullOrWhiteSpace($SourceProjectPath)) {
    $SourceProjectPath = Join-Path $PSScriptRoot "..\.."
}

$resolvedSourceProjectPath = Resolve-AbsolutePath -Path $SourceProjectPath
$manifest = Load-ModuleWorkspaceManifest -SourceProjectPath $resolvedSourceProjectPath

$definitions = if ([string]::IsNullOrWhiteSpace($ModuleName)) {
    @($manifest.modules)
}
else {
    @((Get-ModuleWorkspaceDefinition -Manifest $manifest -ModuleName $ModuleName))
}

function Get-MetaGuidMap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath
    )

    $map = @{}
    Get-ChildItem -LiteralPath $RootPath -Recurse -Filter *.meta -File | ForEach-Object {
        $match = Select-String -Path $_.FullName -Pattern '^guid: (.+)$'
        if ($match) {
            $guid = $match.Matches[0].Groups[1].Value
            if (-not $map.ContainsKey($guid)) {
                $relative = $_.FullName.Substring($RootPath.TrimEnd("\").Length).TrimStart("\")
                $map[$guid] = Normalize-RelativePath -Path $relative
            }
        }
    }

    return $map
}

$assetGuidMap = Get-MetaGuidMap -RootPath (Join-Path $resolvedSourceProjectPath "Assets")

$results = foreach ($definition in $definitions) {
    $moduleRoot = Get-AbsolutePathUnderRoot -RootPath $resolvedSourceProjectPath -RelativePath ("Assets/App/Modules/" + $definition.Name)
    $moduleGuidMap = Get-MetaGuidMap -RootPath $moduleRoot
    $moduleGuidSet = New-Object "System.Collections.Generic.HashSet[string]" ([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($guid in $moduleGuidMap.Keys) {
        [void]$moduleGuidSet.Add($guid)
    }

    $references = @(
        rg -n --glob "*.unity" --glob "*.prefab" --glob "*.asset" 'guid: [0-9a-f]{32}' $moduleRoot |
        ForEach-Object {
            if ($_ -match 'guid: ([0-9a-f]{32})') {
                $matches[1]
            }
        } |
        Sort-Object -Unique
    )

    $crossModuleHits = New-Object "System.Collections.Generic.List[object]"
    foreach ($guid in $references) {
        if ($moduleGuidSet.Contains($guid)) {
            continue
        }

        if (-not $assetGuidMap.ContainsKey($guid)) {
            continue
        }

        $relativePath = $assetGuidMap[$guid]
        if ($relativePath -match '^App\\Modules\\([^\\]+)\\') {
            $ownerModule = $matches[1]
            if ($ownerModule -ne $definition.Name) {
                [void]$crossModuleHits.Add([PSCustomObject]@{
                    moduleName   = $definition.Name
                    ownerModule  = $ownerModule
                    relativePath = ("Assets\" + $relativePath)
                    guid         = $guid
                })
            }
        }
    }

    [PSCustomObject]@{
        moduleName       = $definition.Name
        status           = $definition.Status
        crossModuleCount = $crossModuleHits.Count
        crossModuleHits  = @($crossModuleHits.ToArray())
    }
}

$results | ConvertTo-Json -Depth 6
