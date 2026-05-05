# Backend Agent Instructions

This backend uses clean architecture. Keep changes inside the correct layer and avoid shortcuts that blur infrastructure, application policy, and domain behavior.

## Layers

- `EnterpriseAgentOs.Api`: HTTP, GraphQL, auth context, middleware, startup wiring, request/response DTOs.
- `EnterpriseAgentOs.Application`: use-case orchestration, services, handlers, billing checkpoints, agent turn flow, tool execution coordination.
- `EnterpriseAgentOs.Domain`: records, value objects, domain services, interfaces, domain events, result types, validation rules.
- `EnterpriseAgentOs.Infrastructure`: EF entities/repositories, external adapters, provider dispatch, security wrappers, config classes, migrations.

Do not make Domain depend on Application, Infrastructure, ASP.NET, EF, or hosting concepts.

## Domain And Persistence

- Database entities stay decoupled from domain records. Repositories map EF entities to rich domain records and back.
- Put repository interfaces in Domain and implementations in Infrastructure.
- Keep validation and invariant-setting close to domain records when it is true business behavior.
- Avoid leaking EF tracking behavior, `DbContext`, or entity types outside Infrastructure.

## Application Services

- Application services orchestrate collaborators; they should not know transport details or database schema details.
- Keep responsibilities narrow. If a service comment says it owns only a specific part of the agent loop, respect that boundary.
- Prefer explicit result records for expected business outcomes. Use exceptions for unexpected faults or unrecoverable configuration errors.
- Keep environment checks out of business services. Convert environment into explicit config/policy objects in startup, then inject those policies.

## Events And Logging

- Use MediatR domain events for lifecycle changes where possible, especially agent creation, updates, deletion, turn progress, tool calls, and LLM usage.
- Agent interactions are structured log entries, not chat messages. Preserve typed log semantics such as `message_in`, `tool_call`, `tool_result`, and `message_out`.
- Do not add ad hoc string logs as a substitute for existing typed events or log records.

## Configuration

- Config classes live in `Infrastructure/Common/Configuration`.
- Bind config in `Program.cs`, validate required production values there, and register config as singleton.
- Prefer policy/config names that express behavior, e.g. `EnforceUsageLimits`, instead of checking `IsDevelopment()` deep inside application logic.
- Environment variable examples belong in the root `.env.example`.

## LLM Providers

- `ProviderRegistry` is the stable source for built-in provider/model metadata.
- Hosted provider connection state comes from configured platform keys, not from development mode.
- OpenAI-compatible providers should use the shared dispatcher path unless a provider truly needs translation.
- Anthropic is the format-translation exception; keep provider-specific translation isolated.
- Self-hosted/custom providers are OpenAI-compatible and configured through env-backed config.

## Billing

- Billing guard checks quota and returns explicit quota state.
- Billing checkpoint decides how quota state affects the agent turn.
- Development may disable enforcement through policy config, but provider configuration must still be truthful.
- Credit recording must refuse non-positive usage and preserve billing consistency before continuing a turn.

## GraphQL And Dashboard API

- GraphQL query/mutation classes belong in Api feature folders.
- GraphQL DTOs should be stable and explicit; do not make the dashboard infer provider or billing behavior from model name prefixes.
- URL/tab state and UI behavior belong in the dashboard, not backend GraphQL side effects.

## Tests

- Add focused tests for behavior changes in `apps/backend/tests/EnterpriseAgentOs.Api.Tests`.
- Cover both enabled and disabled policy paths when behavior differs by deployment policy.
- For provider changes, test listing/configured state, dispatch shape, auth header behavior, and model/cost metadata.
- Use small fakes for application-service tests; use EF in-memory only when persistence mapping is part of the behavior.
