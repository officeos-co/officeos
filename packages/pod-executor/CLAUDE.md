# pod-executor — Go Sandbox Toolbox

Minimal Go binary that exposes shell execution and file operations inside each
agent pod. The primary backend interface is a Daytona-like REST toolbox; the
WebSocket PTY endpoint remains available for future interactive terminal work.

## Commands

```bash
go test -v ./...
go build -o pod-executor .
docker build -t harkro123/eaos-pod-executor:latest .
```

## What this binary does

- HTTP server on PORT (default 42617)
- Executes `/bin/bash -lc` commands through `POST /process/execute`
- Reads, writes, and creates files through `/files/*` endpoints
- Keeps a WebSocket PTY endpoint at `/ws`
- Auth: AGENT_TOKEN env var, validated from `Authorization: Bearer` for REST and `?token=` for WebSocket
- Health check: GET /health

## What this binary does NOT do

No LLM calls, no prompt composition, no memory, no bootstrap, no config fetching, no personality files, no knowledge of agents/sessions/users, no Playwright/browser.

## Env vars

| Var         | Required | Description                     |
| ----------- | -------- | ------------------------------- |
| AGENT_TOKEN | Yes      | Bearer token for internal auth  |
| PORT        | No       | Server port (default: 42617)    |

## REST Protocol

Backend → Pod: `POST /process/execute`

```json
{"command":"echo hello","cwd":"/workspace","timeout":10}
```

Pod → Backend:

```json
{"result":"hello\n","exitCode":0}
```

File endpoints:

- `GET /files/download?path=/workspace/file.txt`
- `POST /files/upload?path=/workspace/file.txt`
- `POST /files/folder?path=/workspace&mode=0755`

## WebSocket Protocol

Backend → Pod: `{"id": "abc", "input": "echo hello\\n"}`
Pod → Backend: `{"id": "abc", "type": "output", "data": "hello\\n"}`

## Anti-patterns

- Do not add LLM or agent logic. The backend owns the turn loop.
- Do not add config files. All config comes from env vars.
- Do not add outbound connections. This binary only responds to backend requests.
- Do not add tools/commands beyond shell and file operations. Bash is the tool.
