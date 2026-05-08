# Backend Agent Instructions

This backend uses clean architecture. Keep changes inside the correct layer and avoid shortcuts that blur transport, application policy, domain behavior, and infrastructure. When adding a type, first decide which layer owns the concept, then put it in that layer's existing feature folder.

## Project Layout

- `src/EnterpriseAgentOs.Api`: transport boundary. HTTP endpoints, GraphQL query/mutation/subscription classes, auth context, middleware, startup wiring, and API-only input/output types.
- `src/EnterpriseAgentOs.Application`: use-case orchestration. Application services, event handlers, background services, agent turn flow, tool execution coordination, billing checkpoints, request/result types used by application services.
- `src/EnterpriseAgentOs.Domain`: business contracts and model. Rich records, value objects, domain services/registries, repository/service interfaces, domain events, result primitives, validation/invariant rules.
- `src/EnterpriseAgentOs.Infrastructure`: implementation details. EF entities, `EaosDbContext`, repository implementations, external service adapters, provider dispatch, security wrappers, config classes, migrations, persistence mapping.
- `tests/EnterpriseAgentOs.Api.Tests`: backend tests. The test project references Application and Infrastructure and is used for domain, application, provider, billing, sandbox, and persistence behavior.

Do not make Domain depend on Application, Infrastructure, ASP.NET, EF, hosting, GraphQL, Redis, Stripe, Kubernetes, Docker, or other transport/infrastructure concepts.

## Feature Folders

- Use the existing feature slices: `Agents`, `Analytics`, `Management`, and `Mcp`.
- In Api/Application/Infrastructure, feature folders are under `Features/<FeatureName>`.
- In Domain, existing feature folders are under `features/<FeatureName>` on disk, with namespace `EnterpriseAgentOs.Domain.Features.<FeatureName>`. Follow the current folder casing unless doing a deliberate repo-wide cleanup.
- Shared domain concepts go under `Domain/Common`.
- Shared infrastructure concepts go under `Infrastructure/Common`.
- Cross-cutting API helpers go under `Api/Common`.

## Where Types Go

- Domain records: `Domain/features/<Feature>/Records`. Use these for rich business records that repositories load/save, such as agent, billing, MCP, organization, and log records.
- Domain DTOs: `Domain/features/<Feature>/Dtos` only when the DTO is a stable domain/application contract reused outside one transport class. Do not put GraphQL-only shapes here.
- Domain interfaces: `Domain/features/<Feature>/Interfaces`. Repository interfaces and business port interfaces live here, including interfaces implemented by Application or Infrastructure.
- Domain registries: `Domain/features/<Feature>/Registries` for feature-specific static catalogs. Use `Domain/Common/Services` for shared provider/model/system-prompt registries and definitions.
- Value objects and enums with storage conversion helpers: `Domain/Common/ValueObjects`.
- Result/error primitives: `Domain/Common/Primitives`.
- Domain events: `Domain/Events`, one event per file when practical, inheriting from `DomainEvent`.
- Application services: `Application/Features/<Feature>/Services`.
- Application event handlers: `Application/Features/<Feature>/Handlers`. MediatR notification handlers belong here, not in Api or Infrastructure.
- Application tool code: `Application/Features/Agents/Tools`, with browser tool implementations in `Tools/Browser`.
- Application request/result/types used by services: `Application/Features/<Feature>/Types` or the existing feature-level type file if that is the local convention.
- Application-owned interfaces that are not domain ports and are only used inside application internals may live in `Application/Features/<Feature>/Interfaces`.
- Infrastructure repositories: `Infrastructure/Features/<Feature>/Repositories`, implementing Domain repository interfaces.
- Infrastructure adapters: `Infrastructure/Features/<Feature>/Adapters`, implementing Domain or Application-facing ports for external systems.
- Infrastructure entities: `Infrastructure/Common/Entities`. EF entities are persistence-only and must not be returned from repositories.
- Infrastructure config classes: `Infrastructure/Common/Configuration`.
- Infrastructure security wrappers/protectors: `Infrastructure/Common/Security`.
- EF migrations: `Infrastructure/Migrations`.
- GraphQL queries/mutations/subscriptions: `Api/Features/<Feature>/Queries`, `Mutations`, and `Subscriptions`.
- REST/minimal endpoints: `Api/Features/<Feature>/Endpoints`.
- MVC controllers: `Api/Features/<Feature>/Controllers`.
- GraphQL/API input and payload records: `Api/Features/<Feature>/Types` when transport-specific; keep them stable and explicit.
- Middleware and auth attributes: `Api/Common/Middleware`.
- GraphQL registration helpers and root extension types: `Api/Common` or `Api/Common/Extensions`.

