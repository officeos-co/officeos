```yaml
apiVersion: officeos.io/v1
kind: Provider
metadata:
  name: anthropic
spec:
  type: anthropic
  displayName: Anthropic
  enabled: true
  defaultModel: claude-sonnet-4-6
  models:
    - claude-sonnet-4-6
    - claude-haiku-4-5
  authKind: api_key
  credentials:
    apiKey: ${ANTHROPIC_API_KEY}
```

# Provider

A `Provider` registers an LLM backend that agents can use by name.

Required fields: `apiVersion`, `kind`, `metadata.name`, `spec.type`, `spec.credentials` when enabled.

Useful fields: `spec.defaultModel`, `spec.models`, `spec.displayName`, `spec.enabled`, `spec.authKind`.

Built-in provider types include `anthropic`, `openai`, `codex`, `google`, `xai`, `groq`, `deepseek`, and `openrouter`. Use `custom` when the provider is not in the registry.
