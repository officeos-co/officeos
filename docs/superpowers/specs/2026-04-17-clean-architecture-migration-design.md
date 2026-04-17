# Clean Architecture Migration — Design Spec

## Goal

Migrate the `apps/backend/` monolithic ASP.NET Core project into a 4-project clean architecture solution to enforce layer boundaries at compile time, improve readability, and prepare for enterprise scale.

## Motivation

- Code is hard to navigate — no clear answer to "where does this go?"
- Compile-time enforcement eliminates architectural drift
- Enterprise customers expect clean separation of concerns
- Scalable foundation before the codebase grows further

## Strategy

**Big bang.** All 16 domains migrated in one pass. CLAUDE.md rewritten to reflect new conventions. No backward compatibility.

---

## Project Structure

```
apps/backend/
├── EnterpriseAgentOs.sln
├── src/
│   ├── EnterpriseAgentOs.Domain/
│   │   ├── EnterpriseAgentOs.Domain.csproj   (no dependencies)
│   │   ├── Entities/
│   │   │   ├── Agents/
│   │   │   │   └── Agent.cs                   (domain entity with behavior)
│   │   │   ├── Skills/
│   │   │   ├── Providers/
│   │   │   ├── Auth/
│   │   │   ├── Organizations/
│   │   │   ├── Billing/
│   │   │   ├── Channels/
│   │   │   ├── AgentLogs/
│   │   │   ├── AgentTemplates/
│   │   │   ├── AgentSkills/
│   │   │   ├── RateLimiting/
│   │   │   └── LlmProxy/
│   │   ├── ValueObjects/
│   │   ├── Interfaces/                        (repository contracts)
│   │   ├── Services/                          (domain services — stateless business rules)
│   │   └── Errors/                            (domain exceptions)
│   │
│   ├── EnterpriseAgentOs.Application/
│   │   ├── EnterpriseAgentOs.Application.csproj  (references Domain only)
│   │   ├── UseCases/
│   │   │   ├── Agents/
│   │   │   │   ├── CreateAgentUseCase.cs
│   │   │   │   ├── DeleteAgentUseCase.cs
│   │   │   │   └── ...
│   │   │   ├── Skills/
│   │   │   ├── Providers/
│   │   │   ├── Auth/
│   │   │   ├── Organizations/
│   │   │   ├── Billing/
│   │   │   ├── Channels/
│   │   │   ├── AgentLogs/
│   │   │   ├── AgentTemplates/
│   │   │   ├── AgentSkills/
│   │   │   ├── RateLimiting/
│   │   │   └── LlmProxy/
│   │   ├── DTOs/                              (input/output records per domain)
│   │   ├── Ports/                             (external system interfaces)
│   │   │   ├── IAgentDeployer.cs
│   │   │   ├── ILlmProviderDispatcher.cs
│   │   │   ├── ICreditRecorder.cs
│   │   │   ├── IPostHogClient.cs
│   │   │   └── ISkillRuntimeClient.cs
│   │   └── Services/                          (cross-cutting application services)
│   │
│   ├── EnterpriseAgentOs.Infrastructure/
│   │   ├── EnterpriseAgentOs.Infrastructure.csproj  (references Application + Domain)
│   │   ├── Persistence/
│   │   │   ├── EaosDbContext.cs
│   │   │   ├── Models/                        (EF record types — DB schema source of truth)
│   │   │   ├── Repositories/                  (implements Domain interfaces)
│   │   │   ├── Mappings/                      (Domain entity <-> EF model mappers)
│   │   │   └── Migrations/
│   │   ├── Adapters/
│   │   │   ├── Kubernetes/                    (KubernetesAgentDeployer, NullAgentDeployer)
│   │   │   ├── Stripe/
│   │   │   ├── WorkOs/
│   │   │   ├── PostHog/
│   │   │   ├── Google/                        (OAuth)
│   │   │   └── LlmProviders/                 (dispatcher, smart router, translators)
│   │   ├── Security/                          (Protectors: provider keys, skill creds, etc.)
│   │   └── Configuration/                     (typed config classes from Properties/)
│   │
│   └── EnterpriseAgentOs.Api/
│       ├── EnterpriseAgentOs.Api.csproj       (references all projects)
│       ├── Program.cs                         (composition root — DI wiring only)
│       ├── GraphQL/
│       │   ├── RootTypes.cs
│       │   ├── Queries/                       (per-domain query resolvers)
│       │   ├── Mutations/                     (per-domain mutation resolvers)
│       │   ├── Subscriptions/
│       │   ├── Interceptors/
│       │   └── SkillGateway/                  (dynamic schema generation)
│       ├── Controllers/                       (REST: OAuth, SSE, webhooks, agent-facing, LLM proxy)
│       ├── Middleware/                         (auth, correlation ID)
│       └── Extensions/                        (DI registration helpers)
│
└── tests/
    ├── EnterpriseAgentOs.Domain.Tests/
    ├── EnterpriseAgentOs.Application.Tests/
    ├── EnterpriseAgentOs.Infrastructure.Tests/
    └── EnterpriseAgentOs.Api.Tests/
```

---

## Layer Rules

### Domain (innermost — zero dependencies)

