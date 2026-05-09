# Backend Naming And Style Convention Plan

Date: 2026-05-09

## Implementation Status

Initial refactor pass completed on 2026-05-09:

- The solution now contains one backend project, `src/OffceOs.csproj`, plus the test project.
- Api/Application/Domain/Infrastructure source files were moved into the single backend project under `Features/<Feature>/<Layer>`.
- Top-level `Atlas`, `Data`, and `Mcp` feature folders were folded under `Features/Agents` as `Context`, `Memory`, and `Mcp`.
- Shared code moved under `Common/{Application,Domain,Infrastructure}` and domain events moved under `Events`.
- EF code was centralized under `Database`: `Database/Models`, `Database/Migrations`, and `Database/EaosDbContext.cs`.
- The old `src/OffceOs.Api` wrapper directory was removed; backend code now lives directly under `src`.
- `AGENTS.md` was rewritten to describe the feature-first single-project convention.
- The solution builds successfully with `dotnet build OffceOs.sln`.

Second layout pass completed on 2026-05-09:

- Feature layer folders were flattened. Files now live directly in `Features/<Feature>/<Layer>`.
- Bucket folders such as `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, `Endpoints`, and subdomain wrappers were removed from feature layers.
- The convention is now strong file/type naming over deep folder nesting.

Third layout pass completed on 2026-05-09:

- `Channels` was split out from `Agents` as its own top-level feature.
- Agent tool code received the only nested-folder exception: `Agents/Application/Tools` and `Agents/Application/BrowserTools`.

Fourth layout pass completed on 2026-05-09:

- MediatR notification handlers were moved out of Application into `Features/<Feature>/EventHandlers`.
- Handler namespaces now use `OffceOs.EventHandlers.Features.<Feature>`.

Fifth layout pass completed on 2026-05-09:

- `Context` was split out as its own top-level feature.
- Memory stores and entries moved from Agents/Data naming to Context.
- Old Atlas filenames were renamed to IntegrationIndexing names.
- Integration indexing, indexed records, integration execution, and GitHub integration access now live under Context.

Sixth layout pass completed on 2026-05-09:

- Service registration extensions moved to root `src/Extensions`.
- GraphQL registration extensions moved to root `src/Extensions`.
- Configuration classes moved to root `src/Configuration`.

Seventh architecture pass completed on 2026-05-09:

- Domain `*Dto` files were removed.
- Mirror use-case result records were removed where the Domain record is already the right shape; for example, agent list/create/update now return `AgentRecord` instead of an `AgentResult` copy.
- Remaining use-case request/result records live in Application only when they protect a real boundary or compose data that is not already a Domain record.
- GraphQL/HTTP input and payload records moved to Api where they are transport-only.
- Application service contracts that describe use cases moved out of Domain into Application.
- Multi-step API workflows were moved behind Application services: agent dashboard provisioning, cron jobs, sessions, resources, memory store writes, auth profile/logout, and organization overview.
- API methods may still call Domain repositories directly for simple owner-scoped reads, but writes and orchestration now belong in Application services.
- `ChannelRepository.ListConnectionsAsync` now accepts `ChannelConnectionFilter`, removing in-memory owner filtering from the API.
- Broad `*Types.cs` files were removed or renamed to boundary-specific names such as `*Payloads`, `*Inputs`, `*Projections`, or the main type name.
- Architecture tests were added under `tests/OffceOs.Tests/Architecture` to enforce DTO, broad type-file, mutation/repository, and Domain dependency rules.
- The backend builds successfully with `dotnet build src/OffceOs.csproj --no-restore`.

Remaining cleanup:

- Namespaces still mostly use the old project-first names (`OffceOs.Domain.*`, `OffceOs.Application.*`, `OffceOs.Infrastructure.*`) to keep the big move compiling. Database namespaces were moved to `OffceOs.Database`.
- Some stale broad type names remain around MCP/context (`Integration*`). These should be tightened in a follow-up semantic cleanup.
- Repository interfaces still need the filter-based simplification pass.
- Existing nullable warnings remain.

## Goal

Move the backend toward a single-project, feature-first modular monolith that is easier to understand and work in.

The desired architecture is:

- One backend `.csproj`.
- Five top-level product features: `Agents`, `Channels`, `Context`, `Analytics`, `Management`.
- Inside each feature, keep the clean architecture boundaries: `Domain`, `Application`, `EventHandlers`, `Infrastructure`, `Api`.
- Centralized database code under `src/Database`, because EF context, models, and migrations are shared by the application.

This keeps the separation that matters, but makes feature work local. When working on Agents, the agent domain records, application services, repositories, GraphQL types, and endpoints should be near each other.

The feature layers are intentionally flat. Domain concepts are separated by names, not by nested `Records`/`Interfaces`/subdomain folders.

## Target Shape

Target high-level tree:

```text
src
├── Features
│   ├── Agents
│   │   ├── Domain
│   │   ├── Application
│   │   ├── EventHandlers
│   │   ├── Infrastructure
│   │   └── Api
│   ├── Channels
│   │   ├── Domain
│   │   ├── Application
│   │   ├── EventHandlers
│   │   ├── Infrastructure
│   │   └── Api
│   ├── Context
│   │   ├── Domain
│   │   ├── Application
│   │   ├── EventHandlers
│   │   ├── Infrastructure
│   │   └── Api
│   ├── Analytics
│   │   ├── Domain
│   │   ├── Application
│   │   ├── EventHandlers
│   │   ├── Infrastructure
│   │   └── Api
│   └── Management
│       ├── Domain
│       ├── Application
│       ├── EventHandlers
│       ├── Infrastructure
│       └── Api
├── Common
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── Middleware
├── Configuration
├── Database
│   ├── Models
│   ├── Migrations
│   └── EaosDbContext.cs
├── Events
├── Extensions
├── Program.cs
└── OffceOs.csproj
```

The old four-project split has been collapsed. New backend code should live directly under this `src` tree.

## Feature Rule

Only these top-level features are allowed:

- `Agents`
- `Channels`
- `Context`
- `Analytics`
- `Management`

Do not create top-level feature folders named:

- `Atlas`
- `Data`
- `Mcp`
- `Billing`
- `Auth`
- `Browser`

Those are subdomains inside one of the product features.

## Agents Subdomains

Agents is large, but it should stay flat inside each layer. These are conceptual subdomains, expressed through strong file/type prefixes rather than nested folders.

Recommended Agents shape:

```text
Features/Agents
├── Domain
├── Application
│   ├── Tools
│   └── BrowserTools
├── EventHandlers
├── Infrastructure
└── Api
```

Subdomain meanings:

- `Core`: agent identity, status, ownership, provider/model settings.
- `Runtime`: agent runs, sessions, turn lifecycle, session context.
- `Tools`: built-in tools, tool permissions, tool catalog contracts.
- `Mcp`: MCP servers, credentials, agent assignments, discovered tools.
- `Browser`: browser runtime/session records and browser-specific contracts.
- `Scheduling`: cron jobs and scheduled runs.

Channels owns Telegram/Slack/WhatsApp/Teams-style connections, credentials, agent bindings, inbound routing, sidecar delivery, and channel GraphQL/endpoints.

Context owns markdown-style memory stores, memory entries, external integration connections, integration indexing, indexed records, and integration execution.

## Layer Boundaries Inside A Feature

Even in one project, the boundaries should stay explicit.

Domain owns:

- Business records.
- Value objects.
- Repository interfaces.
- Domain service interfaces only for true domain abstractions, not application use-case orchestration.
- Domain events.
- Filters used by repositories.
- Validation and invariant helpers that are true business rules.

Application owns:

- Use-case services.
- Background jobs.
- Request/result records only when the Domain model is not already the correct shape.
- Application service contracts for use cases, for example `IAgentService`, `IAgentDashboardService`, `IProviderService`, `IAgentLogService`, and `IGdprService`.
- Policies that orchestrate domain behavior.
- Tool execution orchestration.

EventHandlers owns:

- MediatR notification handlers.
- Internal event wiring.
- Thin event-to-application-service delegation.

Infrastructure owns:

- Repository implementations.
- External service adapters and clients.
- Security wrappers/protectors.
- Provider dispatch implementation.
- Configuration for external systems.

Database owns:

- EF entities.
- `EaosDbContext` mappings.
- EF migrations.

Api owns:

- GraphQL queries/mutations/subscriptions.
- REST/minimal endpoints/controllers.
- API input and payload types.
- Transport validation and authorization.
- Mapping between transport types and application requests/results.
- Direct repository calls only for simple owner-scoped reads with no workflow, side effects, or policy decisions.

One project removes project-reference enforcement, so architecture tests must enforce these boundaries by namespace/folder rules.

## Namespace Convention

Use namespaces that mirror the flat feature/layer layout:

```csharp
OffceOs.Features.Agents.Domain
OffceOs.Features.Agents.Application
OffceOs.Features.Agents.Infrastructure
OffceOs.Features.Agents.Api

