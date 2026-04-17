# zeroclaw-agent API contract (1.0)

This document is the **authoritative specification** for the 1.0 rewrite of the EnterpriseAgentOS agent runtime. If you have to pick between this document and any other source, this document wins — open a PR to update it if reality diverges.

All references to "the legacy crate" below point to `packages/zeroclaw-core/`.

---

## 1. Overview & 1.0 scope

`zeroclaw-agent` is a single Rust binary that runs inside a Kubernetes pod. One pod = one agent. It receives a single env var pair at boot, hydrates its runtime configuration from the backend, serves a WebSocket gateway for operators, and runs an LLM-driven tool loop until each user turn completes.

### No default-value fallbacks (global rule)

Every runtime value is either (a) provided in the bootstrap payload or env and MUST be present — panic with a clear error if missing — or (b) a hardcoded constant in the binary. **No** `.unwrap_or_default()`, **no** `Default::default()` on config structs, **no** user-configurable settings with silent fallbacks. If you're tempted to write `.unwrap_or("localhost".into())`, STOP: either it comes from the payload or it's a constant. This rule applies everywhere — bootstrap parsing, env parsing, tool parameter parsing, WS protocol parsing.

### 1.0 does

- Boot from `ZEROCLAW_AGENT_ID` + `BACKEND_URL`. Nothing else.
- Fetch `AgentBootstrapPayload` from `GET {BACKEND_URL}/api/agents/{id}` using `Authorization: Bearer {agent_id}`.
- Write embedded personality templates (`SOUL.md`, `IDENTITY.md`, `BOOTSTRAP.md`) to `memory_dir` on first boot, idempotently. Substitute `{{prompt}}` in `BOOTSTRAP.md` with `systemPrompt` from the payload.
- Serve a WebSocket gateway on `gateway.host:gateway.port` (from payload).
- Execute the agent turn loop against `POST {BACKEND_URL}/v1/chat/completions` (OpenAI-compatible, SSE streamed).
- Dispatch tool calls, including `skill_exec` which hits `POST {BACKEND_URL}/api/graphql`.
- Enforce per-tool Allow/Deny policy from the bootstrap payload, **only** for `skill_exec`.

### 1.0 does NOT do (explicit, per-concern)

- **Channels** — no Slack, Discord, Telegram, Teams, WhatsApp, IMAP/SMTP, Twilio, MQTT. The ingress path is the WS gateway.
- **Cron / heartbeat** — the backend schedules work, not the pod.
- **Plugins** — no dynamic loading. Tools are compiled in.
- **Observability backends** — no Prometheus, no OpenTelemetry. Tracing goes to stderr; the gateway re-emits structured events over WS.
- **Cost tracking** — the backend records credits inside the LLM proxy.
- **Canvas, hooks, MCP, Obsidian, delegate/escalate** — not part of 1.0.
- **Doctor / health / migration** — none. If boot fails, exit non-zero.
- **Multimodal** — text only. No image/audio ingestion inside the pod.
- **i18n** — English-only prompts.
- **Sandboxing (landlock)** — the pod itself is the sandbox via Kubernetes.
- **Provider routing** — the backend LLM proxy chooses the model. The agent just POSTs and streams.
- **Tool approval** — enforced policy only (`Allow`/`Deny`). No `Ask` mode.
- **Local config file** — no `config.toml` anywhere.
- **Sessions management** — the WS handler holds per-connection state only. No persistence across reconnects beyond what `memory_dir` gives us.

---

## 2. Environment contract

The pod reads exactly two environment variables on startup. Both are required; boot panics with a clear error if either is missing or empty.

| Var | Required | Shape | Notes |
|-----|----------|-------|-------|
| `ZEROCLAW_AGENT_ID` | yes | UUID v4 string (36 chars with hyphens) | Validated with `uuid::Uuid::parse_str`. Invalid format → panic. |
| `BACKEND_URL` | yes | Absolute URL, `http://` or `https://`, no trailing slash | Validated via `reqwest::Url::parse`. Trailing slash is stripped before use. |

Nothing else is read from the environment. Pods do not read `RUST_LOG`-style user configuration via env (log level is hardcoded to `INFO` with `tracing_subscriber` filtering by module). No legacy `ZEROCLAW_SKILLS_BACKEND_URL`, no `ZEROCLAW_VAULT_DIR`.

```rust
pub struct Env {
    pub agent_id: uuid::Uuid,
    pub backend_url: String, // no trailing slash, no path suffix
}

impl Env {
    pub fn from_process() -> Result<Self, crate::error::Error>;
}
```

---

## 3. Bootstrap flow

### 3.1 HTTP call

```
GET {backend_url}/api/agents/{agent_id}
Authorization: Bearer {agent_id}
Accept: application/json
```

The backend endpoint is `AgentBootstrapController::GetBootstrap` in `apps/backend/Entities/Agents/AgentBootstrapController.cs`. Authentication uses the agent's own UUID as bearer token (enforced by `[AgentTokenAuth]` on the controller).

### 3.2 Response — `AgentBootstrapPayload`

Authoritative definition in `apps/backend/Entities/Agents/Types/AgentTypes.cs`. Reproduced here verbatim as Rust structs:

```rust
#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AgentBootstrapPayload {
    pub agent_id: uuid::Uuid,
    pub display_name: String,
    pub system_prompt: Option<String>,
    pub provider: AgentProviderBootstrap,
    pub proxy: AgentProxyBootstrap,
    pub gateway: AgentGatewayBootstrap,
    pub skills: Vec<AgentInstalledSkillSummary>,
    pub tool_permissions: AgentToolPermissionsBootstrap,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AgentProviderBootstrap {
    pub name: String,
    pub model: String,       // IGNORED by this crate — backend picks the model per request.
    pub api_url: String,
    pub token_ref: Option<String>, // Always None from backend; retained for wire compat.
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AgentProxyBootstrap {
    pub url: String,         // e.g. "https://api.officeos.co/v1"
    pub token: Option<String>, // Always None — agent uses its own UUID bearer.
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AgentGatewayBootstrap {
    pub host: String,
    pub port: i32,
    pub tls_cert_ref: Option<String>, // Unused in 1.0.
}

#[derive(Debug, Clone, serde::Deserialize)]
pub struct AgentInstalledSkillSummary {
    pub name: String,
}

#[derive(Debug, Clone, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AgentToolPermissionsBootstrap {
    pub entries: Vec<AgentBootstrapToolPermission>,
}

#[derive(Debug, Clone, serde::Deserialize)]
pub struct AgentBootstrapToolPermission {
    pub skill: String,
    pub tool: String,
    pub mode: String, // "allow" | "deny" (lowercase, enforced by backend).
}
```