- No NuGet packages except pure utility libs (e.g., no EF Core, no HotChocolate, no ASP.NET)
- Contains: entities with behavior, value objects, repository interfaces, domain services, domain errors
- Entities are C# classes (not records) with private setters and methods that enforce invariants
- Repository interfaces define contracts only — no implementation details leak in

### Application (references Domain only)

- Contains: use cases (one class per operation), DTOs (records), port interfaces for external systems
- Each use case is a class with a single `ExecuteAsync()` method
- Use cases orchestrate domain entities + repository calls — they don't contain business rules
- Port interfaces abstract infrastructure: `IAgentDeployer`, `ILlmProviderDispatcher`, etc.
- No EF Core, no HTTP, no GraphQL types

### Infrastructure (references Application + Domain)

- Contains: EF Core DbContext + models + migrations, repository implementations, external adapters, security (protectors), typed config classes
- EF models (records in `Persistence/Models/`) are the DB schema source of truth — unchanged from current
- Repository implementations map between EF models and domain entities
- Adapters implement port interfaces from Application

### Api (references all — outermost)

- Contains: Program.cs (DI composition root), GraphQL resolvers, REST controllers, middleware
- Resolvers/controllers are thin — inject use case, call ExecuteAsync, return DTO
- All DI registration in `Extensions/` helper methods
- Two HotChocolate schemas preserved: `agent` + `dashboard`

---

## Domain Mapping

All 16 current domains under `Entities/` map to the new structure:

| Current Domain | Domain Entity | Key Use Cases | Infrastructure Adapters |
|---|---|---|---|
| Agents | Agent | Create, Delete, Patch, RefreshStatus, GetBootstrap | KubernetesAgentDeployer |
| Skills | Skill | Install, Uninstall, UpdateCredentials, Execute | SkillRuntimeClient |
| SkillGateway | (none — infra only) | (none) | SkillTypeModule (dynamic schema) |
| Providers | Provider | Create, Update, Delete, Validate | (none — DB only) |
| Auth | User, Session | Login, Logout, GetProfile | GoogleOAuthAdapter |
| Sso | (uses Auth entities) | SsoLogin, ScimSync | WorkOsAdapter |
| Organizations | Organization, OrgMember | Create, AddMember, RemoveMember | (none — DB only) |
| Billing | Subscription, CreditBalance | Subscribe, RecordUsage, HandleWebhook | StripeAdapter |
| Channels | ChannelConnection | Connect, Disconnect, RouteMessage | (none — webhook-based) |
| Events | SystemEvent | Broadcast | (none — SSE) |
| AgentLogs | AgentLog | Append, Query, Subscribe | (none — DB only) |
| LlmProxy | (no entity — passthrough) | ProxyCompletion | LlmProviderDispatcher |
| RateLimiting | AgentRateLimit | Check, Increment | (none — DB only) |
| Gdpr | (uses existing entities) | ExportData, PurgeData | (none — DB only) |
| PostHog | (no entity) | TrackEvent | PostHogAdapter |
| AgentTemplates | AgentTemplate | Create, Seed, GetAll | (none — DB only) |
| AgentSkills | AgentSkill | Bind, Unbind | (none — DB only) |

---

## What Stays the Same

- PostgreSQL + EF Core + Npgsql (just moves to Infrastructure project)
- HotChocolate GraphQL with agent + dashboard schemas
- `[ExtendObjectType]` auto-registration pattern
- REST endpoints for OAuth, SSE, webhooks, agent-facing, LLM proxy
- Middleware chain (session auth, correlation ID, Serilog)
- CI/CD pipelines (Dockerfile updated to `dotnet build EnterpriseAgentOs.sln`)
- EF Core models as DB schema source of truth
- Migrations workflow

## What Changes

- Single .csproj becomes 4-project .sln
- `Entities/<Domain>/` flat structure becomes layered structure across projects
- Services split into domain services vs. use cases vs. infrastructure adapters
- Static helpers (`KnownModels`, `PlanLimits`, `ModelCostWeights`) become domain services or value objects
- `Properties/` config classes move to Infrastructure/Configuration
- Protectors move to Infrastructure/Security
- Business logic extracted from controllers into use cases
- CLAUDE.md rewritten for clean architecture conventions

---

## Dockerfile Impact

Current Dockerfile builds a single project. Updated to:

```dockerfile
COPY src/EnterpriseAgentOs.Domain/ src/EnterpriseAgentOs.Domain/
COPY src/EnterpriseAgentOs.Application/ src/EnterpriseAgentOs.Application/
COPY src/EnterpriseAgentOs.Infrastructure/ src/EnterpriseAgentOs.Infrastructure/
COPY src/EnterpriseAgentOs.Api/ src/EnterpriseAgentOs.Api/
COPY EnterpriseAgentOs.sln .
RUN dotnet publish src/EnterpriseAgentOs.Api -c Release -o /app
```

---

## CLAUDE.md Update

`apps/backend/CLAUDE.md` will be fully rewritten to document:
- 4-project structure and dependency rules
- Where each type of code goes (decision tree)
- Naming conventions per layer
- Migration workflow (still EF Core, from Infrastructure project)
- DI registration patterns
- GraphQL resolver patterns (thin, delegate to use cases)
