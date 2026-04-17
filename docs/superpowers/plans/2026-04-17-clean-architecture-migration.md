# Clean Architecture Migration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split `apps/backend/` from a single .csproj into a 4-project clean architecture solution with compile-time layer enforcement.

**Architecture:** Domain (zero deps) → Application (refs Domain) → Infrastructure (refs Application+Domain) → Api (refs all). Existing service/repository pattern preserved. Services move to Application, repositories to Infrastructure, interfaces to Domain.

**Tech Stack:** .NET 9, EF Core 9, HotChocolate 14.3, PostgreSQL, Stripe, KubernetesClient, AWS S3

**Approach:** Keep existing service/repository pattern but enforce boundaries via separate projects. No use-case refactor in this pass — that's a follow-up. Focus is on project structure and compile-time enforcement.

---

### Task 1: Create Solution and Project Scaffolding

**Files:**
- Create: `apps/backend/EnterpriseAgentOs.sln`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj`
- Create: `apps/backend/src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj`
- Create: `apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj`
- Modify: `apps/backend/EnterpriseAgentOs.Api.csproj` (update references)

- [ ] **Step 1: Create solution file**

```bash
cd apps/backend
dotnet new sln -n EnterpriseAgentOs
```

- [ ] **Step 2: Create Domain project (zero NuGet dependencies)**

```bash
dotnet new classlib -n EnterpriseAgentOs.Domain -o src/EnterpriseAgentOs.Domain -f net9.0
rm src/EnterpriseAgentOs.Domain/Class1.cs
```

Edit `src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>EnterpriseAgentOs.Domain</RootNamespace>
  </PropertyGroup>
</Project>
```

- [ ] **Step 3: Create Application project (references Domain only)**

```bash
dotnet new classlib -n EnterpriseAgentOs.Application -o src/EnterpriseAgentOs.Application -f net9.0
rm src/EnterpriseAgentOs.Application/Class1.cs
```

Edit `src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>EnterpriseAgentOs.Application</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\EnterpriseAgentOs.Domain\EnterpriseAgentOs.Domain.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create Infrastructure project (references Application + Domain)**

```bash
dotnet new classlib -n EnterpriseAgentOs.Infrastructure -o src/EnterpriseAgentOs.Infrastructure -f net9.0
rm src/EnterpriseAgentOs.Infrastructure/Class1.cs
```

Edit `src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>EnterpriseAgentOs.Infrastructure</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\EnterpriseAgentOs.Application\EnterpriseAgentOs.Application.csproj" />
    <ProjectReference Include="..\EnterpriseAgentOs.Domain\EnterpriseAgentOs.Domain.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.1" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
    <PackageReference Include="Stripe.net" Version="51.0.0" />
    <PackageReference Include="KubernetesClient" Version="15.0.1" />
    <PackageReference Include="AWSSDK.S3" Version="3.7.405.5" />
    <PackageReference Include="Microsoft.AspNetCore.DataProtection" Version="9.0.4" />
  </ItemGroup>
</Project>
```

- [ ] **Step 5: Update Api .csproj — add project references, remove packages that moved**

Move `EnterpriseAgentOs.Api.csproj` stays at `apps/backend/EnterpriseAgentOs.Api.csproj`. Update it:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>EnterpriseAgentOs.Api</RootNamespace>
    <AssemblyName>EnterpriseAgentOs.Api</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="src\EnterpriseAgentOs.Domain\EnterpriseAgentOs.Domain.csproj" />
    <ProjectReference Include="src\EnterpriseAgentOs.Application\EnterpriseAgentOs.Application.csproj" />
    <ProjectReference Include="src\EnterpriseAgentOs.Infrastructure\EnterpriseAgentOs.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="HotChocolate.AspNetCore" Version="14.3.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.1" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="EnterpriseAgentOs.Api.Tests" />
  </ItemGroup>
</Project>
```

- [ ] **Step 6: Add all projects to the solution**

```bash
cd apps/backend
dotnet sln add EnterpriseAgentOs.Api.csproj
dotnet sln add src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj
dotnet sln add src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj
dotnet sln add src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj
```

- [ ] **Step 7: Verify solution builds (will fail — that's expected, just checking structure)**

```bash
dotnet build EnterpriseAgentOs.sln 2>&1 | head -20
```

- [ ] **Step 8: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: scaffold 4-project clean architecture solution"
```

---

### Task 2: Create Domain Layer — Interfaces and Contracts

