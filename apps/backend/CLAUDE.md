# backend — C# ASP.NET Core 9 (Clean Architecture)

Central orchestrator. Owns all state, all credentials, all agent lifecycle, K8s control, LLM proxying, and the skill gateway.

## Commands

```bash
dotnet build EnterpriseAgentOs.sln
dotnet run --project src/EnterpriseAgentOs.Api   # Dev server on :5000
dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj
```

## Project structure — 4 projects, dependencies point inward

```
EnterpriseAgentOs.sln
├── src/EnterpriseAgentOs.Domain/          ← ZERO dependencies
│   ├── Models/                             EF record types (source of truth for DB schema)
│   ├── Interfaces/<Domain>/                Repository + service contracts
│   ├── DTOs/<Domain>/                      Types referenced by interfaces (AgentDto, PlanLimits, etc.)
│   └── GlobalUsings.cs
│
├── src/EnterpriseAgentOs.Application/     ← References: Domain only (+ Infrastructure pragmatically)
│   ├── Services/<Domain>/                  Business logic implementations
│   ├── DTOs/<Domain>/                      Application-layer DTOs
│   └── GlobalUsings.cs
│
├── src/EnterpriseAgentOs.Infrastructure/  ← References: Domain
│   ├── Persistence/
│   │   ├── EaosDbContext.cs                EF Core DbContext (Postgres via Npgsql)
│   │   ├── Models/                         (empty — models live in Domain)
│   │   ├── Repositories/                   EF query implementations
│   │   └── Migrations/                     EF Core migrations — never edit by hand
│   ├── Adapters/
│   │   ├── Kubernetes/                     KubernetesAgentDeployer, NullAgentDeployer
│   │   ├── LlmProviders/                   LlmProviderDispatcher, SmartRouter, AnthropicTranslator
│   │   ├── SkillRuntime/                   SkillRuntimeClient
│   │   ├── Channels/                       ChannelMessageRouter
│   │   ├── WorkOs/                         WorkOsAuthService + SSO types
│   │   ├── PostHog/                        PostHogService
│   │   └── Stripe/                         StripeWebhookService
│   ├── Security/                           Protectors (ProviderKey, SkillCredential, Channel, AgentBackendToken)
│   ├── Configuration/                      Typed config classes + ValueManager
│   └── GlobalUsings.cs
│
├── src/EnterpriseAgentOs.Api/              ← References: all three projects
│   ├── EnterpriseAgentOs.Api.csproj
│   ├── Program.cs                          Composition root — DI wiring, middleware, endpoint mapping
│   ├── GraphQlRootTypes.cs                 GraphQLQueries / GraphQLMutations / GraphQLSubscriptions
│   ├── GlobalUsings.cs
│   ├── appsettings.json
│   ├── GraphQL/
│   │   ├── Queries/<Domain>Queries.cs      [ExtendObjectType(typeof(GraphQLQueries))]
│   │   ├── Mutations/<Domain>Mutations.cs  [ExtendObjectType(typeof(GraphQLMutations))]
│   │   ├── Subscriptions/                  [ExtendObjectType(typeof(GraphQLSubscriptions))]
│   │   ├── SkillGateway/                   SkillTypeModule, AgentAuthInterceptor, Query
│   │   └── Types/                          GraphQL input/payload records
│   ├── Controllers/                        REST endpoints (OAuth, SSE, webhooks, agent-facing)
│   ├── Endpoints/                          Minimal API endpoints (AgentProxyEndpoints)
│   ├── Middleware/                          Auth, correlation ID, AgentTokenAuth
│   └── Extensions/                         DI registration helpers
│
└── EnterpriseAgentOs.Api.Tests/            Integration + unit tests
```

## Layer rules — the compiler enforces these

| Layer | Can reference | Cannot reference |
|-------|--------------|-----------------|
| **Domain** | Nothing (zero deps) | Application, Infrastructure, Api |
| **Application** | Domain, Infrastructure* | Api |
| **Infrastructure** | Domain | Application, Api |
| **Api** | Domain, Application, Infrastructure | — |

*Application references Infrastructure pragmatically (services use EaosDbContext directly). This is a known architectural violation to be resolved in a future refactor.

## Where does new code go? (Decision tree)

