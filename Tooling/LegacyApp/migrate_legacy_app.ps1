param(
    [string]$SourceRoot = "D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4",
    [string]$TargetRoot = "D:\gitea_obj\_unity projection\gitea_BPF"
)

$ErrorActionPreference = "Stop"

function Copy-LegacyPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $sourcePath = Join-Path $SourceRoot $RelativePath
    $targetPath = Join-Path $TargetRoot $RelativePath

    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing source path: $sourcePath"
    }

    $sourceItem = Get-Item -LiteralPath $sourcePath
    if ($sourceItem.PSIsContainer) {
        New-Item -ItemType Directory -Force -Path $targetPath | Out-Null
        Get-ChildItem -LiteralPath $sourcePath -Force | ForEach-Object {
            Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Recurse -Force
        }
        $metaPath = "$sourcePath.meta"
        if (Test-Path -LiteralPath $metaPath) {
            Copy-Item -LiteralPath $metaPath -Destination "$targetPath.meta" -Force
        }
        return
    }

    $targetDir = Split-Path -Parent $targetPath
    if (-not [string]::IsNullOrEmpty($targetDir)) {
        New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
    }

    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Force

    $fileMetaPath = "$sourcePath.meta"
    if (Test-Path -LiteralPath $fileMetaPath) {
        Copy-Item -LiteralPath $fileMetaPath -Destination "$targetPath.meta" -Force
    }
}

$copyPaths = @(
    "Assets\_syc_resource\_my_scenes\00Init\Init.unity",
    "Assets\_syc_resource\_my_scenes\02Lobby\Lobby.unity",
    "Assets\_syc_resource\_my_scenes\T04UDPSprate\UDPLobby.unity",
    "Assets\_syc_resource\_my_scenes\sync\MRTKsync.unity",
    "Assets\_syc_resource\_my_scenes\sync\MRTKsyncsuccess.unity",
    "Assets\_syc_resource\_my_perfabs",
    "Assets\_syc_resource\_my_resources",
    "Assets\GameFramework",
    "Assets\StreamingAssets\UserLogin.json",
    "Assets\StreamingAssets\GameFrameworkVersion.dat",
    "Assets\StreamingAssets\Scenes",
    "Assets\GameMain\Animations",
    "Assets\GameMain\Configs",
    "Assets\GameMain\DataTables",
    "Assets\GameMain\Entity",
    "Assets\GameMain\Localization",
    "Assets\GameMain\Models",
    "Assets\GameMain\Prefabs",
    "Assets\GameMain\Res",
    "Assets\GameMain\Scenes",
    "Assets\GameMain\Sounds",
    "Assets\GameMain\tietu",
    "Assets\GameMain\UI",
    "Assets\GameMain\Scripts",
    "Assets\UI",
    "Assets\NewUI",
    "Assets\Materials",
    "Assets\_Materials",
    "Assets\NetSync_Prefabs_2",
    "Assets\StandarAssets\Fonts",
    "Assets\VolumetricLines\BuiltInRenderPipeline\Materials",
    "Assets\VolumetricLines\BuiltInRenderPipeline\Prefabs",
    "Assets\TextMesh Pro\Fonts",
    "Assets\TextMesh Pro\Resources\Fonts & Materials"
)

foreach ($relativePath in $copyPaths) {
    Copy-LegacyPath -RelativePath $relativePath
}

Write-Host "Legacy app assets copied into target project."
