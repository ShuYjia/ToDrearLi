# BPF Evidence Standards

## Evidence Classes

Classify findings as:

- confirmed runtime evidence
- serialized structure evidence
- deterministic script evidence
- static text or GUID evidence
- user acceptance evidence
- unverified hypothesis

## Unity Evidence

Prefer live Unity MCP evidence when connected. If unavailable, use scene or Prefab YAML, .meta GUID resolution, project settings, deterministic scripts, and Unity logs.

Never treat tool availability as Unity MCP connectivity. Connectivity requires a successful unityMCP response.

## Reporting Language

Use concrete scope:

- exact scene or Prefab path
- exact GameObject or component name
- exact count and method
- exact files changed or intentionally not changed

Do not say fixed unless the validation gate passed.
