param(
    [Parameter(Mandatory=$true)]
    [string]$ScenePath
)

if (-not (Test-Path -LiteralPath $ScenePath)) {
    throw "Scene not found: $ScenePath"
}

Select-String -LiteralPath $ScenePath -Pattern 'TexCore_StepObj|stepIndex|interacterObjs|NodeObjs|TopicObjs|StepObjs' |
    ForEach-Object {
        [pscustomobject]@{
            Line = $_.LineNumber
            Text = $_.Line.Trim()
        }
    } | ConvertTo-Json -Depth 4
