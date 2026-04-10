# v2-backend — C# ASP.NET Core 9

The central orchestrator. Manages agent lifecycle, proxies LLM calls, serves the skill gateway, provisions vaults, and talks to Kubernetes.

## Commands

```bash
dotnet build EnterpriseAgentOs.Api.csproj
dotnet run
```

## Project structure

```
Program.cs                          Composition root — DI, middleware, endpoints
Properties/
  ValueManager.cs                   Config reader (appsettings.json sections)
  KubernetesConfig.cs               Typed config: K8s namespace, image
  CouchDbConfig.cs                  Typed config: CouchDB URL/creds
  SkillGatewayConfig.cs             Typed config: skill gateway URL
Database/
  EaosDbContext.cs                   EF Core context (Postgres via Npgsql)
  Models/                           DB entity records
Entities/
  Agents/                           Agent CRUD, K8s deployer, status sync
  Providers/                        Provider registry, API key encryption, KnownModels
  Skills/                           Skill manifests, credentials, GraphQL gateway
    Implementations/                NotionSkill, GithubSkill, GoogleSkill
    GraphQL/                        HotChocolate query types + auth interceptor
    Docs/                           Skill .md documentation files
  Vault/                            CouchDB vault client, personality templates
  LlmProxy/                        LLM proxy endpoint (forwards to real providers)
```

## Key rules

- **`ValueManager` only in `Program.cs`.** It reads raw config. All other code receives typed config classes (e.g. `KubernetesConfig`, `CouchDbConfig`) via constructor injection. Never import `ValueManager` outside `Program.cs`.
- **Skills are code, not DB rows.** `SkillManifests.cs` is the static registry. The DB stores only install state + encrypted credentials.
- **Return typed objects.** Skill methods return concrete classes (e.g. `NotionSearchResult`), not `Task<object>`. HotChocolate needs types for schema generation.
- **Agent auth via UUID.** Agent pods authenticate with `Authorization: Bearer <agent-uuid>`. The `AgentTokenAuthFilter` and GraphQL `AgentAuthInterceptor` validate against the Agents table.
- **Status is live.** `AgentService.GetAsync` and `ListAsync` call `IAgentDeployer.GetStatusAsync` inline to sync K8s pod status into the DB.

## Adding a new skill

1. Add typed return classes in `Entities/Skills/GraphQL/Types/`.
2. Add implementation class in `Entities/Skills/Implementations/` — takes `HttpClient`, returns typed objects.
3. Add manifest in `SkillManifests.cs` — name, title, description, emoji, credential fields.
4. Add GraphQL resolver in `Entities/Skills/GraphQL/` — thin wrapper around the implementation.
5. Add `.md` doc in `Entities/Skills/Docs/`.
6. Register `HttpClient` in `Program.cs` and add type extension to `AddGraphQLServer()`.
7. No DB migration needed.

## Anti-patterns

- Do not call `ValueManager` outside `Program.cs`.
- Do not return anonymous objects from skill methods — use typed classes.
- Do not add K8s env vars for app config — bake it in `appsettings.json`.
- Do not add NuGet packages for minor convenience.
