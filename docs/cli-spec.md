# OfficeOS CLI And Control Plane Spec

OfficeOS is a declarative infrastructure control plane for async agents. The CLI is the only supported human interface to the backend. The backend still exposes an API, but that API is an implementation detail used by the CLI, runtimes, webhooks, schedules, and future editor integrations.

The CLI should feel close to `kubectl`: resource nouns, stable verbs, manifest-first workflows, table output by default, machine-readable output on request, and explicit contexts.

## Product Boundary

Supported user surface:

- `officeos` CLI.
- Manifest files committed with application code.
- VS Code extension commands that shell out to `officeos` or call the same private control-plane API.

Unsupported user surface:

- Dashboard.
- Public GraphQL.
- Public REST docs as the product interface.
- Dashboard-shaped mutations and payloads.

The current `eaos` binary should become a transitional alias. The canonical binary should be `officeos`.

## Resource Model

Every managed object is a resource:

```yaml
apiVersion: officeos.io/v1
kind: Agent
metadata:
  name: fix-ci
  workspace: platform
  labels:
    team: infra
spec:
  model:
    provider: openai
    name: gpt-5.4
  prompt: Fix CI failures and open a PR.
```

Required resource envelope:

- `apiVersion`: manifest API version.
- `kind`: resource kind.
- `metadata.name`: stable resource name unique within workspace and kind.
- `metadata.workspace`: optional; defaults to current context workspace.
- `metadata.labels`: optional string map used by selectors.
- `metadata.annotations`: optional string map for tool-owned metadata.
- `spec`: desired state.

Core resource kinds:

- `Agent`
- `Integration`
- `Channel`
- `MemoryStore`
- `Browser`
- `Routine`
- `Run`
- `Engine`
- `WorkspaceRuntime`
- `Artifact`
- `Approval`
- `Policy`
- `Secret`
- `Organization`
- `Workspace`
- `AccessGroup`

Backend domain records remain owned by their feature namespaces, for example `OffceOs.Domain.Features.Channels.ChannelConnectionRecord`. Manifest specs are transport/application contracts; they should map into owning feature domain records instead of replacing them.

## CLI Principles

- Default output is concise tables.
- `-o json` and `-o yaml` are stable machine-readable contracts.
- All commands work against the current context unless `--context`, `--workspace`, or `--org` is provided.
- Mutating commands support `--dry-run`.
- Destructive commands require resource names or selectors; no silent broad deletes.
- Long-running operations return immediately by default and can be followed with `logs`, `status`, or `wait`.
- Errors use deterministic exit codes and include resource references.

## Global Flags

```text
officeos [command] [flags]

Global flags:
  --context <name>        Config context from ~/.officeos/config.yaml
  --api-url <url>         Override context API URL
  --token <token>         Override context token, mostly for CI
  --org <name|id>         Organization scope
  --workspace <name|id>   Workspace scope
  -o, --output <format>   table, wide, json, yaml, name
  --no-color              Disable color
  -v, --verbose           Print request IDs and debug context
```

Config path:

```text
~/.officeos/config.yaml
```

The existing `~/.eaos/config.yaml` may be read during migration, but new writes should target `~/.officeos/config.yaml`.

## Command Groups

### Auth And Contexts

```text
officeos login [--api-url <url>] [--context <name>]
officeos logout [--context <name>]
officeos whoami

officeos config get-contexts
officeos config current-context
officeos config use-context <name>
officeos config set-context <name> --api-url <url> [--workspace <name>] [--org <name>]
officeos config delete-context <name>
```

### Declarative Manifests

```text
officeos validate -f <file|dir|url> [--recursive]
officeos diff -f <file|dir|url> [--recursive]
officeos apply -f <file|dir|url> [--recursive] [--dry-run] [--wait]
officeos delete -f <file|dir|url> [--recursive] [--dry-run]
officeos explain <kind>[.<field>]
```

Examples:

```text
officeos apply -f officeos.yaml
officeos apply -f ./officeos --recursive
officeos diff -f routines/fix-ci.yaml
officeos explain Agent.spec.model
```

### Generic Resource Reads