## Dependency Rules

- Api may depend on Application, Domain, and Infrastructure for startup wiring and transport integration.
- Application may depend on Domain abstractions and injected infrastructure-facing contracts. It must not depend on EF entities, `EaosDbContext`, ASP.NET request state, GraphQL types, or database schema details.
- Domain may depend only on BCL and domain-level contracts/primitives. `MediatR.Contracts` is allowed only for `DomainEvent`.
- Infrastructure may depend on Domain and external libraries. It maps between EF entities and Domain records.
- Do not pass transport DTOs into Domain records or repository interfaces. Convert at the Api/Application boundary.
- Do not pass EF entities, tracked queries, or `DbContext` outside Infrastructure.

## Domain And Persistence

- Database entities stay decoupled from domain records. Repositories map EF entities to rich domain records and back.
- Put repository filters next to their repository interfaces in Domain, usually in the same file as the interface.
- Repository methods should accept domain filters/records/value objects, not primitive soup when a filter already exists.
- Rich-load methods should return aggregate records with child record lists populated, as existing `GetByAsync` methods do.
- Use `AsNoTracking()` for read queries unless the implementation is intentionally updating tracked entities.
- Storage strings should be converted through value-object/enum helpers such as `ToStorageString()` and `ToAgentStatus()`, not duplicated ad hoc.
- Keep validation and invariant-setting close to domain records when it is true business behavior. Use factory methods such as `Create(...)` when construction needs validation/defaulting.
- Add/update EF model configuration in `EaosDbContext.OnModelCreating` when an entity changes, including indexes, max lengths, conversions, relationships, and delete behavior.
- Add migrations for schema changes. Do not manually edit snapshots except as part of a generated/fixed migration.

## Application Services

- Application services orchestrate collaborators; they should not know transport details or database schema details.
- Keep responsibilities narrow. If a service comment says it owns only a specific part of the agent loop, respect that boundary.
- The agent turn loop is intentionally split across `AgentTurnService`, `AgentRunLifecycle`, `TurnEventPublisher`, `TurnContextBuilder`, `BillingCheckpoint`, `LlmTurnExecutor`, and `ToolExecutionLoop`. Change the owning class for the behavior, not the nearest caller.
- Use MediatR domain events for lifecycle changes where possible. Application services publish events; handlers react in `Application/Features/*/Handlers`.
- Prefer explicit result records or `AgentResult<T>` for expected business outcomes. Use exceptions for unexpected faults, invalid invariants, or unrecoverable configuration errors.
- Keep environment checks out of business services. Convert environment into explicit config/policy objects in `Program.cs`, then inject those policies.
- Register application services in `ApplicationServiceRegistration.AddApplication`.
- Internal implementation classes should generally be `internal sealed`; public contracts and DTO/record types can be public when referenced across projects.
- For Services and Repositorys we use generic endpoints with FilterObject parmeters instead of exposing multiple methodes e.g. SearchByName() and SearchById() will be compressed into a single SearchBy(filter) endpoint

## Events And Logging

- Domain events represent lifecycle facts: agent creation/update/deletion, turn start/completion/diagnostics, pod connection, LLM usage, tool calls/results, message in/out, channel routing, compaction, and errors.
- Put new events in `Domain/Events` and publish them through MediatR from Application services.
- Put event side effects in Application handlers. For example, log persistence and broadcasts are handler responsibilities, not inline string logging in the turn loop.
- Agent interactions are structured log entries, not chat messages. Preserve typed log semantics such as `MessageIn`, `ToolCall`, `ToolResult`, `MessageOut`, `System`, and typed error categories.
- Use `AgentLogRecord` factory methods when they match the entry being created. Add a typed factory before spreading repeated log construction.
- Do not add ad hoc string logs as a substitute for existing typed events or log records. Serilog/`ILogger` is for operational diagnostics, not the agent timeline.

## API And GraphQL

