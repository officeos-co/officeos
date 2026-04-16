# EnterpriseAgentOS

Kubernetes-native platform for running autonomous AI agents. Single-tenant, self-hosted.

---

## How to work in this repo

**Trust the documentation, not the codebase.** These CLAUDE.md files document every convention, every pattern, every naming rule, and every anti-pattern. Read the relevant CLAUDE.md before touching code — do not explore the codebase to learn conventions. The docs are the source of truth for how to work here.

**Do not read files to understand architecture.** The mental model below and the sub-package CLAUDE.md files explain every data flow, every layer boundary, and every structural pattern. You should not need to read source files to understand how things connect — only to understand specific implementation details when making a change.

**Keep documentation current.** After any change that adds, removes, or modifies a convention, a structural pattern, a naming rule, a new domain/hook/entity, or a new package — update the relevant CLAUDE.md in the same commit. Documentation that falls behind is worse than no documentation. If you added a new backend domain, add it to the domain table. If you added a new dashboard hook, add it to the hook table. If you changed how DI works, update the DI section. This is not optional.

---

## Start here — read the relevant CLAUDE.md before touching any code

| Package | Role | CLAUDE.md |
|---------|------|-----------|
| `apps/dashboard/` | The product — operators manage agents, skills, providers here | `apps/dashboard/CLAUDE.md` |
| `apps/backend/` | Central orchestrator — all state, credentials, K8s control, LLM proxy | `apps/backend/CLAUDE.md` |
| `apps/website/` | Public landing page only — no backend, pure marketing | `apps/website/CLAUDE.md` |
| `apps/changelog/` | Public changelog — reads `.md` files, no backend, pure static | no CLAUDE.md needed |
| `packages/zeroclaw-core/` | Rust agent binary — runs inside each K8s pod | `packages/zeroclaw-core/CLAUDE.md` |
| `packages/skills/` | First-party TypeScript skill packages | `packages/skills/CLAUDE.md` |
| `packages/skill-sdk/` | `@harro/skill-sdk` — `defineSkill`, Zod, type interfaces | `packages/skill-sdk/CLAUDE.md` |
| `packages/skill-runtime/` | Node.js service that executes skills and serves the HTTP skill API | `packages/skill-runtime/CLAUDE.md` |
| `changelog/` | Changelog `.md` content files — read by `apps/changelog/` at build time | no CLAUDE.md needed |
| `k8s/` | Kubernetes manifests only — `backend.yaml`, `frontend.yaml`, `website.yaml`, `changelog.yaml` | no CLAUDE.md needed |
| `docs/` | External user-facing documentation — not developer docs, not agent context | no CLAUDE.md needed |

---

## System mental model

```
Browser
  └─► apps/dashboard/          Next.js — the product UI
        └─► apps/backend/       C# ASP.NET Core — single orchestrator, all credentials live here
              ├─► Postgres       EF Core — agents, providers, skills, sessions, browser cookies
              │                  Schema source of truth: apps/backend/Database/Models/
              ├─► CouchDB        Per-agent vault — personality files (SOUL.md, IDENTITY.md, AGENTS.md)
              ├─► K8s API        Spawns/terminates agent pods, reads live pod status
              └─► skill-runtime  Node.js — executes TypeScript skills, exposes HTTP API

backend spawns pods running ──► packages/zeroclaw-core/  (Rust binary, image: harkro123/zeroclaw:latest)
  Pod boots with ZEROCLAW_AGENT_ID only. Calls backend to get everything:
  provider config, LLM proxy endpoint, GraphQL skill gateway, vault personality files.

apps/website/    Next.js — public landing page. No backend connection. No auth.
apps/changelog/  Next.js — public changelog. Reads .md files. No backend. No auth.
```

**Dashboard = the product.** All operator workflows go through it.  
**Website = advertising.** Completely separate. No shared code with dashboard.  
**Backend = single source of truth.** Credentials never leave it.  
**Agent pods = dumb terminals.** One env var, everything else fetched on boot.  
**Skills = TypeScript npm packages.** Run in skill-runtime, not in the pod.  
**Database schema = C#.** `apps/backend/Database/Models/` + EF Core migrations. Always create and apply a migration when changing any model.

