# Agent Execution Architecture v2

Replaces the Rust `agent-core` binary with two decoupled components: a lightweight Go PTY server in the pod (cloned from GoTTY) and the agent turn loop in the C# backend.

## Problem

The current Rust agent-core binary runs the full agent loop inside the pod: bootstrap, personality seeding, prompt composition, LLM calls, tool dispatch, and WebSocket gateway. This causes:

- **Crash loops with no observability** — if bootstrap fails (e.g. empty systemPrompt), the pod crashes before the gateway starts. The backend never learns why.
- **Rust compile/test overhead** — slow iteration for what is fundamentally I/O forwarding.
- **Fragile boot sequence** — 10-retry bootstrap with exponential backoff before the pod is reachable.
- **Debugging difficulty** — errors happen inside a Rust binary in a K8s pod with no log forwarding.

## Core Principle

**Pod = dumb black box.** A PTY exposed over WebSocket. The backend sends bash strings, gets stdout/stderr back. No knowledge of agents, prompts, LLMs, memory, or sessions. No state, no push, no initiative.

**Backend = the brain.** Owns the turn loop, prompt composition, memory, conversation state, tool routing, and observability. The LLM's tool calls (file_read, shell, grep, etc.) are all translated to bash commands by the backend before being sent to the pod.

**One tool: Bash.** The agent sees a single `bash` tool. The backend sends the command string to the pod's PTY. This is bash-complete by definition — anything bash can do, the agent can do. No separate file_read/file_write/glob/grep tools polluting the LLM's context.

## Architecture

```
Dashboard/Channel
  └─► Backend (C# ASP.NET Core)
        │
        │  Agent Turn Loop (async Task per turn):
        │    1. Compose prompt (personality + memory from Postgres)
        │    2. Call LLM (SSE stream via existing proxy)
        │    3. Parse tool calls
        │    4. Route tool:
        │         bash             →  Send command string to Pod PTY via WebSocket
        │         skill_exec       →  Skill Runtime (existing)
        │         memory_*         →  Postgres directly
        │    5. Collect results, loop to step 1
        │
        ├─► Postgres (conversation state, memory, personality files)
        ├─► Skill Runtime (existing, unchanged)
        └─► Agent Pod (Go PTY Server)

Agent Pod (Go binary, cloned from GoTTY):
  ┌──────────────────────────┐
  │  WebSocket Server :42617 │
  │  PTY (/bin/bash)         │
  │  stdin ← WebSocket       │
  │  stdout/stderr → WS      │
  │  Auth: Bearer token      │
  │  No state, no push       │
  └──────────────────────────┘
```

## Go PTY Server

### Location

