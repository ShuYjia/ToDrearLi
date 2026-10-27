[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleName,

    [string]$SourceProjectPath = "",

    [string]$WorkspaceRoot = "",

    [string]$WorkspacePath = "",

    [switch]$ForceRecreate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "ModuleWorkspace.Common.ps1")

if ([string]::IsNullOrWhiteSpace($SourceProjectPath)) {
    $SourceProjectPath = Join-Path $PSScriptRoot "..\.."
}

$resolvedSourceProjectPath = Resolve-AbsolutePath -Path $SourceProjectPath
$manifest = Load-ModuleWorkspaceManifest -SourceProjectPath $resolvedSourceProjectPath
$definition = Get-ModuleWorkspaceDefinition -Manifest $manifest -ModuleName $ModuleName

if ([string]::IsNullOrWhiteSpace($WorkspacePath)) {
    if ([string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
        $WorkspaceRoot = Get-DefaultModuleWorkspaceRoot `
            -SourceProjectPath $resolvedSourceProjectPath `
            -WorkspaceRootSuffix $definition.WorkspaceRootSuffix
    }

    $resolvedWorkspaceRoot = Resolve-AbsolutePath -Path $WorkspaceRoot
    Ensure-Directory -Path $resolvedWorkspaceRoot
    $WorkspacePath = Join-Path $resolvedWorkspaceRoot $definition.Name
}

$resolvedWorkspacePath = Resolve-AbsolutePath -Path $WorkspacePath
if ($resolvedWorkspacePath.StartsWith($resolvedSourceProjectPath.TrimEnd("\") + "\", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Workspace path must not live inside the main repo.`nSource: $resolvedSourceProjectPath`nWorkspace: $resolvedWorkspacePath"
}

if (Test-Path -LiteralPath $resolvedWorkspacePath) {
    if (-not $ForceRecreate) {
        throw "Workspace already exists: $resolvedWorkspacePath`nRe-run with -ForceRecreate to rebuild it."
    }

    Assert-PathWithinRoot -RootPath (Split-Path -Parent $resolvedWorkspacePath) -CandidatePath $resolvedWorkspacePath -Description "Workspace delete target"
    Remove-Item -LiteralPath $resolvedWorkspacePath -Recurse -Force
}

Ensure-Directory -Path $resolvedWorkspacePath
Initialize-ModuleWorkspaceContent `
    -SourceProjectPath $resolvedSourceProjectPath `
    -WorkspacePath $resolvedWorkspacePath `
    -Definition $definition

Write-Host ""
Write-Host "Module workspace is ready."
Write-Host "Source    : $resolvedSourceProjectPath"
Write-Host "Workspace : $resolvedWorkspacePath"
Write-Host "Module    : $($definition.Name)"
Write-Host "Status    : $($definition.Status)"
Write-Host "Entry     : $($definition.EntryScenePath)"
