# Backend Agent Instructions

This backend is a single-project, feature-first modular monolith. Keep clean architecture boundaries, but keep them local to the feature you are changing.

## Project Layout

- `src`: the single backend project and application host.
- `tests/EnterpriseAgentOs.Api.Tests`: backend tests.

Only these top-level feature folders are allowed under `src/Features`:

- `Agents`
- `Channels`
- `Context`
- `Analytics`
- `Management`

Each feature owns its local layers:

- `Domain`: business records, value objects, repository/service interfaces, filters, domain events, and invariants.
- `Application`: use-case services, background jobs, policies, request/result records, and orchestration.
- `EventHandlers`: MediatR notification handlers and internal event wiring. Keep these thin; delegate behavior to Application services.
- `Infrastructure`: EF repository implementations, external adapters/clients, provider dispatch, security wrappers, and feature infrastructure.
- `Api`: GraphQL queries/mutations/subscriptions, REST/minimal endpoints/controllers, API input/payload types, auth/transport validation.

Feature layer folders are intentionally flat except for agent tool code. Do not add bucket subfolders such as `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, or subdomain folders by default. Use strong file/type names such as `AgentRunRepository.cs`, `MemoryStoreMutations.cs`, and `McpServerRecord.cs`.

Allowed nested feature folders:

- `src/Features/Agents/Application/Tools`
- `src/Features/Agents/Application/BrowserTools`

Shared code lives under:

- `src/Common/Domain`
- `src/Common/Application`
- `src/Common/Infrastructure`
- `src/Common/Middleware`
- `src/Configuration`
- `src/Database`
- `src/Events`
- `src/Extensions`

## Feature Ownership

Do not create top-level feature folders named `Atlas`, `Data`, `Mcp`, `Billing`, `Auth`, or `Browser`.

Agents subdomains are expressed through strong type and file names, not nested folders:

- `Core`: agent identity, status, ownership, provider/model settings.
- `Runtime`: agent runs, sessions, turn lifecycle, session context.
- `Tools`: built-in tools, tool permissions, tool catalog contracts.
- `Mcp`: MCP servers, credentials, agent assignments, discovered tools.
- `Memory`: agent memory stores and entries. This replaces top-level `Data`.
- `Browser`: browser runtime/session records and browser-specific contracts.
- `Scheduling`: cron jobs and scheduled runs.

Channels owns channel connections, credentials, bindings, inbound routing, sidecar delivery, and channel GraphQL/endpoints.

Context owns markdown-style memory stores, memory entries, external integration connections, integration indexing, indexed records, and integration execution.

## Dependency Rules

Even though this is one project, layer boundaries still apply:

- Domain must not depend on Api, Infrastructure, EF, ASP.NET, GraphQL, Redis, Stripe, Kubernetes, Docker, or hosting concepts.
- Application may depend on Domain abstractions and injected infrastructure-facing contracts. It must not depend on EF entities, `EaosDbContext`, ASP.NET request state, GraphQL types, or database schema details.
- Infrastructure may depend on Domain, Database, and external libraries. Repositories map between EF entities and Domain records.
- Api may compose Application, Domain, and Infrastructure.
- Do not pass transport `*Input`/`*Payload` types into Domain records or repository interfaces.
- Do not pass EF entities, tracked queries, or `DbContext` outside Database/Infrastructure.

## Naming

Domain:

- `*Record`: persistent business record loaded/saved by repositories.
- `*Filter`: repository query filter.
- `I*Repository`: persistence port implemented by Infrastructure.
- `I*Service`: only for real domain-facing abstractions. Do not put use-case service contracts in Domain.
- `*Event`: lifecycle fact published through MediatR.
- Value objects keep direct names, for example `Email`, `ToolKey`, or `CronExpression`.

Application:

- `*Request`: use-case input only when a method has a real command/workflow boundary; otherwise prefer explicit parameters.
- `*Result`: use-case output only when returning a Domain record would be wrong or incomplete.
- `*Service`: use-case orchestration.
- `*Contracts`: tight file grouping for application service interfaces plus their request/result records when those types are only useful together.
- `*Policy`: application decision rules.

EventHandlers:

- `*Handler`: MediatR event handler.

Api:

- `*Input`: GraphQL/HTTP input.
- `*Payload`: GraphQL/HTTP output.
- `*Queries`, `*Mutations`, `*Subscriptions`, `*Controller`, `*Endpoint`.
- `*Mapper`: API-only mapping helper.

Infrastructure:

- `*Repository`: implementation of a Domain repository.
- `*Adapter`, `*Client`, `*Gateway`: external system implementation.
- `*Config`: infrastructure config for external systems.
- `*Protector`: credential/data protection implementation.

Database:

- `*Entity`: EF persistence entity under `src/Database/Models`.
- `EaosDbContext`: centralized EF context under `src/Database`.
- EF migrations live under `src/Database/Migrations`.

Avoid:

- `Dto` anywhere under feature Api/Application/Domain. Use `*Input`, `*Payload`, `*Request`, `*Result`, `*Projection`, `*Record`, `*Filter`, or a clearer business contract name.
- Generic `Integration*` names unless the type truly covers more than MCP and context connectors.
- Broad files named `Types.cs`, `Records.cs`, or `Repositories.cs`.
- Duplicate layer files like `AgentTypes.cs` in multiple places.
- Bucket folders named `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, or subdomain wrappers inside feature layers.

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