1. **Is it a pure data contract or interface?** → Domain (`Interfaces/<Domain>/` or `DTOs/<Domain>/`)
2. **Is it business logic / orchestration?** → Application (`Services/<Domain>/`)
3. **Does it talk to a database, external API, or OS?** → Infrastructure (`Persistence/Repositories/`, `Adapters/<SubDir>/`, `Security/`)
4. **Is it an HTTP endpoint, GraphQL resolver, or middleware?** → Api (`Controllers/`, `GraphQL/`, `Middleware/`)
5. **Is it a typed config class?** → Infrastructure (`Configuration/`)

## GraphQL conventions

Two named HotChocolate schemas:

| Endpoint | Schema | Auth | Shape |
|----------|--------|------|-------|
| `POST /api/graphql` | `agent` | Bearer agent-uuid | Dynamic — SkillTypeModule |
| `POST /api/dashboard/graphql` | `dashboard` | Cookie session | Static — per-domain [ExtendObjectType] |

### Resolver conventions

- Resolvers live at `GraphQL/Queries/`, `GraphQL/Mutations/`, `GraphQL/Subscriptions/`
- Namespace: `EnterpriseAgentOs.Api.GraphQL.Queries` / `.Mutations` / `.Subscriptions`
- Each resolver receives services via `[Service] IFooService foo` parameters
- Resolvers are thin — delegate to services, return DTOs
- Input/payload types live at `GraphQL/Types/`, namespace `EnterpriseAgentOs.Api.GraphQL.Types`
- `AddDomainTypeExtensions(typeof(Program).Assembly)` auto-registers all `[ExtendObjectType]` classes

## DI wiring

All DI registration goes through `Extensions/ServiceCollectionExtensions.cs`, not Program.cs.

| What | Register in | Lifetime |
|------|------------|----------|
| Repository | `AddRepositories()` | Scoped |
| Service | `AddApplicationServices()` | Scoped |
| Background service | `AddBackgroundServices()` | Singleton |
| Protector | `AddProtectors()` | Singleton |
| HTTP client | `AddHttpClients()` | via AddHttpClient |
| Typed config | `Program.cs` directly | Singleton |

## Database schema

Models live in `src/EnterpriseAgentOs.Domain/Models/`. Migrations in `src/EnterpriseAgentOs.Infrastructure/Persistence/Migrations/`.

```bash
# Run from apps/backend/
dotnet ef migrations add <Name> --project src/EnterpriseAgentOs.Infrastructure --startup-project .
dotnet ef database update --project src/EnterpriseAgentOs.Infrastructure --startup-project .
```

Always create a migration when changing any model. The app runs `MigrateAsync()` at startup.

## Adding a new domain

1. Add interface in `src/EnterpriseAgentOs.Domain/Interfaces/<Domain>/I<Name>Repository.cs`
2. Add repository in `src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories/<Name>Repository.cs`
3. Add service in `src/EnterpriseAgentOs.Application/Services/<Domain>/<Name>Service.cs`
4. Add GraphQL resolvers in `GraphQL/Queries/<Domain>Queries.cs` + `GraphQL/Mutations/<Domain>Mutations.cs`
5. Add input/payload types in `GraphQL/Types/<Domain>Types.cs`
6. Register in `Extensions/ServiceCollectionExtensions.cs`
7. If DB model needed: add to `Domain/Models/`, configure in `Infrastructure/Persistence/EaosDbContext.cs`, run migration

## Anti-patterns

- **Importing Infrastructure from Domain.** Domain has zero dependencies. If an interface needs a type, that type must be in Domain.
- **Business logic in controllers or resolvers.** Services own the logic.
- **New root GraphQL types.** Extend `GraphQLQueries`/`GraphQLMutations` via `[ExtendObjectType]`.
- **DI registration in Program.cs.** Use ServiceCollectionExtensions.
- **`class` for DTOs.** Use `record`.
- **ValueManager outside Program.cs.** Downstream gets typed config via DI.
- **K8s env vars for app config.** Use appsettings.json.
- **Exposing credentials outside this service.** Decrypted here, injected per-request.
- **Skipping migrations when changing Models/.**
- **Adding EF Core or HotChocolate packages to Domain.** Domain must remain dependency-free.
