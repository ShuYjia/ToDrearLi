param(
    [Parameter(Mandatory=$true)]
    [string]$YamlPath
)

if (-not (Test-Path -LiteralPath $YamlPath)) {
    throw "File not found: $YamlPath"
}

$guids = Select-String -LiteralPath $YamlPath -Pattern 'guid: ([0-9a-f]{32})' -AllMatches |
    ForEach-Object { $_.Matches.Groups[1].Value } |
    Sort-Object -Unique

$root = Get-Location
$metaGuids = Get-ChildItem -Path Assets, Packages -Filter *.meta -Recurse -ErrorAction SilentlyContinue |
    Select-String -Pattern '^guid: ([0-9a-f]{32})' |
    ForEach-Object { $_.Matches.Groups[1].Value } |
    Sort-Object -Unique

Compare-Object -ReferenceObject $guids -DifferenceObject $metaGuids |
    Where-Object SideIndicator -eq '<=' |
    Select-Object @{Name='MissingGuid';Expression={$_.InputObject}} |
    ConvertTo-Json
