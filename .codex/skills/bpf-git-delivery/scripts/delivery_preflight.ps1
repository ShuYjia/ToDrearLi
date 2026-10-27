param(
    [string]$RepoRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $RepoRoot

[pscustomobject]@{
    Branch = (git branch --show-current)
    Head = (git rev-parse --short HEAD)
    Status = (git status --short) -join [Environment]::NewLine
    Staged = (git diff --cached --name-status) -join [Environment]::NewLine
    LfsStatus = if (Get-Command git-lfs -ErrorAction SilentlyContinue) { (git lfs status) -join [Environment]::NewLine } else { "git-lfs not on PATH" }
} | ConvertTo-Json -Depth 4
