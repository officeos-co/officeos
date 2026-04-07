# zeroclaw-core Architecture

> **Audience**: new contributors, reviewers, and future-you who forgot how this codebase fits together.
>
> **Scope**: the production code paths that actually run when an agent pod is alive and doing work. This doc does not catalogue every module; it traces the load-bearing flows and points at the authoritative source files for each step.
>
> **Prerequisite reading**: [`../reference/identity-vault.md`](../reference/identity-vault.md) (how identity reaches the pod) and [`../reference/memory-future.md`](../reference/memory-future.md) (where memory is going).

## Table of contents

1. [Mental model: what the runtime actually is](#mental-model-what-the-runtime-actually-is)
2. [Boot sequence: `zeroclaw daemon` → `AgentCore`](#boot-sequence-zeroclaw-daemon--agentcore)
3. [The agent turn loop](#the-agent-turn-loop)
4. [System prompt assembly](#system-prompt-assembly)
5. [Tool dispatch and execution](#tool-dispatch-and-execution)
6. [Providers: routing, fallback, format bridging](#providers-routing-fallback-format-bridging)
7. [Memory subsystem](#memory-subsystem)
8. [Channels and the HTTP gateway](#channels-and-the-http-gateway)
9. [Security: autonomy, sandboxing, pairing, secrets](#security-autonomy-sandboxing-pairing-secrets)
10. [Observability](#observability)
11. [Configuration and state ownership](#configuration-and-state-ownership)
12. [Where each subsystem lives](#where-each-subsystem-lives)

## Mental model: what the runtime actually is

`zeroclaw-core` is a Rust library + CLI. In production it runs as a single binary inside a Kubernetes pod:

- One pod per agent (provisioned by the Python dashboard backend in `apps/dashboard/backend/`)
- The pod mounts a ConfigMap at `/vault-workspace` containing the agent's personality files (SOUL, IDENTITY, AGENTS, USER, TOOLS, HEARTBEAT, BOOTSTRAP, MEMORY)
- The pod runs `zeroclaw daemon` which starts an `AgentCore`, an HTTP gateway, and whichever channels are configured
- Ingress traffic (webhook POSTs, Telegram updates, WebSocket chat frames) lands at the gateway, gets converted to a `ChannelMessage`, and is handed to `AgentCore::turn` which runs one full LLM loop and returns a response

The codebase is trait-heavy: **Provider**, **Memory**, **Tool**, **Channel**, **Observer**, **ToolDispatcher**, **Sandbox**, **PromptSection** are the main extension points. Concrete implementors live in sibling modules under each trait's directory.

The `AgentCore` (`src/agent/agent.rs`) is the central object. It owns:

- A `Box<dyn Provider>` — the LLM connection
- An `Arc<dyn Memory>` — persistent state
- A `Box<dyn ToolDispatcher>` — parses LLM output into tool calls, formats results back
- A `Vec<Box<dyn Tool>>` — the tools the LLM can call
- An `Arc<dyn Observer>` — metrics / tracing sink
- A `SystemPromptBuilder` — assembles the system prompt each turn
- A `Box<dyn MemoryLoader>` — fetches relevant memory context
- A `Vec<ConversationMessage>` — the running transcript
- Config, model name, temperature, workspace dir, autonomy level

Everything else in the crate is either a concrete implementor of one of those traits or plumbing for the CLI / gateway / daemon layer on top.

## Boot sequence: `zeroclaw daemon` → `AgentCore`

```
zeroclaw daemon
   │
   ▼  src/main.rs::Commands::Daemon  (line 798)
   │
   ▼  src/daemon/mod.rs::run(config, host, port)  (line 48)
   │
   ├──▶ runtime adapter: src/runtime/  (RuntimeAdapter trait)
   │
   ├──▶ security policy compiled from config: src/security/policy.rs
   │
   ├──▶ gateway::run_gateway(host, port, config, event_tx)
   │       src/gateway/mod.rs:344
   │    starts the HTTP + WebSocket server, registers routes,
   │    spawns channels via channels::start_channels(config).
   │
   └──▶ AgentCore::from_config(&config)  (src/agent/agent.rs:360)
           │
           ├──▶ load_personality_strict(&workspace_dir)?
           │       Phase 3 boot gate — fails loudly if SOUL.md /
           │       IDENTITY.md / AGENTS.md missing from ConfigMap mount.
           │
           ├──▶ memory::create_memory_with_storage_and_routes(&config.memory, ...)
           │       Returns a boxed Memory backend (sqlite / obsidian / none).
           │
           ├──▶ providers::create_provider_with_url(...)
           │       Builds the provider stack:
           │       router (optional) → reliable (optional) → compatible OR concrete.
           │
           ├──▶ tools::all_tools_with_runtime(&config, &runtime, ...)
           │       Constructs the ~30 core Tool implementors.
           │
           └──▶ AgentCoreBuilder::build()
                   src/agent/agent.rs:265
                   Produces the Agent struct that every turn runs against.
```

Once this is done, the gateway's route handlers + channel listeners hold an `Arc<Mutex<Agent>>` (via `AppState`) and serialize turns through it. A single agent pod processes one turn at a time — the `Mutex` is real, not theatre.

## The agent turn loop

Every inbound message — whether it came from a webhook POST, a Telegram update, a WebSocket chat frame, or a cron-triggered heartbeat — eventually calls `Agent::turn(user_message)` in `src/agent/agent.rs:738`.

The loop body is about 165 lines. Here's what happens, annotated with the file:line of each step:

```
Agent::turn(user_message)  src/agent/agent.rs:738
│
│ 1. Lazy system prompt seeding
│    If self.history is empty, build the system prompt from
│    personality files + current tool set + security policy
│    and push as the first ConversationMessage.
│    → self.build_system_prompt()  (agent.rs:596)
│    → SystemPromptBuilder::build(&ctx)  (prompt.rs:64)
│
│ 2. Load memory context
│    → self.memory_loader.load_context(memory, user_message, session_id)
│    → DefaultMemoryLoader in agent/memory_loader.rs
│    Runs memory.recall(), applies time decay, filters by relevance,
│    returns a "[Memory context]" block to prepend to the user message.
│
│ 3. Auto-save user message
│    If config.memory.auto_save, store the raw user_message as a
│    MemoryCategory::Conversation entry.
│
│ 4. Enrich user message with date + memory context
│    Prefix with "[CURRENT DATE & TIME: ...]" and the memory context
│    block, then push as ChatMessage::user into history.
│
│ 5. Classify which model to use (router hint)
│    → self.classify_model(user_message)  (agent.rs:664)
│    → src/agent/classifier.rs
│    Uses configurable hints + route_model_by_hint map to pick a
│    concrete model string from the available hints. Defaults to
│    self.model_name if no hint matches.
│
│ 6. Tool-calling inner loop — up to config.max_tool_iterations
│    ┌──────────────────────────────────────────────────────────────┐
│    │  a. Dispatcher renders history as provider-native messages  │
│    │     → self.tool_dispatcher.to_provider_messages(&history)   │
│    │                                                              │
│    │  b. Response cache probe (if temperature == 0.0)            │
│    │     → src/memory/response_cache.rs                          │
│    │     If hit, short-circuit: push assistant message, return.  │
│    │                                                              │
│    │  c. Provider call                                            │
│    │     → self.provider.chat(ChatRequest { messages, tools }, │
│    │                          model, temperature)                │
│    │     → Box<dyn Provider> — could be reliable(router(...))   │
│    │                                                              │
│    │  d. Parse tool calls out of the response                    │
│    │     → self.tool_dispatcher.parse_response(&response)        │
│    │     Returns (text_before_tools, Vec<ParsedToolCall>)        │
│    │                                                              │
│    │  e. Zero tool calls? → final answer path                    │
│    │     Store in response cache, push assistant message,        │
│    │     trim history, return final_text to caller.              │
│    │                                                              │
│    │  f. One or more tool calls? → execute                       │
│    │     → self.execute_tools(&calls)  (agent.rs, execute_tools) │
│    │     Runs each tool through the approval gate + sandbox,     │
│    │     collects Vec<ToolExecutionResult>.                      │
│    │     → tool_dispatcher.format_results(&results)              │
│    │     Pushes a ToolResult ConversationMessage into history.   │
│    │                                                              │
│    │  g. Loop — back to (a) with the tool results in history.   │
│    └──────────────────────────────────────────────────────────────┘
│
│ 7. Safety valve: if we hit max_tool_iterations without a final
│    answer, bail with an error. The pod surfaces this to the
│    calling channel / API client; no infinite tool loops.
│
▼
String: the final assistant text (same value the caller receives)
```

### Streaming variant

`Agent::turn_streamed` (`agent.rs:913`) is the same loop but each step pushes `TurnEvent`s through an `mpsc::Sender<TurnEvent>` for live WebSocket delivery. The final return value matches `turn()` — streaming is purely an observability layer on top.

### History trimming

After every tool-loop iteration, `self.trim_history()` runs — it's a token-budget-aware pruner that drops older turns when the history approaches the provider's context window. Lives in `src/agent/history_pruner.rs`. There's also `src/agent/context_compressor.rs` which can run an LLM-summarization pass when trimming alone isn't enough.

### Loop detection

`src/agent/loop_detector.rs` watches the tool-call stream for cycles ("tool X called with identical args 3 times in a row") and injects a warning back into the conversation. Prevents the classic "LLM gets stuck calling the same tool forever" failure mode.

## System prompt assembly

The system prompt is not a single template string — it's built by composing `PromptSection` trait objects. Each section owns a slice of the prompt and can be swapped/added/removed independently.

```
SystemPromptBuilder::with_defaults()  src/agent/prompt.rs:43
│
├── DateTimeSection       — "## Current Date & Time" — stable across turns so
│                           the cache key works for deterministic prompts
├── IdentitySection       — "## Project Context" — reads SOUL.md, IDENTITY.md,
│                           AGENTS.md, USER.md, TOOLS.md, HEARTBEAT.md,
│                           BOOTSTRAP.md, MEMORY.md from workspace_dir via
│                           personality::load_personality() (lenient)
├── ToolHonestySection    — "## CRITICAL: Tool Honesty" — anti-hallucination rules
├── ToolsSection          — "## Tools" — iterates the Tool list and renders each
│                           via its description(). Locale-aware if ctx.tool_descriptions
│                           is set.
├── SafetySection         — "## Safety" — renders the ctx.security_summary (pre-rendered
│                           in AgentCore::from_config from SecurityPolicy). Full-
│                           autonomy agents skip the "ask before acting" text.
├── SkillsSection         — "## Skills" — lists installed skills with their
│                           skill_id + location
├── WorkspaceSection      — "## Workspace" — workspace_dir path + runtime capabilities
├── RuntimeSection        — "## Runtime" — host, OS, model name
└── ChannelMediaSection   — multimodal handling hints (image extraction, audio
                            transcription)
```

Custom sections can be added via `SystemPromptBuilder::add_section`. The `PromptContext<'a>` passed to `build()` carries everything a section needs: workspace_dir, model_name, tools, skills, dispatcher_instructions, tool_descriptions, security_summary, autonomy_level. See `src/agent/prompt.rs:11-30`.

Sections that produce empty strings are silently dropped, so a feature that isn't configured (e.g. no skills installed) doesn't leave a dangling header.

### Identity files

The `IdentitySection` does NOT error on missing personality files — it uses the **lenient** `personality::load_personality()`. The **strict** gate happens earlier during boot in `AgentCore::from_config` via `personality::load_personality_strict()`, which fails the process if `SOUL.md`, `IDENTITY.md`, or `AGENTS.md` is missing. This separation exists because `from_config` runs once per pod lifetime and can safely exit non-zero, while `prompt.build()` runs every turn and must stay resilient. See [`../reference/identity-vault.md`](../reference/identity-vault.md) for the full identity flow.

## Tool dispatch and execution

`ToolDispatcher` (`src/agent/dispatcher.rs:21`) is the translation layer between the conversation history (as zeroclaw-core sees it) and whatever format the current provider expects.

There are two implementors:

### `NativeToolDispatcher` (`src/agent/dispatcher.rs:171`)

Used when the provider supports native tool calling (Anthropic, OpenAI Chat Completions, Gemini, Ollama with tools=true). The provider returns structured `tool_calls` in its response; this dispatcher just unpacks them. Tool specs are sent alongside messages on each request.

### `XmlToolDispatcher` (`src/agent/dispatcher.rs:30`)

Used when the provider can't do native tool calling. Tool documentation is baked into the system prompt as text (see `prompt_instructions`), the LLM wraps tool invocations in `<tool_call>{...}</tool_call>` tags, and this dispatcher parses them back out. Also strips `<think>...</think>` blocks that reasoning models emit. Used with llama.cpp, older models, and fallback scenarios.

The choice between them happens at `AgentCore::from_config` time based on the provider's `capabilities()`. Once chosen, it's fixed for the lifetime of the agent.

### `ToolDispatcher` trait surface

```rust
pub trait ToolDispatcher: Send + Sync {
    fn parse_response(&self, response: &ChatResponse) -> (String, Vec<ParsedToolCall>);
    fn format_results(&self, results: &[ToolExecutionResult]) -> ConversationMessage;
    fn prompt_instructions(&self, tools: &[Box<dyn Tool>]) -> String;
    fn to_provider_messages(&self, history: &[ConversationMessage]) -> Vec<ChatMessage>;
    fn should_send_tool_specs(&self) -> bool;
}
```

### Execution path

`Agent::execute_tools(calls)` (inside `agent.rs`) delegates to `src/agent/tool_execution.rs`. For each call:

1. **Resolve the Tool instance** from `self.tools` by name. Unknown tool → return an error result; the LLM sees it and usually corrects itself.
2. **Approval gate** (`src/approval/`) — if the tool is in the `always_ask` list or its autonomy level requires confirmation, ask the user via an `ask_user` flow before executing. Auto-approval comes from `config.autonomy.auto_approve` plus per-tool policy.
3. **Sandbox** (`src/security/traits.rs::Sandbox`) — the runtime's sandbox wraps execution. On Linux with `sandbox-landlock` feature enabled, a Landlock ruleset restricts file system access to `workspace_dir`. On other OSes, a no-op sandbox passes through.
4. **Run** `tool.execute(args).await` — returns `ToolResult { output, is_error, ... }`.
5. **Observer event** — `Observer::record_event(ObserverEvent::ToolCallFinished { ... })` — goes to Prometheus + OTel if enabled.
6. **Return** the `ToolExecutionResult`.

If any tool errors, the error is returned as a normal result (not a Rust `Err`) — the LLM reads it as tool output and usually recovers.

### Tool registration

`tools::all_tools_with_runtime()` in `src/tools/mod.rs` is the factory. It builds every built-in tool from config and returns a `Vec<Box<dyn Tool>>`. Survived the Phase 2 cleanup with ~30 tools: `shell`, `file_read`, `file_write`, `file_append`, `file_edit`, `glob_search`, `content_search`, `memory_store`, `memory_recall`, `memory_export`, `memory_forget`, `memory_purge`, `http_request`, `web_fetch`, `web_search_tool`, `mcp_call` and MCP-related tools, `skill_*` tools, `canvas`, `sessions_*`, `poll`, `reaction`, `ask_user`, `escalate`, `delegate`, `tool_search`, `read_skill`.

MCP tools are discovered dynamically — when an MCP server is registered in config, its advertised tools become additional `Box<dyn Tool>` entries at boot time.

## Providers: routing, fallback, format bridging

The provider stack is layered. Here's the surviving architecture post-Phase 2.5:

```
Agent.provider: Box<dyn Provider>
│
▼  typically a wrapper chain:
│
RouterProvider  (src/providers/router.rs)
│   picks a downstream provider based on the hint in the incoming request
│   uses config.model_routes (hint → provider+model mapping)
│
▼
ReliableProvider  (src/providers/reliable.rs)
│   resilient wrapper: rotates across a pool of API keys, retries with
│   exponential backoff, fails over to a fallback provider on auth errors
│   or rate limits
│
▼
One of:
│
├── AnthropicProvider   (src/providers/anthropic.rs)     — claude-*
├── OpenAiProvider      (src/providers/openai.rs)        — gpt-*, o-* (+ Azure deployments via base_url)
├── OllamaProvider      (src/providers/ollama.rs)        — local models
├── OpenRouterProvider  (src/providers/openrouter.rs)    — openrouter aggregator
└── OpenAiCompatibleProvider (src/providers/compatible.rs)
        Covers ~30 community endpoints: groq, mistral, xai, deepseek,
        together, fireworks, cohere, perplexity, lm_studio, llama.cpp,
        z.ai (alias: zai), glm, minimax, qwen, and custom:<url>.
```

### The `Provider` trait

Defined at `src/providers/traits.rs:308`. Key methods:

```rust
pub trait Provider: Send + Sync {
    fn capabilities(&self) -> ProviderCapabilities { ... }
    fn convert_tools(&self, tools: &[ToolSpec]) -> ToolsPayload { ... }
    async fn simple_chat(&self, ...) -> Result<String>;
    async fn chat_with_system(&self, ...) -> Result<String>;
    async fn chat_with_history(&self, ...) -> Result<String>;
    async fn chat(&self, request: ChatRequest<'_>, model: &str, temperature: f64)
        -> Result<ChatResponse>;
}
```

`capabilities()` declares what the provider supports: native tool calling, streaming, image inputs, json mode, etc. The agent uses this to pick the right `ToolDispatcher` at boot time and to gate features like image-enabled messages.

`convert_tools()` returns a `ToolsPayload` enum — either the provider-native format (Anthropic, OpenAI, Gemini have distinct shapes) or `PromptGuided` (fallback: inject tool docs as text). This lets the same tool registry work across providers with different calling conventions.

### Factory

`providers::create_provider_with_url(name, url, api_key, config)` in `src/providers/mod.rs:1051` is the entry point. It:

1. Parses `name` as either a concrete provider ID (`"anthropic"`), an alias (`"zai"` → `compatible` with z.ai endpoint), or a prefixed form (`"custom:https://..."` → `compatible` with custom base URL).
2. Checks the API key via `AuthService` for OAuth-backed providers.
3. Wraps the concrete provider in `ReliableProvider` if reliability is configured.
4. Wraps that in `RouterProvider` if `model_routes` is non-empty.
5. Returns the outermost wrapper as a `Box<dyn Provider>`.

The wrapper chain is constructed once at agent boot — all runtime dispatch is through trait objects.

### Adding a new provider

See [`../contributing/change-playbooks.md`](../contributing/change-playbooks.md). Short version:
1. Create `src/providers/myprov.rs` implementing `Provider`
2. Add a match arm in `create_provider_with_url`
3. If it needs OAuth, wire through `AuthService`
4. Add tests in `providers.test.rs`

## Memory subsystem

Memory is where "what the agent knows about the world" lives — conversational history summaries, learned facts, user context, ongoing project state. The `Memory` trait (`src/memory/traits.rs:112`) is the abstraction; concrete backends implement it.

### Trait surface

```rust
pub trait Memory: Send + Sync {
    fn name(&self) -> &str;
    async fn store(&self, key: &str, content: &str,
                   category: MemoryCategory, session_id: Option<&str>) -> Result<()>;
    async fn recall(&self, query: &str, limit: usize,
                    session_id: Option<&str>, since: Option<&str>, until: Option<&str>)
                    -> Result<Vec<MemoryEntry>>;
    async fn get(&self, key: &str) -> Result<Option<MemoryEntry>>;
    async fn list(&self, category: Option<&MemoryCategory>, session_id: Option<&str>)
                  -> Result<Vec<MemoryEntry>>;
    async fn forget(&self, key: &str) -> Result<bool>;
    async fn count(&self) -> Result<usize>;
    async fn health_check(&self) -> Result<()>;
    // Bulk operations (default: bail):
    async fn purge_namespace(&self, namespace: &str) -> Result<usize>;
    async fn purge_session(&self, session_id: &str) -> Result<usize>;
    async fn export(&self, filter: ExportFilter) -> Result<Vec<MemoryEntry>>;
}
```

### Surviving backends (post-Phase 4)

```
SqliteMemory    src/memory/sqlite.rs       — default. FTS5 + BM25 + vector
                                             search + hybrid merge. Embeddings
                                             cached as BLOBs. Per-pod file at
                                             {workspace}/memory/brain.db.

ObsidianMemory  src/memory/obsidian.rs     — opt-in. Writes memories as markdown
                                             notes via the obsctl Python CLI to
                                             a CouchDB-backed vault. Shared
                                             across agent restarts. No
                                             embeddings / FTS (yet).

NoneMemory      src/memory/none.rs          — test/dev. No-op; all methods return
                                             empty / Ok(()).

NamespacedMemory  src/memory/namespaced.rs  — decorator. Wraps any Memory with
                                               per-tenant namespace isolation.
                                               Used by src/tools/delegate.rs to
                                               give delegated sub-agents their
                                               own memory namespace.
```

### Factory

`memory::create_memory_with_storage_and_routes()` in `src/memory/mod.rs:225` matches `config.memory.backend` against the backend names:

```rust
match classify_memory_backend(backend_name) {
    MemoryBackendKind::Sqlite   => SqliteMemory::with_embedder(...)
    MemoryBackendKind::Obsidian => ObsidianMemory::new()
    MemoryBackendKind::None     => NoneMemory::new()
    _                           => NoneMemory::new() + tracing::warn!("unknown backend")
}
```

### Retrieval pipeline

`SqliteMemory::recall` uses the multi-stage pipeline in `src/memory/retrieval.rs`:

```
recall(query, limit, ...)
│
├── Stage 1: Hot cache
│     Recent recall results keyed by (query, session_id, limit).
│     TTL = config.memory.response_cache_ttl_minutes minutes.
│     Hit → return immediately.
│
├── Stage 2: FTS5 BM25
│     SQLite full-text index over memory entries. Cheap and fast.
│     If top score >= config.memory.fts_early_return_score,
│     return without running vector search.
│
└── Stage 3: Vector search
      Compute query embedding via EmbeddingProvider
      (src/memory/embeddings.rs — OpenAI or Noop).
      Cosine similarity against stored BLOB vectors
      (src/memory/vector.rs). Hybrid-merge with FTS results using
      vector_weight/keyword_weight from config.
      Apply importance (src/memory/importance.rs) and time decay
      (src/memory/decay.rs) to the merged scores.
      Return top-limit entries.
```

### Response cache

`src/memory/response_cache.rs` is an orthogonal in-memory LRU cache of full LLM responses, keyed by `(model, system_prompt, last_user_message)`. Only engaged when `temperature == 0.0` (deterministic prompts). Saves the provider round-trip on repeated identical queries. Unrelated to the `Memory` trait — it's a pure performance optimization that the agent loop checks before each provider call.

### Write path

`Agent::turn` auto-stores the raw user message (if `config.memory.auto_save = true`) under key `user_msg` with category `Conversation`. Tools like `memory_store` let the agent explicitly persist learned facts to the `Core` category. Consolidation (`src/memory/consolidation.rs`, called from `gateway/ws.rs` and `channels/mod.rs` at end-of-turn) runs an LLM summarization pass to extract structured facts from the turn transcript.

### Future

See [`../reference/memory-future.md`](../reference/memory-future.md) — the end state is a centralized memory service that all agents share, with SQLite / Obsidian / the current retrieval pipeline all collapsing into a single `RemoteMemory` trait object that talks HTTP to a memory-service pod. Not yet implemented; Phase 6 work.

## Channels and the HTTP gateway

Ingress. Everything that generates a user message for the agent flows through either a `Channel` or the HTTP gateway.

### The `Channel` trait (`src/channels/traits.rs:95`)

```rust
pub trait Channel: Send + Sync {
    fn name(&self) -> &str;
    async fn send(&self, message: &SendMessage) -> Result<()>;
    async fn listen(&self, tx: mpsc::Sender<ChannelMessage>) -> Result<()>;
    async fn health_check(&self) -> bool;
    // optional: start_typing, stop_typing, send_draft, update_draft, ...
}
```

Surviving implementors (post-Phase 2.4):

- **`TelegramChannel`** (`src/channels/telegram.rs`) — Telegram Bot API + Telegram Web
- **`WebhookChannel`** (`src/channels/webhook.rs`) — accepts generic JSON POSTs
- **`CliChannel`** (`src/channels/cli.rs`) — internal, used by `zeroclaw agent` and tests

### Shared channel infrastructure

- `src/channels/debounce.rs` — debounces rapid-fire messages from the same sender so the agent doesn't reply to every keystroke
- `src/channels/session_backend.rs` + `session_store.rs` + `session_sqlite.rs` — per-sender session tracking (history, context, in-flight cancellation)
- `src/channels/stall_watchdog.rs` — kills runaway turns that have been processing for too long
- `src/channels/transcription.rs` — voice-note → text via OpenAI Whisper / Groq / etc. Used by Telegram voice messages.
- `src/channels/tts.rs` — text → speech (optional per channel)
- `src/channels/media_pipeline.rs` — image + document attachment handling
- `src/channels/link_enricher.rs` — auto-fetches and embeds URL previews

### Channel → agent message path

```
Channel::listen loop
│  (polling / websocket / long-poll, one per channel)
│
▼  ChannelMessage via mpsc::Sender
│
channels::run_channels dispatcher  (src/channels/mod.rs)
│  debounces, deduplicates, applies session scoping
│
▼
AppState's Arc<Mutex<Agent>>
│  single mutex — at most one turn active per agent
│
▼
Agent::turn(user_message)
│
▼  response text
│
Channel::send back to the origin
```

### HTTP gateway (`src/gateway/mod.rs:344`)

`run_gateway()` builds an `axum::Router` with these route groups:

- **Pairing** (`src/gateway/api_pairing.rs`) — `POST /pair` with X-Pairing-Code header. First-time device auth.
- **REST API** (`src/gateway/api.rs`) — `/api/v1/*` endpoints: status, doctor, skills, memory ops, config get/set, webhook receive. Bearer-token authenticated.
- **Webhook** — `POST /webhook` — untyped JSON into the webhook channel
- **WebSocket chat** — `/ws/chat` — bidirectional streaming turn using `Agent::turn_streamed` with mpsc forwarding
- **Nodes WebSocket** (`src/gateway/nodes.rs`) — optional multi-node coordination
- **Health** — `GET /health` (cheap), `GET /metrics` (Prometheus format)
- **Static files** (`src/gateway/static_files.rs`) — serves the web dashboard frontend

All routes go through `auth_rate_limit.rs` rate limiters and `PairingGuard` (constant-time token comparison, per-client pairing code verification).

`AppState` (defined in `gateway/mod.rs`) is the shared state every handler closes over:

```rust
pub struct AppState {
    pub agent: Arc<Mutex<Agent>>,
    pub config: Arc<Mutex<Config>>,
    pub provider: Arc<dyn Provider>,
    pub memory: Arc<dyn Memory>,
    pub observer: Arc<dyn Observer>,
    pub tools_registry: Arc<Vec<Box<dyn Tool>>>,
    pub pairing: Arc<PairingGuard>,
    pub rate_limiter: Arc<GatewayRateLimiter>,
    pub event_tx: broadcast::Sender<Value>,
    pub shutdown_tx: watch::Sender<bool>,
    // ... plus session queue, canvas store, device registry, etc.
}
```

### Session queue

`src/gateway/session_queue.rs` manages a bounded FIFO of pending turns so two rapid requests from the same user don't race — the mutex serializes execution, the queue gives a predictable ordering and backpressure.

## Security: autonomy, sandboxing, pairing, secrets

### Autonomy levels (`src/security/policy.rs:11`)

```rust
pub enum AutonomyLevel {
    Supervised,  // every non-allowlisted tool call requires explicit approval
    Semi,        // some tools auto-approved, others require approval
    Full,        // all allowed tools run without interactive approval
}
```

Configured via `[autonomy] level = ...` in `config.toml`. The agent's system prompt is built differently for each level — `SafetySection` in `prompt.rs` omits "ask before acting" instructions under `Full` autonomy.

### `SecurityPolicy` (`src/security/policy.rs:174`)

Compiled at boot from `config.autonomy`. Holds:

- `workspace_only: bool` — restrict file access to `workspace_dir`
- `allowed_roots: Vec<PathBuf>` — additional allowed paths outside the workspace
- `blocked_paths: Vec<PathBuf>` — explicit deny list
- `allowed_commands: Vec<String>` — shell-command allowlist
- `always_ask: Vec<String>` — tools that always require explicit approval
- `auto_approve: Vec<String>` — tools pre-approved at any autonomy level
- `level: AutonomyLevel`

The `DomainMatcher` handles glob and regex matching for HTTP requests (browser / web_fetch tool).

### Sandbox backends (`src/security/traits.rs:22`)

```
Sandbox trait
│
├── NoopSandbox        src/security/noop.rs        — default, pass-through
├── LandlockSandbox    src/security/landlock.rs    — Linux only, feature-gated
│                      Restricts file system syscalls via the Landlock LSM.
│                      Applied per-tool-execution.
└── SeatbeltSandbox    src/security/seatbelt.rs    — macOS, shelling out to
                                                     `sandbox-exec` (feature-gated,
                                                     experimental)
```

The factory (`src/security/detect.rs:8`) picks a sandbox at boot based on OS, feature flags, and available kernel features. Falls back to `NoopSandbox` if nothing else applies.

### Pairing (`src/security/pairing.rs`)

First-time device auth flow. When the gateway starts without a configured bearer token, it generates a pairing code and prints it to stdout. The first client to POST to `/pair` with the matching `X-Pairing-Code` header gets issued a bearer token. `PairingGuard::pairing_code()` returns the current code; `PairingGuard::require_pairing()` is the middleware check.

### Secret store (`src/security/mod.rs` + `secret_store.rs`)

Encrypts sensitive config fields (API keys, tunnel tokens pre-Phase-4) using ChaCha20-Poly1305 with a workspace-scoped key. `decrypt_optional_secret` and `encrypt_optional_secret` wrap read/write of `Option<String>` fields in `Config::load_or_init` and `save_to_path`.

### IAM-style policy (`src/security/iam_policy.rs`)

`evaluate_tool_access` and `evaluate_workspace_access` are separate from `SecurityPolicy` and implement a more granular permission model for multi-user setups. Currently not widely used but the infrastructure is in place.

## Observability

`Observer` trait at `src/observability/traits.rs:156`:

```rust
pub trait Observer: Send + Sync + 'static {
    fn record_event(&self, event: &ObserverEvent);
    // plus span start/end hooks
}
```

`ObserverEvent` is an enum covering every notable agent event: `TurnStarted`, `TurnEnded`, `ToolCallStarted`, `ToolCallFinished`, `ProviderCallStarted`, `ProviderCallFinished`, `CacheHit`, `CacheMiss`, `MemoryStored`, `MemoryRecalled`, `ChannelMessageReceived`, `ChannelMessageSent`, and so on. Every place in the agent that does something interesting calls `observer.record_event(&event)`.

### Backends (`src/observability/`)

- **`NoopObserver`** (`noop.rs`) — default if nothing else is enabled
- **`LogObserver`** (`log.rs`) — structured logs via `tracing`
- **`VerboseObserver`** (`verbose.rs`) — human-readable progress printer for interactive CLI use
- **`PrometheusObserver`** (`prometheus.rs`) — feature-gated behind `observability-prometheus` (default). Exposes counters, histograms, gauges via the `/metrics` HTTP endpoint.
- **`OtelObserver`** (`otel.rs`) — feature-gated behind `observability-otel`. OTLP exporter for distributed tracing, metrics.
- **`DoraObserver`** (`dora.rs`) — DORA metrics (deployment frequency, lead time, MTTR, change failure rate) — aggregates events over time.
- **`RuntimeTraceObserver`** (`runtime_trace.rs`) — writes event traces to disk for offline analysis via `zeroclaw doctor traces`.

`MultiObserver` (`multi.rs`) composes multiple observers so events fan out to all configured backends simultaneously.

### Prometheus metrics

`metrics.rs` inside the prometheus module defines the actual counters/gauges. Examples: `zeroclaw_turns_total`, `zeroclaw_provider_request_duration_seconds`, `zeroclaw_tool_calls_total{tool=...}`, `zeroclaw_memory_recall_hits_total`, `zeroclaw_cache_hits_total{cache_type=...}`. Served via the `/metrics` endpoint in the gateway.

## Configuration and state ownership

One fact drives everything: **`Config` is a 10,000-line struct in `src/config/schema.rs`**. It's the single source of truth for all tunables.

### Loading

`Config::load_or_init()` (in `schema.rs`, around line 8800):

1. Resolves `zeroclaw_dir` and `workspace_dir` (env vars > `active_workspace.toml` marker > default `~/.zeroclaw/`).
2. Creates the directories if missing.
3. Reads `config.toml` via `toml::from_str`.
4. Runs `serde_ignored` diagnostics to warn about unknown top-level keys (doesn't fail — just logs).
5. Calls `config.autonomy.ensure_default_auto_approve()` to merge the built-in auto-approve list with user overrides.
6. Sets `config.config_path` and `config.workspace_dir` (skipped during serialization).
7. Decrypts sensitive fields via `SecretStore::decrypt_optional_secret`.
8. Returns the fully hydrated `Config`.

Everything downstream takes `&Config` or `Arc<Mutex<Config>>` and reads whatever subsection it cares about. There's no per-subsystem config loader — the config is owned by `main.rs` / the gateway and handed to builders as a slice.

### Runtime mutation

The `Config` inside `AppState` is wrapped in `Arc<Mutex<Config>>` so HTTP handlers can update it at runtime (e.g. `POST /api/v1/config` from the dashboard). Updates go through `hydrate_config_for_save` in `gateway/api.rs` which preserves masked secrets (the UI shows `***` and the server has to re-inject the real value when saving).

### What lives where

| State | Lives in | Ownership |
| --- | --- | --- |
| Identity (SOUL.md, IDENTITY.md, AGENTS.md, ...) | `/vault-workspace` (K8s ConfigMap mount) | Dashboard backend (writer), agent (reader) |
| Memory | `{workspace}/memory/brain.db` (SQLite) or vault (Obsidian) | Agent only |
| Config | `{zeroclaw_dir}/config.toml` | Gateway handlers + CLI commands |
| Secrets (encrypted) | Same config.toml, encrypted fields | SecretStore |
| Conversation history (in-memory) | `Agent.history: Vec<ConversationMessage>` | Agent loop |
| Skills (installed) | `{workspace}/skills/*` | Skill loader |
| MCP server specs | Config + dynamic registration | MCP client pool |

## Where each subsystem lives

A quick reference for navigating the source tree. Every surviving subsystem is in one of these directories:

| Subsystem | Path | Entry point |
| --- | --- | --- |
| **Agent core** (the main loop) | `src/agent/` | `agent.rs:738` — `Agent::turn` |
| **System prompt builder** | `src/agent/prompt.rs` | `SystemPromptBuilder::build` (line 64) |
| **Tool dispatcher** (XML / Native parsing) | `src/agent/dispatcher.rs` | `ToolDispatcher` trait (line 21) |
| **Memory loader** (recall + context) | `src/agent/memory_loader.rs` | `DefaultMemoryLoader::load_context` |
| **Memory backends** | `src/memory/` | `memory::create_memory_with_storage_and_routes` (`mod.rs:225`) |
| **Providers** | `src/providers/` | `providers::create_provider_with_url` (`mod.rs:1051`) |
| **Tools** | `src/tools/` | `tools::all_tools_with_runtime` (`mod.rs`) |
| **Channels** | `src/channels/` | `Channel` trait (`traits.rs:95`) + `run_channels` / `start_channels` (`mod.rs`) |
| **Gateway** (HTTP + WebSocket) | `src/gateway/` | `run_gateway` (`mod.rs:344`) |
| **Security** | `src/security/` | `SecurityPolicy::from_config` (`policy.rs`) + `create_sandbox` (`detect.rs:8`) |
| **Observability** | `src/observability/` | `Observer` trait (`traits.rs:156`) + `MultiObserver` (`multi.rs`) |
| **Config schema** | `src/config/schema.rs` | `Config::load_or_init` (around line 8800) |
| **Runtime adapters** | `src/runtime/` | `RuntimeAdapter` trait (`traits.rs`) + `NativeRuntime` |
| **Cron / scheduler** | `src/cron/` | `Scheduler::run` (`scheduler.rs`) |
| **Heartbeat** | `src/heartbeat/` | `Heartbeat::tick` |
| **Hooks** (lifecycle) | `src/hooks/` | `HookRunner` |
| **Skills loader** | `src/skills/` | `load_skills_with_config` |
| **MCP client** | `src/tools/mcp_*.rs` | `McpClient` |
| **Authentication** (OAuth profiles) | `src/auth/` | `AuthService` |
| **Approval gate** | `src/approval/` | `ApprovalPolicy` |
| **Cost tracking** | `src/cost/` + `src/agent/cost.rs` | `CostTracker` |
| **Health reporting** | `src/health/` | `mark_component_ok`, `mark_component_unhealthy` |
| **Doctor (diagnostics)** | `src/doctor/` | `run` + `run_traces` |

Files you probably don't need to read unless you're specifically working on that feature:

- `src/i18n/` — locale-aware tool description loader (post-strip-down: only `en` remains)
- `src/multimodal.rs` — image + audio extraction helpers
- `src/commands/` — small CLI subcommands (crates of logic too specific for main.rs)
- `src/migration.rs` — config migrations between versions

## Further reading

- [`../reference/identity-vault.md`](../reference/identity-vault.md) — identity file roster, vault provisioning flow, failure-mode contract
- [`../reference/memory-future.md`](../reference/memory-future.md) — centralized memory service plan
- [`../reference/api/config-reference.md`](../reference/api/config-reference.md) — every config key and default
- [`../reference/api/providers-reference.md`](../reference/api/providers-reference.md) — provider IDs, aliases, credential resolution
- [`../reference/api/channels-reference.md`](../reference/api/channels-reference.md) — channel config + capabilities
- [`../reference/cli/commands-reference.md`](../reference/cli/commands-reference.md) — CLI subcommand surface
- [`../ops/operations-runbook.md`](../ops/operations-runbook.md) — day-2 ops
- [`../contributing/change-playbooks.md`](../contributing/change-playbooks.md) — how to add a provider / channel / tool
- [`../../STRIP_DOWN.md`](../../../../STRIP_DOWN.md) — what was deleted in Phases 1-5 and why
