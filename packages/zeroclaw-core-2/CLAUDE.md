# zeroclaw-agent — the 1.0 pod runtime

Minimal Rust binary that runs inside each EnterpriseAgentOS agent pod. This crate is the TDD rewrite of `packages/zeroclaw-core/` — narrower scope, flatter layout, zero "maybe someday" abstractions.

**The authoritative contract is [`API.md`](./API.md).** Every structural question — env vars, bootstrap shape, prompt sections, WS protocol, tool catalog, error types, file layout — is answered there. Read it before editing code.

## Commands

```bash
cargo fmt --all -- --check
cargo clippy --all-targets -- -D warnings
cargo build
cargo test
cargo check --tests          # After deletions — not plain cargo check.
```

## What this crate does (1.0 scope)

A pod boots with exactly two env vars — `ZEROCLAW_AGENT_ID`, `BACKEND_URL` — then:

1. `GET {BACKEND_URL}/api/agents/{id}` with `Authorization: Bearer {agent_id}`.
2. Writes embedded personality templates (`SOUL.md`, `IDENTITY.md`, `AGENTS.md`, `BOOTSTRAP.md`) into `memory_dir` if absent. Substitutes `{{prompt}}` in `BOOTSTRAP.md` with the system prompt from the bootstrap payload.
3. Starts a WebSocket gateway on `gateway.host:gateway.port` from the payload.
4. Runs the agent turn loop: on a user message, it composes the system prompt fresh from `memory_dir` via trait-based sections, POSTs `{BACKEND_URL}/v1/chat/completions` (SSE streamed), dispatches tool calls, and loops until the assistant returns no tool calls.

Tools kept: `skill_exec`, `memory_store`, `memory_recall`, `memory_forget`, `ask_user`, `shell`, `file_read`, `file_write`, `file_edit`, `http_request`, `web_fetch`, `content_search`, `glob_search`, `tool_search`. Nothing else.

## What this crate does NOT do

No channels, no cron, no plugins, no observability backends (Prometheus/OTEL), no cost tracking, no canvas, no hooks, no MCP, no Obsidian, no delegate/escalate, no heartbeat, no doctor/health, no migration, no multimodal, no i18n, no landlock sandboxing, no dialoguer TUI, no ratatui, no MQTT, no SMTP/IMAP, no sessions management beyond WS, no auth schemes beyond the agent-UUID bearer, no tool-approval mode, no provider routing (the backend owns that), no config file (`config.toml`), no CLI flags beyond `--help` / `--version`. If the answer to "should we add X?" isn't in `API.md`, the answer is **no** until the product says otherwise.

## Project structure target

Every file's purpose is listed in the "File layout" section of `API.md`. The tree will be ~30 files. Keep modules flat; do not introduce `util/`, `helpers/`, `common/`, or other junk-drawer modules.

## Extension point

`src/tools/traits.rs::Tool` is the only extension trait. Everything else is concrete.

## Risk tiers

- **High:** `src/bootstrap.rs`, `src/llm.rs`, `src/gateway/**`, `src/tools/skill_exec.rs`. Changes here must be reviewed.
- **Medium:** Any other `src/**` behaviour change.
- **Low:** Docs, tests, comments, formatting.

## Key rules

- **`API.md` is the spec.** If code and `API.md` disagree, code is wrong — fix the code and update `API.md` in the same commit if the spec also needs to change.
- **Clippy is mandatory.** `-D warnings`. Never `#[allow(...)]` without a comment explaining why.
- **`cargo check --tests`** after deleting code, not plain `cargo check`.
- **One concern per PR.** Do not modify unrelated modules.
- **No heavy dependencies for convenience.** The dep list in `Cargo.toml` is deliberately short. If you want a new crate, justify it in the PR description.

## Anti-patterns

- **Do not add a `config.toml` path.** All runtime config flows from env + bootstrap payload. New fields go into `AgentBootstrapPayload` on the backend.
- **Do not reintroduce providers/ routing.** The backend LLM proxy chooses the model; this crate POSTs to `{BACKEND_URL}/v1/chat/completions` and streams SSE back.
- **Do not add hardcoded credentials, URLs, or model names.** Everything comes from the bootstrap payload.
- **Do not add speculative traits.** Only `Tool` is a trait. Implement concrete types first; promote to traits only when a second implementation actually lands.
- **Do not read or write files under `/zeroclaw-data` outside `memory_dir`.**
- **Do not add tool-approval flows.** Tool approval is a backend concern. Here we enforce Allow/Deny from the bootstrap payload, that's it.
- **Do not forget to update `API.md`** when adding a new tool, prompt section, error variant, or WS message type. Docs drift is the thing we are trying to escape.
