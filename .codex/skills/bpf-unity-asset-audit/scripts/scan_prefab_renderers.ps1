param(
    [Parameter(Mandatory=$true)]
    [string]$PrefabRoot
)

if (-not (Test-Path -LiteralPath $PrefabRoot)) {
    throw "Path not found: $PrefabRoot"
}

Get-ChildItem -LiteralPath $PrefabRoot -Filter *.prefab -Recurse |
    ForEach-Object {
        [pscustomobject]@{
            Path = $_.FullName
            RendererMentions = (Select-String -LiteralPath $_.FullName -Pattern 'Renderer:' -SimpleMatch).Count
            MaterialListMentions = (Select-String -LiteralPath $_.FullName -Pattern 'm_Materials:' -SimpleMatch).Count
            NullMaterialRefs = (Select-String -LiteralPath $_.FullName -Pattern '{fileID: 0}' -SimpleMatch).Count
        }
    } | ConvertTo-Json -Depth 4
