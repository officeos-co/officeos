# Office OS — System Architecture

> **Audience:** anyone picking up this monorepo for the first time. Read this before drilling into any package.
>
> **Scope:** the whole Office OS system — frontend, backend, agent runtime, vault, and Kubernetes deployment. Package-level details live in each package's own docs; this doc is the map that tells you where to go next.

## What Office OS is, in one paragraph

Office OS is a single-tenant, Kubernetes-native platform for running **autonomous AI agents**. A user opens the **Mission Control dashboard** (a Next.js web app), creates an agent, and that agent becomes a real Kubernetes pod running a Rust binary (`zeroclaw-core`). The pod reads its personality — who it is, who the user is, what tools it has, how it should behave — from an **Obsidian vault** (one vault per agent, stored in a CouchDB database) that was seeded by the **Python dashboard backend** at provisioning time. Once alive, the agent talks to LLM providers, calls tools, manages its own memory, and delivers replies through Telegram or generic webhooks. The whole system is designed to scale to *hundreds of agents*, so every design choice favours thin stateless runtimes, infrastructure-managed state, and boring reliability.

## The big picture

```
                ┌──────────────────────────────────────────────┐
                │  Mission Control Dashboard (Next.js app)      │
                │  apps/dashboard/frontend/                     │
                │  - Agent CRUD UI                              │
                │  - Live status, logs, approvals               │
                │  - Cypress tests                              │
                └───────────────┬──────────────────────────────┘
                                │ REST + WebSocket
                                │ /api/v1/*
                                ▼
                ┌──────────────────────────────────────────────┐
                │  Dashboard Backend (FastAPI + SQLModel)      │
                │  apps/dashboard/backend/                     │
                │  - Agent lifecycle (create / delete / sync)  │
                │  - Vault provisioning (via obsctl)           │
                │  - K8s Pod / Service / ConfigMap management  │
                │  - Postgres state: users, orgs, boards,      │
                │    agents, tasks, approvals, audit events    │
                │  - Clerk / Google OAuth                      │
                └──┬───────────────┬──────────────┬────────────┘
                   │               │              │
   kubernetes API  │   obsctl      │  VaultClient │
                   ▼               │              ▼
     ┌──────────────────┐          │  ┌──────────────────────┐
     │   K8s cluster    │          │  │  CouchDB (obsidian-  │
     │   - agent pods   │          │  │  livesync)           │
     │   - ConfigMaps   │          │  │  - 1 database / agent│
     │   - Services     │          │  │  - personality files │
     │   - PVCs         │          │  │  - wikilinks, tags   │
     └──┬───────────────┘          │  └──────────┬───────────┘
        │ volume mount              │             │ obsctl
        │ /vault-workspace         │             │ (Python CLI +
        ▼                           │             │  VaultClient API)
     ┌───────────────────────────┐  │             │
     │  Agent pod (1 per agent)  │◄─┘             │
     │  image: zeroclaw          │                │
     │                           │                │
     │  zeroclaw daemon          │                │
     │  ┌─────────────────────┐  │                │
     │  │ AgentCore           │  │                │
     │  │  - turn loop        │  │                │
     │  │  - prompt builder   │  │                │
     │  │  - tool dispatcher  │  │                │
     │  │  - memory (sqlite)  │  │                │
     │  │  - provider chain   │◄─┼────────────────┼──── LLM APIs
     │  │  - HTTP gateway     │  │                │     (Anthropic,
     │  └─────────────────────┘  │                │      OpenAI, ...)
     │                           │                │
     │  channels:                │                │
     │  - telegram               │◄───────────────┼──── Telegram Bot API
     │  - webhook                │                │
     │                           │                │
     │  reads: /vault-workspace  │                │
     │  (identity markdown)      │                │
     └───────────────────────────┘                │
                                                  │
                                   ┌──────────────┴────────────┐
                                   │  obsctl (Python package)  │
                                   │  packages/obsctl/         │
                                   │  - VaultClient Python API │
                                   │  - vault CLI              │
                                   │  - wikilinks / frontmatter│
                                   │  - tags / backlinks       │
                                   └───────────────────────────┘
```

## The five components