Domain DTOs are not allowed by default.

- GraphQL-only shape: put it in the feature's `Api` layer.
- Use-case request/result: put it in the feature's `Application`.
- Persistence model: keep it as a Database `*Entity` under `src/Database/Models`.
- Business record: keep it as a Domain `*Record`.
- Stable cross-layer business contract: may stay in Domain, but avoid the suffix `Dto` if a better domain name exists.

If a new `Dto` appears under `Features/*/Domain`, treat it as a failing architecture review unless there is an explicit documented exception.

## Application Services

- Application services orchestrate collaborators; they should not know transport details or database schema details.
- Keep responsibilities narrow. If a service owns only a specific part of the agent loop, respect that boundary.
- The agent turn loop is intentionally split across `AgentTurnService`, `AgentRunLifecycle`, `TurnEventPublisher`, `TurnContextBuilder`, `BillingCheckpoint`, `LlmTurnExecutor`, and `ToolExecutionLoop`.
- Use MediatR domain events for lifecycle changes where possible. Application services publish events; handlers react in EventHandlers.
- Return Domain records from Application services when the record is already safe and accurate for the use case.
- Add `*Result` only for composed data, external-provider response shapes, calculations, or when exposing the Domain record would leak data or couple the caller to an aggregate it does not need.
- Add `*Request` only when a use case has enough command data or workflow semantics to justify naming it.
- Prefer `AgentResult<T>` for expected success/failure business outcomes.
- Register application services in `ApplicationServiceCollectionExtensions.AddApplication`.
- Internal implementation classes should generally be `internal sealed`.

## Event Handlers

- MediatR notification handlers belong in `Features/<Feature>/EventHandlers`.
- Event handlers are wiring, not business logic. They should translate the event into calls to Application services, infrastructure ports, publishers, or background work.
- Do not put domain invariants, transport validation, repository mapping, or large orchestration flows in handlers.
- Handler namespaces use `EnterpriseAgentOs.EventHandlers.Features.<Feature>`.

## Events And Logging

- Domain events represent lifecycle facts: agent creation/update/deletion, turn start/completion/diagnostics, pod connection, LLM usage, tool calls/results, message in/out, channel routing, compaction, and errors.
- Put event side effects in EventHandlers.
- Agent interactions are structured log entries, not chat messages. Preserve typed log semantics such as `MessageIn`, `ToolCall`, `ToolResult`, `MessageOut`, `System`, and typed error categories.
- Use `AgentLogRecord` factory methods when they match the entry being created.
- Serilog/`ILogger` is for operational diagnostics, not the agent timeline.

## API And GraphQL

- GraphQL query/mutation/subscription classes belong in the feature's `Api` folder and extend `GraphQLQueries`, `GraphQLMutations`, or `GraphQLSubscriptions`.
- API methods authenticate/authorize through existing auth context helpers or middleware.
- API classes translate Api `*Input` records into Application `*Request` records and translate Application `*Result` or Domain `*Record` values into Api `*Payload` only when transport needs a different response shape.
- Public API method parameters must not expose Application `*Request` or `*Result` types directly.
- Throw `GraphQLException` with explicit codes for transport validation/not-found errors at the GraphQL boundary.
- GraphQL DTOs are not allowed by suffix; use stable explicit `*Input` and `*Payload` names.
- Simple owner-scoped reads are allowed in Api.
- Writes, multi-step workflows, domain event publishing, agent runtime/session/tool orchestration, credential protection, provider validation, billing checks, gateway reloads, and policy decisions belong in Application.

## API/Application Boundary

Do not add empty Application services just to forward one repository method, but do not let API methods become use-case orchestration.

