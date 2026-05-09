# Backend Agent Instructions

This backend is a single-project, feature-first modular monolith. Keep clean architecture boundaries, but keep them local to the feature you are changing.

## Project Layout

- `src`: the single backend project and application host.
- `tests/EnterpriseAgentOs.Api.Tests`: backend tests.

Only these top-level feature folders are allowed under `src/Features`:

- `Agents`
- `Analytics`
- `Management`

Each feature owns its local layers:

- `Domain`: business records, value objects, repository/service interfaces, filters, domain events, and invariants.
- `Application`: use-case services, MediatR handlers, background jobs, policies, request/result records, and orchestration.
- `Infrastructure`: EF repository implementations, external adapters/clients, provider dispatch, security wrappers, and feature infrastructure.
- `Api`: GraphQL queries/mutations/subscriptions, REST/minimal endpoints/controllers, API input/payload types, auth/transport validation.

Shared code lives under:

- `src/Common/Domain`
- `src/Common/Application`
- `src/Common/Infrastructure`
- `src/Common/Extensions`
- `src/Common/Middleware`
- `src/Database`
- `src/Events`

## Feature Ownership

Do not create top-level feature folders named `Atlas`, `Data`, `Mcp`, `Billing`, `Auth`, `Browser`, or `Channels`.

Agents subdomains:

- `Core`: agent identity, status, ownership, provider/model settings.
- `Runtime`: agent runs, sessions, turn lifecycle, session context.
- `Tools`: built-in tools, tool permissions, tool catalog contracts.
- `Channels`: channel bindings and channel connections.
- `Mcp`: MCP servers, credentials, agent assignments, discovered tools.
- `Memory`: agent memory stores and entries. This replaces top-level `Data`.
- `Context`: indexed external context/connectors. This replaces top-level `Atlas`.
- `Browser`: browser runtime/session records and browser-specific contracts.
- `Scheduling`: cron jobs and scheduled runs.

## Dependency Rules

Even though this is one project, layer boundaries still apply:

- Domain must not depend on Api, Infrastructure, EF, ASP.NET, GraphQL, Redis, Stripe, Kubernetes, Docker, or hosting concepts.
- Application may depend on Domain abstractions and injected infrastructure-facing contracts. It must not depend on EF entities, `EaosDbContext`, ASP.NET request state, GraphQL types, or database schema details.
- Infrastructure may depend on Domain, Database, and external libraries. Repositories map between EF entities and Domain records.
- Api may compose Application, Domain, and Infrastructure.
- Do not pass transport DTOs into Domain records or repository interfaces.
- Do not pass EF entities, tracked queries, or `DbContext` outside Database/Infrastructure.

## Naming

Domain:

- `*Record`: persistent business record loaded/saved by repositories.
- `*Filter`: repository query filter.
- `I*Repository`: persistence port implemented by Infrastructure.
- `I*Service`: domain/application-facing service contract only when a real abstraction is needed.
- `*Event`: lifecycle fact published through MediatR.

Application:

- `*Request`: use-case input.
- `*Result`: use-case output.
- `*Service`: use-case orchestration.
- `*Policy`: application decision rules.
- `*Handler`: MediatR event handler.

Api:

- `*Input`: GraphQL/HTTP input.
- `*Payload`: GraphQL/HTTP output.
- `*Queries`, `*Mutations`, `*Subscriptions`, `*Controller`, `*Endpoint`.

Infrastructure:

- `*Repository`: implementation of a Domain repository.
- `*Adapter`, `*Client`, `*Gateway`: external system implementation.
- `*Config`: infrastructure config for external systems.
- `*Protector`: credential/data protection implementation.

Database:

- `*Entity`: EF persistence entity under `src/Database/Models`.
- `EaosDbContext`: centralized EF context under `src/Database`.
- EF migrations live under `src/Database/Migrations`.

Avoid broad bucket files such as `Types.cs`, `Records.cs`, and `Repositories.cs` once they contain more than one tight aggregate family.

## Domain And Persistence

