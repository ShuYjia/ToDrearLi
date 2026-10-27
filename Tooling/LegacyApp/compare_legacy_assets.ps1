param(
    [string]$OldProjectRoot = "D:\gitea_obj\_unity projection\gitea_mrtk_sync_old_4",
    [string]$NewProjectRoot = "D:\gitea_obj\_unity projection\gitea_BPF",
    [string]$OutputDirectory = "D:\gitea_obj\_unity projection\gitea_BPF\docs\asset-compare"
)

$ErrorActionPreference = "Stop"

function Resolve-AbsolutePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return [System.IO.Path]::GetFullPath($Path)
}

function Get-MetaGuid {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AssetPath
    )

    $metaPath = "$AssetPath.meta"
    if (-not (Test-Path -LiteralPath $metaPath)) {
        return ""
    }

    foreach ($line in Get-Content -LiteralPath $metaPath -TotalCount 20) {
        if ($line -match '^guid:\s*([0-9a-f]{32})$') {
            return $Matches[1].ToLowerInvariant()
        }
    }

    return ""
}

function Get-RelativePathFromRoot {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RootPath,
        [Parameter(Mandatory = $true)]
        [string]$FullPath
    )

    $rootUri = New-Object System.Uri(($RootPath.TrimEnd('\') + '\'))
    $fileUri = New-Object System.Uri($FullPath)
    $relativeUri = $rootUri.MakeRelativeUri($fileUri)
    return ([System.Uri]::UnescapeDataString($relativeUri.ToString())).Replace('\', '/')
}

function Get-AssetInventory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectRoot,
        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $assetsRoot = Join-Path $ProjectRoot "Assets"
    if (-not (Test-Path -LiteralPath $assetsRoot)) {
        throw "Assets folder not found: $assetsRoot"
    }

    $files = Get-ChildItem -LiteralPath $assetsRoot -Recurse -Force -File |
        Where-Object { $_.Extension -ne ".meta" } |
        Sort-Object FullName

    $records = foreach ($file in $files) {
        $relativePath = Get-RelativePathFromRoot -RootPath $ProjectRoot -FullPath $file.FullName
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $guid = Get-MetaGuid -AssetPath $file.FullName

        [pscustomobject]@{
            ProjectLabel  = $Label
            RelativePath  = $relativePath
            FileName      = $file.Name
            Extension     = $file.Extension.ToLowerInvariant()
            SizeBytes     = $file.Length
            Guid          = $guid
            Sha256        = $hash
        }
    }

    return ,$records
}

function Add-RecordToIndex {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable]$Index,
        [Parameter(Mandatory = $true)]
        [string]$Key,
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Record
    )

    if ([string]::IsNullOrWhiteSpace($Key)) {
        return
    }

    if (-not $Index.ContainsKey($Key)) {
        $Index[$Key] = New-Object System.Collections.Generic.List[object]
    }

    $Index[$Key].Add($Record)
}

function Convert-RecordsToTextLines {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Records
    )

    $lines = @("RelativePath`tGuid`tSizeBytes`tSha256")
    foreach ($record in $Records | Sort-Object RelativePath) {
        $lines += "{0}`t{1}`t{2}`t{3}" -f $record.RelativePath, $record.Guid, $record.SizeBytes, $record.Sha256
    }

    return ,$lines
}

function Convert-ComparisonToTextLines {
    param(
        [Parameter(Mandatory = $true)]
        [object[]]$Rows
    )

    $lines = @("Status`tExpectedExcluded`tOldRelativePath`tOldGuid`tOldSha256`tMatchedNewPath")
    foreach ($row in $Rows | Sort-Object OldRelativePath) {
        $lines += "{0}`t{1}`t{2}`t{3}`t{4}`t{5}" -f `
            $row.Status, `
            $row.ExpectedExcluded, `
            $row.OldRelativePath, `
            $row.OldGuid, `
            $row.OldSha256, `
            $row.MatchedNewPath
    }

    return ,$lines
}

function Test-ExpectedExclusion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $patterns = @(
        '^Assets/GameMain/Plugins/Mirror/',
        '^Assets/GameMain/Plugins/QuickOutline/',
        '^Assets/Demigiant/',
        '^Assets/Plugin/Demigiant/',
        '^Assets/Samples/',
        '^Assets/_syc_resource/_my_scripts/'
    )

    foreach ($pattern in $patterns) {
        if ($RelativePath -match $pattern) {
            return $true
        }
    }

    return $false
}

$OldProjectRoot = Resolve-AbsolutePath -Path $OldProjectRoot
$NewProjectRoot = Resolve-AbsolutePath -Path $NewProjectRoot
$OutputDirectory = Resolve-AbsolutePath -Path $OutputDirectory

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
}

$oldAssets = Get-AssetInventory -ProjectRoot $OldProjectRoot -Label "Old"
$newAssets = Get-AssetInventory -ProjectRoot $NewProjectRoot -Label "New"

$newByGuid = @{}
$newByHash = @{}
$newByName = @{}

foreach ($record in $newAssets) {
    Add-RecordToIndex -Index $newByGuid -Key $record.Guid -Record $record
    Add-RecordToIndex -Index $newByHash -Key $record.Sha256 -Record $record
    Add-RecordToIndex -Index $newByName -Key $record.FileName.ToLowerInvariant() -Record $record
}

