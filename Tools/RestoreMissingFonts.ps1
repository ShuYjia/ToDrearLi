[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ReferenceAssets = 'D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4\Assets',
    [string]$FrameworkAssets = 'D:\gitea_obj\_unity projection\BPFFrameWork\Assets'
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectAssets = Join-Path $projectRoot 'Assets'

function Copy-FontAssetPair {
    param(
        [Parameter(Mandatory)] [string]$SourceRoot,
        [Parameter(Mandatory)] [string]$RelativePath
    )

    $source = Join-Path $SourceRoot $RelativePath
    $sourceMeta = "$source.meta"
    $destination = Join-Path $projectAssets $RelativePath
    $destinationMeta = "$destination.meta"

    if (!(Test-Path -LiteralPath $source) -or !(Test-Path -LiteralPath $sourceMeta)) {
        throw "Reference font asset pair is incomplete: $source"
    }

    $destinationDirectory = Split-Path -Parent $destination
    if (!(Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($destination, "Restore font asset from $source")) {
        Copy-Item -LiteralPath $source -Destination $destination -Force
        Copy-Item -LiteralPath $sourceMeta -Destination $destinationMeta -Force
    }
}

function Copy-WithPreservedMetaGuid {
    param(
        [Parameter(Mandatory)] [string]$Source,
        [Parameter(Mandatory)] [string]$Destination,
        [Parameter(Mandatory)] [string]$Guid,
        [string]$MaterialFileId
    )

    $destinationDirectory = Split-Path -Parent $Destination
    if (!(Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($Destination, "Create compatibility font asset with GUID $Guid")) {
        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        if ($MaterialFileId) {
            $assetContent = Get-Content -LiteralPath $Destination -Raw
            $assetContent = [regex]::Replace($assetContent, '(?m)^--- !u!21 &-?\d+$', "--- !u!21 &$MaterialFileId", 1)
            $assetContent = [regex]::Replace($assetContent, '(?m)^  material: \{fileID: -?\d+\}$', "  material: {fileID: $MaterialFileId}", 1)
            Set-Content -LiteralPath $Destination -Value $assetContent -NoNewline -Encoding utf8
        }

        $sourceMeta = "$Source.meta"
        if (!(Test-Path -LiteralPath $sourceMeta)) {
            throw "Compatibility source meta is missing: $sourceMeta"
        }

        $metaContent = Get-Content -LiteralPath $sourceMeta -Raw
        $metaContent = [regex]::Replace($metaContent, '(?m)^guid: [0-9a-f]{32}$', "guid: $Guid")
        Set-Content -LiteralPath "$Destination.meta" -Value $metaContent -Encoding utf8
    }
}

if (!(Test-Path -LiteralPath $ReferenceAssets)) {
    throw "Reference project Assets directory was not found: $ReferenceAssets"
}

$referencePairs = @()
foreach ($folder in @(
        'GameMain\Res\Fonts',
        'StandarAssets\Fonts',
        'TextMesh Pro\Fonts'
    )) {
    $referencePairs += Get-ChildItem -LiteralPath (Join-Path $ReferenceAssets $folder) -File |
        Where-Object { $_.Extension -ne '.meta' } |
        ForEach-Object { $_.FullName.Substring($ReferenceAssets.Length + 1) }
}

$referencePairs += Get-ChildItem -LiteralPath (Join-Path $ReferenceAssets 'TextMesh Pro\Resources\Fonts & Materials') -File |
    Where-Object { $_.Name -in @('LiberationSans SDF 1.asset', 'SIMHEI SDF.asset', 'SIMHEI.TTF') } |
    ForEach-Object { $_.FullName.Substring($ReferenceAssets.Length + 1) }

foreach ($relativePath in $referencePairs) {
    Copy-FontAssetPair -SourceRoot $ReferenceAssets -RelativePath $relativePath
}

if (!(Test-Path -LiteralPath $FrameworkAssets)) {
    throw "Framework project Assets directory was not found: $FrameworkAssets"
}

Copy-FontAssetPair -SourceRoot $FrameworkAssets -RelativePath 'ArtResources\Font\SIMHEI SDF.asset'
Copy-FontAssetPair -SourceRoot $FrameworkAssets -RelativePath 'ArtResources\Font\SIMHEI.TTF'

$sourceHanBold = Join-Path $projectAssets 'GameMain\Res\Fonts\SourceHanSansCN-Bold.otf'
$sourceHanMediumSdf = Join-Path $projectAssets 'TextMesh Pro\Fonts\SourceHanSansCN-Medium SDF.asset'

Copy-WithPreservedMetaGuid -Source $sourceHanBold -Destination (Join-Path $projectAssets 'A_MRNet\Font\Compatibility\LegacyChineseUIFont.otf') -Guid '622c46022f4f3f34ab0b88ecb985801a'
Copy-WithPreservedMetaGuid -Source $sourceHanMediumSdf -Destination (Join-Path $projectAssets 'A_MRNet\Font\Compatibility\LegacyRewardText SDF.asset') -Guid 'f1cbf047c8d9dd64d91869d22385cb1d' -MaterialFileId '-2439833918384560529'
Copy-WithPreservedMetaGuid -Source $sourceHanBold -Destination (Join-Path $projectAssets 'A_MRNet\Font\Compatibility\LegacyChineseBoldPrimary.otf') -Guid 'c71e87d89e10686408376975c6398270'
Copy-WithPreservedMetaGuid -Source $sourceHanBold -Destination (Join-Path $projectAssets 'A_MRNet\Font\Compatibility\LegacyChineseBoldFallback.otf') -Guid '7bc2bb273a5250142ad119eeeea0a818'

$selawikCompatibility = @{
    'e65983ad3f7b44aa81e726d5ffb472d7' = 'Selawik-Light-MRTKIcons.ttf'
    'a99e8b1ed1154eb58270cc6a18605657' = 'Selawik-Bold-MRTKIcons.ttf'
    'e48b920555144c6da3ee2ab03f0fda88' = 'Selawik-Regular-MRTKIcons.ttf'
    '0fd94703e1ea496bb9999216ac3ece0d' = 'Selawik-Semilight-MRTKIcons.ttf'
    'e869342c56e54acf989b2422b4b80dcc' = 'Selawik-Semibold-MRTKIcons.ttf'
}

foreach ($guid in $selawikCompatibility.Keys) {
    $source = Join-Path $projectRoot "Packages\org.mixedrealitytoolkit.standardassets\Fonts\$($selawikCompatibility[$guid])"
    $destination = Join-Path $projectAssets "A_MRNet\Font\Compatibility\$guid.ttf"
    Copy-WithPreservedMetaGuid -Source $source -Destination $destination -Guid $guid
}