- Database is centralized under `src/Database`.
- Database entities live under `src/Database/Models`.
- The EF context and design-time factory live directly under `src/Database`.
- EF migrations live under `src/Database/Migrations`.
- Database entities stay decoupled from domain records. Repositories map EF entities to rich domain records and back.
- Put repository filters next to their repository interfaces.
- Repository methods should use filter records instead of primitive-heavy method variants.
- Prefer the small repository vocabulary: `ListAsync(filter)`, `GetByAsync(filter)`, `SaveAsync(record)` or `UpsertAsync(record)`, and `DeleteAsync(filter)`.
- Avoid duplicate scope-specific methods such as `ListEntriesAsync(storeId, ownerId)` and `ListEntriesForStoreAsync(storeId)`. Put scope in the filter.
- Rich-load methods should return aggregate records with child record lists populated.
- Use `AsNoTracking()` for read queries unless intentionally updating tracked entities.
- Storage strings should be converted through value-object/enum helpers such as `ToStorageString()` and `ToAgentStatus()`.
- Add/update EF model configuration in `EaosDbContext.OnModelCreating` when an entity changes.
- Add migrations for schema changes. Do not manually edit snapshots except as part of a generated/fixed migration.

## Domain DTO Policy

Domain DTOs are exceptional.

- GraphQL-only shape: put it in the feature's `Api/Types`.
- Use-case request/result: put it in the feature's `Application`.
- Persistence model: keep it as an Infrastructure `*Entity`.
- Business record: keep it as a Domain `*Record`.
- Stable cross-layer business contract: may stay in Domain, but avoid the suffix `Dto` if a better domain name exists.

## Application Services

- Application services orchestrate collaborators; they should not know transport details or database schema details.
- Keep responsibilities narrow. If a service owns only a specific part of the agent loop, respect that boundary.
- The agent turn loop is intentionally split across `AgentTurnService`, `AgentRunLifecycle`, `TurnEventPublisher`, `TurnContextBuilder`, `BillingCheckpoint`, `LlmTurnExecutor`, and `ToolExecutionLoop`.
- Use MediatR domain events for lifecycle changes where possible. Application services publish events; handlers react in Application.
- Prefer explicit result records or `AgentResult<T>` for expected business outcomes.
- Register application services in `ApplicationServiceRegistration.AddApplication`.
- Internal implementation classes should generally be `internal sealed`.

## Events And Logging

- Domain events represent lifecycle facts: agent creation/update/deletion, turn start/completion/diagnostics, pod connection, LLM usage, tool calls/results, message in/out, channel routing, compaction, and errors.
- Put event side effects in Application handlers.
- Agent interactions are structured log entries, not chat messages. Preserve typed log semantics such as `MessageIn`, `ToolCall`, `ToolResult`, `MessageOut`, `System`, and typed error categories.
- Use `AgentLogRecord` factory methods when they match the entry being created.
- Serilog/`ILogger` is for operational diagnostics, not the agent timeline.

## API And GraphQL

- GraphQL query/mutation/subscription classes belong in the feature's `Api` folder and extend `GraphQLQueries`, `GraphQLMutations`, or `GraphQLSubscriptions`.
- API methods authenticate/authorize through existing auth context helpers or middleware.
- API classes translate GraphQL/HTTP input into Application request records and translate Application results into API payloads.
- Throw `GraphQLException` with explicit codes for transport validation/not-found errors at the GraphQL boundary.
- GraphQL DTOs should be stable and explicit.

## Infrastructure

- Register infrastructure services in `InfrastructureServiceRegistration.AddInfrastructure`.
- Repositories implement Domain interfaces and own all EF mapping.
- Adapters implement external systems such as channel sidecars, MCP clients, Stripe, PostHog, Kubernetes/Docker, browser runtime, S3, or LLM provider dispatch.
- Config classes are simple records/classes under Infrastructure/Common or feature Infrastructure configuration.
- Protectors and credential wrappers live in Infrastructure.
- HTTP clients should be registered centrally in Infrastructure or startup wiring.

## Tests

- Add focused tests for behavior changes in `apps/backend/tests/EnterpriseAgentOs.Api.Tests`.
- Put tests under a folder matching the feature/risk area, such as `Agents`, `Billing`, or `Sandbox`.
- Use small fakes for application-service tests.
- Use EF in-memory only when persistence mapping is part of the behavior.
- For provider changes, test listing/configured state, dispatch shape, auth header behavior, and model/cost metadata.
- For billing changes, test quota allowed/exceeded, enabled/disabled policy, and recording consistency.
- For tool changes, test registration/catalog behavior, validation, permission/deferred loading behavior, and runtime call shape.
- For repository or schema changes, test mapping and persistence behavior, not EF internals.

## Running

- Never run the application yourself.
- After code changes, build or lint only to check compilation.
- Do not preserve legacy integrations by default. Prefer one clear big-bang integration unless explicitly told otherwise.
