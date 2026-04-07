# ZeroClaw Config Reference (Operator-Oriented)

This is a high-signal reference for the most common `config.toml`
sections and defaults. The ground truth is `src/config/schema.rs`;
whenever this doc disagrees, `Config` wins.

Config path resolution at startup:

1. `ZEROCLAW_WORKSPACE` override (if set)
2. Persisted `~/.zeroclaw/active_workspace.toml` marker (if present)
3. Default `~/.zeroclaw/config.toml`

ZeroClaw logs the resolved config on startup at `INFO` level:

- `Config loaded` with fields: `path`, `workspace`, `source`, `initialized`

Schema export:

- `zeroclaw config schema` (prints JSON Schema draft 2020-12 to stdout)

## Core Keys

| Key | Default | Notes |
|---|---|---|
| `default_provider` | `openrouter` | provider ID or alias |
| `default_model` | `anthropic/claude-sonnet-4-6` | model routed through the selected provider |
| `default_temperature` | `0.7` | model temperature |
| `api_key` | unset | credential for the default provider |
| `api_url` | unset | base URL override for the default provider |
| `provider_timeout_secs` | schema default | per-call provider timeout |
| `provider_max_tokens` | unset | max_tokens override passed to the provider |

## `[observability]`

| Key | Default | Purpose |
|---|---|---|
| `backend` | `none` | `none`, `noop`, `log`, `prometheus`, `otel`, `opentelemetry`, or `otlp` |
| `otel_endpoint` | `http://localhost:4318` | OTLP HTTP endpoint used when backend is `otel` |
| `otel_service_name` | `zeroclaw` | Service name emitted to OTLP collector |
| `runtime_trace_mode` | `none` | `none`, `rolling`, or `full` |
| `runtime_trace_path` | `state/runtime-trace.jsonl` | JSONL path (workspace-relative unless absolute) |
| `runtime_trace_max_entries` | `200` | Retained events when mode is `rolling` |

Notes:

- Alias values `opentelemetry` and `otlp` map to the same OTel backend.
- Runtime traces may contain model output text; keep disabled by default
  on shared hosts.
- Query with: `zeroclaw doctor traces --limit 20`,
  `zeroclaw doctor traces --event tool_call_result --contains "error"`,
  or `zeroclaw doctor traces --id <trace-id>`.

## Environment Provider Overrides

Provider selection can also be controlled by environment variables.
Precedence:

1. `ZEROCLAW_PROVIDER` (explicit override; always wins when non-empty)
2. `PROVIDER` (legacy fallback; only applied when config provider is
   unset or still `openrouter`)
3. `default_provider` in `config.toml`

## `[agent]`

| Key | Default | Purpose |
|---|---|---|
| `compact_context` | `true` | Shrinks bootstrap + RAG context for small local models |
| `max_tool_iterations` | `10` | Max tool-call loop turns per user message |
| `max_history_messages` | `50` | Conversation history kept per session |
| `parallel_tools` | `false` | Enable parallel tool execution within a single iteration |
| `tool_dispatcher` | `auto` | Tool dispatch strategy |
| `tool_call_dedup_exempt` | `[]` | Tool names exempt from within-turn duplicate-call suppression |
| `tool_filter_groups` | `[]` | Per-turn MCP tool schema filter groups |

Notes:

- Setting `max_tool_iterations = 0` falls back to the safe default `10`.
- `parallel_tools` applies to the `Agent::turn()` API surface. It does
  not gate the runtime loop used by gateway or channel handlers.
- `tool_call_dedup_exempt` example: `tool_call_dedup_exempt = ["browser"]`.

### `tool_filter_groups`

Reduces per-turn token overhead by limiting which MCP tool schemas are
sent to the LLM on each turn. Built-in (non-MCP) tools always pass
through unchanged.

Each entry is a table with:

| Field | Type | Purpose |
|---|---|---|
| `mode` | `"always"` \| `"dynamic"` | `always`: unconditional. `dynamic`: include only when the user message matches a keyword. |
| `tools` | `[string]` | Tool name patterns. Single `*` wildcard supported (prefix/suffix/infix). |
| `keywords` | `[string]` | (Dynamic only) case-insensitive substrings matched against the last user message. |