OffceOs.Features.Analytics.Domain
OffceOs.Features.Management.Application
```

Avoid namespaces like:

```csharp
OffceOs.Domain.Features.Agents.Integrations
OffceOs.Application.Features.Atlas
OffceOs.Infrastructure.Features.Data
```

## Model Naming

Domain:

- `*Record`: persistent business record loaded/saved by repositories.
- `*Filter`: repository query filter.
- `I*Repository`: persistence port implemented in the feature's Infrastructure folder.
- `I*Service`: only for real domain-facing abstractions. Do not put use-case service contracts in Domain.
- `*Event`: lifecycle fact published through MediatR.
- Value objects keep direct names, for example `Email`, `ToolKey`, `CronExpression`.

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

## API/Application Boundary

Do not add empty Application services just to forward one repository method, but do not let API methods become use-case orchestration.

The default is domain-first:

- Return Domain records from Application services when the record is already safe and accurate for the use case.
- Add `*Result` only for composed data, external-provider response shapes, calculations, or when exposing the Domain record would leak data or couple the caller to an aggregate it does not need.
- Add `*Payload` only at the transport boundary when GraphQL/HTTP needs a different response shape.
- Add `*Request` only when a use case has enough command data or workflow semantics to justify naming it.

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

Examples:

```csharp
// OK: simple owner-scoped read.
var row = await memoryStores.GetAsync(id, user.Id, ct);
return row is null ? null : ToPayload(row);

