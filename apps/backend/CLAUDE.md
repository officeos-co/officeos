# backend — C# ASP.NET Core 9

Central orchestrator. Owns all state, all credentials, all agent lifecycle, K8s control, LLM proxying, and the skill gateway.

Domain anatomy matches `/Users/harrokrog/Desktop/ElevenstoicBackend/` — one folder per entity under `Entities/`, queries/mutations at the domain root, records under `Types/`, repository + service at the root, no `GraphQL/` subfolder.

## Commands

```bash
dotnet build EnterpriseAgentOs.Api.csproj
dotnet run                    # Dev server on :5000
dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj
```

## Project structure

```
Program.cs                              Composition only — DI wiring, middleware, endpoint mapping, no business logic
GraphQlRootTypes.cs                     GraphQLQueries / GraphQLMutations / GraphQLSubscriptions — empty roots extended per-domain via [ExtendObjectType]
GlobalUsings.cs                         Assembly-wide usings (root types, middleware, Entities.<Domain>, Entities.<Domain>.Types)
Extensions/
  ServiceCollectionExtensions.cs        AddRepositories() / AddApplicationServices() / AddBackgroundServices() / AddProtectors() / AddHttpClients()
  GraphQLRegistrationExtensions.cs      AddDomainTypeExtensions — scans assembly for [ExtendObjectType] classes
Middleware/
  CorrelationIdMiddleware.cs            X-Correlation-Id per request
  SessionAuthMiddleware.cs              Cookie-based dashboard session; also exposes HashToken helper used by runners/tests
  DashboardAuthMiddleware.cs            HotChocolate field middleware — rejects unauthenticated GraphQL calls
Properties/                             Typed config records (KubernetesConfig, StripeConfig, …) + ValueManager
Database/
  EaosDbContext.cs                      EF Core DbContext (Postgres via Npgsql)
  Models/                               EF entity records — source of truth for the schema
  Migrations/                           EF Core migrations — never edit by hand
Entities/
  <Domain>/                             One folder per domain (see "Domain folder convention" below)
```

## Domain folder convention (Entities/<Domain>/)

Every domain follows the same flat Elevenstoic shape. Files live directly at the domain root — **no nested subdirectories** except `Types/` (input/payload records) and `services/` (only when the domain has ≥2 services).

```
Entities/<Domain>/
  <Domain>Queries.cs         namespace EnterpriseAgentOs.Api.Queries        [ExtendObjectType(typeof(GraphQLQueries))]
  <Domain>Mutations.cs       namespace EnterpriseAgentOs.Api.Mutations      [ExtendObjectType(typeof(GraphQLMutations))]
  <Domain>Subscriptions.cs   namespace EnterpriseAgentOs.Api.Subscriptions  [ExtendObjectType(typeof(GraphQLSubscriptions))] — only when the domain has live subscriptions
  <Domain>Controller.cs      REST controller (rare — GraphQL is primary)
  I<Domain>Service.cs        Service interface (when business logic warrants it)
  <Domain>Service.cs         Business logic
  I<Domain>Repository.cs     Repository interface
  <Domain>Repository.cs      EF queries
  <Domain>Dto.cs             Internal-facing records (service return types etc.) — kept at the root, namespace EnterpriseAgentOs.Api.Entities.<Domain>
  <Domain>Protector.cs       Encryption/decryption helper (if credentials involved)
  <Domain>Seeder.cs          Seed data (if needed at startup)
  Types/
    <Domain>Types.cs         Inputs/payloads, namespace EnterpriseAgentOs.Api.Entities.<Domain>.Types
  services/                  Only when ≥2 services in the domain
```

### Existing domains

