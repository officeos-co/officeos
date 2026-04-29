# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

```bash
# Build
dotnet build EnterpriseAgentOs.sln

# Add EF migration (from apps/backend)
dotnet ef migrations add MigrationName \
  --project src/EnterpriseAgentOs.Infrastructure \
  --startup-project src/EnterpriseAgentOs.Api
```

## Architecture

Clean Architecture with four layers, all under `src/`:

- **Domain** (`EnterpriseAgentOs.Domain`) — Entities, value objects, repository interfaces, domain events. Feature folders under `features/` (lowercase). No dependencies on other layers.
- **Application** (`EnterpriseAgentOs.Application`) — Services, MediatR handlers, business logic. Depends only on Domain.
- **Infrastructure** (`EnterpriseAgentOs.Infrastructure`) — EF Core (Postgres via `EaosDbContext`), repository implementations, external adapters (Stripe, LLM proxy, PostHog, S3/Minio, Kubernetes deployer). Depends on Domain.
- **Api** (`EnterpriseAgentOs.Api`) — Entry point (`Program.cs`). Two GraphQL schemas served by HotChocolate:
  - `"agent"` at `/api/graphql` — agent-pod skill gateway with dynamic per-skill fields via `SkillTypeModule`
  - `"dashboard"` at `/api/dashboard/graphql` — dashboard operator API with static domain queries/mutations/subscriptions
  - REST controllers and minimal API endpoints for channels, health, billing webhooks

## Key Patterns

- **12-Factor Config** — `appsettings.json` contains only non-secret defaults. Secrets are injected via environment variables at runtime (`.env` locally, K8s Secrets in prod). `.env.example` documents all variables. Config classes in `Infrastructure/Common/Configuration/` are bound in `Program.cs` via `builder.Configuration.GetSection("X").Bind(config)` — downstream code receives typed config via DI.
- **MediatR** for command/query dispatch — handlers live in `Application/Features/*/Handlers/`.
- **Feature-sliced** — each feature (Agents, Skills, Channels, Billing, Auth, etc.) has its own folder in each layer.
- **Tests** use xUnit + `WebApplicationFactory<Program>` with Testcontainers (Postgres) and WireMock. Tests override config via `.UseSetting()` or environment variables.
- Credentials are encrypted at rest via `*Protector` classes (DataProtection).

## Infrastructure

- .NET 9, C# top-level statements in Program.cs
- PostgreSQL (EF Core, code-first migrations in `Infrastructure/Common/Migrations/`)
- Redis (distributed cache)
- Kubernetes agent pod deployment (`IAgentDeployer` → `KubernetesAgentDeployer` or `DockerAgentDeployer`)
- Container image: `harkro123/eaos-backend`, port 8000
- Serilog for structured logging