| Component | Path | Language | What it does |
| --- | --- | --- | --- |
| **Frontend** | `apps/dashboard/frontend/` | Next.js 16 + React 19 + Tailwind | Mission Control UI. Users create agents, review approvals, chat with agents, check status. Uses TanStack Query for data fetching and an Orval-generated API client. |
| **Backend** | `apps/dashboard/backend/` | FastAPI + SQLModel + Postgres | The orchestrator. Owns the database (users, orgs, boards, agents, approvals, activity events), provisions agents, seeds their vaults, talks to the Kubernetes API. Sole authority over who exists and what's allowed. |
| **zeroclaw-core** | `packages/zeroclaw-core/` | Rust | The agent runtime. One binary per agent pod. Reads its personality from a mounted ConfigMap, runs an LLM turn loop, calls tools, manages memory, delivers through Telegram / webhook. Trait-heavy, extension-friendly. |
| **obsctl** | `packages/obsctl/` | Python | The vault abstraction layer. Talks to CouchDB (obsidian-livesync) for per-agent vaults. The dashboard backend imports `VaultClient` directly (no CLI subprocess) to seed vault files. Handles wikilinks, tags, frontmatter, backlinks. |
| **KubernetesConfig** | `~/Desktop/KubernetesConfig/` (sibling repo) | YAML + Kustomize | Cluster manifests. Deploys the dashboard backend, frontend, CouchDB (obsidian-livesync), Postgres, metallb, tailscale, cloudflared, etc. Agent pods are **NOT** in static manifests — they're created dynamically by the dashboard backend's `K8sManager`. |

## Deployment model

Everything runs in a **single Kubernetes cluster** (self-hosted, MetalLB-backed). Three kinds of pods:

1. **Infrastructure pods** — CouchDB (obsidian-livesync), Postgres, rate limiters, Cloudflare tunnel, Tailscale operator. Declared as static YAML in the `KubernetesConfig` repo, applied via `kustomize`. Managed by the infra / ops team (you).

2. **Platform pods** — the dashboard frontend and backend. Also declared in `KubernetesConfig/prod/enterprise-agent-os/`. One replica each of `eaos-backend-prod` and `eaos-frontend-prod`. Rebuilt via Docker Hub image pushes.

3. **Agent pods** — *not* declared in YAML. Created on-demand by the dashboard backend's `K8sManager` (`apps/dashboard/backend/app/services/zeroclaw/k8s_manager.py`) via the Kubernetes Python client. One pod per agent, each with:
   - A `Pod` running the `zeroclaw` image with `command: zeroclaw daemon`
   - A `Service` exposing port 42617 (WebSocket + HTTP gateway)
   - A `ConfigMap` mounted read-only at `/vault-workspace` containing the agent's personality markdown files
   - A `PersistentVolumeClaim` mounted at `/zeroclaw-data` for the agent's local SQLite memory file (this goes away when memory is centralized — see Future Work)

The dashboard backend itself runs in-cluster and authenticates to the Kubernetes API via its pod's service account (`load_incluster_config()`).

## Two end-to-end flows

Two flows cover 90% of what Office OS does. If you understand these, you understand the system.

### Flow 1 — "A user creates an agent"