### 3.3 Failure modes

- **Network error / 5xx** — retry with exponential backoff: 1s, 2s, 4s, 8s, 16s, 30s, capped at 30s, **10 attempts total**. After the 10th failure, panic (process exits non-zero; Kubernetes restarts the pod via `restartPolicy: Always`).
- **HTTP 401 / 403** — no retry. Panic immediately with a clear message. This means the backend has revoked the agent's token; human intervention is required.
- **HTTP 404** — no retry. Panic. The agent record does not exist.
- **HTTP 200 with invalid JSON** — no retry. Panic.

There is no local-file fallback. There is no "soft boot" mode. The backend is the source of truth; if it is unavailable after 10 attempts, the pod crashes and Kubernetes retries the whole boot.

---

## 4. RuntimeConfig

The fully hydrated config that the rest of the crate consumes. Constructed once, at boot, from `Env` + `AgentBootstrapPayload`. Never mutated afterwards.

```rust
#[derive(Debug, Clone)]
pub struct RuntimeConfig {
    pub agent_id: uuid::Uuid,
    pub backend_url: String,           // no trailing slash
    pub backend_token: String,         // == agent_id.to_string()
    pub memory_dir: std::path::PathBuf, // "/zeroclaw-data/memory" on pod, tempdir in tests
    pub gateway_host: String,
    pub gateway_port: u16,
    pub system_prompt: String,         // empty string if None in payload
    pub skills: Vec<SkillSummary>,
    pub tool_permissions: std::collections::HashMap<(String, String), Permission>,
    pub display_name: String,
}

#[derive(Debug, Clone)]
pub struct SkillSummary {
    pub name: String,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Permission { Allow, Deny }
```

### Derivations

- `backend_token` = `agent_id.to_string()`.
- `memory_dir` = `PathBuf::from("/zeroclaw-data/memory")` by default; tests override via a test-only constructor.
- `tool_permissions` key = `(skill.to_ascii_lowercase(), tool.to_ascii_lowercase())`. Case folding happens once here so downstream lookups are literal.
- Permission parsing: `"allow"` → `Allow`, `"deny"` → `Deny`. Any other string → treated as `Deny` with a warning log (fail-closed).
- The `provider.model` field is intentionally dropped. This crate never reads it.

---

## 5. Personality & prompt assembly

### 5.1 Embedded templates

Three `.md` files are compiled into the binary via `include_str!`:

| Template | Purpose | Contains `{{prompt}}` substitution? |
|----------|---------|-------------------------------------|
| `SOUL.md` | Agent-agnostic core values / voice | no |
| `IDENTITY.md` | Placeholder — the dashboard overwrites this file with agent-specific identity. 1.0 ships a generic stub. | no |
| `BOOTSTRAP.md` | Operator's system prompt injection point. Contains the literal string `{{prompt}}` where `payload.system_prompt` is written. | **yes** |

`AGENTS.md` is dropped from 1.0 entirely — multi-agent collaboration is not a 1.0 concern.

### 5.2 Idempotent write

On boot, for each template:

1. Compute `dst = memory_dir.join(name)`.
2. If `dst` exists (regardless of content), **skip**. Never overwrite.
3. Otherwise, render and write atomically (`tempfile::NamedTempFile` → `persist`).

`{{prompt}}` substitution is a literal string replace, done only in `BOOTSTRAP.md`, only on first write. If `system_prompt` is empty, the substitution yields an empty string (no error).

### 5.3 System prompt section composition

The system prompt is **re-composed on every turn** from a list of trait objects implementing `PromptSection`. Cheap enough; keeps the prompt fresh (e.g. current date).

```rust
pub trait PromptSection: Send + Sync {
    fn name(&self) -> &'static str;
    fn build(&self, ctx: &PromptContext<'_>) -> Result<String, crate::error::Error>;
}

pub struct PromptContext<'a> {
    pub memory_dir: &'a std::path::Path,
    pub tools: &'a [std::sync::Arc<dyn crate::tools::Tool>],
    pub skills: &'a [crate::config::SkillSummary],
    pub system_prompt: &'a str, // from bootstrap payload, already injected into BOOTSTRAP.md on disk too
}
```

Default section order (port of the legacy `SystemPromptBuilder::with_defaults`, minus `ChannelMediaSection`):

1. `DateTimeSection` — current local date + time + ISO 8601 + tz.
2. `IdentitySection` — concatenates the 3 personality `.md` files from `memory_dir` in this order: `SOUL.md`, `IDENTITY.md`, `BOOTSTRAP.md`. Truncates each file to 20_000 chars with a `[... truncated]` marker. Missing files are silently skipped (lenient loader; the strict check happens once at boot in `bootstrap.rs`).
3. `ToolHonestySection` — static text warning the model never to fabricate tool results.
4. `ToolsSection` — always yields empty in 1.0. Native tool calling is always on; tools are delivered via the `tools` array on each LLM request, never inlined in the system prompt. Kept as a registered section purely so the section list structurally mirrors the legacy builder; its `build` returns `Ok(String::new())`.
5. `SafetySection` — static safety rules; no autonomy levels in 1.0.
6. `SkillsSection` — lists installed skills by name so the model knows what `skill_exec --help` will surface.
7. `WorkspaceSection` — `Working directory: {memory_dir}`.
8. `RuntimeSection` — hostname + OS (no model name; the backend owns the model choice).

**Dropped from legacy:** `ChannelMediaSection`. Media markers are a channel concern; this crate has no channels.

Sections that produce empty output are dropped. Final output is sections joined by `\n\n`.

### 5.4 Strict boot check

At boot (not per-turn), assert that `SOUL.md`, `IDENTITY.md`, and `BOOTSTRAP.md` exist and are non-empty in `memory_dir`. If any are missing after the template write step, panic.

---

## 6. Agent turn loop

### 6.1 High-level pseudocode

