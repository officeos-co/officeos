# EnterpriseAgentOS

> Deploy autonomous AI agents on Kubernetes — from your browser.

## Highlights

- **One click, one pod.** Create an agent from the dashboard and it becomes a real Kubernetes pod in seconds.
- **Credentials never touch the agent.** LLM calls and skill executions are proxied through the backend. Agent pods have zero secrets.
- **Skills as a service.** Connect Notion, GitHub, or Google — agents discover and use them through a CLI-like GraphQL interface. No SDK, no container per skill.
- **Persistent memory.** Each agent has its own CouchDB vault with personality files, editable from the dashboard. Workspace state survives pod restarts on a PVC.
- **Real-time everything.** WebSocket chat with reconnect, SSE log streaming, 10s status polling with live K8s pod phase sync.

## Overview

EnterpriseAgentOS is a self-hosted platform for running autonomous AI agents on Kubernetes. It's built for teams that want full control over their agent infrastructure — no SaaS dependency, no vendor lock-in, no shared tenancy.

The system has three main components: a **Next.js dashboard** for managing agents and skills, a **C# backend** that orchestrates everything (K8s pods, LLM proxy, skill gateway, vault provisioning), and a **Rust agent runtime** that runs inside each pod and does the actual thinking.

```
Dashboard (Next.js)  ──REST/WS──▶  Backend (C# .NET)  ──K8s API──▶  Agent Pod (Rust)
                                        │                                │
                                        ├── Postgres (state)             ├── PVC (workspace)
                                        ├── CouchDB (vaults)             └── WebSocket :42617
                                        └── GraphQL (skills)
```

### How it works

1. User creates an agent in the dashboard (picks a name, provider, model).
2. Backend seeds a CouchDB vault with personality files, creates a K8s pod, and stores the record in Postgres.
3. The pod boots with a single env var (`ZEROCLAW_AGENT_ID`) and derives everything else from the backend at runtime.
4. Agent hydrates its workspace from CouchDB, discovers skills via GraphQL introspection, and starts serving chat on a WebSocket gateway.
5. All LLM calls go through the backend's proxy. The backend resolves the real provider and API key from the agent record.

## Usage

**Create an agent:**

Open `https://dashboard.harrokrog.com`, configure a provider API key, click "+ New agent", pick a model, done. The agent is live in ~20 seconds.

**Chat with it:**

Click the agent row → Chat tab → type a message. The agent responds in real-time via WebSocket with streaming chunks.

**Add a skill:**

Go to Skills → click Notion → Install → paste your Integration Token → Save. Within 30 seconds, every running agent can search and read your Notion workspace:

```
Agent: skill_exec("notion search --query 'meeting notes'")
→ [{id: "abc-123", title: "Q1 Meeting Notes", url: "https://notion.so/..."}]
```

## Repository structure

```
apps/backend/         C# ASP.NET Core 9 — the brain
apps/dashboard/        Next.js 16 + React 19 — the face
packages/zeroclaw-core/  Rust — the muscle (agent runtime)
packages/obsctl/         Python — Obsidian vault CLI
k8s/                     Kubernetes manifests
docs/                    Architecture documentation
```

## Getting started

### Prerequisites

- .NET 9 SDK, Node.js 22+, Rust stable, Docker, kubectl

### Local development

```bash
# Backend
cd apps/backend && dotnet run

# Frontend
cd apps/dashboard && npm install && npm run dev

# Zeroclaw (build only — needs K8s to create agent pods)
cd packages/zeroclaw-core && cargo build
```

### Production

Push to `main` triggers GitHub Actions that build Docker images and deploy to the K8s cluster via Tailscale.

```bash
kubectl apply -f k8s/backend.yaml
kubectl apply -f k8s/frontend.yaml
```

## Documentation

| Doc                                          | Description                                                   |
| -------------------------------------------- | ------------------------------------------------------------- |
| [Agent Lifecycle](docs/agent-lifecycle.md)   | Pod provisioning, vault hydration, status sync                |
| [Skill System](docs/skills.md)               | How skills work, adding new skills, GraphQL gateway           |
| [LLM Proxy](docs/llm-proxy.md)               | How every LLM call flows through the backend                  |
| [Vault System](docs/vault-system.md)         | CouchDB vaults, personality files, knowledge graph philosophy |
| [Zeroclaw Runtime](docs/zeroclaw-runtime.md) | Rust agent internals: boot, turn loop, tools                  |

## Tech stack

| Layer         | Technology                                                 |
| ------------- | ---------------------------------------------------------- |
| Dashboard     | Next.js 16, React 19, Tailwind CSS, lucide-react           |
| Backend       | C# / ASP.NET Core 9, EF Core, HotChocolate GraphQL, Npgsql |
| Agent runtime | Rust, tokio, reqwest, serde                                |
| State         | PostgreSQL 16, CouchDB 3                                   |
| Infra         | Kubernetes (k3s), Cloudflare Tunnel, GitHub Actions        |
| Images        | Docker Hub (`harkro123/*`)                                 |
