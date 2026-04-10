# zeroclaw-core — Rust Agent Runtime

The autonomous agent binary that runs inside each K8s pod. Trait-driven, modular architecture.

## Commands

```bash
cargo fmt --all -- --check
cargo clippy --all-targets -- -D warnings
cargo build
cargo test
cargo check --tests    # Use this (not plain cargo check) after deletions
```

## How it runs in EnterpriseAgentOS

Each agent pod boots with a single env var: `ZEROCLAW_AGENT_ID`. The `gateway_bootstrap` module derives everything from that ID by calling the backend:

- **Provider**: `custom:{backend_url}/v1` — all LLM calls proxy through the backend
- **API key**: the agent UUID (bearer token for all backend calls)
- **Skills**: `{backend_url}/api/graphql` — discovered via introspection
- **Vault**: fetched from `{backend_url}/api/agents/{id}/memory/*`
- **Workspace**: `/zeroclaw-data/workspace` (PVC-backed, cached)

## Key modules

```
src/agent/
  gateway_bootstrap.rs    Derives config from ZEROCLAW_AGENT_ID + backend URL
  vault_bootstrap.rs      Fetches personality files, caches on PVC
  personality.rs          Strict loader (SOUL.md, IDENTITY.md, AGENTS.md required)
  agent.rs                Turn loop, system prompt, tool execution, capability refresh
  prompt.rs               System prompt builder (trait-based section composition)

src/tools/
  traits.rs               Tool trait: name, description, parameters_schema, execute
  skill_exec/             GraphQL-backed CLI tool for all backend skills
    mod.rs                SkillExecTool (single tool, command string parameter)
    parser.rs             Deterministic CLI parser (skill action --flags)
    schema_cache.rs       GraphQL introspection cache + --help generation
    query_builder.rs      CLI command → GraphQL query string
  backend_skill_tool.rs   Legacy per-tool HTTP caller (being replaced by skill_exec)

src/skills/
  mod.rs                  Disk-based skill loading (SKILL.md files)
  live.rs                 Legacy backend capability cache (being replaced)

src/gateway/              WebSocket server on :42617
src/providers/            LLM provider routing + resilient wrapper
src/config/               Schema + config loading/merging
src/memory/               Markdown/SQLite memory backends
src/security/             Policy, pairing, sandboxing
src/channels/             Telegram/Discord/Slack channels
```

## Extension points

- `src/providers/traits.rs` — `Provider` trait
- `src/tools/traits.rs` — `Tool` trait
- `src/channels/traits.rs` — `Channel` trait
- `src/memory/traits.rs` — `Memory` trait
- `src/observability/traits.rs` — `Observer` trait

## Risk tiers

- **Low**: docs, tests, chore
- **Medium**: most `src/**` behavior changes
- **High**: `src/security/**`, `src/gateway/**`, `src/tools/**`, `src/agent/gateway_bootstrap.rs`

## Workflow

1. **Read before write** — inspect existing module, factory wiring, and adjacent tests.
2. **One concern per PR.**
3. **Minimal patch** — no speculative abstractions.
4. **Use `cargo check --tests`** (not plain `cargo check`) after deleting code — catches test-only usages.

## Anti-patterns

- Do not add heavy dependencies for minor convenience.
- Do not silently weaken security policy.
- Do not add config keys without a concrete use case.
- Do not modify unrelated modules "while here".
- Do not bypass failing clippy checks without explanation.
