# Agent Definition Schemas

This feature accepts two related but different schemas:

- **Agent definition config**: the stored per-agent config parsed by `AgentDefinitionParser`.
- **Declarative manifest**: the Kubernetes-style resource envelope parsed by `DeclarativeManifestParser`.

Both YAML and JSON are accepted. Agent definition config uses `snake_case` field names. Declarative manifests use `camelCase` resource specs.

## Agent Definition Config

Used inside agent create/patch flows as `ConfigJson`. This is the normalized config stored on `AgentDefinitionRecord.ConfigJson`.

```yaml
name: Support agent
description: Answers customer questions from docs.
model: claude-sonnet-4-6
system: |-
  Answer from sources.

mcp_servers:
  - name: notion
    type: registered
  - name: slack
    type: url
    url: https://mcp.slack.com/mcp

tools:
  - type: agent_toolset_20260401
    default_config:
      permission_policy:
        type: always_allow
  - type: browser_toolset
  - type: mcp_toolset
    mcp_server_name: notion
    default_config:
      permission_policy:
        type: allow_list
        tools:
          - search
          - read

resources:
  - type: browser
    resource_id: 00000000-0000-0000-0000-000000000001
    access_mode: read_write
    instructions: Verify UI changes.
  - type: memory_store
    resource_id: 00000000-0000-0000-0000-000000000002
    access_mode: read_only

routines:
  - name: Daily summary
    prompt: Summarize active work.
    schedule_triggers:
      - name: Morning
        expression: "0 9 * * 1-5"
    api_triggers:
      - name: Manual
    github_triggers:
      - name: PR events
        owner: acme
        repo: platform
        events:
          - pull_request
        secret: webhook-secret

metadata:
  template: support-agent
```

### Agent Definition Fields

| Field | Required | Notes |
| --- | --- | --- |
| `name` | yes | Trimmed non-empty agent name. |
| `model` | yes | Model id or `auto`, validated later against the selected provider. |
| `description` | no | Trimmed when present. |
| `system` | no | Agent system/bootstrap prompt. |
| `mcp_servers` | no | `type` is `registered` or `url`; `url` type requires `url`. |
| `tools` | no | Defaults to built-in toolset with `always_allow` when omitted. |
| `resources` | no | Attach existing browser, memory store, or channel resources by id. |
| `routines` | no | Each routine needs at least one trigger. |
| `metadata` | no | Free-form JSON/YAML object preserved as metadata. |

Supported toolset types:

```text
agent_toolset_20260401
mcp_toolset
browser_toolset
```

Supported permission policy types:

```text
always_allow
always_deny
allow_list
deny_list
```

`allow_list` and `deny_list` require at least one tool name in `tools`.

Supported resource attachment types:

```text
browser
memory_store
channel
```

Supported access modes:

```text
read_only
read_write
```

## Declarative Manifest

Used by `officeos validate`, `officeos diff`, and `officeos apply`. Each document has a resource envelope:

```yaml
apiVersion: officeos.io/v1
kind: Agent
metadata:
  name: support-agent
spec:
  provider: anthropic
  model: claude-sonnet-4-6
  description: Answers customer questions.
  system: Answer from sources.
```

Multi-resource manifests use `---` separators:

```yaml
apiVersion: officeos.io/v1
kind: Provider
metadata:
  name: anthropic
spec:
  type: anthropic
  credentials:
    apiKey: sk-ant-...
---
apiVersion: officeos.io/v1
kind: Channel
metadata:
  name: support-slack
spec:
  type: slack
  token: xoxb-test
---
apiVersion: officeos.io/v1
kind: MemoryStore
metadata:
  name: product-docs
spec:
  displayName: Product Docs
  entries:
    - key: refund-policy
      content: Refunds are handled by support.
---
apiVersion: officeos.io/v1
kind: Browser
metadata:
  name: qa-browser
spec:
  displayName: QA Browser
---
apiVersion: officeos.io/v1
kind: Agent
metadata:
  name: support-agent
spec:
  provider: anthropic
  model: claude-sonnet-4-6
  description: Answers customer questions.
  system: Answer from sources.
  tools:
    builtin:
      permissionPolicy:
        type: always_allow
    browser:
      permissionPolicy:
        type: allow_list
        tools:
          - browser.open
          - browser.screenshot
  integrations:
    - ref: github
      permissionPolicy:
        type: always_allow
  channels:
    - ref: support-slack
      config:
        mode: mentions
  memoryStores:
    - ref: product-docs
      accessMode: read_only
      instructions: Use as source material.
  browsers:
    - ref: qa-browser
      accessMode: read_write
      instructions: Verify customer-visible pages.
  metadata:
    owner: support
---
apiVersion: officeos.io/v1
kind: Routine
metadata:
  name: daily-support-summary
spec:
  agentRef: support-agent
  prompt: Summarize open support work.
  scheduleTriggers:
    - name: Morning
      expression: "0 9 * * 1-5"
```

### Declarative Resource Kinds

| Kind | Important `spec` fields |
| --- | --- |
| `Provider` | `type`, `displayName`, `enabled`, `defaultModel`, `models`, `authKind`, `credentials` |
| `Channel` | `type`, `displayName`, `enabled`, `token`, `credentials` |
| `Integration` | `builtin`, `provider`, `title`, `description`, `transportType`, `command`, `args`, `url`, `category`, `logo`, `credentialFieldsJson`, `credentials` |
| `MemoryStore` | `displayName`, `entries` with `key` and `content` |
| `Browser` | `displayName` |
| `Agent` | `provider`, `model`, `description`, `system`, `tools`, `integrations`, `channels`, `memoryStores`, `browsers`, `metadata` |
| `Routine` | `agentRef`, `prompt`, `scheduleTriggers`, `apiTriggers`, `githubTriggers` |

### Agent Manifest Notes

- `apiVersion` must be `officeos.io/v1`.
- `metadata.name` is required and is the stable resource name.
- `Agent.spec.provider` must reference a provider declared in the same manifest or already configured in the workspace.
- `Agent.spec.model` defaults to `auto` when omitted.
- `Agent.spec.integrations[].ref`, `channels[].ref`, `memoryStores[].ref`, and `browsers[].ref` reference resource names, not ids.
- Browser tools are automatically added when `Agent.spec.browsers` is non-empty or `Agent.spec.tools.browser` is present.
- `Routine.spec.agentRef` references an agent resource name.
- Declarative provider credentials are supported for API-key-style providers, but Codex subscription auth is intentionally imperative through `officeos provider auth codex`.

## Source Of Truth

When changing the schema, update this file together with:

- `Domain/AgentDefinitionConfig.cs`
- `Application/AgentDefinitionParser.cs`
- `Application/DeclarativeManifestParser.cs`
- `Application/DeclarativeAgentService.cs`
- `tests/AgentDefinitions/*`