```
1. User clicks "New agent" in the Next.js frontend
   └─▶ apps/dashboard/frontend/src/app/agents/ — React component
   └─▶ POST /api/v1/agents  (via Orval-generated client)

2. FastAPI router receives the request
   └─▶ apps/dashboard/backend/app/api/agents.py
   └─▶ Authenticated via Clerk / Google OAuth → ActorContext
   └─▶ Delegates to AgentLifecycleService.create_agent()

3. AgentLifecycleService provisions everything
   └─▶ apps/dashboard/backend/app/services/openclaw/provisioning_db.py
   │
   ├─▶ Insert Agent row into Postgres (name, board, identity, soul,
   │   heartbeat config). status = "provisioning".
   │
   ├─▶ _seed_agent_vault():
   │   ├─▶ Render Jinja2 templates from apps/dashboard/backend/templates/
   │   │   (BOARD_SOUL.md.j2, BOARD_IDENTITY.md.j2, BOARD_AGENTS.md.j2,
   │   │    BOARD_USER.md.j2, BOARD_TOOLS.md.j2, BOARD_HEARTBEAT.md.j2,
   │   │    BOARD_BOOTSTRAP.md.j2, BOARD_MEMORY.md.j2)
   │   │   using the agent's context (user, board, goals)
   │   │
   │   ├─▶ vault_provisioning.provision_agent_vault(agent_id, rendered_files)
   │   │   ├─▶ obsctl.VaultClient.ensure_database("agent-{uuid}")
   │   │   │   → PUT /agent-{uuid} on CouchDB (creates new database)
   │   │   │
   │   │   └─▶ client.write_note(path, content) for each rendered file
   │   │       → PUTs the Obsidian note into CouchDB with YAML frontmatter
   │   │
   │   └─▶ vault_configmap.apply_agent_vault_configmap(agent_id, rendered_files)
   │       └─▶ Creates K8s ConfigMap "eaos-agent-{uuid}-vault" with
   │           the same rendered files as ConfigMap.data entries
   │
   ├─▶ K8sManager.create_container(agent_id, ...)
   │   ├─▶ Creates PVC "zeroclaw-data-{uuid}" (1Gi)
   │   ├─▶ Creates Pod "zeroclaw-{uuid}" with:
   │   │     - image: ghcr.io/zeroclaw-labs/zeroclaw:debian
   │   │     - command: zeroclaw daemon
   │   │     - env: API_KEY, PROVIDER, ZEROCLAW_WORKSPACE=/vault-workspace
   │   │     - volumeMounts:
   │   │         zeroclaw-data → /zeroclaw-data (PVC)
   │   │         vault-workspace → /vault-workspace (ConfigMap, read-only)
   │   └─▶ Creates Service "zeroclaw-{uuid}" exposing port 42617
   │
   └─▶ Update Agent row: status = "active", vault_database = "agent-{uuid}"

4. Agent pod boots
   └─▶ K8s schedules the pod, mounts the ConfigMap + PVC
   └─▶ Container starts: zeroclaw daemon
   └─▶ AgentCore::from_config reads config, then calls
       personality::load_personality_strict(&workspace_dir)
       → reads SOUL.md, IDENTITY.md, AGENTS.md, USER.md, ... from
         /vault-workspace (guaranteed present by K8s API before boot)
       → fails LOUDLY if any REQUIRED file (SOUL, IDENTITY, AGENTS)
         is missing → pod enters CrashLoopBackOff
   └─▶ HTTP gateway starts listening on 0.0.0.0:42617
   └─▶ Channels start listening (Telegram long-poll, webhook receiver)

5. Frontend polls /api/v1/agents/{agent_id}, sees status="active"
   └─▶ Agent is ready. User can chat with it.
```

Critical invariants from Phase 3:
- The K8s API **guarantees** the ConfigMap is present on the pod filesystem before the container starts. No race condition, no init container, no boot-time download.
- The vault is the **source of truth**. The ConfigMap is a refresh-on-restart cache. When the dashboard re-renders templates (template sync endpoint), both the vault and the ConfigMap are updated in lockstep.
- The agent **never** writes to the vault directly. Writes are always mediated by the dashboard backend. Agents are read-only consumers of identity.
- If required personality files are missing, the agent pod **fails loudly** (non-zero exit → CrashLoopBackOff → visible in the dashboard via existing status endpoints). No silent defaults.

### Flow 2 — "A Telegram message arrives"

