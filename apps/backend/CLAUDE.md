# backend — C# ASP.NET Core 9

Central orchestrator. Owns all state, all credentials, all agent lifecycle, K8s control, LLM proxying, and the skill gateway.

## Commands

```bash
dotnet build EnterpriseAgentOs.Api.csproj
dotnet run                    # Dev server on :5000
dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj
```

## Project structure

```
Program.cs                              Composition root — DI registration via extension methods, middleware, minimal API endpoints
Extensions/
  ServiceCollectionExtensions.cs        DI wiring — AddRepositories(), AddApplicationServices(), AddBackgroundServices(), AddProtectors(), AddHttpClients()
Properties/
  ValueManager.cs                       Reads raw appsettings.json sections — used ONLY in Program.cs
  KubernetesConfig.cs                   Typed config: namespace, agent image name
  CouchDbConfig.cs                      Typed config: CouchDB URL + credentials
  SkillGatewayConfig.cs                 Typed config: skill-runtime base URL
  SkillRuntimeConfig.cs                 Typed config: skill-runtime URL
  SkillStorageConfig.cs                 Typed config: MinIO/S3 bucket for skill bundles
  GoogleOAuthConfig.cs                  Typed config: Google OAuth credentials
  WorkOsConfig.cs                       Typed config: WorkOS SSO
  StripeConfig.cs                       Typed config: Stripe billing
  FrontendConfig.cs                     Typed config: dashboard origin URL for CORS
  LiteLlmConfig.cs                      Typed config: LiteLLM proxy
  PlatformKeysConfig.cs                 Typed config: platform-owned API keys
Database/
  EaosDbContext.cs                      EF Core DbContext (Postgres via Npgsql)
  Models/                               EF entity records — this IS the database schema source of truth
  Migrations/                           EF Core migrations — never edit by hand
Entities/
  {DomainName}/                         One folder per domain (see domain structure below)
Middleware/
  CorrelationIdMiddleware.cs            Adds X-Correlation-Id to every request
```

## Domain folder convention (Entities/{DomainName}/)

Every domain follows the same flat structure. All files live directly in the domain folder — **no nested subdirectories** except `GraphQL/` (Skills only) and `services/` (only when there are multiple services).

### Standard domain anatomy

```
Entities/{DomainName}/
  {Name}Controller.cs           API endpoints — thin, delegates to service
  I{Name}Service.cs             Service interface
  {Name}Service.cs              Business logic — the only place domain logic lives
  I{Name}Repository.cs          Repository interface
  {Name}Repository.cs           Data access — EF Core queries only, no business logic
  {Name}Dto.cs                  DTOs and request/response records
  {Name}Protector.cs            Encryption/decryption helper (if credentials involved)
  {Name}Seeder.cs               Seed data (if needed at startup)
```

### Existing domains

| Domain | Has Controller | Has Service | Has Repository | Notes |
|--------|---------------|-------------|----------------|-------|
| `Agents` | AgentController | AgentService | AgentRepository | Also has IAgentDeployer, KubernetesAgentDeployer, NullAgentDeployer, AgentProxyEndpoints |
| `Skills` | SkillController, AgentSkillsController, InternalSkillController | SkillService | SkillRepository, SkillCatalogRepository, BrowserSessionRepository | Also has GraphQL/ subfolder (SkillTypeModule, Query, AgentAuthInterceptor), SkillRuntimeClient, SkillCredentialProtector |
| `Providers` | ProviderController | ProviderService | ProviderRepository | Also has ProviderKeyProtector, ProviderSeeder, KnownModels |
| `Runners` | RunnerController, RunnerApiController, DeviceAuthController | — | RunnerRepository, RunnerJobRepository | Has `services/` subfolder: RunnerJobTimeoutService, RunnerJobWaiter |
| `Auth` | AuthController | — | UserRepository, SessionRepository | Also has SessionAuthMiddleware |
| `Sso` | SsoController, ScimController | WorkOsAuthService | — | |
| `Billing` | BillingController | UserBillingService, OrgBillingService, StripeWebhookService, CreditRecordingService | — | Has `services/` subfolder, ModelCostWeights, PlanLimits |
| `Channels` | ChannelController, AgentChannelController, ChannelWebhooksController | — | ChannelRepository | Has `services/` subfolder: ChannelMessageRouter, ChannelConfigProtector |
| `Events` | SystemEventsController | SystemEventService | — | Also has SystemEventBroadcaster (singleton) |
| `CustomSkills` | CustomSkillsController | — | CustomSkillRepository | |
| `SkillRegistry` | SkillRegistryController | — | SkillRegistryRepository | |
| `AgentSkills` | AgentSkillAssignmentController | — | AgentSkillRepository | |
| `AgentMemory` | AgentMemoryController | — | — | |
| `Vault` | — | — | — | CouchDbVaultClient only |
| `Audit` | AuditController | AuditService | AuditRepository | GET /api/agents/{id}/audit-log — records every skill execution, redacts secrets in paramsJson |
| `LlmProxy` | LlmProxyController | — | — | LlmProviderDispatcher, SmartRouter, AnthropicTranslator, PromptCacheInjector |
| `RateLimiting` | — | IRateLimitService / RateLimitService | IRateLimitRepository / RateLimitRepository | DB-backed per-agent sliding window counter (AgentRateLimitRecord). Enforced in AgentSkillsController.SkillExec(). Config: RateLimitingConfig (SkillExecPerAgentPerHour, EmailPerAgentPerHour, WindowSeconds). |

