# Change Playbooks

Step-by-step guides for common extension and modification patterns in ZeroClaw.

For complete code examples of each extension trait, see [extension-examples.md](./extension-examples.md).

## Extension Points

All extension points are trait-driven. Implement the trait, register in the module factory, add tests.

- `src/providers/traits.rs` (`Provider`)
- `src/channels/traits.rs` (`Channel`)
- `src/tools/traits.rs` (`Tool`)
- `src/memory/traits.rs` (`Memory`)
- `src/observability/traits.rs` (`Observer`)
- `src/runtime/traits.rs` (`RuntimeAdapter`)

## Adding a Provider

Direct providers today: `anthropic`, `openai`, `ollama`, `openrouter`, `reliable`, `router`. The `compatible` provider wraps OpenAI-compatible community endpoints (groq, mistral, xai, deepseek, together, fireworks, cohere, perplexity, lm_studio, llama.cpp, z.ai, glm, minimax, qwen, and similar).

- Prefer configuring an existing OpenAI-compatible endpoint through the `compatible` wrapper before writing a new direct provider.
- If a direct provider is required, implement `Provider` from `src/providers/traits.rs` in `src/providers/`.
- Register in the factory in `src/providers/mod.rs`.
- Add focused tests for factory wiring and error paths.
- Avoid provider-specific behavior leaking into shared orchestration code.

## Adding a Channel

Supported channels: `telegram`, `webhook`, and the internal `cli`. New channels should only be added when there is a concrete operational need.

- Implement `Channel` from `src/channels/traits.rs` in `src/channels/`.
- Keep `send`, `listen`, `health_check`, and typing semantics consistent with existing channels.
- Add channel config to `ChannelsConfig` in `src/config/schema.rs`.
- Register the channel in `src/channels/mod.rs`.
- Cover auth/allowlist/health behavior with tests.

## Adding a Tool

The current tool surface is roughly 30 tools covering shell, filesystem, search, memory, HTTP/web, MCP, skills, canvas, sessions, and interaction primitives (poll, reaction, ask_user, escalate, delegate, tool_search, read_skill). New tools should extend this surface, not reintroduce deleted categories.

- Implement `Tool` from `src/tools/traits.rs` in `src/tools/` with a strict parameter schema.
- Validate and sanitize all inputs.
- Return a structured `ToolResult`; never panic in the runtime path.
- Register in `src/tools/mod.rs` via `default_tools()`.
- For tools with shared state, follow the `Arc<RwLock<T>>` handle pattern described in [ADR-004](../architecture/adr-004-tool-shared-state-ownership.md).

## Security / Runtime / Gateway Changes

- Include threat/risk notes and a rollback strategy.
- Add/update tests or validation evidence for failure modes and boundaries.
- Keep observability useful but non-sensitive.
- For `.github/workflows/**` changes, include Actions allowlist impact in PR notes and update `docs/contributing/actions-source-policy.md` when sources change.

## Docs System / README / IA Changes

- Treat docs navigation as product UX: preserve clear pathing from README to docs hub to category index.
- Keep top-level nav concise; avoid duplicative links across adjacent nav blocks.
- When runtime surfaces change, update related references in `docs/reference/`.
- Keep multilingual entry-point parity for all supported locales (`en`, `zh-CN`, `ja`, `ru`, `fr`, `vi`) when nav or key wording changes.
- When shared docs wording changes, sync corresponding localized docs in the same PR (or explicitly document deferral and follow-up PR).

## Tool Shared State

- Follow the `Arc<RwLock<T>>` handle pattern for any tool that owns long-lived shared state.
- Accept handles at construction; do not create global/static mutable state.
- Use `ClientId` (provided by the daemon) to namespace per-client state — never construct identity keys inside the tool.
- Isolate security-sensitive state (credentials, quotas) per client; broadcast/display state may be shared with optional namespace prefixing.
- Cached validation is invalidated on config change — tools must re-validate before the next execution when signaled.
- See [ADR-004: Tool Shared State Ownership](../architecture/adr-004-tool-shared-state-ownership.md) for the full contract.

## Architecture Boundary Rules

- Extend capabilities by adding trait implementations + factory wiring first; avoid cross-module rewrites for isolated features.
- Keep dependency direction inward to contracts: concrete integrations depend on trait/config/util layers, not on other concrete integrations.
- Avoid cross-subsystem coupling (e.g., provider code importing channel internals, tool code mutating gateway policy directly).
- Keep module responsibilities single-purpose: orchestration in `agent/`, transport in `channels/`, model I/O in `providers/`, policy in `security/`, execution in `tools/`.
- Introduce new shared abstractions only after repeated use (rule-of-three), with at least one real caller.
- For config/schema changes, treat keys as public contract: document defaults, compatibility impact, and migration/rollback path.