| Domain | Has Controller | Has Service | Has Repository | Has Types/ | Notes |
|--------|----------------|-------------|----------------|-----------|-------|
| `Agents` | AgentBootstrapController (`[AgentTokenAuth]`, `GET /api/agents/{id}` — pod-facing boot payload: displayName, systemPrompt, provider/model, proxy, gateway, installed skills, per-tool allow/deny overrides; NO credentials, NO personality .md files — those are embedded in zeroclaw-core and written locally on first boot) | AgentService | AgentRepository | yes | Also has IAgentDeployer, KubernetesAgentDeployer, NullAgentDeployer, AgentProxyEndpoints (browser → pod HTTP/WS passthrough at `/api/agents/{id}/ws` and `/api/agents/{id}/proxy/{**path}`). `createAgent(input.toolPermissions)` persists `AgentToolPermissionRecord` rows (modes: `Allow`/`Deny` — no `Ask`); dashboard reads them back via `getAgentSkills(agentId)`. |
| `Skills` | SkillController (bundle download), AgentSkillsController (agent-pod-facing, `[AgentTokenAuth]`), InternalSkillController (CI seed) | SkillService | SkillRepository, SkillCatalogRepository, BrowserSessionRepository | yes | Dashboard catalog/install/credentials are GraphQL only. SkillRuntimeClient, SkillCredentialProtector, AgentBackendTokenProtector, AgentTokenAuthAttribute all live here. |
| `SkillGateway` | — | — | — | — | Agent-pod dynamic GraphQL gateway. Contains SkillTypeModule (ITypeModule generating per-skill action fields from runtime manifests), Query (placeholder root), AgentAuthInterceptor. Wired in `Program.cs` onto the `agent` schema only — never the dashboard schema. |
| `Providers` | — | ProviderService | ProviderRepository | yes | ProviderKeyProtector, ProviderSeeder, KnownModels |
| `Runners` | — | — | — | — | **Removed in 1.0.** Self-hosted runner infrastructure is post-1.0. All skills execute in the cloud skill-runtime. |
| `Auth` | AuthController (OAuth entry + callback only — `me` and `logout` moved to GraphQL) | — | UserRepository, SessionRepository | yes (UserPayload, UpdateProfileInput) | AuthQueries.me (includes profile fields), AuthMutations.updateProfile / logout. UserRecord carries profile fields (DisplayName, Timezone, NotificationPrefsJson). SessionAuthMiddleware now lives in `Middleware/`. |
| `Sso` | SsoController (browser OAuth), ScimController (machine-to-machine) | WorkOsAuthService | — | — | |
| `Billing` | BillingController (Stripe webhook) | UserBillingService, OrgBillingService, StripeWebhookService, CreditRecordingService | — | yes | Has `services/`, ModelCostWeights, PlanLimits. Dashboard-facing `billing` query returns unified BillingPayload (plan, usage, invoices synced from Stripe, extraUsageEnabled). `setExtraUsageEnabled(enabled)` replaces the old auto-reload knob with a simple on/off overage toggle; `toggleOverage` kept `[Obsolete]`. |
| `Organizations` | — | — | OrganizationRepository | yes (OrganizationPayload, OrgMemberPayload, InviteMemberInput, RenameOrgInput) | GraphQL-only. `org` query auto-creates a default organization per owner (single-tenant-compatible, model supports many). `inviteMember` / `removeMember` / `renameOrg` mutations gated on owner role. Backed by OrganizationRecord + OrgMemberRecord. |
| `Channels` | ChannelWebhooksController (Slack/Telegram/Discord inbound) | — | ChannelRepository | yes | Has `services/`: ChannelMessageRouter, ChannelConfigProtector |
| `Events` | SystemEventsController (SSE stream) | SystemEventService | — | — | SystemEventBroadcaster (singleton) |
| `SkillRegistry` | — | — | — | — | **Removed in 1.0.** Skill marketplace/distribution is post-1.0. |
| `AgentSkills` | — | — | AgentSkillRepository | yes | |
| `Audit` | — | — | — | — | **Merged into AgentLogs.** Secret redaction and tool-call recording now live in `AgentLogService`. The `auditLog` GraphQL query is in `AgentLogsQueries.cs`. |
| `LlmProxy` | LlmProxyController | — | — | — | LlmProviderDispatcher, SmartRouter, AnthropicTranslator, PromptCacheInjector. Injects anti-prompt-injection guardrail system message at position 0. |
| `RateLimiting` | — | IRateLimitService / RateLimitService | IRateLimitRepository / RateLimitRepository | — | DB-backed per-agent sliding window over AgentRateLimitRecord. Config: RateLimitingConfig. |
| `Gdpr` | GdprController | IGdprService / GdprService | — | — | GET /api/gdpr/export, DELETE /api/gdpr/purge. Both require SessionAuth. |
| `PostHog` | — | PostHogService | — | — | Typed GraphQL mutations per use case (`trackPageView`, `trackNavClicked`, `trackSkillInstalled`, `trackSkillConfigured`, `trackChannelConnected`, `trackAgentCreated`, `identifyUser`) — **no** generic `captureEvent` passthrough. Registered via `AddHttpClient<IPostHogService, PostHogService>()`. See `Entities/PostHog/EVENTS.md`. |
| `AgentLogs` | — | IAgentLogService / AgentLogService | IAgentLogRepository / AgentLogRepository | — | Unified log domain. Append-only timeline over `AgentLogRecord` — messages, tool calls, channel events, system events all in one format. Includes secret redaction (merged from Audit), `auditLog` query, and live subscriptions via `ITopicEventSender` topic `agent-log:{agentId}`. |
| `AgentTemplates` | — | IAgentTemplateService / AgentTemplateService | IAgentTemplateRepository / AgentTemplateRepository | — | AgentTemplateSeeder at startup. |

