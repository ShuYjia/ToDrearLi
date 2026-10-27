[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$assetsRoot = Join-Path $projectRoot 'Assets'
$sourceHanGuid = '7b3b87118eb2cc84e9fa80eec1506ae9'
$simheiGuid = '359d66bab08c9c444bda2845274e49b9'
$simheiMaterialId = '-1618942232215921013'
$arialReference = 'm_Font: {fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}'
$sourceHanReference = "m_Font: {fileID: 12800000, guid: $sourceHanGuid, type: 3}"
$iconFontGuids = @(
    '533bdd8d5c92b52448ee2ecf7bd828a4',
    '9b0d0ee11ff70b04d901a29b519cbaa0',
    'fea6b26abeefbc547b6aca1dbda0a34d'
)
$hanEscapePattern = '\\u(?:4[eEfF][0-9A-Fa-f]{2}|[5-9][0-9A-Fa-f]{3})'

$assetFiles = Get-ChildItem -LiteralPath $assetsRoot -Recurse -File |
    Where-Object { $_.Extension -in @('.unity', '.prefab') }

foreach ($assetFile in $assetFiles) {
    $content = Get-Content -LiteralPath $assetFile.FullName -Raw
    $updated = $content.Replace($arialReference, $sourceHanReference)

    $blocks = [regex]::Split($updated, '(?m)(?=^--- )')
    $rewrittenBlocks = foreach ($block in $blocks) {
        if ($block -notmatch 'm_Script: \{fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3\}') {
            $block
            continue
        }

        $fontMatch = [regex]::Match($block, '(?m)^  m_fontAsset: \{fileID: 11400000, guid: ([0-9a-f]{32}), type: 2\}\r?$')
        $textMatch = [regex]::Match($block, '(?m)^  m_text: (.*)$')
        if (!$fontMatch.Success -or !$textMatch.Success -or $fontMatch.Groups[1].Value -notin $iconFontGuids -or $textMatch.Groups[1].Value -notmatch $hanEscapePattern) {
            $block
            continue
        }

        $block = [regex]::Replace(
            $block,
            '(?m)^  m_fontAsset: \{fileID: 11400000, guid: [0-9a-f]{32}, type: 2\}\r?$',
            "  m_fontAsset: {fileID: 11400000, guid: $simheiGuid, type: 2}",
            1
        )
        [regex]::Replace(
            $block,
            '(?m)^  m_sharedMaterial: \{fileID: -?\d+, guid: [0-9a-f]{32}, type: 2\}\r?$',
            "  m_sharedMaterial: {fileID: $simheiMaterialId, guid: $simheiGuid, type: 2}",
            1
        )
    }
    $updated = $rewrittenBlocks -join ''

    if ($updated -ne $content -and $PSCmdlet.ShouldProcess($assetFile.FullName, 'Repair legacy UI font references')) {
        Set-Content -LiteralPath $assetFile.FullName -Value $updated -NoNewline -Encoding utf8
    }
}
