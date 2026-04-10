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
  Skills/                           Skill credentials, runtime client, GraphQL gateway
    GraphQL/                        SkillTypeModule (dynamic), Query, AgentAuthInterceptor
  Vault/                            CouchDB vault client, personality templates
  LlmProxy/                        LLM proxy endpoint (forwards to real providers)
```

## Key rules

- **`ValueManager` only in `Program.cs`.** It reads raw config. All other code receives typed config classes (e.g. `KubernetesConfig`, `CouchDbConfig`) via constructor injection. Never import `ValueManager` outside `Program.cs`.
- **Skills are external, not C#.** Skills are TypeScript modules in `packages/skills/`, executed by the skill-runtime Node.js service. The backend has no hardcoded skill logic — `SkillTypeModule` generates the GraphQL schema dynamically from runtime manifests. `SkillManifests.cs` stores only credential metadata for the dashboard. The DB stores install state + encrypted credentials.
- **Agent auth via UUID.** Agent pods authenticate with `Authorization: Bearer <agent-uuid>`. The `AgentTokenAuthFilter` and GraphQL `AgentAuthInterceptor` validate against the Agents table.
- **Status is live.** `AgentService.GetAsync` and `ListAsync` call `IAgentDeployer.GetStatusAsync` inline to sync K8s pod status into the DB.

## Adding a new skill

1. Create `packages/skills/{name}/skill.ts` using `defineSkill()` from `@eaos/skill-sdk`. Include `returns` schema on each action.
2. Create `packages/skills/{name}/package.json` with `@eaos/skill-sdk` dependency.
3. Add credential metadata to `SkillManifests.cs` (name, title, emoji, credential fields for the dashboard form).
4. Rebuild skill-runtime (`cd packages/skill-runtime && npm run build`). The `SkillTypeModule` auto-generates GraphQL types from the runtime manifest — no C# types or resolvers needed.
5. No DB migration needed.

## Anti-patterns

- Do not call `ValueManager` outside `Program.cs`.
- Do not add hardcoded C# skill implementations or GraphQL resolvers — all skill logic lives in `packages/skills/` and executes in the skill-runtime.
- Do not add K8s env vars for app config — bake it in `appsettings.json`.
- Do not add NuGet packages for minor convenience.
