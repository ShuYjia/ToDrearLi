param(
    [string]$RepoRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $RepoRoot

[pscustomobject]@{
    RepoRoot = (Get-Location).Path
    Branch = (git branch --show-current)
    Head = (git rev-parse --short HEAD)
    Status = (git status --short) -join [Environment]::NewLine
    MainAheadBehind = if (git show-ref --verify --quiet refs/remotes/origin/main) { (git rev-list --left-right --count main...origin/main) } else { "origin/main missing" }
} | ConvertTo-Json -Depth 4
