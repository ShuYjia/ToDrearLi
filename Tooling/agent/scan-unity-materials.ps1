param(
    [Parameter(Mandatory=$true)]
    [string]$Root
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path -LiteralPath $Root)) {
    throw "Path not found: $Root"
}

Get-ChildItem -LiteralPath $Root -Include *.prefab,*.unity -Recurse -File |
    ForEach-Object {
        [pscustomobject]@{
            Path = $_.FullName
            MaterialSections = (Select-String -LiteralPath $_.FullName -Pattern 'm_Materials:' -SimpleMatch).Count
            NullFileIds = (Select-String -LiteralPath $_.FullName -Pattern '{fileID: 0}' -SimpleMatch).Count
            ShaderRefs = (Select-String -LiteralPath $_.FullName -Pattern 'm_Shader:' -SimpleMatch).Count
        }
    } | ConvertTo-Json -Depth 4
