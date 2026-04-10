# EnterpriseAgentOS

Kubernetes-native platform for autonomous AI agents. Single-tenant, self-hosted.

## Repository layout

```
apps/v2-backend/         C# ASP.NET Core 9 — agent lifecycle, LLM proxy, skill gateway, vault, K8s orchestration
apps/v2-frontend/        Next.js 16 + React 19 — Mission Control dashboard
packages/zeroclaw-core/  Rust agent runtime — turn loop, tool execution, memory, channels
packages/obsctl/         Python CLI for Obsidian vault operations (CouchDB backend)
k8s/                     Kubernetes manifests (backend.yaml, frontend.yaml)
docs/                    Architecture and system documentation
```

## Architecture (one paragraph)

User opens the dashboard, creates an agent. The backend provisions a CouchDB vault (personality files), creates a K8s pod running the zeroclaw Rust binary, and stores the agent record in Postgres. The agent pod boots with only `ZEROCLAW_AGENT_ID`, hydrates its workspace from CouchDB, discovers skills via GraphQL introspection, and serves a WebSocket chat gateway. All LLM calls route through the backend's proxy — credentials never leave the backend. Skills are backend-side C# implementations exposed via a GraphQL endpoint; the agent calls them through a single `skill_exec` tool that presents a CLI-like interface.

## Key design decisions

- **Credentials never leave the backend.** Agent pods have no API keys. LLM calls and skill executions are proxied through the backend which injects credentials per-request.
- **Single env var deployment.** Agent pods receive only `ZEROCLAW_AGENT_ID`. Everything else (provider, model, vault, skills) is derived from that ID by calling the backend.
- **CouchDB is the vault source of truth.** Personality files live in per-agent CouchDB databases, cached on the pod's PVC.
- **GraphQL skill gateway.** Skills are HotChocolate query types. Agents discover them via introspection and call them through a CLI-style `skill_exec` tool.
- **Status is live.** `GET /api/agents` calls K8s API inline to refresh pod status. Frontend polls every 10s.

## Commands

```bash
# Backend
cd apps/v2-backend && dotnet build

# Frontend
cd apps/v2-frontend && npm run dev
cd apps/v2-frontend && npx tsc --noEmit

# Zeroclaw
cd packages/zeroclaw-core && cargo build
cd packages/zeroclaw-core && cargo test
cd packages/zeroclaw-core && cargo clippy --all-targets -- -D warnings

# Deploy
kubectl apply -f k8s/backend.yaml
kubectl apply -f k8s/frontend.yaml
```

## Configuration

Backend config lives in `apps/v2-backend/appsettings.json` (Production / Staging sections).
`ValueManager` is only called in `Program.cs`. Downstream code receives typed config classes from `Properties/` via DI.

## Conventions

- Commit after each stage of multi-step work.
- One concern per PR.
- No K8s env vars for app config — use `appsettings.json` baked into the image.
- Docker images push to Docker Hub under `harkro123/` — `:latest` tag only, no SHA tags.
- Prod hostnames: `dashboard.harrokrog.com` (frontend), `api.harrokrog.com` (backend).