Move all repository interfaces and service interfaces to the Domain project. These define the contracts that inner layers expose.

**Files:**
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Agents/IAgentDeployer.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Skills/ISkillRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Skills/ISkillService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Skills/ISkillCatalogRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Skills/IBrowserSessionRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Providers/IProviderRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Providers/IProviderService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Auth/IUserRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Auth/ISessionRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Organizations/IOrganizationRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Channels/IChannelRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/AgentSkills/IAgentSkillRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/AgentLogs/IAgentLogRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/AgentLogs/IAgentLogService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/AgentTemplates/IAgentTemplateRepository.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/AgentTemplates/IAgentTemplateService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Billing/IUserBillingService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Billing/IOrgBillingService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Billing/IStripeWebhookService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Billing/ICreditRecordingService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Gdpr/IGdprService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/Sso/IWorkOsAuthService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/PostHog/IPostHogService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/RateLimiting/IRateLimitService.cs`
- Create: `apps/backend/src/EnterpriseAgentOs.Domain/Interfaces/RateLimiting/IRateLimitRepository.cs`

**Process for each interface file:**

- [ ] **Step 1: Read the current interface file from `Entities/<Domain>/I<Name>.cs`**

Read each interface file to get its exact content, method signatures, and return types.

- [ ] **Step 2: Copy each interface to the Domain project under `Interfaces/<Domain>/`**

For each interface:
1. Copy the file to `src/EnterpriseAgentOs.Domain/Interfaces/<Domain>/`
2. Change the namespace to `EnterpriseAgentOs.Domain.Interfaces.<Domain>`
3. Remove any infrastructure-dependent types from the interface. If the interface returns EF models (e.g., `AgentRecord`), keep them for now — we'll address the model dependency in Task 4.
4. Delete the original file from `Entities/<Domain>/`

Example — `IAgentRepository.cs`:
```csharp
namespace EnterpriseAgentOs.Domain.Interfaces.Agents;

public interface IAgentRepository
{
    // Exact methods copied from current file, preserving signatures
}
```

- [ ] **Step 3: Add `using EnterpriseAgentOs.Domain.Interfaces.<Domain>;` to files that reference these interfaces**

This will be done in bulk during Task 5 (namespace fixup). For now, just move the files.

- [ ] **Step 4: Verify Domain project has no NuGet dependencies**

```bash
cat apps/backend/src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj
# Should have zero PackageReference entries
```

- [ ] **Step 5: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: move all interfaces to Domain layer"
```

---

### Task 3: Move Database Layer to Infrastructure

Move EaosDbContext, Models/, and Migrations/ to the Infrastructure project.

**Files:**
- Move: `apps/backend/Database/` → `apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/`

- [ ] **Step 1: Move Database directory**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence
cp -r apps/backend/Database/EaosDbContext.cs apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/
cp -r apps/backend/Database/Models apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/
cp -r apps/backend/Database/Migrations apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/
```

- [ ] **Step 2: Update namespace in EaosDbContext.cs**

Change `namespace EnterpriseAgentOs.Api.Database` → `namespace EnterpriseAgentOs.Infrastructure.Persistence`

- [ ] **Step 3: Update namespace in all Model files**

Change `namespace EnterpriseAgentOs.Api.Database.Models` → `namespace EnterpriseAgentOs.Infrastructure.Persistence.Models`

- [ ] **Step 4: Update all migration files namespaces**

Change `namespace EnterpriseAgentOs.Api.Database.Migrations` → `namespace EnterpriseAgentOs.Infrastructure.Persistence.Migrations`

Also update the `[DbContext(typeof(...))]` and `[Migration("...")]` attributes — the DbContext type reference must point to the new namespace.

- [ ] **Step 5: Delete old Database/ directory**

```bash
rm -rf apps/backend/Database/
```

- [ ] **Step 6: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: move database layer to Infrastructure project"
```

---

### Task 4: Move Config, Protectors, and Adapters to Infrastructure