```toml
[[agent.tool_filter_groups]]
mode = "always"
tools = ["mcp_vikunja_*"]

[[agent.tool_filter_groups]]
mode = "dynamic"
tools = ["mcp_browser_*"]
keywords = ["browse", "navigate", "open url", "screenshot"]
```

## `[pacing]`

Pacing controls for slow/local LLM workloads (Ollama, llama.cpp, vLLM).

| Key | Default | Purpose |
|---|---|---|
| `step_timeout_secs` | unset | Per-step LLM inference timeout |
| `loop_detection_min_elapsed_secs` | unset | Grace period before loop detection activates |
| `loop_ignore_tools` | `[]` | Tools excluded from identical-output loop detection |
| `message_timeout_scale_max` | `4` | Cap for channel message timeout scaling |
| `loop_detection_enabled` | `true` | Master toggle for loop detection |
| `loop_detection_window_size` | (schema default) | Sliding window size |
| `loop_detection_max_repeats` | (schema default) | Max repeats within the window |

Notes:

- `step_timeout_secs` operates independently of the total channel
  message timeout budget; a step timeout does not consume the overall
  budget.
- `message_timeout_scale_max` must be ≥ 1. The scaling formula is
  `message_timeout_secs * min(max_tool_iterations, message_timeout_scale_max)`.

## `[reliability]`

Resilience configuration for multi-provider fallback, API key rotation,
and retry policies.

| Key | Type | Default | Purpose |
|---|---|---|---|
| `fallback_providers` | `[string]` | `[]` | Ordered fallback provider IDs |
| `model_fallbacks` | `{string: [string]}` | `{}` | Per-model fallback chains |
| `api_keys` | `[string]` | `[]` | Additional API keys for 429 rotation |
| `provider_retries` | `u32` | `2` | Retry attempts per provider before moving on |
| `provider_backoff_ms` | `u64` | `500` | Initial exponential backoff |
| `channel_initial_backoff_secs` | `u64` | `1` | Initial backoff for channel/daemon restart |
| `channel_max_backoff_secs` | `u64` | `60` | Max backoff for channel/daemon restart |
| `scheduler_poll_secs` | `u64` | `5` | Scheduler polling cadence |
| `scheduler_retries` | `u32` | `3` | Max retries for cron job execution |

Notes:

- Fallback providers resolve credentials independently using the
  standard resolution order: explicit config → provider-specific env
  var → `ZEROCLAW_API_KEY` → `API_KEY`.
- Hot-reload enabled: updates take effect on the next channel message
  or provider request without restart.

Fallback triggers:

- Timeout / connection error
- Service unavailable (503)
- Rate limit (429) — first rotates `api_keys`, then moves to next provider
- Model not found — if `model_fallbacks` is configured for that model

Fallback does **not** trigger on 400 (malformed request) or 401/403
(invalid credentials).

## `[security.otp]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable OTP gating for sensitive actions/domains |
| `method` | `totp` | `totp`, `pairing`, or `cli-prompt` |
| `token_ttl_secs` | `30` | TOTP time-step window in seconds |
| `cache_valid_secs` | `300` | Cache window for recently validated OTP codes |
| `gated_actions` | `["shell","file_write","browser_open","browser","memory_forget"]` | Tool actions protected by OTP |
| `gated_domains` | `[]` | Explicit domain patterns requiring OTP |
| `gated_domain_categories` | `[]` | Domain preset categories (`banking`, `medical`, `government`, `identity_providers`) |

Notes:

- Domain patterns support wildcard `*`.
- When `enabled = true` and no OTP secret exists, ZeroClaw generates one
  and prints an enrollment URI once.

## `[security.estop]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable emergency-stop state machine and CLI |
| `state_file` | `~/.zeroclaw/estop-state.json` | Persistent estop state path |
| `require_otp_to_resume` | `true` | Require OTP validation before `resume` |

Corrupted/unreadable estop state falls back fail-closed to `kill_all`.

## `[agents.<name>]`

Delegate sub-agent configurations. Each key under `[agents]` defines a
named sub-agent the primary agent can delegate to.

