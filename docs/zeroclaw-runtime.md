# Zeroclaw Runtime

> Internal architecture of the Rust agent binary that runs in each pod.

## Mental model

`zeroclaw-core` is a Rust binary. In production it runs as `zeroclaw daemon` inside a K8s pod — one pod per agent. The codebase is trait-driven: **Provider**, **Memory**, **Tool**, **Channel**, **Observer** are the main extension points.

The `Agent` struct (`src/agent/agent.rs`) is the central object. It owns:

- `Box<dyn Provider>` — the LLM connection (routes through backend proxy)
- `Arc<dyn Memory>` — persistent state (remote Postgres via backend API in managed mode, SQLite in standalone mode)
- `Vec<Box<dyn Tool>>` — callable tools (including `skill_exec` for backend skills)
- `Box<dyn ToolDispatcher>` — parses LLM output into tool calls
- `SystemPromptBuilder` — assembles the system prompt each turn
- `Vec<ConversationMessage>` — the running conversation transcript

## Boot sequence

```
zeroclaw daemon
  ↓
gateway_bootstrap::apply(config)
  - Reads ZEROCLAW_AGENT_ID
  - Sets provider: custom:{backend_url}/v1
  - Sets API key: agent UUID
  - Sets skills.graphql_url: {backend_url}/api/graphql
  - Sets workspace: /zeroclaw-data/workspace
  ↓
personality_bootstrap::seed(workspace_dir, system_prompt)
  - Personality templates embedded in the binary via include_str!
  - Writes each template to workspace only if it does not already exist
  - Substitutes {{prompt}} in BOOTSTRAP.md with the bootstrap payload's systemPrompt
  ↓
personality::load_personality_strict(workspace_dir)
  - Validates required files exist and are non-empty
  - Fails loudly if missing → pod CrashLoopBackOff
  ↓
Agent::from_config(config)
  - Creates provider, memory, tools, observer
  - Registers skill_exec tool (GraphQL-backed)
  - Builds system prompt
  ↓
Gateway server starts on 0.0.0.0:42617
  - WebSocket at /ws/chat
  - REST at /health, /api/events, /api/sessions, etc.
```

## Turn loop

Every inbound message (WebSocket chat, cron trigger, channel update) calls `Agent::turn(message)`:

```
1. System prompt seeding (lazy, first turn only)
   Build from personality files + tool specs + security policy

2. Backend capability refresh (every 30s)
   Fetch GraphQL schema, swap tool set if changed

3. Memory context loading
   Recall relevant memories, apply time decay, filter by relevance

4. User message enrichment
   Prepend current date/time + memory context

5. LLM call
   Send conversation history + tools to provider
   Provider routes through backend proxy → real LLM API

6. Response parsing
   If text → return as response
   If tool call → execute tool → append result → loop back to step 5
   Max iterations: configurable (default 25)

7. Auto-save
   Store assistant response in memory if auto_save enabled
```

The turn loop runs under a mutex — one turn at a time per pod. This is intentional: it prevents interleaved tool calls from corrupting conversation state.

## Tool execution

Tools implement the `Tool` trait:

```rust
#[async_trait]
pub trait Tool: Send + Sync {
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn parameters_schema(&self) -> serde_json::Value;
    async fn execute(&self, args: serde_json::Value) -> Result<ToolResult>;
}
```

~30 built-in tools ship with the runtime (filesystem, memory, web, shell, etc.). Backend skills are accessed through the `skill_exec` tool which translates CLI-style commands into GraphQL queries.

## System prompt assembly

The system prompt is built from composable `PromptSection` implementations:

1. **PersonalitySection** — SOUL.md, IDENTITY.md, AGENTS.md content
2. **ToolsSection** — list of available tools with JSON schemas
3. **SkillsSection** — skill instructions from `prompts` field (backend skill docs)
4. **SecuritySection** — autonomy level, sandbox policy
5. **DispatcherSection** — tool calling format instructions

Sections are registered via `SystemPromptBuilder::with_defaults()` and rendered in order. The prompt is rebuilt whenever the tool set changes (e.g. after a capability refresh).

## Key files

| File                                 | Purpose                                                                  |
| ------------------------------------ | ------------------------------------------------------------------------ |
| `src/agent/gateway_bootstrap.rs`     | Single-env-var config derivation                                         |
| `src/agent/personality_bootstrap.rs` | Seeds embedded personality templates onto the pod PVC on first boot      |
| `src/agent/agent.rs`                 | Agent struct, turn loop, capability refresh                              |
| `src/agent/prompt.rs`                | System prompt builder                                                    |
| `src/agent/personality.rs`           | Personality file loader                                                  |
| `src/tools/skill_exec/mod.rs`        | GraphQL skill CLI tool                                                   |
| `src/tools/traits.rs`                | Tool trait definition                                                    |
| `src/gateway/mod.rs`                 | HTTP + WebSocket server                                                  |
| `src/providers/router.rs`            | Multi-model routing                                                      |
| `src/config/schema.rs`               | Full config schema                                                       |
| `src/memory/remote.rs`               | Remote memory backend (HTTP calls to backend API)                        |
| `src/memory/sqlite.rs`               | Local SQLite memory backend (standalone mode)                            |
| `src/memory/backend.rs`              | Backend classifier (auto-selects remote when `ZEROCLAW_AGENT_ID` is set) |

## Memory backends

The agent supports two memory backends, selected automatically:

| Mode           | Backend        | Storage                    | When                                 |
| -------------- | -------------- | -------------------------- | ------------------------------------ |
| **Managed**    | `RemoteMemory` | Postgres (via backend API) | `ZEROCLAW_AGENT_ID` is set (K8s pod) |
| **Standalone** | `SqliteMemory` | Local SQLite file          | Running locally without backend      |

The `RemoteMemory` backend calls the backend's `/api/agents/me/memory`, `/api/agents/me/cache`, and `/api/agents/me/conversations` endpoints. This centralizes all agent data in the platform's Postgres database.

SQLite is behind the `local-storage` Cargo feature flag (enabled by default). The `rusqlite` dependency is optional.