```
1. User sends a message to their Telegram bot
   └─▶ Telegram Bot API sends update to the long-polling agent pod
       (or to a webhook if configured that way)

2. Agent pod's TelegramChannel receives the update
   └─▶ src/channels/telegram.rs — Channel::listen loop
   └─▶ Debouncer checks: skip if within debounce window
   └─▶ Forwards ChannelMessage through mpsc::Sender

3. channels::run_channels dispatcher picks it up
   └─▶ src/channels/mod.rs
   └─▶ Resolves sender → agent session
   └─▶ Acquires Arc<Mutex<Agent>> (one turn at a time per pod)
   └─▶ Calls Agent::turn(user_message)

4. Agent::turn runs one full LLM loop
   └─▶ src/agent/agent.rs:738
   │
   ├─▶ Lazy system prompt seed (first turn only)
   │   └─▶ SystemPromptBuilder::build() composes 9 sections:
   │       DateTime, Identity (reads SOUL/IDENTITY/AGENTS/...
   │       from /vault-workspace), ToolHonesty, Tools, Safety,
   │       Skills, Workspace, Runtime, ChannelMedia
   │
   ├─▶ MemoryLoader.load_context() recalls relevant memories
   │   └─▶ src/memory/sqlite.rs — FTS + vector + hybrid search
   │       against /zeroclaw-data/memory/brain.db
   │
   ├─▶ Auto-save user message (if enabled)
   │
   ├─▶ Enrich user_message with [CURRENT DATE] + memory context
   │
   ├─▶ Classify model (hint-based routing)
   │
   └─▶ Tool-calling inner loop, up to max_tool_iterations:
       ├─▶ Provider.chat(ChatRequest { messages, tools }, model, temp)
       │   └─▶ Usually router(reliable(concrete)) chain:
       │       RouterProvider → ReliableProvider → AnthropicProvider
       │       (or OpenAI, Ollama, OpenAiCompatibleProvider)
       │
       ├─▶ ToolDispatcher.parse_response() extracts tool calls
       │
       ├─▶ If zero tool calls: return final text → FLOW EXIT
       │
       └─▶ If tool calls: execute each
           ├─▶ Approval gate (ask_user if policy requires)
           ├─▶ Sandbox wrap (Landlock on Linux, noop elsewhere)
           ├─▶ Tool::execute(args) → ToolResult
           └─▶ Push results into history, loop back to provider call

5. Agent::turn returns final text
   └─▶ channels::run_channels dispatches to TelegramChannel::send
   └─▶ src/channels/telegram.rs formats + splits + sends via Bot API
   └─▶ User sees the reply in Telegram

6. Observer events fired throughout
   └─▶ Prometheus counters updated, OTLP spans exported (if configured)
   └─▶ Dashboard /api/v1/gateways/{id}/container/logs shows the trace
```

Critical invariants:
- **One mutex per agent pod** — at most one turn active at a time. The session queue (`src/gateway/session_queue.rs`) provides bounded FIFO + backpressure.
- **Safety valve** — if the inner loop hits `max_tool_iterations` without a final answer, the turn aborts. No infinite tool loops.
- **Tool errors are returned as results**, not Rust errors — the LLM reads the error and usually recovers.
- **History trimming** runs after each tool iteration. When it can't trim enough, `context_compressor` runs an LLM summarization pass.

## Where each thing lives (repo layout)

```
EnterpriseAgentOs/                        ← this monorepo
├── apps/
│   └── dashboard/                        ← Mission Control (web app)
│       ├── frontend/                     ← Next.js 16 + React 19
│       ├── backend/                      ← FastAPI + SQLModel + Postgres
│       ├── docs/                         ← dashboard-specific docs
│       ├── Makefile                      ← make setup / check / backend-test / ...
│       ├── compose.yml                   ← local dev (db + redis only)
│       └── k8s/                          ← dev-only manifests
│
├── packages/
│   ├── zeroclaw-core/                    ← Rust agent runtime
│   │   ├── src/                          ← ~97k LOC production code
│   │   │   ├── agent/                    ← AgentCore, turn loop, prompt builder
│   │   │   ├── providers/                ← LLM provider clients + wrappers
│   │   │   ├── channels/                 ← Telegram, Webhook, CLI
│   │   │   ├── memory/                   ← SQLite (default) + Obsidian + None
│   │   │   ├── tools/                    ← ~30 tools + MCP
│   │   │   ├── gateway/                  ← HTTP + WebSocket server
│   │   │   ├── security/                 ← SecurityPolicy, Sandbox, PairingGuard
│   │   │   ├── observability/            ← Observer, Prometheus, OTel, DORA
│   │   │   ├── config/                   ← Config struct + schema
│   │   │   └── ...                       ← cron, heartbeat, hooks, runtime, auth, ...
│   │   └── docs/
│   │       ├── architecture/overview.md  ← zeroclaw-core deep-dive
│   │       ├── reference/identity-vault.md
│   │       ├── reference/memory-future.md
│   │       └── reference/api/            ← CLI, config, providers, channels
│   │
│   └── obsctl/                           ← Obsidian vault CLI + Python API
│       ├── vault_cli/
│       │   ├── core/                     ← VaultClient, config, wikilinks
│       │   └── cli/                      ← read, write, search, etc.
│       └── SPEC.md
│
├── docs/
│   └── architecture.md                   ← THIS FILE (system-level)
│
├── STRIP_DOWN.md                         ← strip-down phases 1-5 log
└── README.md

~/Desktop/KubernetesConfig/               ← sibling repo for cluster manifests
├── infrastructure/
│   ├── obsidian-livesync/                ← CouchDB deployment
│   ├── metallb/                          ← load balancer IP pool
│   ├── tailscale/                        ← VPN operator
│   ├── cloudflared/                      ← external ingress
│   └── ...
├── prod/
│   └── enterprise-agent-os/              ← dashboard backend + frontend pods
├── staging/
└── deploy.sh
```