**Files:**
- Move: `apps/backend/Properties/*.cs` → `apps/backend/src/EnterpriseAgentOs.Infrastructure/Configuration/`
- Move: `Entities/Providers/ProviderKeyProtector.cs` → `Infrastructure/Security/`
- Move: `Entities/Skills/SkillCredentialProtector.cs` → `Infrastructure/Security/`
- Move: `Entities/Skills/AgentBackendTokenProtector.cs` → `Infrastructure/Security/`
- Move: `Entities/Channels/ChannelConfigProtector.cs` → `Infrastructure/Security/`
- Move: `Entities/Agents/KubernetesAgentDeployer.cs` → `Infrastructure/Adapters/Kubernetes/`
- Move: `Entities/Agents/NullAgentDeployer.cs` → `Infrastructure/Adapters/Kubernetes/`
- Move: `Entities/Skills/SkillRuntimeClient.cs` → `Infrastructure/Adapters/SkillRuntime/`
- Move: `Entities/LlmProxy/LlmProviderDispatcher.cs` → `Infrastructure/Adapters/LlmProviders/`
- Move: `Entities/LlmProxy/SmartRouter.cs` → `Infrastructure/Adapters/LlmProviders/`
- Move: `Entities/LlmProxy/AnthropicTranslator.cs` → `Infrastructure/Adapters/LlmProviders/`
- Move: `Entities/LlmProxy/PromptCacheInjector.cs` → `Infrastructure/Adapters/LlmProviders/`
- Move: `Entities/Channels/services/ChannelMessageRouter.cs` → `Infrastructure/Adapters/Channels/`
- Move: `Entities/Sso/WorkOsAuthService.cs` → `Infrastructure/Adapters/WorkOs/`
- Move: `Entities/PostHog/PostHogService.cs` → `Infrastructure/Adapters/PostHog/`
- Move: `Entities/Billing/services/StripeWebhookService.cs` → `Infrastructure/Adapters/Stripe/`

