# zeroclaw-core — Rust agent runtime

The binary that runs inside each K8s agent pod. Boots from a single env var, hydrates itself from the backend, then runs the agent turn loop and serves the WebSocket gateway.

## Commands

```bash
cargo fmt --all -- --check   # Format check
cargo clippy --all-targets -- -D warnings
cargo build
cargo test
cargo check --tests          # Use this (not plain cargo check) after deleting code
```

## How an agent pod boots

Each pod receives exactly one env var: `ZEROCLAW_AGENT_ID`. No `config.toml` on the PVC is ever consulted — when `ZEROCLAW_AGENT_ID` is set, `Config::load_or_init` short-circuits to an in-memory `Config::default()` (no file I/O, no directories created) and `Config::save()` is a no-op. The backend is the sole source of truth.

1. `Config::load_or_init` detects pod mode via `ZEROCLAW_AGENT_ID` and returns `Config::default()` with `workspace_dir = /zeroclaw-data/workspace`. Nothing is read from or written to disk.
2. `gateway_bootstrap::apply` seeds provider=`custom:{backend_url}/v1`, api_key=agent UUID, model=`"backend-managed"` placeholder, gateway=0.0.0.0:42617, skills backend+graphql URLs.
3. `gateway_bootstrap::fetch_and_overlay_from_env` calls `GET {backend_url}/api/agents/{id}` (bearer=agent UUID) and overlays the real model from the bootstrap payload; extracts the per-tool allow/deny map. Response contains NO credentials. Fails open — if the backend is unreachable the pod keeps the defaults seeded by `apply`; there is no local-file fallback.
4. Fetches personality files from `{backend_url}/api/agents/{id}/memory/*` → caches on PVC at `/zeroclaw-data/workspace`.
5. Discovers skills via GraphQL introspection of `{backend_url}/api/graphql`.
6. `SkillExecTool` is built with the permission map and blocks any dispatch where `(skill, tool)` is set to `Deny` (returns a `ToolResult` error "tool denied by policy"). Missing keys default to allow. There is no "ask" mode — agents run unattended.
7. Starts the WebSocket gateway on `:42617`.

Local-dev / CLI use (no `ZEROCLAW_AGENT_ID`) still reads `~/.zeroclaw/config.toml` through the legacy path — that branch is orthogonal to pod behavior.

## Project structure

```
src/
  agent/
    gateway_bootstrap.rs    Derives full config from ZEROCLAW_AGENT_ID + backend URL
    vault_bootstrap.rs      Fetches and caches personality files on PVC
    personality.rs          Strict loader — SOUL.md, IDENTITY.md, AGENTS.md are required
    agent.rs                Turn loop, system prompt assembly, tool execution, capability refresh
    prompt.rs               System prompt builder — trait-based section composition
  tools/
    traits.rs               Tool trait: name, description, parameters_schema, execute
    skill_exec/             GraphQL-backed skill tool — single tool, CLI-style interface
      mod.rs                SkillExecTool
      parser.rs             Deterministic CLI parser (skill action --flags)
      schema_cache.rs       GraphQL introspection cache + --help generation
      query_builder.rs      CLI command → GraphQL query string
    backend_skill_tool.rs   Legacy per-tool HTTP caller (being replaced by skill_exec)
  skills/
    mod.rs                  Disk-based skill loading from SKILL.md files
    live.rs                 Legacy backend capability cache (being replaced)
  gateway/                  WebSocket server on :42617
  providers/                LLM provider routing + resilient wrapper
  config/                   Config schema + loading
  memory/                   Markdown/SQLite memory backends
  security/                 Policy, pairing, sandboxing
  channels/                 Telegram/Discord/Slack channel integrations
```

## Extension points

Add new capabilities by implementing the relevant trait:

| Trait | File | Adds |
|-------|------|------|
| `Provider` | `src/providers/traits.rs` | New LLM provider |
| `Tool` | `src/tools/traits.rs` | New built-in tool |
| `Channel` | `src/channels/traits.rs` | New messaging channel |
| `Memory` | `src/memory/traits.rs` | New memory backend |
| `Observer` | `src/observability/traits.rs` | New observability hook |

## Risk tiers

Changes in these areas need extra care:

- **High risk:** `src/security/**`, `src/gateway/**`, `src/tools/**`, `src/agent/gateway_bootstrap.rs`
- **Medium risk:** Most other `src/**` behavior changes
- **Low risk:** Docs, tests, chore, comments

## Key rules

- **`cargo check --tests` after deleting code**, not plain `cargo check`. Deletions can leave test-only usages that plain check misses.
- **No heavy dependencies for minor convenience.** The Rust binary runs in a minimal pod — dependency bloat increases build time and image size.
- **Clippy is mandatory.** `-D warnings` — do not bypass failing checks without explaining why in the PR.
- **One concern per PR.** Do not modify unrelated modules while working on something else.

## Anti-patterns

- Do not add config keys without a concrete use case — the agent derives everything from the backend on boot.
- Do not reintroduce a `config.toml` read path for pod mode. If `ZEROCLAW_AGENT_ID` is set, the pod must not read, create, or write files under `/zeroclaw-data` other than the workspace/personality cache. All runtime config flows through `gateway_bootstrap::apply` + `fetch_and_overlay_from_env`. New fields go in the backend `AgentBootstrapPayload`, not in on-disk TOML.
- Do not silently weaken security policy. Any change to `src/security/**` must be explicit and reviewed.
- Do not bypass failing clippy checks with `#[allow(...)]` without a comment explaining why.
- Do not add speculative abstractions or premature traits — implement for the concrete case first.
- Do not add hardcoded credentials, URLs, or model names — the agent receives everything from the backend.