```text
on user_message(text):
    history.push(user: text)
    loop:
        system_prompt = SystemPromptBuilder::default().build(&ctx)
        messages = [{role: system, content: system_prompt}, ...history]
        prune(&mut messages, max_tokens=8192, keep_recent=4)
        stream = llm.chat_completions(messages, tools=tool_specs())
        let mut assistant_text = String::new()
        let mut tool_calls = vec![]
        while let Some(event) = stream.next():
            match event:
                ContentDelta(text) => { ws.send(assistant_delta { text }); assistant_text.push_str(text) }
                ToolCallStart { id, name, args } => tool_calls.push(pending(id, name, args))
                ToolCallDelta { id, args_chunk } => extend(id, args_chunk)
                ToolCallStop { id } => finalize(id)
                Stop => break
                Error(e) => return ws.send(error { e })
        history.push(assistant: assistant_text, tool_calls)
        if tool_calls.is_empty(): break
        for call in tool_calls:
            loop_detector.record(&call)
            ws.send(tool_call_start { ... })
            result = dispatch(call)  // enforces tool_permissions for skill_exec
            ws.send(tool_call_result { ... })
            history.push(tool_result: call.id, result)
        continue
    ws.send(turn_complete)
```

### 6.2 Concrete rules

- **History** — a `Vec<ChatMessage>` owned by the per-WS-connection `Agent`. Reset only on explicit `cancel` or disconnect.
- **Pruning** — trigger when estimated tokens exceed `max_tokens = 8192`. Keep all `system` messages + last `keep_recent = 4` messages untouched. Collapse older tool-call/tool-result pairs into one-line summaries. Port the algorithm from `packages/zeroclaw-core/src/agent/history_pruner.rs` as-is.
- **Loop detection** — port the 3 patterns from `packages/zeroclaw-core/src/agent/loop_detector.rs`:
  1. **Exact repeat:** same tool + canonicalized args ≥ 3 times → `Warning` first, then `Block` (substitute a "stop repeating yourself" error for the tool result), then `Break` (terminate the turn).
  2. **Ping-pong:** two tools alternating A→B→A→B for ≥ 4 cycles → `Warning`.
  3. **No progress:** same tool ≥ 5 times with differing args but identical result hash → `Warning`.
  Window size: 20 records. Config is hardcoded, not exposed.
- **Termination**
  - Assistant returned no tool calls → emit `turn_complete`, break.
  - `LoopDetectionResult::Break` → emit `error { code: "LOOP" }` + `turn_complete`, break.
  - LLM stream returns an `error` event → emit `error`, break.
  - Client sent `cancel` → abort dispatch, emit `turn_complete { cancelled: true }`, break.
- **Token counting** — cheap estimator: `total_chars / 4 * 1.2`. The backend enforces real limits.

---

## 7. LLM client contract

### 7.1 Endpoint

```
POST {backend_url}/v1/chat/completions
Authorization: Bearer {agent_id}
Content-Type: application/json
Accept: text/event-stream
```

OpenAI-compatible. The backend (`apps/backend/Entities/LlmProxy/LlmProxyController.cs`) translates to Anthropic / LiteLLM as needed. This crate speaks **only** the OpenAI chat completions schema.

### 7.2 Request body

```json
{
  "model": "auto",
  "stream": true,
  "messages": [
    {"role": "system", "content": "..."},
    {"role": "user", "content": "Hello"},
    {"role": "assistant", "content": "...", "tool_calls": [
      {"id": "call_01", "type": "function",
       "function": {"name": "shell", "arguments": "{\"command\":\"ls\"}"}}
    ]},
    {"role": "tool", "tool_call_id": "call_01", "content": "README.md\nsrc/"}
  ],
  "tools": [
    {"type": "function", "function": {
      "name": "shell",
      "description": "...",
      "parameters": { /* JSON schema from Tool::parameters_schema */ }
    }}
  ],
  "tool_choice": "auto"
}
```

The `model` field is sent as the literal string `"auto"` — the backend resolves the real model from the agent record. We do not read `provider.model` from the bootstrap payload.

### 7.3 SSE event stream

The backend streams OpenAI-style SSE chunks. Each `data:` line is a JSON object. A final `data: [DONE]` terminates the stream.

```
data: {"choices":[{"delta":{"content":"Hel"}}]}
data: {"choices":[{"delta":{"content":"lo"}}]}
data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call_01","type":"function","function":{"name":"shell","arguments":""}}]}}]}
data: {"choices":[{"delta":{"tool_calls":[{"index":0,"function":{"arguments":"{\"command\":"}}]}}]}
data: {"choices":[{"delta":{"tool_calls":[{"index":0,"function":{"arguments":"\"ls\"}"}}]}}]}
data: {"choices":[{"delta":{},"finish_reason":"tool_calls"}]}
data: [DONE]
```

Parser contract:

```rust
pub enum LlmEvent {
    ContentDelta { text: String },
    ToolCallStart { index: usize, id: String, name: String },
    ToolCallArgsDelta { index: usize, args_chunk: String },
    Finish { reason: FinishReason },
    Error { message: String },
}

pub enum FinishReason { Stop, ToolCalls, Length, Other(String) }
```

Implementation uses `eventsource-stream` over the `reqwest::Response::bytes_stream()`. Partial tool-call `arguments` strings are concatenated in order of arrival; at `finish_reason == "tool_calls"`, the final concatenation is parsed as JSON before dispatch.

### 7.4 Failure modes

- Non-200 status → return an `Error::Llm` with status + body excerpt. No retry inside the LLM client; the turn loop decides whether to retry (it does not; it emits `error` to the WS and terminates the turn).
- SSE connection drop mid-stream → same treatment as above.
- Malformed chunk → log a warning, skip the chunk, continue.

---

## 8. Gateway WebSocket protocol

### 8.1 Endpoint

```
ws://{gateway_host}:{gateway_port}/ws
```

No TLS on the pod itself; TLS is terminated upstream by the backend's `/api/agents/{id}/ws` passthrough.

### 8.2 Auth

The agent UUID bearer is passed as a **query parameter** on the WebSocket upgrade URL:

```
ws://{gateway_host}:{gateway_port}/ws?token=<agent_uuid>
```

If `token` is missing or ≠ `agent_id`, the server rejects the upgrade with HTTP `401 Unauthorized` (no WS connection established). There is **no `hello` handshake frame** — the client connects and sends `user_message` directly.

Query param chosen over upgrade header: browsers cannot set custom headers on `WebSocket` constructors, so the query-param path works uniformly for the dashboard's `new WebSocket(url)` and for `tokio-tungstenite` clients.