### Naming conventions

- **Interfaces**: `I{Name}Service`, `I{Name}Repository`
- **Implementations**: `{Name}Service`, `{Name}Repository`
- **Controllers**: `{Name}Controller` — route prefix `api/{entities}`
- **Internal DTOs**: `{Name}Dto` — records, at the domain root, namespace `EnterpriseAgentOs.Api.Entities.<Domain>`
- **GraphQL inputs/payloads**: records under `Types/`, namespace `EnterpriseAgentOs.Api.Entities.<Domain>.Types`. Suffix `Input` for resolver arguments, `Payload` for pure GraphQL return types.
- **Config classes**: `{Name}Config` — in `Properties/`, registered as singletons in `Program.cs`
- **Protectors**: `{Name}Protector` — encrypt/decrypt helpers via ASP.NET Data Protection

## GraphQL conventions

The backend exposes **two named HotChocolate schemas** from a single host:

| Endpoint | Schema name | Auth | Consumers | Shape |
|----------|-------------|------|-----------|-------|
| `POST /api/graphql` | `agent` | `AgentAuthInterceptor` (Bearer agent-uuid) | Agent pods (zeroclaw-core) | Dynamic — `SkillTypeModule` (in `Entities/SkillGateway/`) generates per-skill action fields from runtime manifests. |
| `POST /api/dashboard/graphql` | `dashboard` | `DashboardAuthMiddleware` reads `HttpContext.Items["User"]` set by `SessionAuthMiddleware` | Dashboard (`apps/dashboard-2/`) | Static — one file per domain, auto-registered via `AddDomainTypeExtensions`. |

Both are wired in `Program.cs`. Never merge them: the agent schema leaks tool names into introspection and must stay isolated from dashboard operators.

### Root types and per-domain extensions

- Root types (`GraphQLQueries` / `GraphQLMutations` / `GraphQLSubscriptions`) live at `apps/backend/GraphQlRootTypes.cs` under namespace `EnterpriseAgentOs.Api`. They contain only placeholder fields; all real fields come from `[ExtendObjectType]` per domain.
- Per-domain files live at `Entities/<Domain>/<Domain>Queries.cs` etc. Namespace is flattened by layer:
  - `<Domain>Queries.cs`        → `namespace EnterpriseAgentOs.Api.Queries`
  - `<Domain>Mutations.cs`      → `namespace EnterpriseAgentOs.Api.Mutations`
  - `<Domain>Subscriptions.cs`  → `namespace EnterpriseAgentOs.Api.Subscriptions`
- `AddDomainTypeExtensions(typeof(Program).Assembly)` scans the assembly for `[ExtendObjectType]` classes and auto-registers them. **No central list. Adding a new domain = create the file, rebuild, done.**

### Resolver conventions

- Each resolver method receives services via `[Service] IFooService foo` parameters.
- Reading the authenticated dashboard user: `DashboardAuthContextExtensions.GetUser(context)`.
- Throwing for authorization failures: `throw new GraphQLException(...)` with a `"code"` extension (e.g. `"UNAUTHENTICATED"`, `"FORBIDDEN"`, `"NOT_FOUND"`).
- Input types: records under `Entities/<Domain>/Types/`. Keep names `Create{Entity}Input`, `Update{Entity}Input`.
- Subscriptions use `ITopicEventSender` + `[Subscribe]` + `[Topic]`. Topic keys must include the resource id (e.g. `$"agent-log:{agentId}"`).

### What belongs where

- **Dashboard schema:** anything the operator UI needs. Business logic lives in services. Queries/mutations are thin adapters.
- **Agent schema:** only dynamic skill action fields from `Entities/SkillGateway/`. Do not add dashboard concerns here.
- **REST still exists for:** OAuth entry/callback (`/api/auth/google`, `/api/auth/callback/google`), SSE (`/api/system-events/stream`), Stripe webhooks, the agent-proxy passthrough, runner device-code flow, inbound channel webhooks, agent-facing agent-memory/skill-exec/llm-proxy endpoints, and internal seed endpoints.

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

