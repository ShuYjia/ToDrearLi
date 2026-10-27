# -*- coding: utf-8 -*-
"""Minimal MCP streamable-HTTP client for Unity MCP at 127.0.0.1:8080."""
import json, sys, io, urllib.request, threading

BASE = "http://127.0.0.1:8080/mcp"
session_id = None
lock = threading.Lock()

def _post(payload, timeout=60):
    global session_id
    url = BASE
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
    }
    if session_id:
        headers["Mcp-Session-Id"] = session_id
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, headers=headers, method="POST")
    try:
        resp = urllib.request.urlopen(req, timeout=timeout)
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8", errors="replace")
        return {"http_error": e.code, "body": body[:2000]}
    sid = resp.headers.get("Mcp-Session-Id")
    if sid:
        session_id = sid
    body = resp.read().decode("utf-8", errors="replace")
    # parse SSE
    result = None
    for line in body.splitlines():
        if line.startswith("data:"):
            try:
                result = json.loads(line[5:].strip())
            except Exception:
                pass
    if result is None and body.strip().startswith("{"):
        try:
            result = json.loads(body)
        except Exception:
            result = {"raw": body[:2000]}
    return result

def initialize():
    r = _post({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
        "protocolVersion": "2024-11-05", "capabilities": {},
        "clientInfo": {"name": "workbuddy-raw", "version": "1.0"}}})
    _post({"jsonrpc": "2.0", "method": "notifications/initialized"})
    return r

def list_tools():
    return _post({"jsonrpc": "2.0", "id": 2, "method": "tools/list"})

def list_resources():
    return _post({"jsonrpc": "2.0", "id": 3, "method": "resources/list"})

def read_resource(uri):
    return _post({"jsonrpc": "2.0", "id": 4, "method": "resources/read", "params": {"uri": uri}})

def call_tool(name, arguments=None, msg_id=10, timeout=120):
    return _post({"jsonrpc": "2.0", "id": msg_id, "method": "tools/call",
                  "params": {"name": name, "arguments": arguments or {}}}, timeout=timeout)

if __name__ == "__main__":
    cmd = sys.argv[1] if len(sys.argv) > 1 else "info"
    if cmd == "info":
        init = initialize()
        print("== initialize ==")
        print(json.dumps(init, ensure_ascii=False)[:600])
        print("session:", session_id)
        tools = list_tools()
        names = []
        if tools and "result" in tools:
            for t in tools["result"].get("tools", []):
                names.append(t["name"])
        print("== tools (%d) ==" % len(names))
        print("\n".join(names))
    elif cmd == "res":
        initialize()
        print(json.dumps(read_resource(sys.argv[2]), ensure_ascii=False)[:3000])
    elif cmd == "call":
        initialize()
        name = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        r = call_tool(name, args)
        print(json.dumps(r, ensure_ascii=False)[:6000])