- GraphQL query/mutation/subscription classes belong in Api feature folders and extend `GraphQLQueries`, `GraphQLMutations`, or `GraphQLSubscriptions` where appropriate.
- API methods authenticate/authorize through existing auth context helpers or middleware. Do not push dashboard/session auth checks into Application services.
- API classes translate GraphQL/HTTP input into Application request records and translate Application results into API payloads.
- Throw `GraphQLException` with explicit codes for transport validation/not-found errors at the GraphQL boundary.
- GraphQL DTOs should be stable and explicit; do not make the dashboard infer provider, billing, or permission behavior from model name prefixes or display strings.
- Dashboard URL/tab state and UI behavior belong in the dashboard, not backend GraphQL side effects.
- Cache invalidation for API query caches belongs at the API/application boundary where the mutation knows which dashboard cache keys it invalidates.

## Infrastructure

- Register infrastructure services in `InfrastructureServiceRegistration.AddInfrastructure`.
- Repositories implement Domain interfaces and own all EF mapping. Keep mapping helpers private/internal inside the repository unless multiple repositories genuinely share them.
- Adapters implement external systems such as channel sidecars, MCP clients, Stripe, PostHog, Kubernetes/Docker, browser runtime, S3, or LLM provider dispatch.
- Config classes are simple records/classes under `Infrastructure/Common/Configuration`. Bind and validate them in `Program.cs`, then register them as singleton instances.
- Protectors and credential wrappers live in `Infrastructure/Common/Security`; do not expose plaintext credentials beyond the smallest necessary scope.
- HTTP clients should be registered centrally in Infrastructure or startup wiring, not newed up inside services.

## Configuration

- Bind config in `Program.cs`, validate required production values there, and register config as singleton.
- The local `.env` is only for development. Staging/Production receive config from Kubernetes secrets and environment variables.
- Prefer policy/config names that express behavior, e.g. `EnforceUsageLimits`, instead of checking `IsDevelopment()` deep inside application logic.
- Support both appsettings-style PascalCase and environment variable names where existing helpers do so.
- Environment variable examples belong in the root `.env.example`.

## LLM Providers

- `ProviderRegistry` is the stable source for built-in provider/model metadata.
- Hosted provider connection state comes from configured platform keys, not from development mode.
- OpenAI-compatible providers should use the shared dispatcher path unless a provider truly needs translation.
- Anthropic is the format-translation exception; keep provider-specific translation isolated.
- Self-hosted/custom providers are OpenAI-compatible and configured through env-backed config.
- Provider changes must preserve truthful configured/unconfigured state, model metadata, cost weights, and dispatch auth behavior.

## Billing

- Billing guard checks quota and returns explicit quota state.
- Billing checkpoint decides how quota state affects the agent turn.
- Development may disable enforcement through policy config, but provider configuration must still be truthful.
- Credit recording must refuse non-positive usage and preserve billing consistency before continuing a turn.
- Billing behavior that differs by deployment policy needs tests for both enforced and disabled paths.

## Agent Tools

- Built-in tool implementations live in `Application/Features/Agents/Tools`.
- Browser tools live in `Application/Features/Agents/Tools/Browser` and use `IBrowserService`/`IBrowserRuntimeClient` abstractions.
- Tool names, runtime names, permission scopes, deferral behavior, read-only flags, and concurrency flags are part of the tool contract. Update catalog/permission tests when those change.
- Tool validation should return `ToolValidationResult` for expected bad input; tool execution should return `AgentResult<ToolResult>` for expected tool failures.
- Do not bypass the sandbox abstractions from tool code.

## Tests

- Add focused tests for behavior changes in `apps/backend/tests/EnterpriseAgentOs.Api.Tests`.
- Put tests under a folder matching the feature/risk area, such as `Agents`, `Billing`, or `Sandbox`.
- Use small fakes for application-service tests.
- Use EF in-memory only when persistence mapping is part of the behavior.
- For provider changes, test listing/configured state, dispatch shape, auth header behavior, and model/cost metadata.
- For billing changes, test quota allowed/exceeded, enabled/disabled policy, and recording consistency.
- For tool changes, test registration/catalog behavior, validation, permission/deferred loading behavior, and runtime call shape.
- For repository or schema changes, test mapping and persistence behavior, not EF internals.
