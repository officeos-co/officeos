# LLM Proxy

> How every LLM call flows from agent pod → backend → real provider — without the agent ever seeing an API key.

## Why a proxy

Agent pods run untrusted Rust code with tool execution (shell, file, web). Giving them raw API keys would be a credential leak waiting to happen. Instead, the backend sits in the middle:

```
Agent pod                         Backend                           Provider
   │                                │                                  │
   │  POST /v1/chat/completions     │                                  │
   │  Bearer: <agent-uuid>          │                                  │
   │  model: "backend-managed"      │                                  │
   │ ─────────────────────────────▶ │                                  │
   │                                │  1. Validate agent UUID          │
   │                                │  2. Look up agent record         │
   │                                │     → provider: "openai"         │
   │                                │     → model: "gpt-4o"            │
   │                                │  3. Decrypt provider API key     │
   │                                │  4. Replace model in request     │
   │                                │  5. Forward to upstream          │
   │                                │ ─────────────────────────────▶   │
   │                                │                                  │
   │                                │  ◀───── SSE stream ────────────  │
   │  ◀──── SSE stream ──────────  │                                  │
```

## How the agent connects

`gateway_bootstrap.rs` sets the provider to `custom:{backend_url}/v1`. Zeroclaw's `compatible` provider sends all LLM calls to that URL as standard OpenAI-format requests. The bearer token is the agent's UUID.

The agent sends `model: "backend-managed"` — a placeholder. The backend ignores it and substitutes the real model from the agent's DB record.

## Request flow (`LlmProxyController.cs`)

1. **Auth**: `[AgentTokenAuth]` validates `Authorization: Bearer <uuid>` against the Agents table.
2. **Agent lookup**: fetches the agent record to get `provider` and `model`.
3. **Key resolution**: `_providers.GetDecryptedKeyAsync(provider)` decrypts the API key from Postgres.
4. **Dispatch**: `LlmProviderDispatcher.DispatchAsync(provider, apiKey, model, body)` routes to the upstream.
5. **Stream relay**: the upstream SSE response is piped byte-for-byte back to the agent pod.

## Provider routing (`LlmProviderDispatcher.cs`)

Two code paths based on provider:

### OpenAI-compatible providers

Straight passthrough — replace `model` in the JSON body, set `Authorization: Bearer <key>`, forward to the provider's base URL:

| Provider | Base URL |
|----------|---------|
| openai | `https://api.openai.com/v1` |
| groq | `https://api.groq.com/openai/v1` |
| deepseek | `https://api.deepseek.com/v1` |
| xai | `https://api.x.ai/v1` |
| openrouter | `https://openrouter.ai/api/v1` |
| ollama | `http://localhost:11434/v1` |

### Anthropic

Anthropic uses a different request/response format. `AnthropicTranslator` handles the conversion:

- **Request**: OpenAI format → Anthropic Messages API format (extract system message, map roles, set `max_tokens`)
- **Response**: Anthropic SSE stream → OpenAI-compatible SSE stream (translate `content_block_delta` → `choices[0].delta.content`)

The agent always speaks OpenAI format. The translation is invisible.

## Adding a new provider

1. If OpenAI-compatible: add one line to `OpenAiCompatBaseUrls` in `LlmProviderDispatcher.cs`.
2. If non-OpenAI format: add a translator class (like `AnthropicTranslator`) and a dispatch branch.
3. Add the provider name to `SeedProvidersAsync` in `Program.cs`.
4. Add its models to `KnownModels.cs`.
5. No zeroclaw changes needed — the agent doesn't know which provider it's using.

## What the agent sees

The agent thinks it's talking to a custom OpenAI-compatible endpoint. It never knows:
- Which provider is actually serving the request
- What the real model name is
- What the API key is
- Whether Anthropic format translation is happening

This means provider/model changes in the dashboard take effect on the next LLM call — no pod restart needed.

## Key files

| File | Purpose |
|------|---------|
| `apps/v2-backend/Entities/LlmProxy/LlmProxyController.cs` | `POST /v1/chat/completions` — auth, lookup, dispatch, stream |
| `apps/v2-backend/Entities/LlmProxy/LlmProviderDispatcher.cs` | Routes to upstream by provider, swaps model + key |
| `apps/v2-backend/Entities/LlmProxy/AnthropicTranslator.cs` | OpenAI ↔ Anthropic format translation |
| `packages/zeroclaw-core/src/agent/gateway_bootstrap.rs` | Sets `custom:{backend_url}/v1` as the provider |
| `apps/v2-backend/Entities/Providers/KnownModels.cs` | Valid provider→model mappings |