// Not OK: workflow belongs in Application.
await repository.SaveAsync(record, ct);
await publisher.Publish(new AgentUpdatedEvent(record.Id), ct);
await cache.RemoveAsync(cacheKey, ct);
```

## Repository Style

Repositories should be small and predictable.

Allowed method vocabulary:

```csharp
Task<IReadOnlyList<TRecord>> ListAsync(TFilter filter, CancellationToken ct = default);
Task<TRecord?> GetByAsync(TFilter filter, CancellationToken ct = default);
Task<TRecord> SaveAsync(TRecord record, CancellationToken ct = default);
Task<TRecord> UpsertAsync(TRecord record, CancellationToken ct = default);
Task<bool> DeleteAsync(TFilter filter, CancellationToken ct = default);
```

Use filters instead of method explosion.

Avoid:

- `GetByIdAsync`, `GetByNameAsync`, `SearchByNameAsync`, and `SearchByIdAsync` on the same repository.
- Primitive-heavy methods when a filter or request record is clearer.
- Duplicate methods for ownership scopes, such as `ListEntriesAsync(storeId, ownerId)` and `ListEntriesForStoreAsync(storeId)`.
- Repositories that manage multiple lifecycles unless the aggregate truly owns those children.

Memory example:

```csharp
public sealed record AgentMemoryStoreFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public Guid? AgentId { get; init; }
}

public sealed record AgentMemoryEntryFilter
{
    public Guid? Id { get; init; }
    public Guid? StoreId { get; init; }
    public Guid? OwnerId { get; init; }
    public string? Key { get; init; }
}

public interface IAgentMemoryStoreRepository
{
    Task<IReadOnlyList<AgentMemoryStoreRecord>> ListAsync(AgentMemoryStoreFilter filter, CancellationToken ct = default);
    Task<AgentMemoryStoreRecord?> GetByAsync(AgentMemoryStoreFilter filter, CancellationToken ct = default);
    Task<AgentMemoryStoreRecord> SaveAsync(AgentMemoryStoreRecord store, CancellationToken ct = default);
    Task<bool> DeleteAsync(AgentMemoryStoreFilter filter, CancellationToken ct = default);
}

