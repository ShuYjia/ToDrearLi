[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ModuleName,

    [string]$SourceProjectPath = "",

    [string]$MirrorRepositoryPath = "",

    [string]$MirrorRemoteUrl = "",

    [switch]$Push,

    [switch]$AllowEmptyCommit
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
$branchMirror = Get-ModuleBranchMirrorSettings -Manifest $manifest
$branchName = Get-ModuleBranchName -BranchMirrorSettings $branchMirror -ModuleName $definition.Name

if ([string]::IsNullOrWhiteSpace($MirrorRepositoryPath)) {
    $mirrorRoot = Get-DefaultModuleBranchMirrorRoot `
        -SourceProjectPath $resolvedSourceProjectPath `
        -ScratchRootSuffix $branchMirror.WorkspaceRootSuffix
    Ensure-Directory -Path $mirrorRoot
    $MirrorRepositoryPath = Join-Path $mirrorRoot "module-branches"
}

$resolvedMirrorRepositoryPath = Resolve-AbsolutePath -Path $MirrorRepositoryPath
if ($resolvedMirrorRepositoryPath.Equals($resolvedSourceProjectPath, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Mirror repository path must not be the main repo path."
}

if ([string]::IsNullOrWhiteSpace($MirrorRemoteUrl)) {
    $MirrorRemoteUrl = Resolve-GitRemoteUrl -RepositoryPath $resolvedMirrorRepositoryPath -RemoteName "origin"
}

Ensure-GitMirrorClone -RepositoryPath $resolvedMirrorRepositoryPath -RemoteUrl $MirrorRemoteUrl

if (-not [string]::IsNullOrWhiteSpace($MirrorRemoteUrl)) {
    Ensure-GitRemoteConfigured -RepositoryPath $resolvedMirrorRepositoryPath -RemoteName "origin" -RemoteUrl $MirrorRemoteUrl
    Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("fetch", "origin", "--prune") | Out-Null
}

$remoteBranchRef = "origin/$branchName"
$remoteBranchCheck = Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("show-ref", "--verify", "--quiet", "refs/remotes/$remoteBranchRef") -AllowFailure

if ($remoteBranchCheck.ExitCode -eq 0) {
    Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("checkout", "-B", $branchName, $remoteBranchRef) | Out-Null
}
else {
    $localBranchCheck = Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("show-ref", "--verify", "--quiet", "refs/heads/$branchName") -AllowFailure
    if ($localBranchCheck.ExitCode -eq 0) {
        Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("checkout", $branchName) | Out-Null
    }
    else {
        $baseBranch = ""
        foreach ($candidate in $branchMirror.LegacyDefaultBranches) {
            if ([string]::IsNullOrWhiteSpace($candidate)) {
                continue
            }

            $candidateCheck = Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("show-ref", "--verify", "--quiet", "refs/remotes/origin/$candidate") -AllowFailure
            if ($candidateCheck.ExitCode -eq 0) {
                $baseBranch = "origin/$candidate"
                break
            }

            $candidateLocalCheck = Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("show-ref", "--verify", "--quiet", "refs/heads/$candidate") -AllowFailure
            if ($candidateLocalCheck.ExitCode -eq 0) {
                $baseBranch = $candidate
                break
            }
        }

        if ([string]::IsNullOrWhiteSpace($baseBranch)) {
            Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("checkout", "--orphan", $branchName) | Out-Null
        }
        else {
            Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("checkout", "-B", $branchName, $baseBranch) | Out-Null
        }
    }
}

Initialize-ModuleWorkspaceContent `
    -SourceProjectPath $resolvedSourceProjectPath `
    -WorkspacePath $resolvedMirrorRepositoryPath `
    -Definition $definition `
    -PreserveGitDirectory

Set-WorkspaceMarkerOverrides `
    -WorkspacePath $resolvedMirrorRepositoryPath `
    -SourceProjectPath $resolvedSourceProjectPath `
    -DistributionMode "module-branch-mirror" `
    -BranchName $branchName

Validate-UnityMetaPairs -RootPath $resolvedMirrorRepositoryPath -EditableRoots $definition.EditablePaths

Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("rm", "-r", "--cached", "--ignore-unmatch", ".") -AllowFailure | Out-Null

$gitAddPaths = New-Object "System.Collections.Generic.List[string]"
foreach ($relativePath in (ConvertTo-UniqueStringArray -Values (@($definition.ReadonlyPaths) + @($definition.EditablePaths) + @($manifest.generatedFiles) + @(".mcp.json")))) {
    [void]$gitAddPaths.Add((ConvertTo-GitPathSpec -Path $relativePath))
}

Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments (@("add", "--force", "--") + @($gitAddPaths.ToArray())) | Out-Null
$statusOutput = Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("status", "--short")

$hasChanges = $false
foreach ($line in $statusOutput.Output) {
    if (-not [string]::IsNullOrWhiteSpace($line)) {
        $hasChanges = $true
        break
    }
}

if ($hasChanges) {
    $commitMessage = "chore(module-branch): publish $($definition.Name) baseline"
    $commitBody = @"
Why:
- refresh the isolated module branch mirror from the BPF main repo
- keep the module branch aligned with the current allow-list baseline

What:
- republish readonly baseline and editable module content for $($definition.Name)
- preserve module workspace metadata for round-trip sync back to the main repo
"@

    Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("commit", "-m", $commitMessage, "-m", $commitBody) | Out-Null
}
elseif ($AllowEmptyCommit) {
    $commitMessage = "chore(module-branch): republish $($definition.Name) baseline"
    Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("commit", "--allow-empty", "-m", $commitMessage) | Out-Null
}

if ($Push) {
    if ([string]::IsNullOrWhiteSpace($MirrorRemoteUrl)) {
        throw "Push was requested, but no mirror remote URL is configured."
    }

    Invoke-Git -WorkingDirectory $resolvedMirrorRepositoryPath -Arguments @("push", "-u", "origin", $branchName) | Out-Null
}

Write-Host ""
Write-Host "Module branch mirror is ready."
Write-Host "Source         : $resolvedSourceProjectPath"
Write-Host "Mirror repo    : $resolvedMirrorRepositoryPath"
Write-Host "Mirror remote  : $MirrorRemoteUrl"
Write-Host "Module         : $($definition.Name)"
Write-Host "Branch         : $branchName"
Write-Host "Status         : $($definition.Status)"
Write-Host "Committed      : $hasChanges"
Write-Host "Pushed         : $Push.IsPresent"
