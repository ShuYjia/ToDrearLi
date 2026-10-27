param(
    [Parameter(Mandatory=$true)]
    [string]$TargetPrefab,
    [Parameter(Mandatory=$true)]
    [string]$SourcePrefab
)

[pscustomobject]@{
    TargetPrefab = $TargetPrefab
    SourcePrefab = $SourcePrefab
    Note = "Use Unity AssetDatabase for authoritative Mesh and slot matching. This wrapper records the intended deterministic comparison contract."
} | ConvertTo-Json