$comparisonRows = foreach ($oldRecord in $oldAssets) {
    $expectedExcluded = Test-ExpectedExclusion -RelativePath $oldRecord.RelativePath
    $status = "Missing"
    $matchedNewPath = ""

    if ($oldRecord.Guid -and $newByGuid.ContainsKey($oldRecord.Guid)) {
        $matchedByGuid = $newByGuid[$oldRecord.Guid]
        $samePathMatch = $matchedByGuid | Where-Object { $_.RelativePath -eq $oldRecord.RelativePath }
        if ($samePathMatch) {
            $status = "MatchedByGuidSamePath"
            $matchedNewPath = ($samePathMatch | Select-Object -First 1).RelativePath
        }
        else {
            $status = "MatchedByGuidMoved"
            $matchedNewPath = (($matchedByGuid | Select-Object -ExpandProperty RelativePath) -join " | ")
        }
    }
    elseif ($newByHash.ContainsKey($oldRecord.Sha256)) {
        $matchedByHash = $newByHash[$oldRecord.Sha256]
        $status = "MatchedByHashDifferentGuid"
        $matchedNewPath = (($matchedByHash | Select-Object -ExpandProperty RelativePath) -join " | ")
    }
    elseif ($newByName.ContainsKey($oldRecord.FileName.ToLowerInvariant())) {
        $matchedByName = $newByName[$oldRecord.FileName.ToLowerInvariant()]
        $status = "SameNameOnly"
        $matchedNewPath = (($matchedByName | Select-Object -ExpandProperty RelativePath) -join " | ")
    }

    [pscustomobject]@{
        Status           = $status
        ExpectedExcluded = $expectedExcluded
        OldRelativePath  = $oldRecord.RelativePath
        OldGuid          = $oldRecord.Guid
        OldSha256        = $oldRecord.Sha256
        MatchedNewPath   = $matchedNewPath
    }
}

$summaryLines = New-Object System.Collections.Generic.List[string]
$summaryLines.Add("Legacy Asset Compare Summary")
$summaryLines.Add("OldProjectRoot: $OldProjectRoot")
$summaryLines.Add("NewProjectRoot: $NewProjectRoot")
$summaryLines.Add("GeneratedAt: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$summaryLines.Add("")
$summaryLines.Add("Old asset count (non-meta): $($oldAssets.Count)")
$summaryLines.Add("New asset count (non-meta): $($newAssets.Count)")
$summaryLines.Add("")

$statusCounts = $comparisonRows | Group-Object Status | Sort-Object Name
foreach ($group in $statusCounts) {
    $summaryLines.Add(("Status {0}: {1}" -f $group.Name, $group.Count))
}

$unexpectedMissing = $comparisonRows | Where-Object { $_.Status -eq "Missing" -and -not $_.ExpectedExcluded }
$expectedExcludedMissing = $comparisonRows | Where-Object { $_.Status -eq "Missing" -and $_.ExpectedExcluded }
$sameNameOnly = $comparisonRows | Where-Object { $_.Status -eq "SameNameOnly" }
$matchedByHashDifferentGuid = $comparisonRows | Where-Object { $_.Status -eq "MatchedByHashDifferentGuid" }

$summaryLines.Add("")
$summaryLines.Add("Unexpected missing count: $($unexpectedMissing.Count)")
$summaryLines.Add("Expected excluded missing count: $($expectedExcludedMissing.Count)")
$summaryLines.Add("Same-name-only count: $($sameNameOnly.Count)")
$summaryLines.Add("Matched-by-hash-different-guid count: $($matchedByHashDifferentGuid.Count)")
$summaryLines.Add("")

$summaryLines.Add("Top unexpected missing directories:")
$topUnexpected = $unexpectedMissing |
    ForEach-Object {
        $parts = $_.OldRelativePath.Split('/')
        $prefix = if ($parts.Count -ge 3) { ($parts[0..2] -join "/") } else { $_.OldRelativePath }
        [pscustomobject]@{ Prefix = $prefix }
    } |
    Group-Object Prefix |
    Sort-Object -Property @{ Expression = "Count"; Descending = $true }, @{ Expression = "Name"; Descending = $false } |
    Select-Object -First 20

foreach ($entry in $topUnexpected) {
    $summaryLines.Add(("  {0}: {1}" -f $entry.Name, $entry.Count))
}

$oldAssetsPath = Join-Path $OutputDirectory "old-assets.txt"
$newAssetsPath = Join-Path $OutputDirectory "new-assets.txt"
$compareAllPath = Join-Path $OutputDirectory "old-to-new-compare.txt"
$unexpectedMissingPath = Join-Path $OutputDirectory "old-assets-missing-unexpected.txt"
$expectedExcludedPath = Join-Path $OutputDirectory "old-assets-missing-expected-excluded.txt"
$summaryPath = Join-Path $OutputDirectory "summary.txt"

Set-Content -LiteralPath $oldAssetsPath -Value (Convert-RecordsToTextLines -Records $oldAssets) -Encoding UTF8
Set-Content -LiteralPath $newAssetsPath -Value (Convert-RecordsToTextLines -Records $newAssets) -Encoding UTF8
Set-Content -LiteralPath $compareAllPath -Value (Convert-ComparisonToTextLines -Rows $comparisonRows) -Encoding UTF8
Set-Content -LiteralPath $unexpectedMissingPath -Value (Convert-ComparisonToTextLines -Rows $unexpectedMissing) -Encoding UTF8
Set-Content -LiteralPath $expectedExcludedPath -Value (Convert-ComparisonToTextLines -Rows $expectedExcludedMissing) -Encoding UTF8
Set-Content -LiteralPath $summaryPath -Value $summaryLines -Encoding UTF8

Write-Host "Wrote: $summaryPath"
Write-Host "Wrote: $oldAssetsPath"
Write-Host "Wrote: $newAssetsPath"
Write-Host "Wrote: $compareAllPath"
Write-Host "Wrote: $unexpectedMissingPath"
Write-Host "Wrote: $expectedExcludedPath"