| Key | Default | Purpose |
|---|---|---|
| `provider` | _required_ | Provider name |
| `model` | _required_ | Model for the sub-agent |
| `system_prompt` | unset | Optional system prompt override |
| `api_key` | unset | Optional API key override (encrypted when `secrets.encrypt = true`) |
| `temperature` | unset | Temperature override |
| `max_depth` | `3` | Max recursion depth for nested delegation |
| `agentic` | `false` | Enable multi-turn tool-call loop mode |
| `allowed_tools` | `[]` | Tool allowlist for agentic mode |
| `max_iterations` | `10` | Max tool-call iterations in agentic mode |
| `timeout_secs` | `120` | Timeout for non-agentic calls |
| `agentic_timeout_secs` | `300` | Timeout for agentic loops |
| `skills_directory` | unset | Scoped skills directory (workspace-relative) |

Notes:

- `agentic = true` requires at least one matching entry in
  `allowed_tools`.
- The `delegate` tool is excluded from sub-agent allowlists to prevent
  re-entrant delegation loops.

```toml
[agents.researcher]
provider = "openrouter"
model = "anthropic/claude-sonnet-4-6"
agentic = true
allowed_tools = ["web_search", "http_request", "file_read"]
```

## `[runtime]`

| Key | Default | Purpose |
|---|---|---|
| `reasoning_enabled` | unset | Global reasoning/thinking override for providers that support it |

- `false` sends `think: false` to Ollama.
- `true` sends `think: true`.
- Unset keeps provider defaults.

## `[skills]`

| Key | Default | Purpose |
|---|---|---|
| `open_skills_enabled` | `false` | Opt-in loading/sync of the community `open-skills` repo |
| `open_skills_dir` | unset | Optional local path (defaults to `$HOME/open-skills` when enabled) |
| `prompt_injection_mode` | `full` | `full` or `compact` |

Env overrides: `ZEROCLAW_OPEN_SKILLS_ENABLED`,
`ZEROCLAW_OPEN_SKILLS_DIR`, `ZEROCLAW_SKILLS_PROMPT_MODE`.

## `[composio]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable Composio managed OAuth tools |
| `api_key` | unset | Composio API key used by the `composio` tool |
| `entity_id` | `default` | Default `user_id` sent on connect/execute calls |

Legacy alias: `enable = true` is accepted as `enabled = true`.

## `[cost]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable cost tracking |
| `daily_limit_usd` | `10.00` | Daily spend limit |
| `monthly_limit_usd` | `100.00` | Monthly spend limit |
| `warn_at_percent` | `80` | Warn when spending hits this % |
| `allow_override` | `false` | Allow requests to exceed budget with `--override` |

## `[multimodal]`

| Key | Default | Purpose |
|---|---|---|
| `max_images` | `4` | Max image markers per request |
| `max_image_size_mb` | `5` | Per-image size limit |
| `allow_remote_fetch` | `false` | Allow `http(s)` image URLs in markers |

Marker syntax: `` [IMAGE:<source>] ``. Allowed MIME types: `image/png`,
`image/jpeg`, `image/webp`, `image/gif`, `image/bmp`. Non-vision
providers return a structured capability error
(`capability=vision`) instead of silently dropping images.

## `[browser]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable `browser_open` tool |
| `allowed_domains` | `[]` | Allowed domains (exact/subdomain or `"*"`) |
| `session_name` | unset | Browser session name for automation |
| `backend` | `agent_browser` | `"agent_browser"`, `"rust_native"`, `"computer_use"`, or `"auto"` |
| `native_headless` | `true` | Headless mode for the rust-native backend |
| `native_webdriver_url` | `http://127.0.0.1:9515` | WebDriver endpoint |
| `native_chrome_path` | unset | Optional Chrome/Chromium executable path |

### `[browser.computer_use]`

| Key | Default | Purpose |
|---|---|---|
| `endpoint` | `http://127.0.0.1:8787/v1/actions` | Sidecar endpoint |
| `api_key` | unset | Optional bearer token (encrypted) |
| `timeout_ms` | `15000` | Per-action request timeout |
| `allow_remote_endpoint` | `false` | Reject non-loopback endpoints unless set |
| `window_allowlist` | `[]` | Window title/process allowlist |