---

## Skill execution flow

```
Agent pod (Rust)
  → skill_exec tool — parses CLI command, sends GraphQL query to backend
  → backend SkillTypeModule — dynamic HotChocolate schema, no hardcoded skill knowledge in C#
  → skill-runtime POST /execute — Zod validates params, injects decrypted credentials + sandboxed fetch
  → TypeScript skill action — calls real API/CLI/SDK, returns result
```

---

## CI/CD — push to main, everything deploys automatically

No manual build or deploy commands ever.

| Workflow | Triggers on | Builds | Deploys |
|----------|-------------|--------|---------|
| `deploy-backend-prod.yml` | `apps/backend/**`, `k8s/backend.yaml` | Tests → `harkro123/eaos-backend:latest` | `kubectl rollout restart deployment/eaos-backend-prod` |
| `deploy-dashboard-prod.yml` | `apps/dashboard/**`, `k8s/frontend.yaml` | `harkro123/eaos-frontend:latest` | `kubectl rollout restart deployment/eaos-frontend-prod` (legacy — dormant) |
| `deploy-dashboard-2-prod.yml` | `apps/dashboard-2/**`, `k8s/dashboard-2.yaml` | `harkro123/eaos-dashboard-2:latest` | `kubectl rollout restart deployment/eaos-dashboard-2-prod` |
| `deploy-website-prod.yml` | `apps/website/**`, `k8s/website.yaml` | `harkro123/eaos-website:latest` | `kubectl rollout restart deployment/eaos-website-prod` |
| `deploy-changelog-prod.yml` | `apps/changelog/**`, `k8s/changelog.yaml` | `harkro123/eaos-changelog:latest` | `kubectl rollout restart deployment/eaos-changelog-prod` |
| `build-zeroclaw-image.yml` | `packages/zeroclaw-core/**` | `harkro123/zeroclaw:latest` | No deploy — new pods pick up `:latest` on next spawn |
| `build-skill-runtime.yml` | `packages/skill-runtime/**`, `packages/skill-sdk/**`, `packages/skills/**` | `harkro123/eaos-skill-runtime:latest` | Rollout restart + seed manifests to backend DB via `POST /api/internal/seed-manifests` |
| `publish-skill-sdk.yml` | `packages/skill-sdk/**` | — | npm publish `@harro/skill-sdk` |
| `sync-skill-repos.yml` | `packages/skills/**` | — | Syncs first-party skills to their individual repos |

CI connects to the cluster via **Tailscale** + `kubectl` with `KUBE_TOKEN`. All images push to Docker Hub under `harkro123/` with `:latest` only.

---

## Conventions

- **Commit after each stage** of multi-step work. Each commit must leave the codebase in a working state. Prefer 3 solid commits over 10 small ones.
- **One concern per PR.**
- **No K8s env vars for app config.** Use `appsettings.json` baked into the Docker image.
- **Image registry:** Docker Hub `harkro123/`, `:latest` tag only — no SHA tags.
- **Prod hostnames:** `dashboard.officeos.co` (dashboard), `api.officeos.co` (backend), `officeos.co` (website), `changelog.officeos.co` (changelog).
- **Schema migrations:** always create and apply an EF Core migration when changing `apps/backend/Database/Models/`.
- **Update the relevant CLAUDE.md in the same commit** when adding a new domain, hook, entity, convention, or structural pattern. If the docs don't reflect the code, the next agent will do it wrong.

---

## Skill rules

- **No channel skills.** Slack, Discord, Teams, Telegram, WhatsApp, Twilio, iMessage — native channel integrations handle messaging. Never build skills for them.
- **No prompt-only skills.** Every skill must call a real API, CLI, or SDK. Planning templates and review checklists are agent personality — they belong in the agent's system prompt, not the skill registry.
- **Spec-driven order:** (1) `SKILL.md` spec, (2) tests, (3) implementation.
