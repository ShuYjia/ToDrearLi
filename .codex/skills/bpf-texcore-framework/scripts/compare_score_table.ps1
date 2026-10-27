param(
    [string]$WorkbookPath = "docs/texcore-module-score-data.xlsx"
)

if (-not (Test-Path -LiteralPath $WorkbookPath)) {
    throw "Score workbook not found: $WorkbookPath"
}

[pscustomobject]@{
    Workbook = (Resolve-Path -LiteralPath $WorkbookPath).Path
    Note = "Extract worksheet data, then map score items to scene actions. Do not infer stepIndex from row number."
} | ConvertTo-Json
