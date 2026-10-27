param(
    [string]$RepoRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"
Set-Location -LiteralPath $RepoRoot

$required = @(
    "AGENTS.md",
    ".mcp.json",
    "docs/current-project-baseline.md",
    "docs/framework-pico-mainline-notes.md",
    "docs/texcore-module-score-data.xlsx"
)

$required | ForEach-Object {
    [pscustomobject]@{
        Path = $_
        Exists = Test-Path -LiteralPath $_
    }
} | ConvertTo-Json -Depth 4
