# Backend Architecture Drift Post-Mortem

Date: 2026-05-09

## Summary

The backend has architectural rules in `AGENTS.md`, but those rules are not enforced by the build, tests, analyzers, or CI. The codebase drifted into a shape that is technically layered but hard to understand: four separate projects, repeated feature folders, extra top-level domains, inconsistent names, and many domain contracts that force constant context switching.

The desired direction is a single backend project organized feature-first:

```text
Features/Agents/{Domain,Application,Infrastructure,Api}
Features/Analytics/{Domain,Application,Infrastructure,Api}
Features/Management/{Domain,Application,Infrastructure,Api}
```

The clean architecture boundaries should remain, but they should live inside each feature instead of being split across separate projects.

## Impact

- New contributors cannot reliably infer the correct pattern from the tree.
- Tests no longer compile against the current implementation names.
- `AGENTS.md` describes rules that are not enforced.
- Feature ownership is unclear: MCP, memory/data, Atlas, and generic integrations are partially mixed.
- The Domain project is too hard to scan because it contains too many DTOs, records, and interfaces without a clear "core model first" path.
- Working on one feature requires jumping across Api, Application, Domain, and Infrastructure projects.

## Evidence

Command run:

```bash
dotnet build EnterpriseAgentOs.sln --no-restore
```

Result:

- `EnterpriseAgentOs.Domain`, `EnterpriseAgentOs.Infrastructure`, `EnterpriseAgentOs.Application`, and `EnterpriseAgentOs.Api` built.
- `EnterpriseAgentOs.Api.Tests` failed with 30 compile errors.

Representative failures:

- Tests import `EnterpriseAgentOs.Domain.Features.Mcp`, but MCP implementation files use `EnterpriseAgentOs.Domain.Features.Agents.Integrations`.
- Tests import `EnterpriseAgentOs.Application.Features.Atlas`, but Atlas implementation files use `EnterpriseAgentOs.Application.Features.Agents.Integrations`.
- Tests expect MCP names such as `McpServerService`, `McpServerRecord`, `IMcpServerRepository`, and `McpTransportType`.
- Implementation provides generic integration names such as `IntegrationDefinitionService`, `IntegrationDefinitionRecord`, `IIntegrationDefinitionRepository`, and `IntegrationTransportType`.

Representative drift:

- `src/EnterpriseAgentOs.Domain/features/Mcp/Interfaces/IMcpServerRepository.cs` is physically in `Mcp`, but declares namespace `EnterpriseAgentOs.Domain.Features.Agents.Integrations` and interface `IIntegrationDefinitionRepository`.
- `src/EnterpriseAgentOs.Domain/features/Atlas/Interfaces/IAtlasRepositories.cs` is physically in `Atlas`, but declares namespace `EnterpriseAgentOs.Domain.Features.Agents.Integrations`. `Atlas` should not be a top-level domain.
- `src/EnterpriseAgentOs.Domain/features/Data/Interfaces/IMemoryStoreRepository.cs` is physically in `Data`, but memory should belong under Agents.
- `src/EnterpriseAgentOs.Infrastructure/Features/Atlas/Repositories/AtlasRepositories.cs` groups many repository implementations in one file, while Agents repositories are mostly one implementation per file.
- `IMemoryStoreRepository` has duplicate owner-scoped and store-scoped entry methods, which is a symptom of repository interfaces growing by accretion.

## Root Causes

1. `AGENTS.md` is documentation only.

   There is no `.editorconfig`, `Directory.Build.props`, analyzer, architecture test, namespace check, or CI rule that converts those instructions into a failing build.

2. Refactors were partial.

   Folder moves toward `Mcp` and `Atlas` were made without completing namespace/type/API renames. That left the code in a half-renamed state.

3. Feature ownership was not strict enough.

   The product/sidebar domains are `Agents`, `Analytics`, and `Management`, but the backend allowed peer domains such as `Atlas`, `Data`, and `Mcp`.

4. Clean architecture was applied project-first instead of feature-first.

   The layer separation is valuable, but splitting layers into separate projects makes simple feature work require too many jumps.

5. Domain became a dumping ground for cross-layer models.

   `Domain/features/Agents` contains DTOs, repository interfaces, provider contracts, browser records, session records, channel records, runtime records, and permissions. The result is technically layered but cognitively expensive.

6. Repository interfaces grew by accretion.

   New use cases added new methods instead of reshaping repositories around filters and aggregate ownership.

## Corrective Direction

Move to a single-project, feature-first modular monolith.

Target:

```text
src/EnterpriseAgentOs.Backend
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
└── Common
    ├── Domain
    ├── Infrastructure
    └── Api
```

Rules:

- `Atlas`, `Data`, and `Mcp` are not top-level features.
- MCP moves under `Features/Agents/*/Mcp`.
- Memory/data moves under `Features/Agents/*/Memory`.
- Atlas/context connectors move under `Features/Agents/*/Context`.
- Domain DTOs are exceptional. GraphQL shapes go to Api. Use-case shapes go to Application.
- Repository interfaces use filter objects and a small method vocabulary: `ListAsync`, `GetByAsync`, `SaveAsync`/`UpsertAsync`, `DeleteAsync`.
- Broad bucket files such as `AtlasRecords.cs`, `IAtlasRepositories.cs`, `AtlasRepositories.cs`, and `AgentTypes.cs` should be split or renamed.

## Migration Plan

1. Update `AGENTS.md` to state the desired feature-first single-project convention.
2. Add architecture tests that describe the final shape.
3. Create the single-project target folder structure.
4. Move Agents code into `Features/Agents/{Domain,Application,Infrastructure,Api}`.
5. Move `Mcp`, `Data`, and `Atlas` under Agents subdomains and rename stale `Integration*` types.
6. Move Analytics code into `Features/Analytics/{Domain,Application,Infrastructure,Api}`.
7. Move Management code into `Features/Management/{Domain,Application,Infrastructure,Api}`.
8. Collapse genuinely shared primitives/config/helpers into `Common`.
9. Remove the old four backend projects from the solution once all code has moved.
10. Enable architecture tests as required CI checks.

## Prevention

- Treat architecture docs as executable policy.
- Do not merge folder-only refactors unless namespaces, public types, DI registrations, tests, and GraphQL names are updated in the same PR.
- Prefer deleting stale names over aliasing them.
- Keep feature boundaries literal: folder, namespace, type names, test folders, and DI registrations should all say the same thing.
- Bias toward fewer public domain contracts. Add a repository or record only when it has a clear aggregate owner and use case.

## Acceptance Criteria

- There is one backend `.csproj`.
- Only `Agents`, `Analytics`, and `Management` exist as top-level feature folders.
- Each feature owns its `Domain`, `Application`, `Infrastructure`, and `Api` code locally.
- No source file declares `namespace EnterpriseAgentOs.*.Features.Agents.Integrations`.
- No top-level `Atlas`, `Data`, or `Mcp` feature folder remains.
- MCP files live under Agents and use `Mcp*` language consistently.
- Context/connector files live under Agents and use `AgentContext*` language consistently.
- Memory files live under Agents and use `AgentMemory*` language consistently.
- Repository interfaces use filters instead of duplicate primitive-heavy methods.
- Domain DTOs are removed or explicitly justified.
- `dotnet build` and `dotnet test` pass.
- Architecture tests fail if the convention regresses.
