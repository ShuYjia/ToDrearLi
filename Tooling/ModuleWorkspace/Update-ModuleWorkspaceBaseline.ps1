[CmdletBinding()]
param(
    [string]$WorkspacePath = "",

    [string]$SourceProjectPath = ""
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

Refresh-ReadonlyBaselineInWorkspace `
    -SourceProjectPath $resolvedSourceProjectPath `
    -WorkspacePath $resolvedWorkspacePath `
    -Definition $definition

Write-Host ""
Write-Host "Readonly baseline refreshed."
Write-Host "Source    : $resolvedSourceProjectPath"
Write-Host "Workspace : $resolvedWorkspacePath"
Write-Host "Module    : $($definition.Name)"
