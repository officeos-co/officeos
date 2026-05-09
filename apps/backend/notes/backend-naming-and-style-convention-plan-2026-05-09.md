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

Remaining cleanup:

- Namespaces still mostly use the old project-first names (`EnterpriseAgentOs.Domain.*`, `EnterpriseAgentOs.Application.*`, `EnterpriseAgentOs.Infrastructure.*`) to keep the big move compiling. Database namespaces were moved to `EnterpriseAgentOs.Database`.
- Some stale type names remain around MCP/context (`Integration*`, `Atlas*`). These should be renamed in a follow-up semantic cleanup.
- Domain `*Dto` type names still need classification/migration.
- Repository interfaces still need the filter-based simplification pass.
- Existing nullable warnings remain.

## Goal

Move the backend toward a single-project, feature-first modular monolith that is easier to understand and work in.

The desired architecture is:

- One backend `.csproj`.
- Four top-level product features: `Agents`, `Channels`, `Analytics`, `Management`.
- Inside each feature, keep the clean architecture boundaries: `Domain`, `Application`, `Infrastructure`, `Api`.
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
│   │   ├── Infrastructure
│   │   └── Api
│   ├── Channels
│   │   ├── Domain
│   │   ├── Application
│   │   ├── Infrastructure
│   │   └── Api
│   ├── Analytics
│   │   ├── Domain
│   │   ├── Application
│   │   ├── Infrastructure
│   │   └── Api
│   └── Management
│       ├── Domain
│       ├── Application
│       ├── Infrastructure
│       └── Api
├── Common
│   ├── Domain
│   ├── Infrastructure
│   └── Api
├── Database
│   ├── Models
│   ├── Migrations
│   └── EaosDbContext.cs
├── Events
├── Program.cs
└── EnterpriseAgentOs.Api.csproj
```

The old four-project split has been collapsed. New backend code should live directly under this `src` tree.

## Feature Rule

Only these top-level features are allowed:

- `Agents`
- `Channels`
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
├── Infrastructure
└── Api
```

Subdomain meanings:

- `Core`: agent identity, status, ownership, provider/model settings.
- `Runtime`: agent runs, sessions, turn lifecycle, session context.
- `Tools`: built-in tools, tool permissions, tool catalog contracts.
- `Mcp`: MCP servers, credentials, agent assignments, discovered tools.
- `Memory`: agent memory stores and entries. This replaces top-level `Data`.
- `Context`: indexed external context/connectors currently called Atlas. This replaces top-level `Atlas`.
- `Browser`: browser runtime/session records and browser-specific contracts.
- `Scheduling`: cron jobs and scheduled runs.

Channels owns Telegram/Slack/WhatsApp/Teams-style connections, credentials, agent bindings, inbound routing, sidecar delivery, and channel GraphQL/endpoints.

## Layer Boundaries Inside A Feature

Even in one project, the boundaries should stay explicit.

Domain owns:

- Business records.
- Value objects.
- Repository interfaces.
- Domain service interfaces.
- Domain events.
- Filters used by repositories.
- Validation and invariant helpers that are true business rules.

Application owns:

- Use-case services.
- MediatR handlers.
- Background jobs.
- Request/result records for use cases.
- Policies that orchestrate domain behavior.
- Tool execution orchestration.

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
- `I*Service`: domain/application-facing service contract only when a real abstraction is needed.
- `*Event`: lifecycle fact published through MediatR.
- Value objects keep direct names, for example `Email`, `ToolKey`, `CronExpression`.

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

Avoid:

- `Dto` in Domain unless it is a stable domain contract and not tied to GraphQL, HTTP, EF, or one use case.
- Generic `Integration*` names unless the type truly covers more than MCP and context connectors.
- Broad files named `Types.cs`, `Records.cs`, or `Repositories.cs`.
- Duplicate layer files like `AgentTypes.cs` in multiple places.
- Bucket folders named `Records`, `Interfaces`, `Dtos`, `Services`, `Repositories`, `Adapters`, `Queries`, `Mutations`, `Types`, or subdomain wrappers inside feature layers.

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

Most current Domain DTOs should move.

Rules:

- GraphQL-only shape: move to the feature's `Api` layer.
- Use-case request/result: move to the feature's `Application`.
- Persistence model: keep as Infrastructure `*Entity`.
- Business record: keep as Domain `*Record`.
- Cross-layer stable business contract: may stay in Domain, but avoid the suffix `Dto` if a better domain name exists.

The first cleanup target is now the current `*Dto` type names that remain in flat Domain layers.

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
4. Rename stale `Atlas*`, `Integration*`, and broad `*Types`/`*Dto` names.
5. Simplify repository interfaces with filter records.
6. Add architecture tests that enforce the final shape.
7. Run build/tests and enable architecture tests as required CI checks.

## Enforcement

Add architecture tests under:

```text
tests/EnterpriseAgentOs.Api.Tests/Architecture
```

Required tests:

- Only top-level feature folders are `Agents`, `Channels`, `Analytics`, `Management`.
- Each feature has explicit `Domain`, `Application`, `Infrastructure`, and `Api` folders when it contains that layer.
- Feature layer folders do not contain nested bucket folders unless explicitly allowlisted. Current allowlist: `Agents/Application/Tools` and `Agents/Application/BrowserTools`.
- No namespace contains `Features.Agents.Integrations`.
- No namespace contains top-level `Features.Atlas`, `Features.Data`, or `Features.Mcp`.
- Domain namespaces do not reference Api or Infrastructure namespaces.
- Api types are not used by Domain or Infrastructure.
- Infrastructure repository methods do not expose `*Entity` types.
- Repository implementation files contain one repository class.
- Domain `*Dto` type names are empty or explicitly allowlisted.

Also add:

- `.editorconfig` for namespace and style consistency.
- `Directory.Build.props` for shared build settings.
- CI step for `dotnet build` and `dotnet test`.

## Acceptance Criteria

- There is one backend `.csproj`.
- The only top-level feature folders are `Agents`, `Channels`, `Analytics`, and `Management`.
- Each feature owns its `Domain`, `Application`, `Infrastructure`, and `Api` code locally.
- Feature layers are flat; new separation is done with strong file/type names, except `Agents/Application/Tools` and `Agents/Application/BrowserTools`.
- `Atlas`, `Data`, and `Mcp` do not exist as top-level feature folders.
- A new contributor can understand agent behavior by opening `Features/Agents`.
- A new contributor can understand channel behavior by opening `Features/Channels`.
- Memory, MCP, browser, scheduling, and context are discoverable as Agents subdomains.
- Domain DTOs are removed or explicitly justified.
- Repository interfaces use filters and have no duplicate scope-specific methods.
- `dotnet build` and `dotnet test` pass.
- Architecture tests enforce the convention.