public interface IAgentMemoryEntryRepository
{
    Task<IReadOnlyList<AgentMemoryEntryRecord>> ListAsync(AgentMemoryEntryFilter filter, CancellationToken ct = default);
    Task<AgentMemoryEntryRecord?> GetByAsync(AgentMemoryEntryFilter filter, CancellationToken ct = default);
    Task<AgentMemoryEntryRecord> SaveAsync(AgentMemoryEntryRecord entry, CancellationToken ct = default);
    Task<bool> DeleteAsync(AgentMemoryEntryFilter filter, CancellationToken ct = default);
}
```

The Application layer decides whether `OwnerId` is required for a user-facing use case. Infrastructure only applies the filter.

## File Style

Default:

- One public top-level type per file.
- One repository implementation per file.
- One domain event per file.
- One GraphQL query/mutation/subscription class per file.

Allowed exceptions:

- Small helper enums in the same file as the related value object.
- Request/result pairs that are only used together.
- A tight record family where splitting would hurt readability.

Do not keep broad bucket files like:

- `AtlasRecords.cs`
- `IAtlasRepositories.cs`
- `AtlasRepositories.cs`
- `AgentTypes.cs`
- `ProviderTypes.cs`

## Domain DTO Policy

Domain DTOs are not allowed by default. The current Domain DTO cleanup is complete.

Rules:

- GraphQL-only shape: move to the feature's `Api` layer.
- Use-case request/result: move to the feature's `Application`.
- Persistence model: keep as Infrastructure `*Entity`.
- Business record: keep as Domain `*Record`.
- Cross-layer stable business contract: may stay in Domain only when it is a real business concept; avoid the suffix `Dto`.

If a new `Dto` appears under `Features/*/Domain`, treat it as a failing architecture review unless there is an explicit documented exception.

## Remaining Semantic Rename Targets

The physical feature cleanup is complete. Rename public types that still carry old concepts:

- `IntegrationDefinitionRecord` -> `McpServerRecord`
- `IntegrationTransportType` -> `McpTransportType`
- `IIntegrationDefinitionRepository` -> `IMcpServerRepository`
- `IAgentIntegrationRepository` -> `IAgentMcpServerRepository`
- `IntegrationCredentialRecord` -> `McpCredentialRecord`
- `IIntegrationCredentialRepository` -> `IMcpCredentialRepository`
- `IntegrationConnectionRecord` -> `AgentContextSourceRecord`
- `IntegrationConnectionService` -> `AgentContextSourceService`
- `IntegrationExecutionService` -> `AgentContextExecutionService`
- `IntegrationExecuteTool` -> `AgentContextExecuteTool`
- `MemoryStoreRecord` -> `AgentMemoryStoreRecord`
- `MemoryStoreEntryRecord` -> `AgentMemoryEntryRecord`

## Migration Order

1. Freeze the desired convention in this note and `AGENTS.md`.
2. Keep the single-project target folder structure.
3. Keep feature layers flat and rely on strong names.
4. Rename stale broad `Integration*` names where they no longer describe the owning feature.
5. Simplify repository interfaces with filter records.
6. Extend architecture tests as new rules become concrete.
7. Run build/tests and enable architecture tests as required CI checks.

## Static Analysis Enforcement

Naming and layer conventions are enforced by the local Roslyn analyzer:

```text
analyzers/OffceOs.Architecture.Analyzers
```

The analyzer runs during `dotnet build` through an analyzer project reference in `src/OffceOs.csproj`.

Analyzer code is split by responsibility:

- `ArchitectureDiagnostics.cs`: diagnostic IDs, messages, and severities.
- `ArchitecturePaths.cs`: backend path and layer detection helpers.
- `NamingConvention.cs`: layer suffix vocabulary and dependency field naming rules.
- `*ArchitectureRule.cs`: one focused rule registration and implementation per analyzer concern.
- `BackendArchitectureAnalyzer.cs`: thin Roslyn entry point that composes the rule classes.

Current diagnostics:

- `EAOS001`: Domain must not define `*Dto` types.
- `EAOS002`: Feature layers must not use broad `*Types.cs` files.
- `EAOS003`: API mutations must not inject repositories; call Application services for use cases.
- `EAOS004`: Domain must not depend on outer layers or transport/infrastructure frameworks.
- `EAOS005`: Feature Api/Application layers must not define `*Dto` types.
- `EAOS006`: Feature type names must match their layer vocabulary.
- `EAOS007`: Feature type suffixes must be declared in the correct layer/path.
- `EAOS008`: Public API boundary methods must not expose Application `*Request`/`*Result` types as parameters.
- `EAOS009`: Backend source files must not declare local `using` directives; all imports live in `src/GlobalUsings.cs`.
- `EAOS010`: Private readonly dependency fields must match the injected type name, with no shortened aliases.

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

Global using enforcement:

- All backend source imports belong in `src/GlobalUsings.cs`.
- Do not add local `using` directives to any file under `src` other than `GlobalUsings.cs`.
- If a global namespace creates a collision, do not reintroduce a local import. Remove the broad global import when possible and use a fully qualified name at the call site, for example `Cronos.CronExpression` or `HotChocolate.Execution.ISourceStream<T>`.

Dependency field naming enforced by `EAOS010`:

- Private readonly injected dependency fields use the dependency type name without shortening.
- Strip only the leading interface `I`, then camel-case the remaining type name.
- Examples: `IChannelRepository _channelRepository`, `IChannelGateway _channelGateway`, `IAgentRepository _agentRepository`, `ChannelCredentialProtector _channelCredentialProtector`, `ChannelReplyContext _channelReplyContext`.
- Framework/common exceptions keep the obvious type-derived names: `ILogger<T> _logger`, `IPublisher _publisher`, `HttpClient _httpClient`, `EaosDbContext _eaosDbContext`, `IDistributedCache _distributedCache`.
- Avoid aliases such as `_repo`, `_db`, `_agents`, `_gateway`, `_protector`, `_events`, `_cache`, `_http`, `_browser`, or `_runtime`.


Keep architecture tests only for behavior that cannot be checked cheaply at compile time. Static analyzer rules should cover naming, forbidden dependencies, and direct injection conventions.

Additional checks still worth adding:

- Only top-level feature folders are `Agents`, `Channels`, `Context`, `Analytics`, `Management`.
- Each feature has explicit `Domain`, `Application`, `EventHandlers`, `Infrastructure`, and `Api` folders when it contains that layer.
- Feature layer folders do not contain nested bucket folders unless explicitly allowlisted. Current allowlist: `Agents/Application/Tools` and `Agents/Application/BrowserTools`.
- No namespace contains `Features.Agents.Integrations`.
- No namespace contains top-level `Features.Atlas`, `Features.Data`, or `Features.Mcp`.
- Domain service interfaces do not depend on Application request/result types.
- Infrastructure repository methods do not expose `*Entity` types.
- Repository implementation files contain one repository class.

Also add:

- `.editorconfig` for namespace and style consistency.
- `Directory.Build.props` for shared build settings.
- CI step for `dotnet build` and `dotnet test`.

## Acceptance Criteria

- There is one backend `.csproj`.
- The only top-level feature folders are `Agents`, `Channels`, `Context`, `Analytics`, and `Management`.
- Each feature owns its `Domain`, `Application`, `EventHandlers`, `Infrastructure`, and `Api` code locally.
- Feature layers are flat; new separation is done with strong file/type names, except `Agents/Application/Tools` and `Agents/Application/BrowserTools`.
- `Atlas`, `Data`, and `Mcp` do not exist as top-level feature folders.
- A new contributor can understand agent behavior by opening `Features/Agents`.
- A new contributor can understand channel behavior by opening `Features/Channels`.
- A new contributor can understand memory and integration indexing behavior by opening `Features/Context`.
- MCP, browser, scheduling, and runtime are discoverable as Agents subdomains.
- Domain DTOs are removed.
- Use-case request/result records live in Application.
- GraphQL/HTTP input/payload records live in Api.
- API writes and multi-step workflows go through Application services.
- Repository interfaces use filters and have no duplicate scope-specific methods.
- `dotnet build` and `dotnet test` pass.
- Architecture tests enforce the convention.
