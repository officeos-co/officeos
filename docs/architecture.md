# Architecture

> System design for EnterpriseAgentOS — the map that tells you where everything lives.

## System overview

EnterpriseAgentOS is a single-tenant, Kubernetes-native platform for autonomous AI agents. A user creates an agent in the web dashboard, and that agent becomes a real K8s pod running a Rust binary. The system is designed around three principles: **credentials never touch agents**, **pods are self-bootstrapping**, and **state is infrastructure-managed**.

```
┌──────────────────────────────────────────────────────┐
│  Dashboard (Next.js 16)                               │
│  dashboard.harrokrog.com                              │
│  - Agent CRUD, chat, logs, memory editor              │
│  - Skill marketplace (install/configure)              │
│  - Provider management (API keys)                     │
└────────────────────┬─────────────────────────────────┘
                     │ REST + WebSocket
                     ▼
┌──────────────────────────────────────────────────────┐
│  Backend (C# ASP.NET Core 9)                          │
│  api.harrokrog.com                                    │
│  - Agent lifecycle (K8s pod/svc/pvc management)       │
│  - LLM proxy (/v1/chat/completions)                   │
│  - Skill gateway (GraphQL at /api/graphql)            │
│  - Vault provisioning (CouchDB)                       │
│  - WebSocket proxy to agent pods                      │
│  - Postgres for all persistent state                  │
└──┬──────────────┬──────────────┬─────────────────────┘
   │              │              │
   ▼              ▼              ▼
┌────────┐  ┌──────────┐  ┌──────────────────┐
│ K8s    │  │ CouchDB  │  │ PostgreSQL       │
│ cluster│  │ (vaults) │  │ (agents,         │
│        │  │ 1 db per │  │  providers,      │
│        │  │  agent   │  │  skill creds)    │
└──┬─────┘  └──────────┘  └──────────────────┘
   │
   ▼
┌──────────────────────────────────────────────────────┐
│  Agent Pod (one per agent)                            │
│  image: harkro123/zeroclaw:latest                     │
│  env: ZEROCLAW_AGENT_ID=<uuid>  (the only env var)   │
│                                                       │
│  Boot: derives everything from backend via agent ID   │
│  - LLM calls → backend proxy (no direct API keys)    │
│  - Skills → GraphQL introspection + execution         │
│  - Vault → fetched from backend memory proxy          │
│  - WebSocket gateway on :42617                        │
│  - PVC at /zeroclaw-data for workspace persistence    │
└──────────────────────────────────────────────────────┘
```

## Components

### Dashboard (`apps/v2-frontend/`)

Next.js 16 + React 19 + Tailwind CSS. Routes:
- `/agents` — agent list (grid, polling, delete)
- `/agents/{id}` — agent detail (tabs: overview, chat, sessions, memory, crons, cost, tools, skills, doctor, config, logs)
- `/skills` — skill grid (install, configure, detail pages)
- `/providers` — provider API key management

Communicates with the backend via REST (`apiFetch`) and WebSocket (`agentWsUrl`). Agent pod data (chat, logs, sessions) is proxied through the backend — the dashboard never talks to pods directly.

### Backend (`apps/v2-backend/`)

C# ASP.NET Core 9. The central orchestrator:

| Responsibility | How |
|---------------|-----|
| Agent CRUD | `AgentService` + `AgentRepository` (Postgres) |
| K8s management | `KubernetesAgentDeployer` (pods, services, PVCs) |
| LLM proxy | `/v1/chat/completions` — injects real provider credentials per-request |
| Skill gateway | HotChocolate GraphQL at `/api/graphql` with agent-token auth |
| Vault provisioning | `CouchDbVaultClient` creates per-agent databases + personality files |
| Status sync | `GetStatusAsync` calls K8s API inline on every agent fetch |
| WebSocket proxy | `AgentProxyEndpoints` relays browser ↔ pod WebSocket frames |

Config pattern: `ValueManager` reads `appsettings.json` in `Program.cs` only. All other code receives typed config classes (`KubernetesConfig`, `CouchDbConfig`, `SkillGatewayConfig`) via DI.

### Agent Runtime (`packages/zeroclaw-core/`)

Rust binary (~97k LOC). Runs inside each agent pod:

| Module | Purpose |
|--------|---------|
| `agent/gateway_bootstrap.rs` | Derives all config from `ZEROCLAW_AGENT_ID` + backend URL |
| `agent/vault_bootstrap.rs` | Fetches personality files from backend, caches on PVC |
| `agent/personality.rs` | Strict loader — fails loudly if required files missing |
| `agent/agent.rs` | Turn loop, system prompt, tool execution |
| `tools/skill_exec/` | GraphQL-backed CLI tool for skill discovery + execution |
| `gateway/` | WebSocket server on :42617 |
| `providers/` | LLM provider routing (custom proxy to backend) |

### Data stores

| Store | Purpose | Persistence |
|-------|---------|-------------|
| PostgreSQL | Agents, providers, skill credentials | `eaos-postgres` Deployment + 10Gi PVC |
| CouchDB | Per-agent vault (SOUL.md, IDENTITY.md, AGENTS.md) | `eaos-couchdb` Deployment + 20Gi PVC |
| PVC per agent | Workspace cache, session state | 1Gi per agent pod |

## Key data flows

### Agent creation

See [Agent Lifecycle](agent-lifecycle.md) for the full step-by-step.

### Chat message

```
Browser → wss://api.harrokrog.com/api/agents/{id}/ws
  → Backend proxy → ws://zeroclaw-{id}.default.svc.cluster.local:42617/ws/chat
    → zeroclaw gateway receives message
    → Agent turn loop:
      1. Build system prompt (personality + skills)
      2. Call LLM via backend proxy (POST /v1/chat/completions)
      3. If tool call: execute via skill_exec (GraphQL) or local tool
      4. Stream response chunks back via WebSocket
    → Browser renders streaming response
```

### Skill execution

See [Skill System](skills.md) for the full architecture.

## Deployment

### K8s manifests

- `k8s/backend.yaml` — RBAC, Postgres, CouchDB, backend Deployment + Service + PVC
- `k8s/frontend.yaml` — frontend Deployment + Service

### CI/CD

Three GitHub Actions workflows:
- `build-zeroclaw-image.yml` — builds + pushes `harkro123/zeroclaw:latest`
- `deploy-backend-prod.yml` — builds + pushes `harkro123/eaos-backend:latest`, applies manifest, restarts
- `deploy-dashboard-prod.yml` — builds + pushes `harkro123/eaos-frontend:latest`, applies manifest, restarts

All workflows connect to the k3s cluster via Tailscale.

### Networking

- Frontend + backend exposed externally via **Cloudflare Tunnel** (no LoadBalancer, no Ingress)
- Agent pods are ClusterIP only — accessible via the backend's WebSocket proxy
- CouchDB + Postgres are ClusterIP (in-cluster only)

## Design constraints

- **Single-tenant.** One workspace, one cluster. No multi-tenancy.
- **No secrets on pods.** Agent pods receive exactly one env var. Everything else comes from the backend.
- **Skills are global.** All agents share the same configured skills.
- **`:latest` tags only.** No SHA-pinned image tags.
- **EnsureCreatedAsync, not migrations.** Schema changes require a DB wipe during active development.
