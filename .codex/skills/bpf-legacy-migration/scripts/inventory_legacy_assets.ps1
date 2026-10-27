param(
    [string]$Root = "Assets/APP/OldMRTK"
)

if (-not (Test-Path -LiteralPath $Root)) {
    throw "Legacy root not found: $Root"
}

Get-ChildItem -LiteralPath $Root -Recurse -File |
    Group-Object Extension |
    Sort-Object Name |
    ForEach-Object {
        [pscustomobject]@{
            Extension = $_.Name
            Count = $_.Count
        }
    } | ConvertTo-Json
