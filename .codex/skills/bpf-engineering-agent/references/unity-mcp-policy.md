# BPF Unity MCP Policy

Probe Unity MCP on every Unity-related turn.

Use:

1. manage_editor.telemetry_ping
2. if inconclusive, one lightweight read-only call such as active scene query

Treat Unity MCP as connected only when a real unityMCP call returns success.

If disconnected:

- continue with repo files, Unity CLI, adb, and deterministic scripts when appropriate
- do not claim runtime validation
- do not use MCP-only actions

Do not repeat MCP origin debates unless .mcp.json, Packages/manifest.json, or live endpoint evidence changes.