- [ ] **Step 1: Move config classes**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Infrastructure/Configuration
cp apps/backend/Properties/*.cs apps/backend/src/EnterpriseAgentOs.Infrastructure/Configuration/
```

Update all namespaces: `EnterpriseAgentOs.Api.Properties` → `EnterpriseAgentOs.Infrastructure.Configuration`

- [ ] **Step 2: Move protectors**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Infrastructure/Security
```

Copy each protector file, update namespace to `EnterpriseAgentOs.Infrastructure.Security`.

- [ ] **Step 3: Move adapters**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Infrastructure/Adapters/{Kubernetes,SkillRuntime,LlmProviders,Channels,WorkOs,PostHog,Stripe}
```

Copy each adapter file to its directory, update namespace to `EnterpriseAgentOs.Infrastructure.Adapters.<SubDir>`.

- [ ] **Step 4: Move repository implementations**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/Repositories
```

Move all `*Repository.cs` files (not interfaces) from each domain:
- `AgentRepository.cs`, `ProviderRepository.cs`, `SkillRepository.cs`, `SkillCatalogRepository.cs`, `BrowserSessionRepository.cs`, `AgentSkillRepository.cs`, `UserRepository.cs`, `SessionRepository.cs`, `ChannelRepository.cs`, `AgentTemplateRepository.cs`, `AgentLogRepository.cs`, `OrganizationRepository.cs`

Update namespace to `EnterpriseAgentOs.Infrastructure.Persistence.Repositories`.
Each repository implements the interface from `EnterpriseAgentOs.Domain.Interfaces.<Domain>`.

- [ ] **Step 5: Delete moved files from their original locations**

Remove all original files that were moved (protectors, adapters, repositories, config classes).

- [ ] **Step 6: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: move infrastructure concerns (config, protectors, adapters, repositories)"
```

---

### Task 5: Move Services to Application Layer

**Files:**
- Move: `Entities/Agents/AgentService.cs` → `Application/Services/Agents/`
- Move: `Entities/Providers/ProviderService.cs` → `Application/Services/Providers/`
- Move: `Entities/Skills/SkillService.cs` → `Application/Services/Skills/`
- Move: `Entities/Billing/services/UserBillingService.cs` → `Application/Services/Billing/`
- Move: `Entities/Billing/services/OrgBillingService.cs` → `Application/Services/Billing/`
- Move: `Entities/Billing/services/CreditRecordingService.cs` → `Application/Services/Billing/`
- Move: `Entities/Gdpr/GdprService.cs` → `Application/Services/Gdpr/`
- Move: `Entities/AgentTemplates/AgentTemplateService.cs` → `Application/Services/AgentTemplates/`
- Move: `Entities/AgentLogs/AgentLogService.cs` → `Application/Services/AgentLogs/`
- Move: `Entities/Billing/ModelCostWeights.cs` → `Application/Services/Billing/`
- Move: `Entities/Billing/PlanLimits.cs` → `Application/Services/Billing/`
- Move: `Entities/Providers/KnownModels.cs` → `Application/Services/Providers/`
- Move: All `*Dto.cs` files → `Application/DTOs/<Domain>/`
- Move: All `Types/*Types.cs` files → `Application/DTOs/<Domain>/`
- Move: Seeder files → `Application/Services/<Domain>/`

- [ ] **Step 1: Create Application directory structure**

```bash
mkdir -p apps/backend/src/EnterpriseAgentOs.Application/Services/{Agents,Providers,Skills,Billing,Gdpr,AgentTemplates,AgentLogs,Sso,Events}
mkdir -p apps/backend/src/EnterpriseAgentOs.Application/DTOs/{Agents,Providers,Skills,Billing,Channels,Auth,Organizations,AgentSkills,AgentLogs,AgentTemplates,Gdpr,PostHog,Sso}
```

- [ ] **Step 2: Move service implementations**

Copy each service file, update namespace to `EnterpriseAgentOs.Application.Services.<Domain>`.
Add `using EnterpriseAgentOs.Domain.Interfaces.<Domain>;` for interface references.
Add `using EnterpriseAgentOs.Infrastructure.Persistence;` for DbContext references (Application references Domain, but services that need Infrastructure types will need the Infrastructure reference — however, per clean architecture, Application should NOT reference Infrastructure).

**Important:** Services that directly use `EaosDbContext` need to be refactored to use repository interfaces instead. If a service bypasses the repository and queries the DbContext directly, extract that query into the repository interface + implementation.

- [ ] **Step 3: Move DTOs and Type files**

Copy all `*Dto.cs` and `Types/*Types.cs` files to `Application/DTOs/<Domain>/`.
Update namespaces to `EnterpriseAgentOs.Application.DTOs.<Domain>`.

- [ ] **Step 4: Move static helpers (KnownModels, PlanLimits, ModelCostWeights)**

These are domain knowledge, but they reference no infrastructure. Move to Application:
- `KnownModels.cs` → `Application/Services/Providers/KnownModels.cs`
- `PlanLimits.cs` → `Application/Services/Billing/PlanLimits.cs`  
- `ModelCostWeights.cs` → `Application/Services/Billing/ModelCostWeights.cs`

- [ ] **Step 5: Move seeder files**

- `ProviderSeeder.cs` → `Application/Services/Providers/ProviderSeeder.cs`
- `SkillSeeder.cs` → `Application/Services/Skills/SkillSeeder.cs`
- `AgentTemplateSeeder.cs` → `Application/Services/AgentTemplates/AgentTemplateSeeder.cs`

- [ ] **Step 6: Delete moved files from original locations**

- [ ] **Step 7: Ensure Application.csproj does NOT reference Infrastructure**

The Application project must only reference Domain. If services need DbContext, they must go through repository interfaces. Check and fix any direct EF Core usage in service files.

- [ ] **Step 8: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: move services, DTOs, and seeders to Application layer"
```

---

### Task 6: Refactor Api Project — Keep Only Presentation Concerns

What remains in the Api project: Program.cs, controllers, GraphQL resolvers, middleware, extensions, GraphQlRootTypes.cs.

**Files:**
- Keep: `apps/backend/Program.cs`
- Keep: `apps/backend/GraphQlRootTypes.cs`
- Keep: `apps/backend/Extensions/`
- Keep: `apps/backend/Middleware/`
- Move remaining files from `Entities/` to appropriate Api subdirectories

- [ ] **Step 1: Create Api presentation structure**

```bash
mkdir -p apps/backend/GraphQL/{Queries,Mutations,Subscriptions}
mkdir -p apps/backend/Controllers
```

- [ ] **Step 2: Move GraphQL resolvers**

Move all `*Queries.cs`, `*Mutations.cs`, `*Subscriptions.cs` from `Entities/<Domain>/` to:
- `GraphQL/Queries/<Domain>Queries.cs`
- `GraphQL/Mutations/<Domain>Mutations.cs`
- `GraphQL/Subscriptions/<Domain>Subscriptions.cs`

Keep their namespaces as `EnterpriseAgentOs.Api.Queries`, `EnterpriseAgentOs.Api.Mutations`, `EnterpriseAgentOs.Api.Subscriptions` (these are used by HotChocolate `[ExtendObjectType]` scanning).

Update `using` statements to reference new service/repository namespaces.

- [ ] **Step 3: Move controllers**

Move all `*Controller.cs` from `Entities/<Domain>/` to `Controllers/`:
- `AgentBootstrapController.cs`
- `AuthController.cs`
- `BillingController.cs`
- `ChannelWebhooksController.cs`
- `GdprController.cs`
- `LlmProxyController.cs`
- `SkillController.cs`
- `AgentSkillsController.cs`
- `InternalSkillController.cs`
- `SsoController.cs`
- `ScimController.cs`
- `SystemEventsController.cs` (from Events domain)
- `AgentLogController.cs`

Update namespaces to `EnterpriseAgentOs.Api.Controllers`.

- [ ] **Step 4: Move SkillGateway files**

```bash
mkdir -p apps/backend/GraphQL/SkillGateway
```

Move `Entities/SkillGateway/` contents:
- `SkillTypeModule.cs` → `GraphQL/SkillGateway/`
- `Query.cs` → `GraphQL/SkillGateway/`
- `AgentAuthInterceptor.cs` → `GraphQL/SkillGateway/`

Namespace: `EnterpriseAgentOs.Api.GraphQL.SkillGateway`

- [ ] **Step 5: Move AgentProxyEndpoints.cs**

Move `Entities/Agents/AgentProxyEndpoints.cs` → `apps/backend/Endpoints/AgentProxyEndpoints.cs`

Namespace: `EnterpriseAgentOs.Api.Endpoints`

- [ ] **Step 6: Move AgentTokenAuthAttribute.cs**

Move `Entities/Skills/AgentTokenAuthAttribute.cs` → `apps/backend/Middleware/AgentTokenAuthAttribute.cs`

- [ ] **Step 7: Move Billing subscription models**

`UserSubscription` and `OrgSubscription` are referenced in `EaosDbContext` but defined under `Entities/Billing/`. Move them to Infrastructure/Persistence/Models/.

- [ ] **Step 8: Delete the now-empty Entities/ directory**

```bash
rm -rf apps/backend/Entities/
```

- [ ] **Step 9: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: move presentation concerns to Api layer, delete Entities/"
```

---

### Task 7: Fix All Namespace References and Build

This is the critical task — update every `using` statement, every fully-qualified type reference in Program.cs, and every DI registration.

- [ ] **Step 1: Update GlobalUsings.cs**

Replace the current GlobalUsings.cs with layer-appropriate usings. The Api project's GlobalUsings.cs should reference all layers:

```csharp
// Domain
global using EnterpriseAgentOs.Domain.Interfaces.Agents;
global using EnterpriseAgentOs.Domain.Interfaces.Skills;
global using EnterpriseAgentOs.Domain.Interfaces.Providers;
global using EnterpriseAgentOs.Domain.Interfaces.Auth;
global using EnterpriseAgentOs.Domain.Interfaces.Organizations;
global using EnterpriseAgentOs.Domain.Interfaces.Channels;
global using EnterpriseAgentOs.Domain.Interfaces.AgentSkills;
global using EnterpriseAgentOs.Domain.Interfaces.AgentLogs;
global using EnterpriseAgentOs.Domain.Interfaces.AgentTemplates;
global using EnterpriseAgentOs.Domain.Interfaces.Billing;
global using EnterpriseAgentOs.Domain.Interfaces.Gdpr;
global using EnterpriseAgentOs.Domain.Interfaces.Sso;
global using EnterpriseAgentOs.Domain.Interfaces.PostHog;
global using EnterpriseAgentOs.Domain.Interfaces.RateLimiting;

// Application
global using EnterpriseAgentOs.Application.Services.Agents;
global using EnterpriseAgentOs.Application.Services.Providers;
global using EnterpriseAgentOs.Application.Services.Skills;
global using EnterpriseAgentOs.Application.Services.Billing;
global using EnterpriseAgentOs.Application.DTOs.Agents;
global using EnterpriseAgentOs.Application.DTOs.Skills;
global using EnterpriseAgentOs.Application.DTOs.Providers;
// ... etc for all DTOs

// Infrastructure
global using EnterpriseAgentOs.Infrastructure.Persistence;
global using EnterpriseAgentOs.Infrastructure.Persistence.Models;
global using EnterpriseAgentOs.Infrastructure.Configuration;
global using EnterpriseAgentOs.Infrastructure.Security;
global using EnterpriseAgentOs.Infrastructure.Adapters.Kubernetes;
global using EnterpriseAgentOs.Infrastructure.Adapters.LlmProviders;
global using EnterpriseAgentOs.Infrastructure.Adapters.SkillRuntime;
global using EnterpriseAgentOs.Infrastructure.Adapters.Channels;
global using EnterpriseAgentOs.Infrastructure.Adapters.WorkOs;
global using EnterpriseAgentOs.Infrastructure.Adapters.PostHog;
global using EnterpriseAgentOs.Infrastructure.Adapters.Stripe;
global using EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

// Framework (keep existing)
global using HotChocolate.AspNetCore;
global using HotChocolate.Resolvers;
global using HotChocolate.Subscriptions;
global using HotChocolate.Types;
global using HotChocolate;
global using Microsoft.AspNetCore.DataProtection;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.EntityFrameworkCore;
global using Serilog;
global using Serilog.Events;
global using System.Text.Json;
global using System.Text;
global using k8s;
global using k8s.Models;
```

- [ ] **Step 2: Update Program.cs — replace all fully-qualified type references**

Replace all `EnterpriseAgentOs.Api.Properties.*` → `EnterpriseAgentOs.Infrastructure.Configuration.*`
Replace all `EnterpriseAgentOs.Api.Database.*` → `EnterpriseAgentOs.Infrastructure.Persistence.*`
Replace all `EnterpriseAgentOs.Api.Entities.Agents.*` → appropriate new namespace
Replace all `EnterpriseAgentOs.Api.Entities.Skills.*` → appropriate new namespace
Replace all `EnterpriseAgentOs.Api.Entities.SkillGateway.*` → `EnterpriseAgentOs.Api.GraphQL.SkillGateway.*`
Replace all `EnterpriseAgentOs.Api.Extensions.*` → stays the same
Replace all `EnterpriseAgentOs.Api.Middleware.*` → stays the same

- [ ] **Step 3: Update ServiceCollectionExtensions.cs — all DI registrations**

Replace all `EnterpriseAgentOs.Api.Entities.<Domain>.*` references with the new namespaces:
- Interfaces → `EnterpriseAgentOs.Domain.Interfaces.<Domain>`
- Implementations (repos) → `EnterpriseAgentOs.Infrastructure.Persistence.Repositories`
- Implementations (services) → `EnterpriseAgentOs.Application.Services.<Domain>`
- Implementations (protectors) → `EnterpriseAgentOs.Infrastructure.Security`
- Implementations (adapters) → `EnterpriseAgentOs.Infrastructure.Adapters.<SubDir>`

- [ ] **Step 4: Update GraphQLRegistrationExtensions.cs**

No changes needed — it scans `typeof(Program).Assembly`, which is the Api assembly. But now resolvers are in `GraphQL/Queries/` etc., which is still the same assembly. Should still work.

**However:** if resolvers reference services via `[Service]` parameter injection, the type names must match the new interface namespaces. Verify each resolver file has the correct `using` statements.

- [ ] **Step 5: Fix all resolver files — update using statements**

For each `*Queries.cs`, `*Mutations.cs`, `*Subscriptions.cs` in `GraphQL/`:
- Add `using` for new service namespaces
- Add `using` for new DTO namespaces
- Remove old `using EnterpriseAgentOs.Api.Entities.<Domain>` references

- [ ] **Step 6: Fix all controller files — update using statements**

Same process for each controller in `Controllers/`.

- [ ] **Step 7: Fix Infrastructure project files — update references**

Repository implementations need:
- `using EnterpriseAgentOs.Domain.Interfaces.<Domain>;` (for interface)
- `using EnterpriseAgentOs.Infrastructure.Persistence;` (for DbContext)
- `using EnterpriseAgentOs.Infrastructure.Persistence.Models;` (for EF models)

Adapter implementations need:
- `using EnterpriseAgentOs.Domain.Interfaces.<Domain>;` (for interface, if implementing one)
- `using EnterpriseAgentOs.Infrastructure.Configuration;` (for config classes)

- [ ] **Step 8: Fix Application project files — update references**

Service implementations need:
- `using EnterpriseAgentOs.Domain.Interfaces.<Domain>;` (for repository/service interfaces)
- `using EnterpriseAgentOs.Application.DTOs.<Domain>;` (for DTOs)

**Critical:** Services must NOT reference `EnterpriseAgentOs.Infrastructure.*`. If they use `EaosDbContext` directly, those queries must be extracted to repository interfaces in Domain and implemented in Infrastructure.

- [ ] **Step 9: Build and fix errors iteratively**

```bash
cd apps/backend
dotnet build EnterpriseAgentOs.sln 2>&1 | head -50
```

Fix errors one by one. Common issues:
- Missing `using` statements
- Fully-qualified names that still use old namespace
- Services directly using `EaosDbContext` — needs repository extraction
- Circular references between projects

- [ ] **Step 10: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: fix all namespace references across 4 projects"
```

---

### Task 8: Update Dockerfile and CI

**Files:**
- Modify: `apps/backend/Dockerfile`

- [ ] **Step 1: Update Dockerfile**

```dockerfile
# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY apps/backend/EnterpriseAgentOs.sln ./
COPY apps/backend/EnterpriseAgentOs.Api.csproj ./
COPY apps/backend/src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj src/EnterpriseAgentOs.Domain/
COPY apps/backend/src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj src/EnterpriseAgentOs.Application/
COPY apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj src/EnterpriseAgentOs.Infrastructure/
RUN dotnet restore EnterpriseAgentOs.sln
COPY apps/backend/. ./
RUN dotnet publish EnterpriseAgentOs.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8000
ENV ASPNETCORE_ENVIRONMENT=Production
COPY --from=build /app/publish ./
EXPOSE 8000
ENTRYPOINT ["dotnet", "EnterpriseAgentOs.Api.dll"]
```

- [ ] **Step 2: Verify Docker build locally (optional)**

```bash
docker build -f apps/backend/Dockerfile -t eaos-backend-test .
```

- [ ] **Step 3: Update test project references if needed**

Check `EnterpriseAgentOs.Api.Tests.csproj` — it may need references to the new projects.

- [ ] **Step 4: Commit**

```bash
git add -A apps/backend/
git commit -m "feat: update Dockerfile for multi-project build"
```

---

### Task 9: Rewrite CLAUDE.md for Clean Architecture

**Files:**
- Modify: `apps/backend/CLAUDE.md`

- [ ] **Step 1: Rewrite CLAUDE.md**

Replace the entire file with documentation reflecting the new 4-project structure:

Key sections:
- Project structure diagram (4 projects, what goes where)
- Layer rules (what each project can reference)
- Domain conventions (interfaces, no NuGet deps)
- Application conventions (services, DTOs)
- Infrastructure conventions (repos, adapters, config, protectors)
- Api conventions (resolvers, controllers, middleware)
- Adding a new domain (step-by-step for new structure)
- DI wiring (updated ServiceCollectionExtensions)
- Build commands (`dotnet build EnterpriseAgentOs.sln`)
- Migration commands (from Infrastructure project context)
- Anti-patterns (importing Infrastructure in Application, etc.)

- [ ] **Step 2: Update root CLAUDE.md build commands if needed**

The root CLAUDE.md references `dotnet build EnterpriseAgentOs.Api.csproj`. Update to `dotnet build EnterpriseAgentOs.sln`.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "docs: rewrite CLAUDE.md for clean architecture conventions"
```

---

### Task 10: Final Verification

- [ ] **Step 1: Clean build**

```bash
cd apps/backend
dotnet clean EnterpriseAgentOs.sln
dotnet build EnterpriseAgentOs.sln
```

Expected: 0 errors, 0 warnings (or only pre-existing warnings).

- [ ] **Step 2: Run tests**

```bash
dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj
```

Expected: All existing tests pass.

- [ ] **Step 3: Verify layer boundaries**

Check that Domain.csproj has no PackageReference entries.
Check that Application.csproj only references Domain.
Check that Infrastructure.csproj references Application + Domain + NuGet packages.
Check that Api.csproj references all three projects.

```bash
grep -c "PackageReference" apps/backend/src/EnterpriseAgentOs.Domain/EnterpriseAgentOs.Domain.csproj
# Expected: 0
grep "ProjectReference" apps/backend/src/EnterpriseAgentOs.Application/EnterpriseAgentOs.Application.csproj
# Expected: only Domain
```

- [ ] **Step 4: Verify no files remain in Entities/**

```bash
ls apps/backend/Entities/ 2>&1
# Expected: No such file or directory
```

- [ ] **Step 5: Final commit if any fixes were needed**

```bash
git add -A apps/backend/
git commit -m "fix: final clean architecture verification fixes"
```
