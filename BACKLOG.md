# MAKE SDK WORK IN ANY LANGUAGE

Extism (extism.org) — WASM-based plugin system. Developers write skills in any
language (Rust, Go, Python, JS, C), compile to .wasm, and your runtime loads
them into a single process. No containers per skill, no build pipelines. SDKs
exist for most languages. You'd get the "upload a zip" DX you have now but
language-agnostic. The host (your skill-runtime) provides host functions for
HTTP, credentials, logging — which maps directly to your SkillContext.

OpenFaaS — if you want the container-per-skill model. Handles build, deploy,
scaling on K8s. But it's a whole platform to operate and overkill for your
scale.

No. You wouldn't sacrifice anything. Extism gives you:

- Same single-process model (no containers per skill)
- Same upload-a-file DX (.wasm instead of .zip)
- Same sandboxing (WASM is more isolated than Node.js eval, actually)
- Same host-controlled I/O (you provide fetch/credentials as host functions,
  just like SkillContext)
- Any language that compiles to WASM

The only cost is time:

1. Rewrite the skill-runtime executor to load .wasm modules instead of esbuild
   bundles
2. Write the host functions (fetch, log, credentials) as WASM imports
3. Build a compile step per language (TS→WASM, Python→WASM, Go→WASM)
4. Rewrite or adapt the three existing skills to target WASM
5. The manifest/defineSkill contract stays the same — it's just how execution
   happens underneath

No architectural trade-off, no new services to operate, no K8s complexity
increase. It's a pure upgrade path. The reason I said "don't do it yet" is only
time — it's weeks of work to replace something that works fine today for a
capability nobody is asking for yet.

# Agent skills in current session

In frontend it should be visible if a skill is like on the newest version

# Agent skill assignment

Skills should be assigned to agents not globally avilable anymore

# Remove internal mysql entirely

# Self-hosted skill runtime (remote skills)

## Problem

Agents run in our cluster, but enterprise customers have internal services
(ERPs, databases, proprietary APIs) behind their firewall that cloud-hosted
skills can't reach. Requiring customers to deploy the full platform on-prem
is too much friction.

## Solution

Follow the GitHub self-hosted runner model: agents stay in our cluster,
customers deploy a lightweight skill-runtime container in their own network
that registers back to our platform.

## Architecture

```
┌─────────────────────────┐         ┌──────────────────────────────┐
│   Our Cluster           │         │   Customer Network           │
│                         │         │                              │
│  Agent Pod              │         │  Self-hosted skill-runtime   │
│    ↓                    │         │    ↓                         │
│  Backend (LLM proxy,    │◄────────│  Outbound tunnel             │
│   skill gateway)        │  tunnel │  (Cloudflare/Tailscale)      │
│    ↓                    │         │    ↓                         │
│  GraphQL introspection  │         │  Custom skills (JS/TS)       │
│  discovers remote skills│         │    ↓                         │
│                         │         │  Internal APIs (ERP, DB...)  │
└─────────────────────────┘         └──────────────────────────────┘
```

- **Agents stay in our cluster** — we manage runtime, LLM proxy, orchestration
- **Customers deploy one container** — a skill-runtime with their custom skills
- **Credentials stay in customer's network** — internal API keys never leave their infra
- **No inbound ports** — skill-runtime connects outbound to our platform

## Components

### 1. Registration / handshake protocol
- Self-hosted runtime starts up, authenticates with our backend using a
  customer-issued token
- Sends its skill manifests (same format as cloud skills)
- Backend registers these as remote skills, tagged to the customer's org
- Heartbeat to track online/offline status

### 2. Secure tunnel
- Customer's skill-runtime establishes an outbound-only tunnel to our backend
- Options: Cloudflare Tunnel, Tailscale, or our own WebSocket-based relay
- No customer firewall changes required — all connections are outbound
- TLS everywhere, mutual auth via the registration token

### 3. Skill execution flow
- Agent discovers remote skills via GraphQL introspection (same as cloud skills)
- Agent calls `skill_exec` → backend routes to the remote skill-runtime
  via the tunnel
- Remote skill-runtime executes the skill against internal APIs
- Result flows back through the tunnel to the agent

### 4. Customer deployment artifact
- Single Docker image: `harkro123/skill-runtime:latest`
- Customer bundles their custom skills into it (or mounts as volume)
- Config: just our platform URL + registration token
- `docker run -e PLATFORM_URL=... -e REGISTRATION_TOKEN=... harkro123/skill-runtime`

## Key design decisions

- **Same skill-sdk** — customers use `@harro/skill-sdk` and `defineSkill` with
  Zod schemas, identical to cloud skills. Zero new abstractions.
- **Same discovery** — remote skills appear in GraphQL introspection alongside
  cloud skills. Agents don't know or care where a skill runs.
- **Outbound-only connectivity** — modeled after GitHub self-hosted runners.
  No VPN, no port forwarding, no firewall changes.
- **Graceful degradation** — if the tunnel drops, remote skills show as
  unavailable. Agent can still use cloud skills.