```text
officeos get <kind|kind/name> [name] [-l <selector>] [--all-workspaces]
officeos describe <kind|kind/name> [name]
officeos delete <kind> <name> [--dry-run]
officeos delete <kind> -l <selector> [--dry-run]
```

Examples:

```text
officeos get agents
officeos get channels -o wide
officeos get routines -l team=infra
officeos describe agent fix-ci
officeos delete memorystore docs
```

Supported plural aliases:

```text
agents, integrations, channels, memorystores, browsers, routines,
runs, engines, runtimes, artifacts, approvals, policies, secrets,
organizations, orgs, workspaces, accessgroups
```

### Runs

```text
officeos run <agent-name> --task <text> [--engine <name>] [--repo <url|path>] [--ref <ref>]
officeos run -f <run-manifest.yaml> [--wait]
officeos get runs [--agent <name>] [--status <status>]
officeos describe run <run-id>
officeos cancel run <run-id>
officeos wait run <run-id> [--for <condition>] [--timeout <duration>]
```

Examples:

```text
officeos run fix-ci --task "Fix failing backend tests"
officeos run code-review --repo . --task "Review current branch"
officeos wait run 01hw... --for complete --timeout 30m
```

### Logs And Events

```text
officeos logs run/<run-id> [-f]
officeos logs agent/<agent-name> [-f] [--since <duration>]
officeos events [--kind <kind>] [--since <duration>]
officeos audit [--since <duration>] [--actor <user>] [--resource <kind/name>]
```

Logs are typed entries, not chat messages. The CLI should preserve backend log semantics such as `message_in`, `tool_call`, `tool_result`, `message_out`, `system`, and `error`.

### Integrations, Channels, And Secrets

Resource creation should prefer manifests, but credential bootstrap needs interactive commands:

```text
officeos secret set <name> --from-literal <key=value>
officeos secret set <name> --from-file <key=path>
officeos secret get <name>
officeos secret delete <name>

officeos integration auth <integration-name>
officeos integration test <integration-name>

officeos channel test <channel-name> --message <text>
officeos channel activate <channel-name>
officeos channel deactivate <channel-name>
```

Secret values are never printed unless an explicit future `--reveal` flow is added with policy checks.

### Models And Providers

```text
officeos models
officeos providers
officeos provider auth <provider-name>
officeos provider set-default <provider-name> [--model <model>]
```

### Completion And Schema

```text
officeos completion <bash|zsh|fish|powershell>
officeos schema [-o json]
```

The only supported manifest API version is `officeos.io/v1`. The VS Code extension should consume `officeos schema -o json` for completion, diagnostics, and hover docs.

## Output Contracts

Default table:

```text
NAME       KIND    STATUS   AGE
fix-ci     Agent   Ready    4d
slack      Channel Active   2d
```

Name output:

```text
agent/fix-ci
channel/slack
```

JSON/YAML output returns resource envelopes:

```json
{
  "apiVersion": "officeos.io/v1",
  "kind": "Agent",
  "metadata": {
    "name": "fix-ci",
    "workspace": "platform"
  },
  "status": {
    "phase": "Ready"
  }
}
```

## Exit Codes

- `0`: success.
- `1`: command failed.
- `2`: invalid command usage or invalid flags.
- `3`: validation failed.
- `4`: authentication or authorization failed.
- `5`: resource not found.
- `6`: conflict or optimistic concurrency failure.
- `7`: timeout while waiting.

## Backend Control Plane Shape

The backend remains a long-running server because async runs, schedules, webhooks, shared memory, audit, and runners must outlive the local CLI process. The API should be private and CLI-oriented.

Target API prefix:

```text
/api/control-plane/v1
```

Minimum endpoints:

```text
POST /auth/device/code
POST /auth/device/token
GET  /me

POST /manifests/validate
POST /manifests/diff
POST /manifests/apply
DELETE /manifests
GET  /schema

GET    /resources
GET    /resources/{kind}
GET    /resources/{kind}/{name}
DELETE /resources/{kind}/{name}

POST /runs
GET  /runs
GET  /runs/{id}
POST /runs/{id}/cancel
GET  /runs/{id}/logs
GET  /events
GET  /audit
```

Existing endpoints can be bridged initially:

- `/api/cli/*` -> `/api/control-plane/v1/auth/*`
- `/api/declarative/*` -> `/api/control-plane/v1/manifests/*`
- `/api/cli/code/*` -> `/api/control-plane/v1/runs` and `/logs`

## Backend Refactor Plan

### 1. Remove Dashboard Surface

Delete or stop registering:

- `GraphQlRootTypes.cs`
- `HotChocolate` GraphQL setup.
- Dashboard query/mutation/subscription API files.
- `DashboardAuthMiddleware`.
- dashboard CORS policy and `FrontendConfig`.
- dashboard-only PostHog mutations.
- Swagger as a product interface.

Keep non-dashboard ingress:

- CLI auth.
- Manifest control-plane endpoints.
- Channel inbound webhooks.
- Routine/webhook triggers.
- Agent runtime callbacks.
- Health checks.

### 2. Expand Declarative Resource Application

Current `AgentDefinitions` owns manifest parsing for agents. Replace the agent-only shape with a general declarative resource application service:

```text
Features/AgentDefinitions/Application
  DeclarativeManifestParser.cs
  DeclarativeResourceService.cs
  DeclarativeResourceContracts.cs
```

The service should:

- Parse multi-document YAML.
- Validate resource envelope.
- Route each resource to the owning feature applicator.
- Produce deterministic validate/diff/apply results.
- Publish domain events for lifecycle changes.

Owning features keep their domain records and services:

- `Agent` -> `Features/Agents`
- `Channel` -> `Features/Channels`
- `Integration` -> `Features/Integrations`
- `MemoryStore` -> `Features/Context`
- `Routine` -> `Features/AgentRoutines`
- `Policy`, `Organization`, `Workspace`, `AccessGroup` -> `Features/Management`
- `Provider` or model defaults -> `Features/Providers`

### 3. Add Resource Listing Projections

`officeos get <kind>` needs a generic resource read model. Add a small Application-level projection contract:

```text
ResourceSummaryProjection
ResourceDetailProjection
ResourceStatusProjection
```

Each feature maps its domain records into the projection. The projection is not a domain DTO; it is CLI/control-plane output.

### 4. Convert Runs Into First-Class Resources

`Run` should become the central execution object, not a dashboard session side effect.

Run fields:

- `metadata.name` optional human name.
- `spec.agentRef`
- `spec.task`
- `spec.engineRef`
- `spec.workspaceRuntimeRef`
- `spec.repository`
- `spec.inputs`
- `status.phase`
- `status.startedAt`
- `status.completedAt`
- `status.result`
- `status.artifacts`

`CliCodeController` should collapse into run creation and run log streaming.

### 5. Keep Feature Ownership

Do not create one giant generic resource domain model. The generic manifest layer is orchestration. The source of truth remains feature-owned domain records such as:

- `OffceOs.Domain.Features.Channels.ChannelConnectionRecord`
- `OffceOs.Domain.Features.Agents.AgentRecord`
- `OffceOs.Domain.Features.Context.MemoryStoreRecord`
- `OffceOs.Domain.Features.AgentRoutines.AgentRoutineRecord`

Resource applicators should be thin Application services that call existing feature services/repositories and publish events.

### 6. CLI Implementation Order

1. Rename config path to `~/.officeos/config.yaml` and keep `~/.eaos` read fallback.
2. Add parser/router support for `get`, `describe`, `delete`, `logs`, `run`, `wait`, `config`, `models`, and `providers`.
3. Add generic `resources-api.ts` and `runs-api.ts`.
4. Add table, wide, name, JSON, and YAML output helpers.
5. Keep `validate`, `diff`, `apply`, and `export` working while endpoint names migrate.

### 7. Backend Implementation Order

1. Add `/api/control-plane/v1` endpoints that wrap current CLI/declarative services.
2. Add generic resource list/detail projections for existing domain records.
3. Move code-session behavior to `Run` endpoints.
4. Delete dashboard GraphQL registration and dashboard-only API files.
5. Remove dashboard-only packages and config once compilation is clean.
6. Expand manifest apply from `Agent` to `Channel`, `Integration`, `MemoryStore`, `Routine`, `Policy`, and `WorkspaceRuntime`.
