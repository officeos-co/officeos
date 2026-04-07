# ZeroClaw Providers Reference

This document maps provider IDs, aliases, and credential environment
variables.

The ground truth for provider routing is `src/providers/mod.rs`, in
particular `create_provider_with_url_and_options` and its `match name`
factory.

## How to List Providers

```bash
zeroclaw providers
```

## Credential Resolution Order

Runtime resolution order is:

1. Explicit credential from config/CLI
2. Provider-specific env var(s)
3. Generic fallback env vars: `ZEROCLAW_API_KEY` then `API_KEY`

For resilient fallback chains (`reliability.fallback_providers`), each
fallback provider resolves credentials independently. The primary
provider's explicit credential is not reused for fallback providers.

## Provider Families

ZeroClaw ships six direct providers plus a generic OpenAI-compatible
wrapper that auto-recognises roughly thirty community endpoints by alias
or URL.

### Direct providers

| ID          | Env var(s)                                   | Notes |
|-------------|----------------------------------------------|-------|
| `anthropic` | `ANTHROPIC_API_KEY` (or subscription token)  | Claude API |
| `openai`    | `OPENAI_API_KEY`                             | OpenAI and Azure OpenAI deployments via `api_url` override |
| `ollama`    | optional `OLLAMA_API_KEY`                    | Local Ollama server; `api_url` may point to a remote instance |
| `openrouter`| `OPENROUTER_API_KEY`                         | OpenRouter aggregator |
| `reliable`  | n/a (wraps other providers)                  | Resilient wrapper: rotates API keys, retries, and fails over across a list of underlying providers. Configured via `[reliability]`. |
| `router`    | n/a (wraps other providers)                  | Hint-based router: picks a provider from `[[model_routes]]` using the `hint:<name>` string in the incoming request. |

### OpenAI-compatible wrapper (`compatible`)

The compatible provider handles any endpoint that speaks the OpenAI
chat-completions protocol. It is selected either by a known alias or
explicitly via `custom:<url>`.

Known aliases recognised in `src/providers/mod.rs`:

- `groq`
- `mistral`
- `xai` (alias `grok`)
- `deepseek`
- `together` (alias `together-ai`)
- `fireworks` (alias `fireworks-ai`)
- `cohere`
- `perplexity`
- `lmstudio` (alias `lm-studio`)
- `llamacpp` (alias `llama.cpp`)
- `z.ai` (aliases `zai`, covered via `zai_base_url`)
- `glm` (covered via `glm_base_url`, uses Zhipu JWT auth)
- `minimax` (plus regional aliases)
- `qwen` (plus `dashscope`, `qwen-code`, `qwen-oauth` OAuth flow)

Additional community endpoints wired in the factory (may evolve — always
check `src/providers/mod.rs` for the authoritative list): `venice`,
`vercel`/`vercel-ai`, `cloudflare`/`cloudflare-ai`, `moonshot`/`kimi`,
`kimi-code`, `synthetic`, `opencode`/`opencode-zen`, `opencode-go`,
`qianfan`, `doubao`, `bailian`, `novita`, `sglang`, `vllm`, `osaurus`,
`nvidia`/`nvidia-nim`, `astrai`, `siliconflow`, `aihubmix`,
`litellm`, `cerebras`, `sambanova`, `hyperbolic`, `deepinfra`,
`huggingface`/`hf`, `ai21`, `reka`, `baseten`, `nscale`, `anyscale`,
`nebius`, `friendli`, `lepton`, `stepfun`, `baichuan`, `yi`/`01ai`,
`hunyuan`, `avian`.

Use `custom:https://your-api.example.com` in `default_provider` to
target any other OpenAI-compatible endpoint the factory does not know
about.

## Fallback Provider Chains (`reliable`)

ZeroClaw supports automatic failover to alternative providers when the
primary encounters:

- Timeout or connection errors
- Service unavailability (503)
- Rate limits (429), after exhausting API key rotation
- Model not found errors (with per-model fallback configured)

Configure fallback chains in `config.toml`:

```toml
[reliability]
fallback_providers = ["anthropic", "groq", "openrouter"]
provider_retries = 2
provider_backoff_ms = 500
```

Behavior:

1. Try primary provider (with `provider_retries` and exponential backoff)
2. On transient failure, move to the first fallback provider
3. Repeat for each fallback in order
4. On permanent errors (400, 401, 403), skip to fallback immediately

Each fallback provider resolves credentials independently, can be from a
different API family (OpenAI-compatible → Anthropic → local Ollama), and
reuses the same requested model if available or triggers model fallback
if configured.

### API key rotation on rate limits

When a provider returns 429 (rate limit), ZeroClaw:

1. Rotates to the next API key in `reliability.api_keys` on the same
   provider/model.
2. If all keys are exhausted, proceeds to `fallback_providers`.

```toml
api_key = "sk-primary"  # always tried first

[reliability]
api_keys = ["sk-backup-1", "sk-backup-2"]
```

### Model fallbacks

When a specific model is unavailable or rate-limited, configure
per-model fallbacks:

```toml
[reliability.model_fallbacks]
"gpt-4o" = ["gpt-4-turbo", "gpt-3.5-turbo"]
"claude-opus-4-20250514" = ["claude-sonnet-4-20250514"]
```

Fallback is triggered when:

- Model is not found in the provider's available models
- Provider returns an error mentioning the model
- Model is rate-limited and API key rotation is exhausted

## Model Routing (`router` / `hint:<name>`)

Use `[[model_routes]]` to bind stable task hints to concrete
provider/model pairs:

```toml
[[model_routes]]
hint = "reasoning"
provider = "openrouter"
model = "anthropic/claude-opus-4-20250514"

[[model_routes]]
hint = "fast"
provider = "groq"
model = "llama-3.3-70b-versatile"
```

Then call with a hint model name:

```text
hint:reasoning
```

## Embedding Routing (`hint:<name>`)

You can route embedding calls with the same hint pattern using
`[[embedding_routes]]`. Set `[memory].embedding_model` to a
`hint:<name>` value to activate routing.

```toml
[memory]
embedding_model = "hint:semantic"

[[embedding_routes]]
hint = "semantic"
provider = "openai"
model = "text-embedding-3-small"
dimensions = 1536

[[embedding_routes]]
hint = "archive"
provider = "custom:https://embed.example.com/v1"
model = "your-embedding-model-id"
dimensions = 1024
```

Supported embedding providers:

- `none`
- `openai`
- `custom:<url>` (OpenAI-compatible embeddings endpoint)

## Ollama Notes

- Vision input is supported through user-message image markers:
  `` [IMAGE:<source>] ``.
- Remote Ollama instances: set `api_url` (example:
  `https://ollama.example.com`). A trailing `/api` is normalized
  automatically.
- `:cloud` model suffix requires a remote `api_url`; local discovery
  intentionally excludes `:cloud` entries.
- Reasoning/thinking can be forced with `[runtime].reasoning_enabled`
  (`true`/`false`/unset), which maps to Ollama's `think` request field.

## Custom Endpoints

OpenAI-compatible endpoint:

```toml
default_provider = "custom:https://your-api.example.com"
```

## Upgrading Models Safely

Use stable hints and update only route targets when providers deprecate
model IDs:

1. Keep call sites stable (`hint:reasoning`, `hint:semantic`).
2. Change only the target model under `[[model_routes]]` or
   `[[embedding_routes]]`.
3. Run `zeroclaw doctor` and `zeroclaw status`.
4. Smoke-test one representative flow (chat + memory retrieval) before
   rollout.
