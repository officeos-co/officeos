# pod-executor — Go PTY-over-WebSocket Server

Minimal Go binary that exposes a bash PTY over WebSocket. Runs inside each agent pod. Cloned from GoTTY, stripped to essentials.

## Commands

```bash
go test -v ./...
go build -o pod-executor .
docker build -t harkro123/eaos-pod-executor:latest .
```

## What this binary does

- WebSocket server on PORT (default 42617)
- Each connection spawns /bin/bash via PTY
- Bridges stdin/stdout between WebSocket and PTY
- Auth: AGENT_TOKEN env var, validated from ?token= query param
- Health check: GET /health

## What this binary does NOT do

No LLM calls, no prompt composition, no memory, no bootstrap, no config fetching, no personality files, no knowledge of agents/sessions/users, no Playwright/browser.

## Env vars

| Var | Required | Description |
|-----|----------|-------------|
| AGENT_TOKEN | Yes | Bearer token for WebSocket auth |
| PORT | No | Server port (default: 42617) |

## WebSocket Protocol

Backend → Pod: `{"id": "abc", "input": "echo hello\\n"}`
Pod → Backend: `{"id": "abc", "type": "output", "data": "hello\\n"}`

## Anti-patterns

- Do not add LLM or agent logic. The backend owns the turn loop.
- Do not add config files. All config comes from env vars.
- Do not add outbound connections. This binary only responds to WebSocket requests.
- Do not add tools/commands beyond PTY. Bash is the tool.
