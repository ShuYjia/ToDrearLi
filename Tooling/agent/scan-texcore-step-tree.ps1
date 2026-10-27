param(
    [Parameter(Mandatory=$true)]
    [string]$ScenePath
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $ScenePath)) {
    throw "Scene not found: $ScenePath"
}

Select-String -LiteralPath $ScenePath -Pattern 'TexCore_StepObj|interacterObjs|NodeObjs|TopicObjs|StepObjs|stepIndex' |
    ForEach-Object {
        [pscustomobject]@{
            Line = $_.LineNumber
            Text = $_.Line.Trim()
        }
    } | ConvertTo-Json -Depth 4