Allowed in Api:

- Simple owner-scoped reads.
- Transport input validation.
- Mapping Api `*Input` to Application `*Request`.
- Mapping Application `*Result` or Domain `*Record` to Api `*Payload`.
- GraphQL/HTTP error translation.
- Cache invalidation for GraphQL dashboard response caches.

Not allowed in Api:

- Writes directly to repositories.
- Multi-step workflows.
- Ownership checks that require more than passing `UserContext.Id` into a repository filter.
- Domain event publishing.
- Agent runtime/session/tool orchestration.
- Gateway reloads, credential protection, log append/send behavior, provider validation, billing checks, or policy decisions.

## Infrastructure

- Register infrastructure services in `InfrastructureServiceCollectionExtensions.AddInfrastructure`.
- Repositories implement Domain interfaces and own all EF mapping.
- Adapters implement external systems such as channel sidecars, MCP clients, Stripe, PostHog, Kubernetes/Docker, browser runtime, S3, or LLM provider dispatch.
- Config classes are simple records/classes under root `src/Configuration`.
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

## Static Analysis

Naming and layer conventions are enforced by the local Roslyn analyzer:

```text
analyzers/EnterpriseAgentOs.Architecture.Analyzers
```

The analyzer runs during `dotnet build` through an analyzer project reference in `src/EnterpriseAgentOs.Api.csproj`.

Current diagnostics:

- `EAOS001`: Domain must not define `*Dto` types.
- `EAOS002`: Feature layers must not use broad `*Types.cs` files.
- `EAOS003`: API mutations must not inject repositories; call Application services for use cases.
- `EAOS004`: Domain must not depend on outer layers or transport/infrastructure frameworks.
- `EAOS005`: Feature Api/Application layers must not define `*Dto` types.
- `EAOS006`: Feature type names must match their layer vocabulary.
- `EAOS007`: Feature type suffixes must be declared in the correct layer/path.
- `EAOS008`: Public API boundary methods must not expose Application `*Request`/`*Result` types as parameters.

Layer vocabulary enforced by `EAOS006`:

- Domain: `*Record`, `*Filter`, `*Event`, `*Result`, `*Request`, `*Response`, `*Config`, `*Message`, `*Context`, `*Definition`, `*Provider`, `*Kinds`, `*State`, `*Descriptor`, `*Tool`, `*Deployment`, `*Row`, `*Options`, `*Page`, `*Overview`, `*Exception`, `*Subscription`, `*Limit`, and `I*Repository`/`I*Service`/explicit domain ports.
- Application: `*Service`, `*Request`, `*Result`, `*Policy`, `*Projection`, `*Entry`, `*Item`, `*Export`, `*Context`, `*Builder`, `*Executor`, `*Publisher`, `*Resolver`, `*Parser`, `*Detector`, `*Checkpoint`, `*Guard`, `*Lifecycle`, `*Scope`, `*Loop`, `*Session`, `*Connection`, `*Tool`, `*Registry`, `*Factory`, `*Store`, and closely named orchestration helpers.
- Api: `*Input`, `*Payload`, `*Queries`, `*Mutations`, `*Subscriptions`, `*Controller`, `*Endpoint`, `*Mapper`, plus bootstrap/summary payload helper names.
- Infrastructure: `*Repository`, `*Adapter`, `*Client`, `*Gateway`, `*Config`, `*Protector`, `*Dispatcher`, `*Translator`, `*Sandbox`, `*Store`, `*Injector`, `*Router`, `*Service`, `*Manager`, `*Handle`, `*Response`.
- EventHandlers: `*Handler`.

Path placement enforced by `EAOS007`:

- `*Input`, `*Payload`, `*Queries`, `*Mutations`, `*Subscriptions`, `*Controller`, and `*Endpoint` belong in `Api`.
- `*Projection`, `*Export`, and `*Policy` belong in `Application`.
- `*Record`, `*Filter`, and `*Event` belong in `Domain`.
- `*Request` and `*Result` belong in `Application` or `Domain`, not `Api` or `Infrastructure`.
- `I*Repository` belongs in `Domain`; repository implementation classes belong in `Infrastructure`.
- `*Handler` belongs in `EventHandlers`.
- `*Entity` belongs in `src/Database/Models`, never under `src/Features`.
- Public API method parameters use Api `*Input` records and map to Application `*Request` records inside the method body.

## Running

- Never run the application yourself.
- After code changes, build or lint only to check compilation.
- Do not preserve legacy integrations by default. Prefer one clear big-bang integration unless explicitly told otherwise.