### Naming conventions

- **Interfaces**: `I{Name}Service`, `I{Name}Repository`
- **Implementations**: `{Name}Service`, `{Name}Repository`
- **Controllers**: `{Name}Controller` — route prefix `api/{entities}`
- **DTOs**: `{Name}Dto` — all records, not classes
- **Config classes**: `{Name}Config` — in `Properties/`, registered as singletons in `Program.cs`
- **Protectors**: `{Name}Protector` — encrypt/decrypt helpers using ASP.NET Data Protection

## DI wiring — how to add new services

**All DI registration goes through `Extensions/ServiceCollectionExtensions.cs`**, not `Program.cs` directly.

| What you're adding | Register in | Lifetime |
|---|---|---|
| Repository | `AddRepositories()` | Scoped |
| Service | `AddApplicationServices()` | Scoped |
| Background service | `AddBackgroundServices()` | Singleton + `AddHostedService` |
| Protector / encryptor | `AddProtectors()` | Singleton |
| HTTP client | `AddHttpClients()` | via `AddHttpClient<T>()` |
| Typed config | `Program.cs` directly | Singleton — bind from `appsettings.json` section |

**`Program.cs` contains zero business logic.** It reads config via `ValueManager`, binds typed configs, calls the extension methods, and sets up middleware/endpoints. That's it.

## Database schema

Schema lives in `Database/Models/`. Migrations are generated and applied via EF Core:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

**Always create and apply a migration when changing any model.** The app runs `db.Database.MigrateAsync()` at startup — if the schema doesn't match, it crashes.

## Architecture patterns

- **Controller → Service → Repository.** Controllers are thin — they validate input, call the service, return the result. Business logic lives in services. Data access lives in repositories. Controllers never call repositories directly.
- **Credentials never leave this service.** All LLM calls and skill executions are proxied here. The backend decrypts credentials per-request and injects them. Agent pods have no secrets.
- **Agent auth via UUID bearer token.** Pods authenticate with `Authorization: Bearer <agent-uuid>`. `AgentTokenAuthAttribute` and GraphQL `AgentAuthInterceptor` validate against the Agents table.
- **Dynamic GraphQL from skill manifests.** `SkillTypeModule` implements HotChocolate `ITypeModule` and generates the entire skill schema at startup from the skill-runtime `/manifests` response. No C# code knows about specific skills.
- **Status is always live.** `AgentService.GetAsync` and `ListAsync` call `IAgentDeployer.GetStatusAsync` inline on every request — pod status is never stale.
- **Config hierarchy:** `Program.cs` reads raw config via `ValueManager`, constructs typed config classes (e.g. `KubernetesConfig`), and registers them as singletons. All other code receives typed config via constructor injection.

## Key rules

- **`ValueManager` only in `Program.cs`.** All downstream code gets typed config classes via DI.
- **DI registration only in `ServiceCollectionExtensions.cs`.** Not scattered across `Program.cs`.
- **Controller → Service → Repository.** Controllers delegate to services. Services contain business logic. Repositories contain EF queries. No shortcuts.
- **All files flat in the domain folder.** No nested `Models/`, `Services/`, `Interfaces/` subdirectories (exception: `services/` for domains with multiple services, `GraphQL/` for Skills).
- **No hardcoded skill logic in C#.** Skills are TypeScript in `packages/skills/`. The only skill metadata in C# is credential field UI in `SkillManifests.cs`.
- **Migrations on every schema change.** Changing `Database/Models/` without a migration will crash the app at startup.
- **Config baked into the image.** Use `appsettings.json`, not K8s env vars.

## Adding a new domain

1. Create `Entities/{DomainName}/` folder.
2. Add controller, interface(s), implementation(s), DTOs — follow the naming convention above.
3. Register in `ServiceCollectionExtensions.cs` — repository in `AddRepositories()`, service in `AddApplicationServices()`.
4. If it needs a DB model, add to `Database/Models/` and `EaosDbContext.cs`, then run `dotnet ef migrations add`.

## Adding a new skill (backend side only)

1. Add credential UI metadata to `SkillManifests.cs`.
2. No DB migration needed (credential store is generic key-value).
3. No C# types or resolvers needed — `SkillTypeModule` picks up new skills from the runtime manifest automatically.

The TypeScript implementation lives in `packages/skills/`. See `packages/skills/CLAUDE.md`.

## Anti-patterns

- Do not call `ValueManager` outside `Program.cs`. Downstream services get typed config classes via constructor injection.
- Do not register DI services directly in `Program.cs`. Use the extension methods in `ServiceCollectionExtensions.cs`.
- Do not put business logic in controllers. Controllers validate, call the service, and return. Logic lives in the service.
- Do not call repositories from controllers. The flow is always Controller → Service → Repository.
- Do not create nested subdirectories inside domain folders (`Models/`, `Interfaces/`, `Services/`). All files live flat.
- Do not add C# implementations for specific skills. All skill logic is TypeScript in `packages/skills/`.
- Do not add hardcoded GraphQL resolvers for skills. `SkillTypeModule` generates them dynamically.
- Do not add K8s env vars for configuration. Config belongs in `appsettings.json`.
- Do not expose credentials outside this service. They are decrypted here and injected per-request only.
- Do not skip creating a migration when changing `Database/Models/`.
