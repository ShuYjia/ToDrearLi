param(
    [string]$ProjectRoot = "D:\gitea_obj\_unity projection\gitea_BPF"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
$moveCount = 0
$skipCount = 0
$removedDirectoryCount = 0
$removedMetaCount = 0

function Resolve-UnderProjectRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    if (-not $fullPath.StartsWith($ProjectRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path is outside project root: $fullPath"
    }

    return $fullPath
}

function Ensure-Directory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $fullPath = Resolve-UnderProjectRoot -Path $Path
    if (-not (Test-Path -LiteralPath $fullPath)) {
        New-Item -ItemType Directory -Force -Path $fullPath | Out-Null
    }
}

function Move-PathWithMeta {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationPath
    )

    if (-not (Test-Path -LiteralPath $SourcePath)) {
        return
    }

    $sourceFullPath = Resolve-UnderProjectRoot -Path $SourcePath
    $destinationFullPath = Resolve-UnderProjectRoot -Path $DestinationPath

    $destinationParent = Split-Path -Parent $destinationFullPath
    if (-not [string]::IsNullOrWhiteSpace($destinationParent)) {
        Ensure-Directory -Path $destinationParent
    }

    if (Test-Path -LiteralPath $destinationFullPath) {
        Write-Host "Skip existing: $destinationFullPath"
        $script:skipCount++
        return
    }

    Move-Item -LiteralPath $sourceFullPath -Destination $destinationFullPath
    $script:moveCount++

    $sourceMetaPath = "$sourceFullPath.meta"
    if (Test-Path -LiteralPath $sourceMetaPath) {
        Move-Item -LiteralPath $sourceMetaPath -Destination "$destinationFullPath.meta"
        $script:moveCount++
    }
}

function Move-TopLevelFilesByPattern {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,
        [Parameter(Mandatory = $true)]
        [string]$Pattern,
        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory
    )

    if (-not (Test-Path -LiteralPath $SourceDirectory)) {
        return
    }

    $sourceFullPath = Resolve-UnderProjectRoot -Path $SourceDirectory
    $targetFullPath = Resolve-UnderProjectRoot -Path $TargetDirectory
    Ensure-Directory -Path $targetFullPath

    Get-ChildItem -LiteralPath $sourceFullPath -Force |
        Where-Object { -not $_.PSIsContainer -and $_.Name -notlike "*.meta" -and $_.BaseName -match $Pattern } |
        ForEach-Object {
            Move-PathWithMeta -SourcePath $_.FullName -DestinationPath (Join-Path $targetFullPath $_.Name)
        }
}

function Move-RemainingTopLevelFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceDirectory,
        [Parameter(Mandatory = $true)]
        [string]$TargetDirectory
    )

    if (-not (Test-Path -LiteralPath $SourceDirectory)) {
        return
    }

    $sourceFullPath = Resolve-UnderProjectRoot -Path $SourceDirectory
    $targetFullPath = Resolve-UnderProjectRoot -Path $TargetDirectory
    Ensure-Directory -Path $targetFullPath

    Get-ChildItem -LiteralPath $sourceFullPath -Force |
        Where-Object { -not $_.PSIsContainer -and $_.Name -notlike "*.meta" } |
        ForEach-Object {
            Move-PathWithMeta -SourcePath $_.FullName -DestinationPath (Join-Path $targetFullPath $_.Name)
        }
}

function Remove-MetaOnlyDirectories {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RootDirectories
    )

    foreach ($rootDirectory in $RootDirectories) {
        if (-not (Test-Path -LiteralPath $rootDirectory)) {
            continue
        }

        $rootFullPath = Resolve-UnderProjectRoot -Path $rootDirectory
        $directories = Get-ChildItem -LiteralPath $rootFullPath -Directory -Recurse |
            Sort-Object { $_.FullName.Length } -Descending

        foreach ($directory in $directories) {
            $children = Get-ChildItem -LiteralPath $directory.FullName -Force
            $nonMetaChildren = $children | Where-Object { $_.Name -notlike "*.meta" }
            if ($nonMetaChildren.Count -ne 0) {
                continue
            }

            foreach ($metaChild in $children | Where-Object { -not $_.PSIsContainer -and $_.Name -like "*.meta" }) {
                Remove-Item -LiteralPath $metaChild.FullName -Force
                $script:removedMetaCount++
            }

            $directoryMetaPath = "$($directory.FullName).meta"
            if (Test-Path -LiteralPath $directoryMetaPath) {
                Remove-Item -LiteralPath $directoryMetaPath -Force
                $script:removedMetaCount++
            }

            Remove-Item -LiteralPath $directory.FullName -Force
            $script:removedDirectoryCount++
        }
    }
}