### 8.3 Message envelope

All frames are JSON text. Every frame has a `type` string. Additional fields are type-specific.

### 8.4 Client → Server messages

| `type` | Fields | Meaning |
|--------|--------|---------|
| `user_message` | `text: string`, `id?: string` | Start a new turn. `id` is a client-chosen correlation id (echoed in all downstream events). |
| `cancel` | `id?: string` | Cancel the current turn. If `id` is given, cancel only if it matches the active turn. |

### 8.5 Server → Client messages

| `type` | Fields | Meaning |
|--------|--------|---------|
| `assistant_delta` | `turn_id: string`, `text: string` | Incremental assistant text. |
| `tool_call_start` | `turn_id: string`, `call_id: string`, `tool: string`, `args: object` | A tool is about to run. |
| `tool_call_result` | `turn_id: string`, `call_id: string`, `tool: string`, `success: bool`, `output: string`, `error?: string` | Tool finished. |
| `turn_complete` | `turn_id: string`, `cancelled: bool` | End of turn. |
| `error` | `turn_id?: string`, `code: string`, `message: string` | Runtime error. Codes: `LOOP`, `LLM`, `TOOL`, `BAD_REQUEST`, `INTERNAL`. |

### 8.6 Example session

```
(client opens ws://.../ws?token=123e4567-...)
C → S: {"type":"user_message","id":"t1","text":"list my files"}
S → C: {"type":"assistant_delta","turn_id":"t1","text":"Sure, "}
S → C: {"type":"tool_call_start","turn_id":"t1","call_id":"call_01","tool":"shell","args":{"command":"ls"}}
S → C: {"type":"tool_call_result","turn_id":"t1","call_id":"call_01","tool":"shell","success":true,"output":"README.md\nsrc/"}
S → C: {"type":"assistant_delta","turn_id":"t1","text":"You have README.md and src/."}
S → C: {"type":"turn_complete","turn_id":"t1","cancelled":false}
```

### 8.7 Cardinality

One active turn per connection. If the client sends `user_message` while a turn is already in flight, the server responds with `error { code: "BAD_REQUEST", message: "turn in flight" }` and does not start a second turn.

---

## 9. Tool trait

Reproduced verbatim from the legacy crate (`packages/zeroclaw-core/src/tools/traits.rs`). This is **the only** trait clients of the crate extend.

```rust
use async_trait::async_trait;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolResult {
    pub success: bool,
    pub output: String,
    pub error: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ToolSpec {
    pub name: String,
    pub description: String,
    pub parameters: serde_json::Value,
}

#[async_trait]
pub trait Tool: Send + Sync {
    fn name(&self) -> &str;
    fn description(&self) -> &str;
    fn parameters_schema(&self) -> serde_json::Value;
    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult>;

    fn spec(&self) -> ToolSpec {
        ToolSpec {
            name: self.name().to_string(),
            description: self.description().to_string(),
            parameters: self.parameters_schema(),
        }
    }
}
```

No changes from legacy.

A `Registry` struct in `src/tools/mod.rs` holds `Vec<Arc<dyn Tool>>`, supports `get(name)` and `specs()`, and is built once per `Agent` at construction.

---

## 10. Tools catalog

Every tool's current behaviour is the authority. Paths below point at the legacy files to copy-adapt from.

Permission enforcement: **only `skill_exec` consults `tool_permissions`**. All other tools are always allowed (the backend has no concept of their dispatch — they are local to the pod).

### 10.1 `skill_exec`

- **Name:** `skill_exec`
- **Description:** `Execute a skill command. Works like a CLI: use --help at any level to discover skills, actions, and arguments. Examples: "--help", "notion --help", "notion search --query meetings", "github repos --visibility PRIVATE".`
- **Parameters:**
  ```json
  {"type":"object","properties":{"command":{"type":"string","description":"CLI-style command. Use --help to discover available skills and actions."}},"required":["command"]}
  ```
- **Returns:** `ToolResult { success, output, error }` where `output` is pretty-printed GraphQL `data` JSON on success or the `--help` render text.
- **External dependency:** `POST {backend_url}/api/graphql` with the agent's UUID bearer.
- **Behaviour:**
  1. Parse `command` via deterministic CLI parser → `ParsedCommand::Help(level)` or `ParsedCommand::Query { skill, action, args }`. Parser grammar: `[skill [action]] [--flag value | --flag=value | --flag]*`. Unquoted tokens are split on whitespace; quoted tokens preserve whitespace. Boolean flags with no value are `true`.
  2. Ensure the GraphQL schema cache is loaded. On first use, run an introspection query: `{ __schema { queryType { fields { name args { name type { kind name ofType { kind name ofType { kind name } } } } } } mutationType { fields { name args { ... } } } } }`. Cache forever (no TTL; 1.0 agents are short-lived).
  3. If `Help`: render cached help text for the level (`root`, `skill`, `action`). See the legacy `schema_cache.rs` for the exact render format (preserved verbatim).
  4. If `Query`:
     - Consult `tool_permissions` with key `(skill.to_ascii_lowercase(), action.to_ascii_lowercase())`. On `Deny` → return `ToolResult { success: false, error: Some("tool denied by policy: {skill}:{action} is set to deny for this agent") }`.
     - Build a GraphQL query via `query_builder`: field name = `{skill}_{action}` or nested — follow the legacy builder (`tools/skill_exec/query_builder.rs`).
     - POST to `{backend_url}/api/graphql` with `Content-Type: application/json` + bearer auth.
     - Truncate response at 1 MiB with `"\n... [response truncated at 1MB]"` suffix.
     - Parse response: if `errors` present → `success = false`, `output = pretty-printed errors`, `error = "GraphQL query returned errors"`. If `data` present → `success = true`, `output = pretty-printed data`. Else pass through text with status-derived success.
- **Error modes:** unreachable backend → `success = false, error = "Could not load skill schema from backend: {e}"`; HTTP build failure; policy deny; GraphQL errors; parse failures in CLI string (rendered as `ToolResult { success: false, output: "<usage error>" }`).
- **Copy-from map:** lift-with-medium-changes from `packages/zeroclaw-core/src/tools/skill_exec/{mod.rs,parser.rs,query_builder.rs,schema_cache.rs}`. Strip the `ToolPermission` import and replace with this crate's `config::Permission`. Keep the parser grammar verbatim.

### 10.2 `memory_store`