## Architecture patterns

- **Controller → Service → Repository** for REST. GraphQL resolvers may call a service or go direct to the repository for simple CRUD — mirroring Elevenstoic.
- **Credentials never leave this service.** All LLM calls and skill executions are proxied here. The backend decrypts credentials per-request and injects them. Agent pods have no secrets.
- **Agent auth via UUID bearer token.** Pods authenticate with `Authorization: Bearer <agent-uuid>`. `AgentTokenAuthAttribute` (REST) and `AgentAuthInterceptor` (GraphQL, in `Entities/SkillGateway/`) validate against the Agents table.
- **Dynamic GraphQL from skill manifests.** `SkillTypeModule` implements HotChocolate `ITypeModule` and generates the entire skill schema at startup from the skill-runtime `/manifests` response. No C# code knows about specific skills.
- **Status is always live.** `AgentService.GetAsync` and `ListAsync` call `IAgentDeployer.GetStatusAsync` inline on every request.
- **Config hierarchy:** `Program.cs` reads raw config via `ValueManager`, constructs typed config classes, registers them as singletons. All other code takes typed config via constructor injection.

## Database schema

Schema lives in `Database/Models/`. Migrations are generated and applied via EF Core:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

**Always create and apply a migration when changing any model.** The app runs `db.Database.MigrateAsync()` at startup — if the schema doesn't match, it crashes.

## Adding a new domain

1. Create `Entities/<DomainName>/` folder.
2. Add `<Domain>Queries.cs` / `<Domain>Mutations.cs` at the root (namespaces `EnterpriseAgentOs.Api.Queries` / `.Mutations`). Annotate with `[ExtendObjectType(typeof(GraphQLQueries|GraphQLMutations))]`.
3. Add `Types/<Domain>Types.cs` for inputs and payloads (namespace `EnterpriseAgentOs.Api.Entities.<Domain>.Types`). Add that namespace to `GlobalUsings.cs`.
4. Add controller/service/repository files flat at the domain root. Follow the naming convention.
5. Register repository + service in `Extensions/ServiceCollectionExtensions.cs`.
6. If it needs a DB model, add to `Database/Models/` and `EaosDbContext.cs`, then run `dotnet ef migrations add`.

## Adding a new skill (backend side only)

1. Add credential UI metadata to `SkillManifests.cs`.
2. No DB migration needed (credential store is generic key-value).
3. No C# types or resolvers needed — `SkillTypeModule` picks up new skills from the runtime manifest automatically.

The TypeScript implementation lives in `packages/skills/`.

## PostHog

PostHog capture is server-side via `IPostHogService`. dashboard-2 never holds the API key; it calls one of the typed `track*` mutations (e.g. `trackPageView`, `trackAgentCreated`). There is no generic `captureEvent(name, properties)` — every event has a dedicated mutation with a typed input, so the GraphQL schema enumerates every event we fire. See `Entities/PostHog/EVENTS.md` for the full catalog.

## Anti-patterns

- **New root GraphQL types.** Never add another `[QueryType]` / `[MutationType]` class. Extend `GraphQLQueries` / `GraphQLMutations` via `[ExtendObjectType]`.
- **`Entities/<Domain>/GraphQL/` subfolders.** Flat at the domain root. The only per-domain subfolders allowed are `Types/` and `services/`.
- **`ValueManager` outside `Program.cs`.** Downstream services get typed config via DI.
- **DI registration in `Program.cs`.** Use the extension methods in `ServiceCollectionExtensions.cs`.
- **Business logic in controllers or resolvers.** Services own the logic.
- **Repositories called from REST controllers.** REST flow is always Controller → Service → Repository. GraphQL resolvers may skip the service for simple CRUD only.
- **`class` for DTOs.** Inputs and payloads are `record`s.
- **Hardcoded skill logic in C#.** Skills are TypeScript in `packages/skills/`.
- **Hardcoded GraphQL resolvers for skills.** `SkillTypeModule` generates them.
- **K8s env vars for app config.** Use `appsettings.json`.
- **Expose credentials outside this service.** They are decrypted here and injected per-request.
- **Skipping a migration when changing `Database/Models/`.**