## State and data ownership

Every piece of state in Office OS has exactly one owner. Understanding this table tells you which component to look at when something is wrong.

| State | Lives in | Owner | Written by | Read by |
| --- | --- | --- | --- | --- |
| Users, orgs, boards, tasks, approvals | Postgres | Dashboard backend | Dashboard backend | Dashboard backend, frontend (via API) |
| Agent provisioning metadata | Postgres `agents` table | Dashboard backend | Dashboard backend | Dashboard backend, frontend |
| Agent identity files (SOUL.md etc.) | CouchDB vault `agent-{uuid}` | Dashboard backend | Dashboard backend (via obsctl) | Agent pod (via ConfigMap mount) |
| Agent identity files (refresh cache) | K8s ConfigMap `eaos-agent-{uuid}-vault` | Dashboard backend | Dashboard backend (via K8s API) | Agent pod kernel (volume mount) |
| Agent working memory | SQLite `/zeroclaw-data/memory/brain.db` | Agent pod | Agent pod | Agent pod |
| Agent conversation history (in-memory) | `Agent.history: Vec<ConversationMessage>` | Agent pod | Agent pod | Agent pod |
| LLM API keys (secret) | Dashboard backend env vars → injected into pod env | Dashboard backend | Infra / operator | Agent pod |
| Pod / Service / ConfigMap / PVC | K8s etcd | Dashboard backend (via K8s API) | Dashboard backend | K8s scheduler + kubelet |
| Channel credentials (Telegram bot token) | Vault (encrypted) + pod env | Dashboard backend | Dashboard backend | Agent pod |
| Metrics | Prometheus (scrape from agent pods + backend) | Infrastructure | Every component | Grafana / alerting |

Important implication: if you delete an agent from the dashboard, the dashboard backend deletes the Postgres row, the K8s Pod, Service, ConfigMap, PVC, **and** the CouchDB database. The agent is gone.

## Request paths — one-liners

These are the most common paths through the system. Each one has a single entry point you can trace from.

- **Create agent** → `POST /api/v1/agents` → `AgentLifecycleService.create_agent` → provisioning → K8s create
- **Delete agent** → `DELETE /api/v1/agents/{id}` → `AgentLifecycleService.delete_agent` → K8s teardown + vault DB drop
- **Chat with agent (web)** → Next.js chat component → `/ws/chat` on the agent pod's gateway → `Agent::turn_streamed`
- **Chat with agent (Telegram)** → Telegram Bot API → `TelegramChannel::listen` → agent pod → `Agent::turn` → Telegram Bot API reply
- **Check agent status** → `GET /api/v1/gateways/{id}/container/status` → `K8sManager.get_status` → K8s API `read_namespaced_pod`
- **View agent logs** → `GET /api/v1/gateways/{id}/container/logs` → `K8sManager.get_logs` → K8s API `read_namespaced_pod_log`
- **Sync templates to all agents** → `POST /api/v1/gateways/{id}/templates/sync` → re-render Jinja2 → re-seed vaults + refresh ConfigMaps (*note: this path currently does not yet re-trigger vault seeding — deferred from Phase 3*)
- **Login** → Clerk or Google OAuth → FastAPI session → Postgres `users` row