- **Name:** `memory_store`
- **Description:** `Store a fact, preference, or note in long-term memory. Use category 'core' for permanent facts, 'daily' for session notes, 'conversation' for chat context, or a custom category name.`
- **Parameters:**
  ```json
  {"type":"object","properties":{
    "key":{"type":"string","description":"Unique key for this memory"},
    "content":{"type":"string","description":"The information to remember"},
    "category":{"type":"string","description":"'core' | 'daily' | 'conversation' | custom. Defaults to 'core'."}
  },"required":["key","content"]}
  ```
- **Returns:** `success=true`, `output = "Stored {key} in {category}"`.
- **External dependency:** local FS. Writes `{memory_dir}/{category}/{key}.md` with a YAML front-matter header (`created`, `key`, `category`).
- **Permission check:** none beyond the policy being `Allow`/absent for `skill_exec` — this tool is always allowed.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/memory_store.rs`. Strip the `SecurityPolicy` dependency; keep the `Memory` trait abstraction over markdown backend.

### 10.3 `memory_recall`

- **Name:** `memory_recall`
- **Description:** `Search long-term memory for relevant facts. Returns scored results ranked by relevance. Supports keyword search, time-only query (since/until), or both.`
- **Parameters:** `{query?: string, limit?: int=5, since?: RFC3339, until?: RFC3339}`. No `search_mode` in 1.0 — BM25 only (no embeddings).
- **Returns:** pretty-printed list: `## {key} ({category}, {timestamp})\n{content}\n` per hit.
- **External dependency:** local FS scan of `memory_dir/**/*.md`.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/memory_recall.rs`. Drop the embedding and hybrid code paths; retain BM25 over tokenized file contents.

### 10.4 `memory_forget`

- **Name:** `memory_forget`
- **Description:** `Remove a memory by key. Use to delete outdated facts or sensitive data.`
- **Parameters:** `{key: string, category?: string}`. When `category` is omitted, searches all categories and deletes the first match.
- **Returns:** `success=true` if removed; `success=false, error="not found"` otherwise.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/memory_forget.rs`. Strip security policy.

### 10.5 `shell`

- **Name:** `shell`
- **Description:** `Run a shell command inside the agent workspace and return stdout/stderr.`
- **Parameters:** `{command: string, timeout_secs?: int=60, cwd?: string}`. `cwd` is resolved relative to `memory_dir` and must stay inside it.
- **Returns:** `success = (exit_code == 0)`, `output = "stdout:\n{stdout}\nstderr:\n{stderr}\nexit: {code}"`.
- **Behaviour:** spawns `/bin/sh -lc {command}` on Unix. Env vars whitelisted to `PATH HOME TERM LANG LC_ALL LC_CTYPE USER SHELL TMPDIR`. Output truncated at 1 MiB. Timeout kills via `Child::kill`.
- **Copy-from map:** lift-with-minor-changes from `packages/zeroclaw-core/src/tools/shell.rs`. Drop the `RuntimeAdapter` and `Sandbox` wiring — 1.0 runs directly.

### 10.6 `file_read`

- **Name:** `file_read`
- **Description:** `Read file contents with line numbers. Supports partial reading via offset and limit.`
- **Parameters:** `{path: string, offset?: int=1, limit?: int}`. Relative paths resolve from `memory_dir`.
- **Returns:** line-numbered content (`cat -n` style).
- **Behaviour:** Max file size 10 MiB. Binary files read with lossy UTF-8. No PDF extraction in 1.0 (drop the `pdf-extract` path).
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/file_read.rs`. Drop PDF and SecurityPolicy.

### 10.7 `file_write`

- **Name:** `file_write`
- **Description:** `Write contents to a file in the workspace.`
- **Parameters:** `{path: string, content: string}`. Relative to `memory_dir`; absolute paths are rejected.
- **Returns:** `success=true, output="wrote {n} bytes to {path}"`.
- **Behaviour:** atomic write via tempfile+rename. Creates parent dirs.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/file_write.rs`.

### 10.8 `file_edit`

- **Name:** `file_edit`
- **Description:** `Edit a file by replacing an exact string match with new content.`
- **Parameters:** `{path: string, old_string: string, new_string: string, replace_all?: bool=false}`. `old_string` must occur exactly once unless `replace_all` is true.
- **Returns:** `success=true, output="edited {path}"`.
- **Copy-from map:** lift-with-minor-changes from `packages/zeroclaw-core/src/tools/file_edit.rs`.

### 10.9 `http_request`