## `[http_request]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable `http_request` tool |
| `allowed_domains` | `[]` | Allowed domains (exact/subdomain or `"*"`) |
| `max_response_size` | `1000000` | Max response size in bytes |
| `timeout_secs` | `30` | Request timeout |

Deny-by-default: if `allowed_domains` is empty, all HTTP requests are
rejected. Local/private targets are blocked even when `"*"` is set.

## `[google_workspace]`

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable the `google_workspace` tool |
| `credentials_path` | unset | Google service-account / OAuth JSON path |
| `default_account` | unset | Default Google account passed to `gws` |
| `allowed_services` | built-in list | Accessible services |
| `rate_limit_per_minute` | `60` | Max `gws` calls per minute |
| `timeout_secs` | `30` | Per-call execution timeout |
| `audit_log` | `false` | Emit an `INFO` log for every `gws` call |

`[[google_workspace.allowed_operations]]` entries pin specific
`(service, resource, sub_resource, methods)` combinations; when the
array is empty, every combination within `allowed_services` is
available.

## `[gateway]`

| Key | Default | Purpose |
|---|---|---|
| `host` | `127.0.0.1` | Bind address |
| `port` | `42617` | Listen port |
| `require_pairing` | `true` | Require pairing before bearer auth |
| `allow_public_bind` | `false` | Block accidental public exposure |
| `path_prefix` | unset | URL path prefix for reverse-proxy deployments |

`path_prefix` must start with `/` and must not end with `/`.

## `[autonomy]`

| Key | Default | Purpose |
|---|---|---|
| `level` | `supervised` | `read_only`, `supervised`, or `full` |
| `workspace_only` | `true` | Reject absolute paths unless explicitly disabled |
| `allowed_commands` | _required for shell exec_ | Allowlist of executables or `"*"` |
| `forbidden_paths` | built-in protected list | Explicit path denylist |
| `allowed_roots` | `[]` | Additional roots allowed outside workspace |
| `max_actions_per_hour` | `20` | Per-policy action budget |
| `max_cost_per_day_cents` | `500` | Per-policy spend guardrail |
| `require_approval_for_medium_risk` | `true` | Approval gate for medium-risk commands |
| `block_high_risk_commands` | `true` | Hard block for high-risk commands |
| `auto_approve` | `[]` | Tool operations always auto-approved |
| `always_ask` | `[]` | Tool operations that always require approval |

Notes:

- Shell separator/operator parsing is quote-aware. Unquoted chaining
  (`;`, `|`, `&&`, `||`, background, redirects) is still enforced.
- Access outside the workspace requires `allowed_roots`, even when
  `workspace_only = false`.

## `[memory]`

| Key | Default | Purpose |
|---|---|---|
| `backend` | `sqlite` | `sqlite`, `obsidian`, or `none` |
| `auto_save` | `true` | Persist user-stated inputs (assistant output excluded) |
| `embedding_provider` | `none` | `none`, `openai`, or `custom:<url>` |
| `embedding_model` | `text-embedding-3-small` | Embedding model ID or `hint:<name>` |
| `embedding_dimensions` | `1536` | Expected vector size |
| `vector_weight` | `0.7` | Hybrid ranking vector weight |
| `keyword_weight` | `0.3` | Hybrid ranking keyword weight |
| `search_mode` | (schema default) | `bm25`, `embedding`, or `hybrid` |
| `min_relevance_score` | `0.4` | Drop memories below this hybrid score |
| `embedding_cache_size` | `10000` | LRU cache for embeddings |
| `chunk_max_tokens` | `512` | Max tokens per chunk when splitting |
| `response_cache_enabled` | `false` | Cache LLM responses to dodge duplicate spend |
| `response_cache_ttl_minutes` | `60` | TTL for cached responses |
| `response_cache_max_entries` | `5000` | LRU size for response cache |
| `response_cache_hot_entries` | `256` | In-memory hot cache size |
| `retrieval_stages` | `["cache","fts","vector"]` | Pipeline stages in order |
| `rerank_enabled` | `false` | LLM reranker when candidate count ≥ threshold |
| `rerank_threshold` | `5` | Candidate count that triggers rerank |
| `fts_early_return_score` | `0.85` | Skip vector stage above this FTS score |
| `default_namespace` | `default` | Namespace for memory entries |
| `conflict_threshold` | `0.85` | Cosine-similarity conflict threshold |
| `sqlite_open_timeout_secs` | unset | Max seconds to wait when opening the SQLite DB |