function Remove-OrphanMetaFiles {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$RootDirectories
    )

    foreach ($rootDirectory in $RootDirectories) {
        if (-not (Test-Path -LiteralPath $rootDirectory)) {
            continue
        }

        $rootFullPath = Resolve-UnderProjectRoot -Path $rootDirectory
        Get-ChildItem -LiteralPath $rootFullPath -Recurse -File -Filter "*.meta" | ForEach-Object {
            $basePath = $_.FullName.Substring(0, $_.FullName.Length - 5)
            if (-not (Test-Path -LiteralPath $basePath)) {
                Remove-Item -LiteralPath $_.FullName -Force
                $script:removedMetaCount++
            }
        }
    }
}

$wholePathMoves = @(
    @{ Source = "Assets\GameMain\Configs"; Destination = "Assets\App\Shared\Config\GameMain" },
    @{ Source = "Assets\GameMain\DataTables"; Destination = "Assets\App\Shared\DataTables\GameMain" },
    @{ Source = "Assets\GameMain\Localization"; Destination = "Assets\App\Shared\Localization\GameMain" },
    @{ Source = "Assets\GameMain\Sounds"; Destination = "Assets\App\Shared\Audio\LegacySoundArchive\GameMainSounds" },
    @{ Source = "Assets\GameMain\tietu\Sounds"; Destination = "Assets\App\Shared\Audio\LegacySoundArchive\TietuSounds" },
    @{ Source = "Assets\GameMain\Animations\Animation\PQ"; Destination = "Assets\App\Modules\NetworkCabinetWiring\Animations\LegacyClips\WiringClips" },
    @{ Source = "Assets\GameMain\Animations\Animation\WHY\HJ"; Destination = "Assets\App\Modules\RackAssembly\Animations\LegacyClips\RackClips" },
    @{ Source = "Assets\GameMain\Animations\Animation\WHY\BZQ"; Destination = "Assets\App\Modules\DisplayWallAssembly\Animations\LegacyClips\DisplayWallClips" },
    @{ Source = "Assets\GameMain\Animations\Animation\JYJ"; Destination = "Assets\App\Modules\ChargingStationAssembly\Animations\LegacyClips\AssemblyClips" },
    @{ Source = "Assets\GameMain\Animations\Animator\PQ"; Destination = "Assets\App\Modules\NetworkCabinetWiring\Animations\LegacyAnimator\WiringControllers" },
    @{ Source = "Assets\GameMain\Animations\Animator\WHY\HJ"; Destination = "Assets\App\Modules\RackAssembly\Animations\LegacyAnimator\HJ" },
    @{ Source = "Assets\GameMain\Animations\Animator\WHY\BZQ"; Destination = "Assets\App\Modules\DisplayWallAssembly\Animations\LegacyAnimator\BZQ" },
    @{ Source = "Assets\GameMain\Animations\Animator\JYJ"; Destination = "Assets\App\Modules\ChargingStationAssembly\Animations\LegacyAnimator\AssemblyControllers" },
    @{ Source = "Assets\GameMain\Entity\Resources"; Destination = "Assets\App\Shared\Resources\LegacyEntityResources" },
    @{ Source = "Assets\GameMain\Entity\ShareLocatuionEntities"; Destination = "Assets\App\Shared\Prefabs\System\ShareLocatuionEntities" },
    @{ Source = "Assets\GameMain\Entity\UIEntity"; Destination = "Assets\App\Shared\Prefabs\UIEntity" },
    @{ Source = "Assets\GameMain\Entity\WHY_HJ"; Destination = "Assets\App\Modules\RackAssembly\Prefabs\LegacyEntity\RackVariants" },
    @{ Source = "Assets\GameMain\Scenes\Conformity1.1AndTest"; Destination = "Assets\App\Shared\Scenes\Tests\Conformity1.1AndTest" },
    @{ Source = "Assets\GameMain\Scenes\Conformity2.1"; Destination = "Assets\App\Shared\Scenes\Tests\Conformity2.1" },
    @{ Source = "Assets\GameMain\Scenes\202412"; Destination = "Assets\App\Shared\Scenes\History\202412" },
    @{ Source = "Assets\GameMain\Scenes\Scenes"; Destination = "Assets\App\Shared\Scenes\Composite\Scenes" },
    @{ Source = "Assets\GameMain\Plugins\BouncyCastle"; Destination = "Assets\App\ThirdPartyAdapters\LegacyDlls\BouncyCastle" },
    @{ Source = "Assets\GameMain\Scripts\Base"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Base" },
    @{ Source = "Assets\GameMain\Scripts\BuiltinData"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\BuiltinData" },
    @{ Source = "Assets\GameMain\Scripts\Data"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Data" },
    @{ Source = "Assets\GameMain\Scripts\DataTable"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\DataTable" },
    @{ Source = "Assets\GameMain\Scripts\Definition"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Definition" },
    @{ Source = "Assets\GameMain\Scripts\Entity"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Entity" },
    @{ Source = "Assets\GameMain\Scripts\Enum"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Enum" },
    @{ Source = "Assets\GameMain\Scripts\Event"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Event" },
    @{ Source = "Assets\GameMain\Scripts\Extension"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Extension" },
    @{ Source = "Assets\GameMain\Scripts\FrameWorks"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\FrameWorks" },
    @{ Source = "Assets\GameMain\Scripts\Item"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Item" },
    @{ Source = "Assets\GameMain\Scripts\Level"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Level" },
    @{ Source = "Assets\GameMain\Scripts\Localization"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Localization" },
    @{ Source = "Assets\GameMain\Scripts\Procedure"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Procedure" },
    @{ Source = "Assets\GameMain\Scripts\Sound"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Sound" },
    @{ Source = "Assets\GameMain\Scripts\Utility"; Destination = "Assets\App\Shared\Scripts\LegacyFramework\Utility" },
    @{ Source = "Assets\GameMain\Scripts\Editor"; Destination = "Assets\App\Editor\LegacyEditor" },
    @{ Source = "Assets\GameMain\Scripts\Service"; Destination = "Assets\App\Shared\Scripts\LegacyService" },
    @{ Source = "Assets\GameMain\Scripts\UI"; Destination = "Assets\App\Shared\Scripts\LegacyUI" },
    @{ Source = "Assets\GameMain\Scripts\Scene"; Destination = "Assets\App\Shared\Scripts\LegacySceneFlow" },
    @{ Source = "Assets\GameMain\Scripts\Camera"; Destination = "Assets\App\Shared\Scripts\LegacySceneFlow\Camera" },
    @{ Source = "Assets\GameMain\Scripts\Menu"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\Menu" },
    @{ Source = "Assets\GameMain\Scripts\Multiplayer"; Destination = "Assets\App\Shared\Scripts\LegacyNetwork" },
    @{ Source = "Assets\GameMain\Scripts\NewS"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\NewS" }
)

foreach ($move in $wholePathMoves) {
    Move-PathWithMeta -SourcePath (Join-Path $ProjectRoot $move.Source) -DestinationPath (Join-Path $ProjectRoot $move.Destination)
}

$zdpAnimatorRules = @(
    @{ Pattern = "^(SDZ_ZT_LiuJiao|SDZ_ZT_ShiZi)$"; Target = "Assets\App\Shared\Animations\DrillInteractionControllers" },
    @{ Pattern = "^(AwardShowPanel|Dingzi|Dingzi1|KaiGuan 1|KaiGuan|TipsPanel|ZiGongSi|ZiGongSi11|ZiGongSi12|ZiGongSi13|ZiGongSi15|ZiGongSi3|ZiGongSi5|ZiGongSi7|ZiGongSi9)$"; Target = "Assets\App\Modules\AgvCommissioning\Animations\LegacyAnimator\AgvTaskControllers" }
)

foreach ($rule in $zdpAnimatorRules) {
    Move-TopLevelFilesByPattern `
        -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\Animations\Animator\ZDP") `
        -Pattern $rule.Pattern `
        -TargetDirectory (Join-Path $ProjectRoot $rule.Target)
}

$zdpClipRules = @(
    @{ Pattern = "^(SDZ_ZT_LiuJiao|SDZ_ZT_ShiZi)$"; Target = "Assets\App\Shared\Animations\LegacyClips\SharedDrillInteractionClips" },
    @{ Pattern = "^(Dingzi|Dingzi1|GetAward|kaiguan|TipsPanel|ZiGongSi|ZiGongSi11|ZiGongSi12|ZiGongSi13|ZiGongSi15|ZiGongSi3|ZiGongSi5|ZiGongSi7|ZiGongSi9)$"; Target = "Assets\App\Modules\AgvCommissioning\Animations\LegacyClips\AgvTaskClips" }
)

foreach ($rule in $zdpClipRules) {
    Move-TopLevelFilesByPattern `
        -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\Animations\Animation\ZDP") `
        -Pattern $rule.Pattern `
        -TargetDirectory (Join-Path $ProjectRoot $rule.Target)
}

$entityRules = @(
    @{ Pattern = "^(WHY_FuWuQi-6|UPan|UI_XiTongJingXiangAnZhuang-6|JiGui-6)$"; Target = "Assets\App\Modules\ServerAssembly\Prefabs\LegacyEntity" },
    @{ Pattern = "^(LSL_RMS_Install|RMS_System_Installer-9)$"; Target = "Assets\App\Modules\RmsInstall\Prefabs\LegacyEntity" },
    @{ Pattern = "^(LSL_RMS_Import)$"; Target = "Assets\App\Modules\RmsImport\Prefabs\LegacyEntity" },
    @{ Pattern = "^(HuaXian|DiMian|ErWeiMa-2|JiHaoBi|JiZhunDian|JuanChi|LineRendererPrefab|ShuiPingYi)$"; Target = "Assets\App\Modules\LayoutMarking\Prefabs\LegacyEntity" },
    @{ Pattern = "^(WHY_HJ-11|HuoJia_blue-10|HuoJia_blue-11|HuoJia_cube-10|HuoJia_cube-11|HuoJia_green-10|HuoJia_green-11|HuoJia_shi-10|HuoJia_shi-11|JiaoShui-10-1|BanShou-10|banshou-2|banshou_2)$"; Target = "Assets\App\Modules\RackAssembly\Prefabs\LegacyEntity" },
    @{ Pattern = "^(WHY_BZQ-10|BoZhongQiang_blue-10|BoZhongQiang_blue-11|BoZhongQiang_cube-10|BoZhongQiang_cube-11|BoZhongQiang_green-10|BoZhongQiang_green-11|BoZhongQiang_shi-10|BoZhongQiang_shi-11|ShouDianZuan-11)$"; Target = "Assets\App\Modules\DisplayWallAssembly\Prefabs\LegacyEntity" },
    @{ Pattern = "^(changjing-2|YangJiaoChui-2|ShouDianZuan-CDZ|chuizi_collider-2|chuizi_collider_2|chuiz_part|NeiLiuJiaoBanShou-2|步骤提示UI-2)$"; Target = "Assets\App\Modules\ChargingStationAssembly\Prefabs\LegacyEntity" },
    @{ Pattern = "^(ChangJing-8|AGV1-4|ShouDianZuan-JQR)$"; Target = "Assets\App\Modules\AgvCommissioning\Prefabs\LegacyEntity" },
    @{ Pattern = "^(NetWork-03_2 1-3|ChaTou1-6|ChaTou2-6|ChaTou2-6 1-3)$"; Target = "Assets\App\Modules\NetworkCabinetWiring\Prefabs\LegacyEntity" },
    @{ Pattern = "^(changjing-4|HuoJia-4)$"; Target = "Assets\App\Modules\RackPlacement\Prefabs\LegacyEntity" },
    @{ Pattern = "^(橡胶锤-10)$"; Target = "Assets\App\Modules\RackAssembly\Prefabs\LegacyEntity" },
    @{ Pattern = "^(ShouDianZuan-9)$"; Target = "Assets\App\Modules\AgvCommissioning\Prefabs\LegacyEntity" },
    @{ Pattern = "^(GameFramework|MR_ChangJing-3|RemoteConnection|RenWu_UI-6|UI-10|UI-11|ZhiYin-10|ZhiYin-11|ZhiYin-6|ChangJianWenTi-7|JianCe-7|WHY_Question-7|XuanZeTi-7|ChangJing-05|ChangJing-3|MoDou)$"; Target = "Assets\App\Shared\Prefabs\System\LegacyEntity" }
)

foreach ($rule in $entityRules) {
    Move-TopLevelFilesByPattern `
        -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\Entity") `
        -Pattern $rule.Pattern `
        -TargetDirectory (Join-Path $ProjectRoot $rule.Target)
}

$tietuRules = @(
    @{ Pattern = "(ChaTou|ChuanKouXian|LuYouQi|JiaoHuanJi|FuWuQi|ShuiJingTou|TongXian|USB|JiGui|DuanZi|ZhuJi|XianShiQi|ChaPai|YiHaoJiGui|txm|PingMu)"; Target = "Assets\App\Modules\NetworkCabinetWiring\Materials\LegacyTietu" },
    @{ Pattern = "(JiHaoBi|ShuiPingYi|JuanChi|ErWeiMa|DiMian|HuaXian|JiZhun|WangGe|网格)"; Target = "Assets\App\Modules\LayoutMarking\Materials\LegacyTietu" },
    @{ Pattern = "(HuoJia|JiaoShui|BanShou|ZhiXiang|LuoSi)"; Target = "Assets\App\Modules\RackAssembly\Materials\LegacyTietu" },
    @{ Pattern = "(BoZhongQiang|BZQ|bzq)"; Target = "Assets\App\Modules\DisplayWallAssembly\Materials\LegacyTietu" },
    @{ Pattern = "(CDZ|ChongDian|YangJiaoChui|ShouDianZuan|DianZuan)"; Target = "Assets\App\Modules\ChargingStationAssembly\Materials\LegacyTietu" },
    @{ Pattern = "(AGV|JiQiRen)"; Target = "Assets\App\Modules\AgvCommissioning\Materials\LegacyTietu" }
)

foreach ($rule in $tietuRules) {
    Move-TopLevelFilesByPattern `
        -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\tietu") `
        -Pattern $rule.Pattern `
        -TargetDirectory (Join-Path $ProjectRoot $rule.Target)
}

Move-RemainingTopLevelFiles `
    -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\tietu") `
    -TargetDirectory (Join-Path $ProjectRoot "Assets\App\Shared\Materials\LegacyTietu")

Move-RemainingTopLevelFiles `
    -SourceDirectory (Join-Path $ProjectRoot "Assets\GameMain\Animations") `
    -TargetDirectory (Join-Path $ProjectRoot "Assets\App\Shared\Animations\LegacyLoose")

$manualTailMoves = @(
    @{ Source = "Assets\GameMain\Entity\整体.fbx"; Destination = "Assets\App\Modules\DisplayWallAssembly\Models\DisplayWallKit\整体.fbx" },
    @{ Source = "Assets\GameMain\Entity\橡胶锤-11.FBX"; Destination = "Assets\App\Modules\RackAssembly\Models\RackKit\橡胶锤-11.FBX" },
    @{ Source = "Assets\GameMain\Entity\箭头(1).FBX"; Destination = "Assets\App\Shared\Models\WorkshopShared\箭头(1).FBX" },
    @{ Source = "Assets\GameMain\Models\ShouBuUI.fbx"; Destination = "Assets\App\Shared\Models\WorkshopShared\ShouBuUI.fbx" },
    @{ Source = "Assets\GameMain\Scenes\Main.unity"; Destination = "Assets\App\Shared\Scenes\LegacyMainline\Main.unity" },
    @{ Source = "Assets\GameMain\Scenes\Main 1.unity"; Destination = "Assets\App\Shared\Scenes\LegacyMainline\Main 1.unity" },
    @{ Source = "Assets\GameMain\Scenes\MRTK.unity"; Destination = "Assets\App\Shared\Scenes\Composite\MRTK.unity" },
    @{ Source = "Assets\GameMain\Scenes\MRTK 1.unity"; Destination = "Assets\App\Shared\Scenes\History\MRTK 1.unity" },
    @{ Source = "Assets\GameMain\Scenes\MRTK 2.unity"; Destination = "Assets\App\Shared\Scenes\History\MRTK 2.unity" },
    @{ Source = "Assets\GameMain\Scenes\MRTK.sync-conflict-20241121-142302-PD4YLLL.unity"; Destination = "Assets\App\Shared\Scenes\History\Conflict\MRTK.sync-conflict-20241121-142302-PD4YLLL.unity" },
    @{ Source = "Assets\GameMain\Scenes\MRTK.sync-conflict-20241121-143926-L7NTOCC.unity"; Destination = "Assets\App\Shared\Scenes\History\Conflict\MRTK.sync-conflict-20241121-143926-L7NTOCC.unity" },
    @{ Source = "Assets\GameMain\Scenes\SycObjTest.unity"; Destination = "Assets\App\Shared\Scenes\Tests\SycObjTest.unity" },
    @{ Source = "Assets\GameMain\Plugins\LitJson.dll"; Destination = "Assets\App\ThirdPartyAdapters\LegacyDlls\LitJson.dll" },
    @{ Source = "Assets\GameMain\Scripts\CustomTank.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\CustomTank.cs" },
    @{ Source = "Assets\GameMain\Scripts\ChangeScene.cs"; Destination = "Assets\App\Shared\Scripts\LegacySceneFlow\ChangeScene.cs" },
    @{ Source = "Assets\GameMain\Scripts\HjBzqTaskController.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\HjBzqTaskController.cs" },
    @{ Source = "Assets\GameMain\Scripts\GetOperationAuthority.cs"; Destination = "Assets\App\Shared\Scripts\LegacyService\GetOperationAuthority.cs" },
    @{ Source = "Assets\GameMain\Scripts\AutoStartSeverORClient.cs"; Destination = "Assets\App\Shared\Scripts\LegacyNetwork\AutoStartSeverORClient.cs" },
    @{ Source = "Assets\GameMain\Scripts\AutoStartSeverChat.cs"; Destination = "Assets\App\Shared\Scripts\LegacyNetwork\AutoStartSeverChat.cs" },
    @{ Source = "Assets\GameMain\Scripts\ProgressController.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\ProgressController.cs" },
    @{ Source = "Assets\GameMain\Scripts\SycObj.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\SycObj.cs" },
    @{ Source = "Assets\GameMain\Scripts\SycButtonController.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\SycButtonController.cs" },
    @{ Source = "Assets\GameMain\Scripts\TaskController.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\TaskController.cs" },
    @{ Source = "Assets\GameMain\Scripts\TANK.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\TANK.cs" },
    @{ Source = "Assets\GameMain\Scripts\PersonDefine.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\PersonDefine.cs" },
    @{ Source = "Assets\GameMain\Scripts\OnTrigger.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\OnTrigger.cs" },
    @{ Source = "Assets\GameMain\Scripts\ObjofBagDefineATTACH.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\ObjofBagDefineATTACH.cs" },
    @{ Source = "Assets\GameMain\Scripts\ObjofBagDefine.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\ObjofBagDefine.cs" },
    @{ Source = "Assets\GameMain\Scripts\NewBehaviourScript.cs"; Destination = "Assets\App\Shared\Scripts\LegacyGameplay\NewBehaviourScript.cs" },
    @{ Source = "Assets\GameMain\Scripts\LSL\DictationHandler.cs"; Destination = "Assets\App\Modules\RmsInstall\Scripts\VoiceInput\DictationHandler.cs" },
    @{ Source = "Assets\GameMain\Scripts\LSL\InputField_Button.cs"; Destination = "Assets\App\Shared\Scripts\RmsWorkflow\InputField_Button.cs" },
    @{ Source = "Assets\GameMain\Scripts\LSL\InputField_InputField.cs"; Destination = "Assets\App\Shared\Scripts\RmsWorkflow\InputField_InputField.cs" },
    @{ Source = "Assets\GameMain\Scripts\LSL\UGUIInputField_InputField.cs"; Destination = "Assets\App\Shared\Scripts\RmsWorkflow\UGUIInputField_InputField.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\JiFen.cs"; Destination = "Assets\App\Shared\Scripts\TaskRewardFlow\JiFen.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\JianQian.cs"; Destination = "Assets\App\Shared\Scripts\TaskRewardFlow\JianQian.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\BuyNextStep.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\BuyNextStep.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\CheckRobotLeft.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\CheckRobotLeft.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\CheckRobotRight.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\CheckRobotRight.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\ShowBuyPage.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\ShowBuyPage.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\ZDP\TaskJianChaButton.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\TaskButtons\TaskJianChaButton.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\ZDP\TaskKaiJiButton.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\TaskButtons\TaskKaiJiButton.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\JiFen\ZDP\TaskXiFuButton.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskRewardFlow\TaskButtons\TaskXiFuButton.cs" },
    @{ Source = "Assets\GameMain\Scripts\ZDP\ChongZhi.cs"; Destination = "Assets\App\Modules\AgvCommissioning\Scripts\TaskFlowSupport\ChongZhi.cs" },
    @{ Source = "Assets\GameMain\Animations\Animation\ZDP\SDZ_BanJi.anim"; Destination = "Assets\App\Modules\ChargingStationAssembly\Animations\LegacyClips\AssemblyClips\SDZ_BanJi_State0.anim" },
    @{ Source = "Assets\GameMain\Res\Models.meta"; Destination = "Assets\App\Shared\LegacyArchive\GameMainRes\Models.meta" },
    @{ Source = "Assets\GameMain\Res\Prefabs.meta"; Destination = "Assets\App\Shared\LegacyArchive\GameMainRes\Prefabs.meta" },
    @{ Source = "Assets\GameMain\Res\Scenes.meta"; Destination = "Assets\App\Shared\LegacyArchive\GameMainRes\Scenes.meta" }
)

foreach ($move in $manualTailMoves) {
    Move-PathWithMeta -SourcePath (Join-Path $ProjectRoot $move.Source) -DestinationPath (Join-Path $ProjectRoot $move.Destination)
}

$emptyDirectoryRemovals = @(
    "Assets\GameMain\Res\Models",
    "Assets\GameMain\Res\Prefabs",
    "Assets\GameMain\Res\Scenes"
)

foreach ($relativeDirectory in $emptyDirectoryRemovals) {
    $fullDirectory = Join-Path $ProjectRoot $relativeDirectory
    if (Test-Path -LiteralPath $fullDirectory) {
        $childCount = (Get-ChildItem -LiteralPath $fullDirectory -Force | Measure-Object).Count
        if ($childCount -eq 0) {
            Remove-Item -LiteralPath $fullDirectory -Force
        }
    }
}

$cleanupRoots = @(
    (Join-Path $ProjectRoot "Assets\GameMain"),
    (Join-Path $ProjectRoot "Assets\_syc_resource")
)

Remove-MetaOnlyDirectories -RootDirectories $cleanupRoots
Remove-OrphanMetaFiles -RootDirectories $cleanupRoots

Write-Host "Moved items: $moveCount"
Write-Host "Skipped existing targets: $skipCount"
Write-Host "Removed empty directories: $removedDirectoryCount"
Write-Host "Removed orphan/meta-only files: $removedMetaCount"