- **Name:** `http_request`
- **Description:** `Make an HTTP request to an API endpoint.`
- **Parameters:** `{url: string, method?: "GET"|"POST"|"PUT"|"DELETE"|"PATCH", headers?: object, body?: string, timeout_secs?: int=30}`.
- **Returns:** `{status, headers, body}` serialized as a JSON string in `output`.
- **Behaviour:** denies private IP ranges by default (RFC1918, link-local, loopback). 5 MiB response cap.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/http_request.rs`. Drop allowed-domains policy wiring (everything is allowed except private IPs). Keep the private-IP guard.

### 10.10 `web_fetch`

- **Name:** `web_fetch`
- **Description:** `Fetch a web page and convert HTML to plain text for LLM consumption.`
- **Parameters:** `{url: string, timeout_secs?: int=30}`.
- **Returns:** plain text body.
- **Behaviour:** GET only, follows redirects (≤10), converts HTML via `nanohtml2text`. **No Firecrawl fallback in 1.0.** 5 MiB response cap.
- **Copy-from map:** adapt `packages/zeroclaw-core/src/tools/web_fetch.rs`. Drop Firecrawl. Uses `nanohtml2text` for HTML-to-text conversion.

### 10.11 `content_search`

- **Name:** `content_search`
- **Description:** `Search file contents by regex pattern within the workspace.`
- **Parameters:** `{pattern: string, path?: string=".", glob?: string, case_insensitive?: bool, line_numbers?: bool=true, max_results?: int=1000}`.
- **Returns:** ripgrep-style `{path}:{line}:{match}` lines.
- **Behaviour:** uses `rg` when available (shelled out), falls back to `grep -rn -E`. 1 MiB output cap, 30s timeout.
- **Copy-from map:** lift-with-minor-changes from `packages/zeroclaw-core/src/tools/content_search.rs`.

### 10.12 `glob_search`

- **Name:** `glob_search`
- **Description:** `Search for files matching a glob pattern within the workspace.`
- **Parameters:** `{pattern: string}`. E.g. `**/*.rs`.
- **Returns:** newline-separated sorted list of relative paths (max 1000).
- **Copy-from map:** lift-with-minor-changes from `packages/zeroclaw-core/src/tools/glob_search.rs`.

### 10.13 `tool_search` — DROPPED

Dropped from 1.0 entirely. Skill discovery is done exclusively via `skill_exec --help` at root, skill, or action level. The LLM is taught this pattern in the system prompt.

---

## 11. Memory & personality file I/O

### 11.1 `memory_dir` layout

```
{memory_dir}/
  SOUL.md                    Personality — always present after boot.
  IDENTITY.md                Personality — always present after boot.
  BOOTSTRAP.md               Personality — always present after boot, with the operator's system prompt substituted in.
  core/                      Category directory, created lazily by memory_store.
    preferences.md
    ...
  daily/
    2026-04-16-standup.md
  conversation/
    ...
  {custom}/                  User-defined categories.
```

### 11.2 Memory file format

```
---
key: preferences
category: core
created: 2026-04-16T12:34:56Z
---
The operator prefers concise replies and Markdown output.
```

### 11.3 Memory trait

```rust
#[async_trait::async_trait]
pub trait Memory: Send + Sync {
    async fn store(&self, key: &str, content: &str, category: MemoryCategory) -> Result<(), Error>;
    async fn recall(&self, query: &MemoryQuery) -> Result<Vec<MemoryHit>, Error>;
    async fn forget(&self, key: &str, category: Option<&str>) -> Result<bool, Error>;
}

pub enum MemoryCategory { Core, Daily, Conversation, Custom(String) }

pub struct MemoryQuery {
    pub text: Option<String>,
    pub since: Option<chrono::DateTime<chrono::Utc>>,
    pub until: Option<chrono::DateTime<chrono::Utc>>,
    pub limit: usize,
}

pub struct MemoryHit {
    pub key: String,
    pub category: String,
    pub content: String,
    pub created: chrono::DateTime<chrono::Utc>,
    pub score: f32,
}
```

Implementation: `MarkdownMemory { root: PathBuf }`, walking the category directories on each `recall`. Port from `packages/zeroclaw-core/src/memory/{traits.rs,markdown.rs}` — **lift with moderate trimming** (drop embeddings, SQLite, remote, namespaced, response cache).

No DB. No sync to backend.

---

## 12. Error types

Top-level error owned by `src/error.rs`:

```rust
#[derive(thiserror::Error, Debug)]
pub enum Error {
    #[error("environment: {0}")]
    Env(String),

    #[error("bootstrap http error: {0}")]
    BootstrapHttp(#[from] reqwest::Error),

    #[error("bootstrap unauthorized")]
    BootstrapUnauthorized,

    #[error("bootstrap agent not found")]
    BootstrapNotFound,

    #[error("bootstrap invalid payload: {0}")]
    BootstrapPayload(String),

    #[error("personality: required file {0} missing or empty")]
    Personality(String),

    #[error("llm: {0}")]
    Llm(String),

    #[error("memory io: {0}")]
    MemoryIo(#[from] std::io::Error),

    #[error("gateway: {0}")]
    Gateway(String),

    #[error("tool {tool}: {message}")]
    Tool { tool: String, message: String },

    #[error(transparent)]
    Other(#[from] anyhow::Error),
}

pub type Result<T> = std::result::Result<T, Error>;
```

All `Result<T>` inside the crate uses this alias. Tool implementations still return `anyhow::Result<ToolResult>` per the `Tool` trait (compatibility with the trait; surface-level errors come back as `ToolResult { success: false, error: Some(...) }`).

---

## 13. Configuration knobs

There are none that the operator controls at the pod level. The only surface area is:

- Env vars (§2): `ZEROCLAW_AGENT_ID`, `BACKEND_URL`.
- CLI flags: `--help`, `--version`. Nothing else.
- The bootstrap payload, which is itself sourced from backend state that dashboards configure.

No `config.toml`, no `RUST_LOG`, no `~/.zeroclaw/*`. Do not add any.

---

## 14. Observability

- **Logging:** `tracing_subscriber::fmt().with_writer(std::io::stderr)` at `INFO` level. Module-level filtering via a hardcoded `EnvFilter::new("info,zeroclaw_agent=debug")`.
- **No metrics.** No Prometheus endpoint, no OTEL exporter.
- **No structured log shipping.** The WS gateway re-emits `tool_call_start`, `tool_call_result`, `assistant_delta`, and `error` events — operator observability lives there and in the backend's `AgentLogs` domain.

The backend's `/api/agents/{id}/proxy/*` passthrough lets operators scrape stderr via `kubectl logs` through the dashboard; that's the whole story.

---

## 15. Non-goals (do not add these without product approval)

- Cost tracking — backend owns credits.
- Channels — no native Slack / Discord / Telegram / Teams / WhatsApp / Twilio / iMessage / IMAP / MQTT.
- Multi-agent delegation (`delegate`, `escalate` tools).
- MCP — no client, no transport, no deferred loading.
- Obsidian vault integration.
- First-party plugins outside the compiled-in tool set.
- Cron / heartbeat — the backend schedules.
- Auth schemes beyond the agent-UUID bearer.
- Tool approval flows / `Ask` mode.
- Canvas / visual workspace.
- i18n / non-English prompts.
- Landlock / seccomp sandboxing inside the binary.
- TUI (ratatui/dialoguer/crossterm).
- Interactive CLI prompts.
- Binary self-update.
- Any embedding-based vector memory.
- `tool_search` tool — dropped. `skill_exec --help` is the sole skill-discovery surface.
- Default-value fallbacks for any runtime config (see §1 Overview). Every value is explicit or the process panics.

---

## 16. File layout

Final `src/` tree, ~26 Rust files plus 3 embedded `.md` templates. Keep it flat.

```
packages/zeroclaw-core-2/
  Cargo.toml
  Dockerfile
  CLAUDE.md
  API.md
  src/
    main.rs                 Entry — loads env, runs bootstrap, spawns gateway + loop, blocks on shutdown signal.
    lib.rs                  Re-exports for tests.
    env.rs                  `Env::from_process` — reads and validates the two env vars.
    bootstrap.rs            `GET /api/agents/{id}`, retry loop, `AgentBootstrapPayload` → `RuntimeConfig`, strict personality check.
    config.rs               `RuntimeConfig`, `SkillSummary`, `Permission`.
    error.rs                Top-level `Error` enum + `Result<T>`.
    llm.rs                  OpenAI-compat client: request builder, SSE parser, `LlmEvent`, `FinishReason`, `ToolCallAccumulator`.
    agent/
      mod.rs                `Agent` struct — owns history, registry, memory, llm client, config.
      turn_loop.rs          The turn loop. Drives `LlmEvent` stream → WS events → tool dispatch.
      prompt.rs             `PromptSection` trait, 8 default section impls, `SystemPromptBuilder`.
      history.rs            `ChatMessage`, `History`, `ToolCall` record types.
      history_pruner.rs     Token estimator + pruning algorithm (port of legacy).
      loop_detector.rs      Sliding-window detector with 3 patterns (port of legacy).
    personality/
      mod.rs                `write_templates_if_absent`, `load_personality_strict`, `load_personality_lenient`.
      templates/
        SOUL.md
        IDENTITY.md
        BOOTSTRAP.md
    gateway/
      mod.rs                `serve(config, agent_factory).await` — starts Axum app on host:port.
      ws.rs                 Axum WS upgrade handler with `?token=` query-param auth, per-connection `Agent` lifecycle.
      protocol.rs           Client/server message enums with `#[serde(tag = "type")]` — the wire types.
    memory/
      mod.rs                `Memory` trait, `MemoryCategory`, `MemoryQuery`, `MemoryHit`.
      markdown.rs           `MarkdownMemory` — filesystem-backed impl with BM25 search.
    tools/
      mod.rs                `Registry`, factory `build_default(...)`.
      traits.rs             `Tool`, `ToolResult`, `ToolSpec`.
      skill_exec.rs         All of `SkillExecTool` + parser + query builder + schema cache inlined or kept as a submodule tree (`skill_exec/{mod.rs,parser.rs,query_builder.rs,schema_cache.rs}` if it exceeds 600 LOC).
      memory_store.rs
      memory_recall.rs
      memory_forget.rs
      shell.rs
      file_read.rs
      file_write.rs
      file_edit.rs
      http_request.rs
      web_fetch.rs
      content_search.rs
      glob_search.rs
  tests/
    bootstrap_integration.rs
    turn_loop_integration.rs
    tools_integration.rs
    gateway_integration.rs
    personality_integration.rs
```

Budget: **no file exceeds 500 LOC.** If one starts to, split it. The whole crate targets ≤ 5,000 LOC.

---

## 17. Integration test plan

Phase 2 delivers these tests against the spec. Each is end-to-end against local fakes (`wiremock` for HTTP/SSE, an in-process Axum test client for WS). No tests hit the real backend.

| # | Name | Verifies |
|---|------|----------|
| 1 | `bootstrap_fetches_and_builds_runtime_config` | `GET /api/agents/{id}` with correct headers, JSON parse, `RuntimeConfig` fields populated. Uses `wiremock` serving a canned `AgentBootstrapPayload`. |
| 2 | `bootstrap_retries_on_5xx_up_to_ten_times` | First 9 responses are 503, 10th is 200; verifies the pod boots successfully and backoff caps at 30s. |
| 3 | `bootstrap_panics_on_401` | No retry on unauthorized. |
| 4 | `personality_templates_written_on_first_boot` | Empty `memory_dir` → all 3 `.md` files exist after boot, `{{prompt}}` substituted. |
| 5 | `personality_templates_skip_when_present` | Pre-populate `SOUL.md` with custom content → unchanged after boot. |
| 6 | `turn_loop_streams_assistant_text_only` | Mock LLM returns 3 content chunks + stop. WS receives 3 `assistant_delta` + `turn_complete`, no tool calls. |
| 7 | `turn_loop_dispatches_tool_and_continues` | Mock LLM returns `shell` tool call, then (after tool result) a plain text completion. WS receives `tool_call_start`, `tool_call_result`, `assistant_delta`, `turn_complete`. |
| 8 | `turn_loop_prunes_history_at_token_threshold` | Push 100 long messages, trigger a turn, assert the messages sent to LLM include only system + keep_recent + collapsed summaries. |
| 9 | `turn_loop_breaks_on_exact_repeat_loop` | LLM repeats the same `shell` call 3× with the same args → detector emits `Break`, WS receives `error { code: "LOOP" }` then `turn_complete`. |
| 10 | `skill_exec_denied_by_policy` | Bootstrap says `{skill: "notion", tool: "search", mode: "deny"}` → tool dispatch returns `success=false` with the deny message; no GraphQL call to the backend. |
| 11 | `skill_exec_help_uses_cache` | First `--help` triggers one introspection POST; second `--help` triggers zero. |
| 12 | `ws_rejects_wrong_token_query_param` | Client connects with `?token=<wrong>` → server rejects the upgrade with HTTP 401. |
| 13 | `ws_user_message_while_in_flight_errors` | Second `user_message` during an active turn → server sends `error { code: "BAD_REQUEST" }`. |
These are the TDD targets. The crate is complete when all tests go green and clippy is clean.

---

## 18. Copy-from-zeroclaw-core map

Summary: numbers are indicative; consult the tables below for the authoritative breakdown.

### Lift (minor import rewiring + strip `SecurityPolicy`/`RuntimeAdapter` unless noted)

| New path | Legacy path |
|----------|-------------|
| `src/tools/traits.rs` | `src/tools/traits.rs` |
| `src/tools/shell.rs` | `src/tools/shell.rs` |
| `src/tools/file_edit.rs` | `src/tools/file_edit.rs` |
| `src/tools/glob_search.rs` | `src/tools/glob_search.rs` |
| `src/tools/content_search.rs` | `src/tools/content_search.rs` |
| `src/agent/history_pruner.rs` | `src/agent/history_pruner.rs` |
| `src/agent/loop_detector.rs` | `src/agent/loop_detector.rs` |
| `src/agent/history.rs` | `src/agent/history.rs` |
| `src/tools/skill_exec.rs::parser` | `src/tools/skill_exec/parser.rs` |
| `src/tools/skill_exec.rs::query_builder` | `src/tools/skill_exec/query_builder.rs` |
| `src/personality/templates/SOUL.md` | `apps/backend/Entities/Vault/Templates/SOUL.md` (lift verbatim) |
| `src/personality/templates/IDENTITY.md` | `apps/backend/Entities/Vault/Templates/IDENTITY.md` (strip `{{agent_name}}` etc. — 1.0 ships a generic stub) |
| `src/personality/templates/BOOTSTRAP.md` | NEW — author fresh with a clearly present `{{prompt}}` token |

### Adapt (medium changes — strip legacy features, re-thread config types)

| New path | Legacy path | Notes |
|----------|-------------|-------|
| `src/tools/skill_exec.rs` (top level + schema_cache) | `src/tools/skill_exec/{mod.rs,schema_cache.rs}` | Swap `ToolPermission` for `config::Permission`; strip `gateway_bootstrap` imports. |
| `src/tools/file_read.rs` | `src/tools/file_read.rs` | Drop PDF extraction path; strip `SecurityPolicy`. |
| `src/tools/file_write.rs` | `src/tools/file_write.rs` | Strip `SecurityPolicy`. |
| `src/tools/http_request.rs` | `src/tools/http_request.rs` | Keep private-IP guard; drop allowed-domains config. |
| `src/tools/web_fetch.rs` | `src/tools/web_fetch.rs` | Drop Firecrawl fallback. |
| `src/tools/memory_store.rs` | `src/tools/memory_store.rs` | Drop `SecurityPolicy`; plug into new `Memory` trait. |
| `src/tools/memory_recall.rs` | `src/tools/memory_recall.rs` | BM25 only. |
| `src/tools/memory_forget.rs` | `src/tools/memory_forget.rs` | Strip `SecurityPolicy`. |
| `src/memory/mod.rs` (trait) | `src/memory/traits.rs` | Keep surface narrow. |
| `src/memory/markdown.rs` | `src/memory/markdown.rs` | Drop embeddings, SQLite backends; keep BM25 + front-matter parser. |
| `src/agent/prompt.rs` | `src/agent/prompt.rs` | Drop `ChannelMediaSection`; drop autonomy-level branches in `SafetySection`; drop `i18n` lookup in `ToolsSection`. |
| `src/personality/mod.rs` | `src/agent/personality.rs` | Keep strict + lenient loaders. Add `write_templates_if_absent`. |
| `src/gateway/protocol.rs` | n/a (new) | Shape defined in §8; can borrow JSON schema ideas from legacy `gateway/ws.rs`. |

### Rewrite (legacy file is the tangle we're escaping)

| New path | Why |
|----------|-----|
| `src/agent/turn_loop.rs` | Legacy `agent/loop_.rs` is 4615 LOC. Rewrite from §6 pseudocode. |
| `src/agent/mod.rs` | Legacy `agent/agent.rs` is 1592 LOC — rewire minimally. |
| `src/llm.rs` | Legacy `providers/` dir is a routing system. 1.0 speaks OpenAI-compat SSE only. |
| `src/bootstrap.rs` | Legacy `agent/gateway_bootstrap.rs` carries provider-seeding that 1.0 drops. |
| `src/env.rs` | Legacy config loader handles too many surfaces. Here: two env vars. |
| `src/gateway/mod.rs` | Axum app wiring — fresh. |
| `src/gateway/ws.rs` | Query-param auth (§8.2); legacy had pairing/subprotocol/header logic. |

---

## 19. Resolved design questions

Phase 2 kickoff resolved every open question in Phase 1. Resolutions are baked into the body of this document; this section records the decisions for traceability.

1. **LLM endpoint — RESOLVED: `POST {backend_url}/v1/chat/completions` (OpenAI-compatible).**
   Request body follows OpenAI Chat Completions shape (`messages`, `tools`, `stream: true`, `tool_choice: "auto"`). SSE response uses OpenAI `data:` frames with `delta.content`, `delta.tool_calls`, and `finish_reason`. Authorization header is `Bearer {agent_id}`. All Anthropic-style `/v1/messages` references are removed from this spec.

2. **Tool-schema delivery — RESOLVED: native tool calling always on.**
   `ToolsSection` of the system prompt always yields empty. Tool schemas are delivered on every LLM request via the `tools` array, not inlined in the system prompt. The backend's LLM proxy is responsible for translating to each provider's native tool-calling API.

3. **`memory_dir` path — RESOLVED: `/zeroclaw-data/memory`.**
   Fresh path; PVCs are per-agent and 1.0 deploys are new.

4. **Gateway handshake — RESOLVED: no `hello` frame.**
   The client connects and sends `user_message` directly. The agent UUID bearer is passed as a **WebSocket URL query parameter** (`?token=<uuid>`), not as an upgrade header (browsers cannot set custom headers on `new WebSocket(url)`). Missing or mismatched token → the upgrade is rejected with HTTP 401 before any WS frames are exchanged.

5. **Gateway port — RESOLVED: strictly from the bootstrap payload.**
   `payload.gateway.port` must be a non-zero positive integer in `1..=65535`. `0`, missing, or negative → `Error::BootstrapPayload` at boot. No default. No fallback.

6. **`tool_search` tool — RESOLVED: DROPPED.**
   Skill discovery is done exclusively via `skill_exec --help` (root / skill / action levels). `tool_search` is removed from the tool catalog, file layout, copy-from map, integration test plan, and prompt sections.

7. **Personality `AGENTS.md` — RESOLVED: DROPPED.**
   Embedded templates are SOUL.md, IDENTITY.md, BOOTSTRAP.md (three files, not four). The `AGENTS.md` notion of multi-agent collaboration is deferred until multi-agent actually ships.

8. **Bootstrap retry budget — RESOLVED: 10 attempts, 1→30s exponential cap.** Already in §3.3.

9. **`skill_exec` schema cache TTL — RESOLVED: forever (no invalidation).** Already in §10.1. Operators restart the pod to pick up newly installed skills.

10. **`shell` runtime image deps — RESOLVED: bash, git, ripgrep, curl.** Already in the Dockerfile. No node runtime by default.

### Global rule derived from these resolutions

> **No default-value fallbacks.** Every runtime value is either (a) provided in the bootstrap payload or env and must be present — panic with a clear error if missing — or (b) a hardcoded constant in the binary. No `.unwrap_or_default()`, no `Default::default()` on config structs, no user-configurable settings with silent fallbacks. If you're tempted to write `.unwrap_or("localhost".into())`, STOP: either it comes from the payload or it's a constant.

This rule is echoed in §1 Overview and §15 Non-Goals so it cannot be missed when reading either section in isolation.
