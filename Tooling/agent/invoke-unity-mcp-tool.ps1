<#
.SYNOPSIS
    Call one tool on the project-locked Unity MCP server (.mcp.json -> unityMCP).

.DESCRIPTION
    Performs the MCP HTTP handshake (initialize -> notifications/initialized)
    and then calls tools/call for the given tool name with the given JSON
    arguments. Prints the tool result as JSON. Used when the harness session has
    no native unityMCP tool binding but the local MCPForUnity server is up.

.EXAMPLE
    ./invoke-unity-mcp-tool.ps1 -Tool manage_scene -Arguments '{"action":"get_active"}'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Tool,
    [string]$Arguments = '{}',
    [string]$Endpoint = 'http://127.0.0.1:8080/mcp',
    [int]$TimeoutSec = 120
)

$ErrorActionPreference = 'Stop'

function Read-McpResponse {
    param([string]$Content)

    $objects = @()
    foreach ($line in ($Content -split "`r?`n")) {
        if ($line -like 'data: *') {
            $payload = $line.Substring(6).Trim()
            if ($payload) { $objects += ($payload | ConvertFrom-Json) }
        }
    }

    if ($objects.Count -eq 0 -and $Content.Trim()) {
        $objects += ($Content.Trim() | ConvertFrom-Json)
    }

    return $objects
}

$sessionHeaders = @{ Accept = 'application/json, text/event-stream' }

$initBody = @{
    jsonrpc = '2.0'
    id      = 1
    method  = 'initialize'
    params  = @{
        protocolVersion = '2024-11-05'
        capabilities    = @{}
        clientInfo      = @{ name = 'bpf-agent-cli'; version = '1.0' }
    }
} | ConvertTo-Json -Depth 10

$initResponse = Invoke-WebRequest -Uri $Endpoint -Method Post -ContentType 'application/json' `
    -Headers $sessionHeaders -Body $initBody -TimeoutSec $TimeoutSec -UseBasicParsing

$sessionId = $initResponse.Headers['Mcp-Session-Id']
if ($sessionId -is [array]) { $sessionId = $sessionId[0] }
if ($sessionId) { $sessionHeaders['Mcp-Session-Id'] = $sessionId }

Invoke-WebRequest -Uri $Endpoint -Method Post -ContentType 'application/json' `
    -Headers $sessionHeaders -Body '{"jsonrpc":"2.0","method":"notifications/initialized"}' `
    -TimeoutSec $TimeoutSec -UseBasicParsing | Out-Null

$callBody = @{
    jsonrpc = '2.0'
    id      = 2
    method  = 'tools/call'
    params  = @{
        name      = $Tool
        arguments = ($Arguments | ConvertFrom-Json)
    }
} | ConvertTo-Json -Depth 30

$callResponse = Invoke-WebRequest -Uri $Endpoint -Method Post -ContentType 'application/json' `
    -Headers $sessionHeaders -Body $callBody -TimeoutSec $TimeoutSec -UseBasicParsing

$messages = Read-McpResponse -Content $callResponse.Content
$result = $messages | Where-Object { $_.id -eq 2 } | Select-Object -First 1

if (-not $result) {
    Write-Output $callResponse.Content
    exit 0
}

if ($result.error) {
    Write-Output ('MCP ERROR: ' + ($result.error | ConvertTo-Json -Depth 10))
    exit 1
}

$texts = @()
foreach ($item in @($result.result.content)) {
    if ($item.type -eq 'text') { $texts += $item.text }
}

if ($texts.Count -gt 0) {
    foreach ($text in $texts) { Write-Output $text }
} else {
    Write-Output ($result.result | ConvertTo-Json -Depth 30)
}
