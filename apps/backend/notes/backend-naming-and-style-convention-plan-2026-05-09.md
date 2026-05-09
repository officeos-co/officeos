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

Remaining cleanup:

- Namespaces still mostly use the old project-first names (`EnterpriseAgentOs.Domain.*`, `EnterpriseAgentOs.Application.*`, `EnterpriseAgentOs.Infrastructure.*`) to keep the big move compiling. Database namespaces were moved to `EnterpriseAgentOs.Database`.
- Some stale type names remain around MCP/context (`Integration*`, `Atlas*`). These should be renamed in a follow-up semantic cleanup.
- Domain DTO folders still exist and need classification/migration.
- Repository interfaces still need the filter-based simplification pass.
- Existing nullable warnings remain.

## Goal

Move the backend toward a single-project, feature-first modular monolith that is easier to understand and work in.

The desired architecture is:

- One backend `.csproj`.
- Three top-level product features: `Agents`, `Analytics`, `Management`.
- Inside each feature, keep the clean architecture boundaries: `Domain`, `Application`, `Infrastructure`, `Api`.
- Centralized database code under `src/Database`, because EF context, models, and migrations are shared by the application.

This keeps the separation that matters, but makes feature work local. When working on Agents, the agent domain records, application services, repositories, GraphQL types, and endpoints should be near each other.

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
- `Analytics`
- `Management`

Do not create top-level feature folders named:

- `Atlas`
- `Data`
- `Mcp`
- `Billing`
- `Auth`
- `Browser`
- `Channels`

Those are subdomains inside one of the three product features.

## Agents Subdomains

Agents is large, so it needs subfolders. These are not top-level domains.

Recommended Agents shape:

```text
Features/Agents
├── Domain
│   ├── Core
│   ├── Runtime
│   ├── Tools
│   ├── Channels
│   ├── Mcp
│   ├── Memory
│   ├── Context
│   ├── Browser
│   └── Scheduling
├── Application
│   ├── Core
│   ├── Runtime
│   ├── Tools
│   ├── Channels
│   ├── Mcp
│   ├── Memory
│   ├── Context
│   ├── Browser
│   └── Scheduling
├── Infrastructure
│   ├── Repositories
│   ├── Adapters
│   └── Configuration
└── Api
    ├── Queries
    ├── Mutations
    ├── Subscriptions
    ├── Endpoints
    └── Types
```

Subdomain meanings:

- `Core`: agent identity, status, ownership, provider/model settings.
- `Runtime`: agent runs, sessions, turn lifecycle, session context.
- `Tools`: built-in tools, tool permissions, tool catalog contracts.
- `Channels`: Telegram/Slack/WhatsApp/Teams bindings and channel connections.
- `Mcp`: MCP servers, credentials, agent assignments, discovered tools.
- `Memory`: agent memory stores and entries. This replaces top-level `Data`.
- `Context`: indexed external context/connectors currently called Atlas. This replaces top-level `Atlas`.
- `Browser`: browser runtime/session records and browser-specific contracts.
- `Scheduling`: cron jobs and scheduled runs.

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

Use namespaces that mirror the feature-first layout:

```csharp
EnterpriseAgentOs.Features.Agents.Domain.Core
EnterpriseAgentOs.Features.Agents.Application.Runtime
EnterpriseAgentOs.Features.Agents.Infrastructure.Repositories
EnterpriseAgentOs.Features.Agents.Api.Mutations

EnterpriseAgentOs.Features.Analytics.Domain
EnterpriseAgentOs.Features.Management.Application.Billing
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

- GraphQL-only shape: move to the feature's `Api/Types`.
- Use-case request/result: move to the feature's `Application`.
- Persistence model: keep as Infrastructure `*Entity`.
- Business record: keep as Domain `*Record`.
- Cross-layer stable business contract: may stay in Domain, but avoid the suffix `Dto` if a better domain name exists.

The first cleanup target should be the current `Domain/features/Agents/Dtos` folder.

## Top-Level Feature Cleanup

Move current feature folders as follows:

- Current `Atlas` -> `Features/Agents/{Domain,Application,Infrastructure,Api}/Context`.
- Current `Data` -> `Features/Agents/{Domain,Application,Infrastructure,Api}/Memory`.
- Current `Mcp` -> `Features/Agents/{Domain,Application,Infrastructure,Api}/Mcp`.

Rename public types while moving:

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
2. Add architecture tests that describe the final shape, even if initially skipped or marked pending.
3. Create the single-project target folder structure.
4. Move `Agents` code feature-first while preserving behavior.
5. Move `Mcp`, `Data`, and `Atlas` under `Agents` subdomains and rename stale `Integration*` types.
6. Move `Analytics` code feature-first.
7. Move `Management` code feature-first.
8. Collapse shared code into `Common/{Domain,Infrastructure,Api}` only when it is genuinely shared.
9. Remove the old projects from the solution after all code has moved.
10. Run build/tests and enable architecture tests as required CI checks.

## Enforcement

Add architecture tests under:

```text
tests/EnterpriseAgentOs.Api.Tests/Architecture
```

Required tests:

- Only top-level feature folders are `Agents`, `Analytics`, `Management`.
- Each feature has explicit `Domain`, `Application`, `Infrastructure`, and `Api` folders when it contains that layer.
- No namespace contains `Features.Agents.Integrations`.
- No namespace contains top-level `Features.Atlas`, `Features.Data`, or `Features.Mcp`.
- Domain namespaces do not reference Api or Infrastructure namespaces.
- Api types are not used by Domain or Infrastructure.
- Infrastructure repository methods do not expose `*Entity` types.
- Repository implementation files contain one repository class.
- Domain `Dtos` folder is empty or explicitly allowlisted.

Also add:

- `.editorconfig` for namespace and style consistency.
- `Directory.Build.props` for shared build settings.
- CI step for `dotnet build` and `dotnet test`.

## Acceptance Criteria

- There is one backend `.csproj`.
- The only top-level feature folders are `Agents`, `Analytics`, and `Management`.
- Each feature owns its `Domain`, `Application`, `Infrastructure`, and `Api` code locally.
- `Atlas`, `Data`, and `Mcp` do not exist as top-level feature folders.
- A new contributor can understand agent behavior by opening `Features/Agents`.
- Memory, MCP, browser, channels, scheduling, and context are discoverable as Agents subdomains.
- Domain DTOs are removed or explicitly justified.
- Repository interfaces use filters and have no duplicate scope-specific methods.
- `dotnet build` and `dotnet test` pass.
- Architecture tests enforce the convention.