`packages/pod-executor/` — Go module, cloned from [GoTTY](https://github.com/yudai/gotty) and refactored top-down.

### What we keep from GoTTY

- PTY spawning (`kr/pty` or `creack/pty`)
- WebSocket server (`gorilla/websocket`)
- stdin/stdout bridging between WebSocket and PTY

### What we remove from GoTTY

- xterm.js frontend / HTML serving
- HTTP server (only WebSocket)
- TLS termination (K8s handles this)
- Client authentication via HTTP (we use token in WS handshake)
- Preferences/arguments system
- Reconnect logic
- All browser-facing features

### What we add

- Bearer token auth on WebSocket handshake
- Structured message framing (see Protocol below)
- Graceful shutdown

### What the Go binary does NOT do

- No LLM calls
- No prompt composition
- No memory management
- No bootstrap/config fetching
- No personality file seeding
- No knowledge of agents, sessions, or users
- No outbound connections
- No Playwright/browser — that is a separate, decoupled system

### Binary and Image

- Single static Go binary, target ~10-15MB
- Base image: `alpine:latest` (~5MB)
- Standard tools installed in image: `bash`, `curl`, `git`, `python3` (as needed by agents)
- Total image target: significantly under 100MB
- Image registry: `harkro123/eaos-pod-executor:latest`

## WebSocket Protocol

PTY-over-WebSocket with structured framing. Backend sends command strings, pod streams PTY output back.

### Command (Backend → Pod)

```json
{ "id": "abc123", "input": "cat /etc/hostname\n" }
```

The `id` correlates the command with its output. The `input` is written directly to the PTY's stdin. The `\n` is the enter key — the PTY sees it as a command submission.

### Output (Pod → Backend, streaming)

```json
{"id": "abc123", "type": "output", "data": "my-pod-name\n"}
{"id": "abc123", "type": "output", "data": "$ "}
```

The pod streams raw PTY output as it arrives. The backend detects command completion by watching for the shell prompt (configurable marker).

### Completion Detection

The PTY is a raw terminal — there is no explicit "done" signal. The backend detects completion by:

1. Setting a known `PS1` prompt marker at session start (e.g. `PS1='__EAOS_DONE:$?__\n$ '`)
2. Watching output for the marker pattern
3. Extracting the exit code from the marker

### Errors

PTY-level errors (command not found, permission denied) appear naturally in stdout/stderr. The exit code in the prompt marker tells the backend if the command succeeded.

### Authentication

Bearer token in WebSocket URL: `ws://pod:42617/ws?token={agent_id}`. Pod validates against `AGENT_TOKEN` env var. Rejects connection if token doesn't match.

## Agent Turn Loop (Backend)

### Execution Model

- Async `Task` per turn in the ASP.NET process
- Stateless — no queue, no persistent worker
- If the backend restarts, in-flight turns are lost (industry standard, acceptable)
- Backend pods scale horizontally; each can run many concurrent turns

### Tool Translation

The LLM sees a single `bash` tool. The backend sends the command string directly to the pod PTY. Examples:

| LLM tool call                  | Sent to PTY              |
| ------------------------------ | ------------------------ |
| `bash("cat /app/config.json")` | `cat /app/config.json\n` |
| `bash("find . -name '*.ts'")`  | `find . -name '*.ts'\n`  |
| `bash("npm install express")`  | `npm install express\n`  |
| `bash("python3 train.py")`     | `python3 train.py\n`     |

No translation layer, no mapping. The string goes straight to the PTY.

### Connection to Pod

- Connection-per-turn: open WebSocket when turn starts, close when turn ends
- No persistent connection management
- Pod address: `ws://{podName}.default.svc.cluster.local:42617/ws?token={agentId}`

### Memory

- Personality files (SOUL.md, IDENTITY.md, BOOTSTRAP.md content) stored in Postgres
- `memory_store`, `memory_recall`, `memory_forget` operate on Postgres directly
- No files on the pod's PVC for memory or personality
- Prompt composition happens in the backend using data from Postgres

### Conversation State

- Full conversation history in Postgres
- Backend composes the prompt fresh each turn from personality + memory + conversation history

## Pod Lifecycle

- 1 Pod = 1 Agent (unchanged from current architecture)
- Pod runs as long as the agent is active (unchanged)
- On backend restart: pod stays alive, in-flight turns lost, next message reconnects
- Scale-to-zero optimization is a future project, not in scope

## Security

- Bearer token: pod receives `AGENT_TOKEN` as env var, validates on WebSocket handshake
- Kubernetes Network Policy: pods only reachable from backend (additional layer)
- No mTLS (unnecessary for intra-cluster)

## What Gets Removed

| Current                                       | Replacement                                      |
| --------------------------------------------- | ------------------------------------------------ |
| `packages/agent-core/` (Rust)                 | `packages/pod-executor/` (Go, cloned from GoTTY) |
| Bootstrap sequence (10-retry config fetch)    | Pod needs no config — just token env var         |
| Personality file seeding to PVC               | Personality in Postgres, composed by backend     |
| Prompt composition in Rust                    | Prompt composition in C# backend                 |
| LLM proxy call from pod                       | LLM call from backend (already has the proxy)    |
| `include_str!` embedded templates             | Templates in Postgres or backend code            |
| WebSocket gateway with agent logic            | Dumb PTY-over-WebSocket                          |
| 12+ agent tools (file_read, file_write, etc.) | Single `bash` tool — bash-complete               |

## Migration Path

1. Clone GoTTY, refactor to minimal PTY-over-WebSocket server with auth
2. Build Docker image (`harkro123/eaos-pod-executor:latest`)
3. Add agent turn loop to backend (new Application service)
4. Add memory/personality tables to Postgres (migration)
5. Update `KubernetesAgentDeployer` to deploy new image
6. Update message delivery to use new turn loop instead of forwarding to pod
7. Remove `packages/agent-core/`
8. Update CI: remove `build-zeroclaw-image.yml`, add `build-pod-executor.yml`

## Kubernetes Scaling

- **Backend pods** scale horizontally — each handles many concurrent agent turns as async tasks
- **Agent pods** are 1:1 with agents — scale by number of active agents
- Go PTY server is ultra-lightweight (~5MB RSS) — many agent pods per node
- No shared state between backend pods (Postgres is the shared state)
