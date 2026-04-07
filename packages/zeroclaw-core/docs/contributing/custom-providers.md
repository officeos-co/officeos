# Custom Provider Configuration

ZeroClaw supports custom OpenAI-compatible endpoints through the `compatible` provider wrapper in `src/providers/compatible.rs`. Direct first-class providers are limited to `anthropic`, `openai`, `ollama`, `openrouter`, `reliable`, and `router`.

The `compatible` wrapper covers roughly thirty community endpoints, including: groq, mistral, xai, deepseek, together, fireworks, cohere, perplexity, lm_studio, llama.cpp, z.ai, glm, minimax, qwen, and similar OpenAI-compatible services.

## When to Use Which Provider

- **First-class direct providers** (`anthropic`, `openai`, `ollama`, `openrouter`): use the provider ID directly.
- **Any OpenAI-compatible endpoint**: use `compatible` with a named endpoint or an explicit base URL.
- **Reliability wrapping / routing**: use `reliable` or `router` to compose the above.

## Config File

Edit `~/.zeroclaw/config.toml`:

```toml
api_key = "your-api-key"
default_provider = "compatible:groq"
default_model = "llama-3.1-70b-versatile"
```

Or point at an arbitrary OpenAI-compatible base URL:

```toml
default_provider = "compatible"
api_url = "https://your-api.example.com/v1"
api_key = "your-api-key"
default_model = "your-model-name"
```

## Environment Variables

```bash
export ZEROCLAW_API_KEY="your-api-key"
zeroclaw agent
```

## Local OpenAI-Compatible Servers

Any local server that speaks the OpenAI chat completions API (llama.cpp's `llama-server`, LM Studio, vLLM, SGLang, etc.) can be used through `compatible` by pointing `api_url` at its `/v1` endpoint.

Example for a local `llama-server`:

```toml
default_provider = "compatible"
api_url = "http://127.0.0.1:8080/v1"
default_model = "your-local-model"
default_temperature = 0.7
```

API keys are optional for local servers unless the server was started with authentication enabled.

## Testing Configuration

```bash
zeroclaw agent
zeroclaw agent -m "test message"
```

## Troubleshooting

### Authentication Errors

- Verify the API key is correct.
- Check the endpoint URL format (must include `http://` or `https://`).
- Ensure the endpoint is reachable from your network.

### Model Not Found

- Confirm the model name matches the endpoint's available models.
- Verify available models from the same endpoint and key:

```bash
curl -sS https://your-api.example.com/v1/models \
  -H "Authorization: Bearer $ZEROCLAW_API_KEY"
```

- If the gateway does not implement `/models`, send a minimal chat request and inspect the returned error text.

### Connection Issues

- Test endpoint accessibility: `curl -I https://your-api.example.com`
- Verify firewall/proxy settings.
- Check the provider status page.

## Examples

### Named Community Endpoint

```toml
default_provider = "compatible:deepseek"
api_key = "your-deepseek-key"
default_model = "deepseek-chat"
```

### Corporate Proxy

```toml
default_provider = "compatible"
api_url = "https://llm-proxy.corp.example.com/v1"
api_key = "internal-token"
default_model = "gpt-4"
```

### Local LLM Server

```toml
default_provider = "compatible"
api_url = "http://localhost:8080/v1"
default_model = "local-model"
```
