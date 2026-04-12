# EnterpriseAgentOS

Kubernetes-native platform for autonomous AI agents. Single-tenant, self-hosted. Trying to get into ycombinator being the intellifence layer for Agents. I see the product as the obvious next iteration after openclaw. Problems of openclaw are that an agent is just a virtrual machine which you have to ssh into really often not just for initialisation or go torugh the web ui. Agents dont know each other. Tools arent centrally logged, tools cant be shared. So our Architecture is the solution. Agents are mostly decoupled in the cloud hosted as npm packages exposing a graphql api. However we still have all the capabilties of openclaw unlike claude managed agents where agents run entirely in the cloud and thus can only access mcp servers. Our architecture can control any software which exposes a graphql api. And still has full system control.
Further context about what we are buildig is in /Users/harrokrog/Documents/Optimierung/Office OS.md for the website we orientate on /Users/harrokrog/Documents/Optimierung/References/YC Website Blueprint.md and we evaluate based on /Users/harrokrog/Documents/Optimierung/Clipping/YC Landing Page Teardown 50 Lessons.md

## Repository layout

```
apps/v2-backend/         C# ASP.NET Core 9 — agent lifecycle, LLM proxy, skill gateway, vault, K8s orchestration
apps/v2-frontend/        Next.js 16 + React 19 — Mission Control dashboard
apps/website/            Next.js 15 + Bun + shadcn — public landing page (harrokrog.com)
packages/zeroclaw-core/  Rust agent runtime — turn loop, tool execution, memory, channels
packages/skill-sdk/      @harro/skill-sdk — TypeScript SDK for defining skills (defineSkill + Zod)
packages/skill-runtime/  Node.js skill execution service — loads bundled skills, exposes HTTP API
packages/skills/         First-party skills (notion, github, google, obsidian) built with the SDK
packages/obsctl/         Python CLI for Obsidian vault operations (CouchDB backend)
k8s/                     Kubernetes manifests (backend.yaml, frontend.yaml, website.yaml)
docs/                    Architecture and system documentation
```

## Architecture (one paragraph)

User opens the dashboard, creates an agent. The backend provisions a CouchDB vault (personality files), creates a K8s pod running the zeroclaw Rust binary, and stores the agent record in Postgres. The agent pod boots with only `ZEROCLAW_AGENT_ID`, hydrates its workspace from CouchDB, discovers skills via GraphQL introspection, and serves a WebSocket chat gateway. All LLM calls route through the backend's proxy — credentials never leave the backend. Skills are TypeScript modules defined with `@harro/skill-sdk` and executed in a separate Node.js skill-runtime service. The backend generates a dynamic GraphQL schema from runtime manifests via `SkillTypeModule` (HotChocolate `ITypeModule`) — no hardcoded skill knowledge in C#. The agent calls skills through a single `skill_exec` tool that presents a CLI-like interface over GraphQL.

## Key design decisions

- **Credentials never leave the backend.** Agent pods have no API keys. LLM calls and skill executions are proxied through the backend which injects credentials per-request.
- **Single env var deployment.** Agent pods receive only `ZEROCLAW_AGENT_ID`. Everything else (provider, model, vault, skills) is derived from that ID by calling the backend.
- **CouchDB is the vault source of truth.** Personality files live in per-agent CouchDB databases, cached on the pod's PVC.
- **GraphQL skill gateway.** Skills are defined in TypeScript (`@harro/skill-sdk`), executed in a separate Node.js skill-runtime. The backend generates GraphQL types dynamically from runtime manifests via `SkillTypeModule` (`ITypeModule`). Agents discover skills via introspection and call them through a CLI-style `skill_exec` tool.
- **Status is live.** `GET /api/agents` calls K8s API inline to refresh pod status. Frontend polls every 10s.
- **CICD handles everything** no manual commands need to be done. CICD handles building, testing and deploying.

## Conventions

- Commit after each stage of multi-step work.
- One concern per PR.
- No K8s env vars for app config — use `appsettings.json` baked into the image.
- Docker images push to Docker Hub under `harkro123/` — `:latest` tag only, no SHA tags.
- Prod hostnames: `dashboard.harrokrog.com` (frontend), `api.harrokrog.com` (backend).
- Update docs/ if changes have been done or major feature has been added. Same for CLAUDE.md prompt if its relevant to the prompt
- When working on long running tasks do iterative commits