## Cross-repo boundaries

The boundaries between components are hard — cross them only via the defined interfaces:

- **Frontend ↔ Backend** = REST + WebSocket over `/api/v1/*`. Frontend never touches the database directly. Contract lives in the Orval-generated client (`apps/dashboard/frontend/src/api/generated/`) derived from the backend's OpenAPI spec.
- **Backend ↔ Agent pods** = K8s API (for lifecycle) + HTTP API on the agent pod's own gateway (for live interaction). The backend does NOT share a database with the agent; each agent has its own memory.
- **Backend ↔ CouchDB** = `obsctl.VaultClient` Python API. The backend imports it directly as a library, not via shell-out. One CouchDB database per agent. See `apps/dashboard/backend/app/services/openclaw/vault_provisioning.py`.
- **Agent ↔ CouchDB** = not direct. Agents read their personality files from the ConfigMap mount, not from CouchDB. The only component that talks to CouchDB at runtime is the dashboard backend.
- **Agent ↔ LLM providers** = HTTPS to Anthropic / OpenAI / Ollama / OpenRouter / etc. Each agent has its own API key and rate limit budget (provisioned from the backend).
- **Backend ↔ K8s** = in-cluster service account via the `kubernetes` Python client. Dashboard pod has RBAC permissions for Pod / Service / ConfigMap / PVC CRUD in its own namespace.

## Future work (large open design decisions)

These are real roadmap items captured in dedicated specs:

1. **Centralized memory backend** → see [`packages/zeroclaw-core/docs/reference/memory-future.md`](../packages/zeroclaw-core/docs/reference/memory-future.md). Replace the per-pod SQLite with a stateless `RemoteMemory` backend that talks HTTP to a dedicated `memory-service` pod. Enables cross-agent memory sharing, eliminates per-pod PVCs, and makes memory backups a single central job. Phase 6 work.
2. **Template sync retargeting** → the `POST /api/v1/gateways/{id}/templates/sync` endpoint still writes through the old OpenClaw gateway protocol; it should be updated to re-seed vaults + refresh ConfigMaps for existing agents. Deferred from Phase 3.
3. **Alembic head merge** → the dashboard backend has three pre-existing Alembic heads from before Phase 3. A future cleanup should merge them into a single linear migration history.
4. **Multi-agent coordination layer** → no current infrastructure for agents to directly message each other. They can use `delegate` tool + session queues today, but no first-class "agent team" abstraction.

## Deeper docs

Once you understand the big picture from this doc, drill into the package-specific docs:

- [`packages/zeroclaw-core/docs/architecture/overview.md`](../packages/zeroclaw-core/docs/architecture/overview.md) — zeroclaw-core runtime deep-dive (turn loop, prompt assembly, tool dispatch, provider routing, memory subsystem, security, observability, config)
- [`packages/zeroclaw-core/docs/reference/identity-vault.md`](../packages/zeroclaw-core/docs/reference/identity-vault.md) — personality file roster, vault provisioning flow, Phase 3 boot gate invariants
- [`packages/zeroclaw-core/docs/reference/memory-future.md`](../packages/zeroclaw-core/docs/reference/memory-future.md) — centralized memory service plan
- [`packages/zeroclaw-core/docs/reference/api/`](../packages/zeroclaw-core/docs/reference/api/) — CLI, config, provider, channel reference catalogs
- [`apps/dashboard/docs/`](../apps/dashboard/docs/) — dashboard-specific architecture, development setup, deployment
- [`packages/obsctl/SPEC.md`](../packages/obsctl/SPEC.md) — vault CLI and Python API spec
- [`STRIP_DOWN.md`](../STRIP_DOWN.md) — the five-phase strip-down that got us to the current architecture, with per-commit detail

## The one rule

When you're unsure who owns a piece of state or where a request should go: **the dashboard backend is the orchestrator**. It owns every lifecycle decision. The agent pods are disposable workers that do what the backend tells them to do. The frontend is a view layer. The vault is a cache-with-history. When in doubt, follow the data back to the backend — that's where the answers live.