Notes:

- `backend = "sqlite"` is the default and recommended for all runtimes.
- `backend = "obsidian"` is an opt-in backend that persists memories
  into a local Obsidian vault as Markdown notes.
- `backend = "none"` disables memory persistence entirely; the agent
  runs with no long-term recall.

## `[[model_routes]]` and `[[embedding_routes]]`

Use route hints so integrations can keep stable names while model IDs
evolve.

### `[[model_routes]]`

| Key | Default | Purpose |
|---|---|---|
| `hint` | _required_ | Task hint name (e.g. `"reasoning"`, `"fast"`) |
| `provider` | _required_ | Provider to route to |
| `model` | _required_ | Model to use with that provider |
| `api_key` | unset | Optional API key override |

### `[[embedding_routes]]`

| Key | Default | Purpose |
|---|---|---|
| `hint` | _required_ | Route hint name |
| `provider` | _required_ | `"none"`, `"openai"`, or `"custom:<url>"` |
| `model` | _required_ | Embedding model |
| `dimensions` | unset | Optional dimension override |
| `api_key` | unset | Optional API key override |

```toml
[memory]
embedding_model = "hint:semantic"

[[model_routes]]
hint = "reasoning"
provider = "openrouter"
model = "provider/model-id"

[[embedding_routes]]
hint = "semantic"
provider = "openai"
model = "text-embedding-3-small"
dimensions = 1536
```

## `[query_classification]`

Automatic model hint routing — maps user messages to `[[model_routes]]`
hints based on content patterns.

| Key | Default | Purpose |
|---|---|---|
| `enabled` | `false` | Enable automatic query classification |
| `rules` | `[]` | Classification rules (evaluated in priority order) |

Each rule supports `hint`, `keywords`, `patterns`, `min_length`,
`max_length`, and `priority`.

## `[channels_config]`

Top-level channel options are configured under `channels_config`.
ZeroClaw ships only `cli`, `telegram`, and `webhook` as working
channels — any other sub-tables you see in the JSON schema are legacy
fields without a backing implementation.

| Key | Default | Purpose |
|---|---|---|
| `cli` | `true` | Enable the interactive CLI channel |
| `message_timeout_secs` | `300` | Base per-message LLM+tool timeout |
| `ack_reactions` | `true` | 👀/✅/⚠️ receipts on inbound messages |
| `show_tool_calls` | `false` | Forward tool-call notes to the channel |
| `session_persistence` | `true` | Persist per-session history |
| `session_backend` | `sqlite` | `sqlite` or `jsonl` |
| `session_ttl_hours` | `0` | Auto-archive stale sessions (0 = disabled) |
| `debounce_ms` | `0` | Accumulate burst messages before dispatch |

Notes:

- Runtime timeout budget is `message_timeout_secs * scale`, where
  `scale = min(max_tool_iterations, cap)` with a minimum of `1`. The
  default cap is `4` (override via `[pacing].message_timeout_scale_max`).
- Values below `30` are clamped to `30`.
- While `zeroclaw channel start` or `zeroclaw daemon` is running,
  updates to `default_provider`, `default_model`, `default_temperature`,
  `api_key`, `api_url`, and `reliability.*` are hot-applied on the next
  inbound message.
- Per-channel config examples live in
  [channels-reference.md](channels-reference.md).

## Security-Relevant Defaults

- deny-by-default channel allowlists (`[]` means deny all)
- pairing required on gateway by default
- public bind disabled by default
- `autonomy.workspace_only = true` by default

## Validation Commands

After editing config:

```bash
zeroclaw status
zeroclaw doctor
zeroclaw channel doctor
```

## Related Docs

- [channels-reference.md](channels-reference.md)
- [providers-reference.md](providers-reference.md)
- [../cli/commands-reference.md](../cli/commands-reference.md)
