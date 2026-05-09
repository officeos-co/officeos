# Backend Naming And Style Convention Plan

Date: 2026-05-09

## Implementation Status

Initial refactor pass completed on 2026-05-09:

- The solution now contains one backend project, `src/EnterpriseAgentOs.Api.csproj`, plus the test project.
- Api/Application/Domain/Infrastructure source files were moved into the single backend project under `Features/<Feature>/<Layer>`.
- Top-level `Atlas`, `Data`, and `Mcp` feature folders were folded under `Features/Agents` as `Context`, `Memory`, and `Mcp`.
- Shared code moved under `Common/{Application,Domain,Infrastructure}` and domain events moved under `Events`.
- EF code was centralized under `Database`: `Database/Models`, `Database/Migrations`, and `Database/EaosDbContext.cs`.
- The old `src/EnterpriseAgentOs.Api` wrapper directory was removed; backend code now lives directly under `src`.
- `AGENTS.md` was rewritten to describe the feature-first single-project convention.
- The solution builds successfully with `dotnet build EnterpriseAgentOs.sln`.

Second layout pass completed on 2026-05-09:

- Feature layer folders were flattened. Files now live directly in `Features/<Feature>/<Layer>`.
- Bucket folders such as `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, `Endpoints`, and subdomain wrappers were removed from feature layers.
- The convention is now strong file/type naming over deep folder nesting.

Third layout pass completed on 2026-05-09:

- `Channels` was split out from `Agents` as its own top-level feature.
- Agent tool code received the only nested-folder exception: `Agents/Application/Tools` and `Agents/Application/BrowserTools`.

Fourth layout pass completed on 2026-05-09:

- MediatR notification handlers were moved out of Application into `Features/<Feature>/EventHandlers`.
- Handler namespaces now use `EnterpriseAgentOs.EventHandlers.Features.<Feature>`.

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
- Use-case request/result records moved to Application, for example `CreateAgentRequest`, `AgentResult`, `ProviderResult`, analytics result shapes, and GDPR export shapes.
- GraphQL/HTTP input and payload records moved to Api where they are transport-only.
- Application service contracts that describe use cases moved out of Domain into Application.
- Multi-step API workflows were moved behind Application services: agent dashboard provisioning, cron jobs, sessions, resources, memory store writes, auth profile/logout, and organization overview.
- API methods may still call Domain repositories directly for simple owner-scoped reads, but writes and orchestration now belong in Application services.
- `ChannelRepository.ListConnectionsAsync` now accepts `ChannelConnectionFilter`, removing in-memory owner filtering from the API.
- The backend builds successfully with `dotnet build src/EnterpriseAgentOs.Api.csproj --no-restore`.

Remaining cleanup:

- Namespaces still mostly use the old project-first names (`EnterpriseAgentOs.Domain.*`, `EnterpriseAgentOs.Application.*`, `EnterpriseAgentOs.Infrastructure.*`) to keep the big move compiling. Database namespaces were moved to `EnterpriseAgentOs.Database`.
- Some stale broad type names remain around MCP/context (`Integration*`, `*Types`). These should be tightened in a follow-up semantic cleanup.
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
└── EnterpriseAgentOs.Api.csproj
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
- Request/result records for use cases.
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
EnterpriseAgentOs.Features.Agents.Domain
EnterpriseAgentOs.Features.Agents.Application
EnterpriseAgentOs.Features.Agents.Infrastructure
EnterpriseAgentOs.Features.Agents.Api

EnterpriseAgentOs.Features.Analytics.Domain
EnterpriseAgentOs.Features.Management.Application
```

Avoid namespaces like:

```csharp
EnterpriseAgentOs.Domain.Features.Agents.Integrations
EnterpriseAgentOs.Application.Features.Atlas
EnterpriseAgentOs.Infrastructure.Features.Data
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

- `*Request`: use-case input.
- `*Result`: use-case output.
- `*Service`: use-case orchestration.
- `*Contracts`: tight file grouping for application service interfaces plus their request/result records when those types are only useful together.
- `*Policy`: application decision rules.

EventHandlers:

- `*Handler`: MediatR event handler.

Api:

- `*Input`: GraphQL/HTTP input.
- `*Payload`: GraphQL/HTTP output.
- `*GqlDto`: tolerated only as an existing GraphQL projection name; prefer `*Payload` for new code.
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

Avoid:

- `Dto` in Domain. Use `*Record`, `*Filter`, value-object names, or a clearer business contract name.
- Generic `Integration*` names unless the type truly covers more than MCP and context connectors.
- Broad files named `Types.cs`, `Records.cs`, or `Repositories.cs`.
- Duplicate layer files like `AgentTypes.cs` in multiple places.
- Bucket folders named `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, or subdomain wrappers inside feature layers.

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
4. Rename stale broad `Integration*` and `*Types` names where they no longer describe the owning feature.
5. Simplify repository interfaces with filter records.
6. Add architecture tests that enforce the final shape.
7. Run build/tests and enable architecture tests as required CI checks.

## Enforcement

Add architecture tests under:

```text
tests/EnterpriseAgentOs.Api.Tests/Architecture
```

Required tests:

- Only top-level feature folders are `Agents`, `Channels`, `Context`, `Analytics`, `Management`.
- Each feature has explicit `Domain`, `Application`, `EventHandlers`, `Infrastructure`, and `Api` folders when it contains that layer.
- Feature layer folders do not contain nested bucket folders unless explicitly allowlisted. Current allowlist: `Agents/Application/Tools` and `Agents/Application/BrowserTools`.
- No namespace contains `Features.Agents.Integrations`.
- No namespace contains top-level `Features.Atlas`, `Features.Data`, or `Features.Mcp`.
- Domain namespaces do not reference Api or Infrastructure namespaces.
- Api types are not used by Domain or Infrastructure.
- Domain does not contain files named `*Dto.cs` or types ending in `Dto`.
- Domain service interfaces do not depend on Application request/result types.
- API mutation methods do not inject repositories except for simple read validation; writes go through Application services.
- Infrastructure repository methods do not expose `*Entity` types.
- Repository implementation files contain one repository class.
- Domain `*Dto` type names are empty.

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
