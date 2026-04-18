# Icons, Likes, Comments & Install Flow — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove emoji from the entire stack, surface skill logos and channel icons from the backend, wire dashboard to real GraphQL data (delete mock data), and connect likes/comments/install flows.

**Architecture:** Backend already has all mutations for likes, comments, install/uninstall. The gap is: (1) `SkillDashboardDto` exposes `Emoji` not `Logo`, (2) `ChannelTypeGqlDto` has no logo, (3) dashboard uses mock data instead of real queries, (4) `SourceCodeUrl` is stored per-skill instead of convention-generated. We fix the backend schema, remove emoji everywhere, add logos, then rewire the dashboard.

**Tech Stack:** C# ASP.NET Core 9 + EF Core (backend), TypeScript + Next.js 16 + Apollo (dashboard), TypeScript (skill-sdk, skill-runtime), Bun (dashboard tests)

---

### Task 1: Remove emoji from Skill SDK types

**Files:**
- Modify: `packages/skill-sdk/src/types.ts:68-72`

- [ ] **Step 1: Write the test — verify SkillDefinition no longer accepts emoji**

There's no test file for the SDK types. Since SkillDefinition is a TypeScript interface (compile-time only), the test is the type check. Skip to implementation.

- [ ] **Step 2: Remove the emoji field from SkillDefinition**

In `packages/skill-sdk/src/types.ts`, remove lines 68-72 (the `emoji` field and its JSDoc):

```typescript
  /**
   * Emoji icon — optional fallback kept for backwards compatibility.
   * @deprecated Prefer `logo` (inline SVG). Will be removed in a future release.
   */
  emoji?: string;
```

- [ ] **Step 3: Type-check the SDK**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skill-sdk && npx tsc --noEmit`
Expected: PASS (no other file in skill-sdk references emoji)

- [ ] **Step 4: Commit**

```bash
git add packages/skill-sdk/src/types.ts
git commit -m "refactor: remove deprecated emoji field from SkillDefinition"
```

---

### Task 2: Bulk-remove emoji from all 62 skill files via regex

**Files:**
- Modify: all `packages/skills/*/skill.ts` files (62 files)

- [ ] **Step 1: Preview the regex match**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs && grep -rn 'emoji:' packages/skills/*/skill.ts | head -5`
Expected: Lines like `packages/skills/linear/skill.ts:17:  emoji: "📊",`

- [ ] **Step 2: Remove all emoji lines with sed**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs && sed -i '' '/^[[:space:]]*emoji:[[:space:]]*"[^"]*",\{0,1\}$/d' packages/skills/*/skill.ts`

This deletes any line matching `  emoji: "...",` across all skill files in one pass.

- [ ] **Step 3: Verify no emoji references remain**

Run: `grep -rn 'emoji' packages/skills/*/skill.ts`
Expected: No output (zero matches)

- [ ] **Step 4: Type-check skills build**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skill-runtime && npm run build`
Expected: Build succeeds — all skills compile without the emoji field

- [ ] **Step 5: Commit**

```bash
git add packages/skills/
git commit -m "refactor: remove emoji from all 62 skill definitions"
```

---

### Task 3: Remove emoji from skill-runtime manifest serialization

**Files:**
- Modify: `packages/skill-runtime/src/manifest.ts:16,115`

- [ ] **Step 1: Remove emoji from SkillManifest interface**

In `packages/skill-runtime/src/manifest.ts`, remove line 16:
```typescript
  emoji?: string;
```

- [ ] **Step 2: Remove emoji from extractManifest return object**

In `packages/skill-runtime/src/manifest.ts`, in the `return` block of `extractManifest()` (around line 111-121), remove:
```typescript
    emoji: def.emoji,
```

- [ ] **Step 3: Build skill-runtime**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skill-runtime && npm run build`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add packages/skill-runtime/src/manifest.ts
git commit -m "refactor: remove emoji from skill-runtime manifest serialization"
```

---

### Task 4: Backend — Remove Emoji, add Logo to SkillDashboardDto, auto-generate SourceCodeUrl, add Logo to ChannelTypeGqlDto

**Files:**
- Modify: `apps/backend/src/EnterpriseAgentOs.Domain/Models/SkillRecord.cs:15-16`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Skills/SkillDto.cs:20,70-81`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/Services/Skills/SkillService.cs:76,257`
- Modify: `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs:1-15,95-108`
- Modify: `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Mutations/SkillMutations.cs:173-187`
- Modify: `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/ChannelTypes.cs:20-23`
- Modify: `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Queries/ChannelQueries.cs:27-34`
- Modify: `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Channels/ChannelDto.cs:58-104`
- Modify: `apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs` (SkillRecord config)
- Create: `apps/backend/EnterpriseAgentOs.Api.Tests/SkillDashboardTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `apps/backend/EnterpriseAgentOs.Api.Tests/SkillDashboardTests.cs`:

```csharp
namespace EnterpriseAgentOs.Api.Tests;

public sealed class SkillDashboardTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public SkillDashboardTests(Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    private void SeedManifests()
    {
        var manifest = """
        [
          {
            "name": "github",
            "title": "GitHub",
            "logo": "<svg viewBox=\"0 0 24 24\"><path d=\"M12 0C5.37 0 0 5.37 0 12c0 5.3 3.44 9.8 8.2 11.39.6.11.82-.26.82-.58v-2.17C5.67 21.28 4.97 19 4.97 19c-.55-1.39-1.34-1.76-1.34-1.76-1.09-.75.08-.73.08-.73 1.21.08 1.85 1.24 1.85 1.24 1.07 1.84 2.81 1.31 3.5 1 .1-.78.42-1.31.76-1.61-2.66-.3-5.47-1.33-5.47-5.93 0-1.31.47-2.38 1.24-3.22-.13-.3-.54-1.52.12-3.17 0 0 1.01-.32 3.3 1.23a11.5 11.5 0 0 1 6.02 0c2.28-1.55 3.29-1.23 3.29-1.23.66 1.65.25 2.87.12 3.17.77.84 1.24 1.91 1.24 3.22 0 4.61-2.81 5.63-5.48 5.92.43.37.81 1.1.81 2.22v3.29c0 .32.22.7.82.58A12.01 12.01 0 0 0 24 12c0-6.63-5.37-12-12-12z\"/></svg>",
            "description": "GitHub integration",
            "doc": "GitHub skill docs",
            "actions": {
              "create_issue": {
                "description": "Create a GitHub issue",
                "params": { "type": "object", "properties": { "repo": { "type": "string" } } }
              }
            },
            "credentialFields": [
              { "key": "token", "label": "Token", "kind": "password", "required": true }
            ]
          }
        ]
        """;

        _factory.SkillRuntimeMock.Reset();
        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(manifest));
    }

    [Fact]
    public async Task Skills_Query_Returns_Logo_Not_Emoji()
    {
        SeedManifests();

        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client,
            "{ skills { name logo sourceCodeUrl } }");

        var skills = data.GetProperty("skills");
        Assert.True(skills.GetArrayLength() > 0);
        var skill = skills.EnumerateArray().First(s => s.GetProperty("name").GetString() == "github");
        var logo = skill.GetProperty("logo").GetString();
        Assert.NotNull(logo);
        Assert.StartsWith("<svg", logo);
        var sourceCodeUrl = skill.GetProperty("sourceCodeUrl").GetString();
        Assert.Equal("https://github.com/officeos/integrations/tree/main/packages/github", sourceCodeUrl);
    }

    [Fact]
    public async Task Skills_Query_Has_No_Emoji_Field()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");

        // Querying 'emoji' should fail — the field no longer exists
        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client,
            "{ skills { name emoji } }");
        Assert.True(raw.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Skills_Query_Returns_Installed_Status()
    {
        SeedManifests();
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await Infrastructure.TestHelpers.InstallSkillAsync(client, "github");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client,
            "{ skills { name installed } }");

        var skill = data.GetProperty("skills").EnumerateArray()
            .First(s => s.GetProperty("name").GetString() == "github");
        Assert.True(skill.GetProperty("installed").GetBoolean());
    }

    [Fact]
    public async Task ChannelTypes_Query_Returns_Logo()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client,
            "{ channelTypes { type displayName logo } }");

        var types = data.GetProperty("channelTypes");
        Assert.True(types.GetArrayLength() >= 7);
        var slack = types.EnumerateArray().First(t => t.GetProperty("type").GetString() == "slack");
        var logo = slack.GetProperty("logo").GetString();
        Assert.NotNull(logo);
        Assert.StartsWith("<svg", logo);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj --filter "FullyQualifiedName~SkillDashboardTests" --no-restore`
Expected: FAIL — `logo`, `installed`, `sourceCodeUrl` fields don't exist on the GraphQL schema yet, emoji still exists

- [ ] **Step 3: Remove Emoji from SkillRecord model**

In `apps/backend/src/EnterpriseAgentOs.Domain/Models/SkillRecord.cs`, remove:
```csharp
    [MaxLength(8)]
    public string Emoji { get; set; } = string.Empty;
```

- [ ] **Step 4: Remove Emoji from EaosDbContext SkillRecord configuration**

In `apps/backend/src/EnterpriseAgentOs.Infrastructure/Persistence/EaosDbContext.cs`, in the SkillRecord entity configuration block, remove:
```csharp
    e.Property(s => s.Emoji).HasMaxLength(8);
```

Also remove the separate SourceCodeUrl configuration block:
```csharp
modelBuilder.Entity<EnterpriseAgentOs.Domain.Models.SkillRecord>(e =>
{
    e.Property(s => s.SourceCodeUrl).HasMaxLength(512);
});
```

- [ ] **Step 5: Remove SourceCodeUrl from SkillRecord model**

In `apps/backend/src/EnterpriseAgentOs.Domain/Models/SkillRecord.cs`, remove:
```csharp
    /// <summary>Public URL (typically GitHub) to the skill's source code — surfaced on the skill detail page.</summary>
    [MaxLength(512)]
    public string? SourceCodeUrl { get; set; }
```

- [ ] **Step 6: Remove Emoji from RuntimeManifest, add Logo**

In `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Skills/SkillDto.cs`, in class `RuntimeManifest`:

Replace:
```csharp
    public required string Emoji { get; set; }
```
With:
```csharp
    public string? Logo { get; set; }
```

- [ ] **Step 7: Remove Emoji from SkillDto**

In `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Skills/SkillDto.cs`, in record `SkillDto`:

Replace:
```csharp
    string Emoji,
```
With nothing — just remove the line. Adjust the record to not include Emoji.

- [ ] **Step 8: Update SkillService.InstallAsync — remove Emoji assignment**

In `apps/backend/src/EnterpriseAgentOs.Application/Services/Skills/SkillService.cs`, in `InstallAsync()` method, remove line 76:
```csharp
                Emoji = liveManifest.Emoji,
```

- [ ] **Step 9: Update SkillService.ToDto — remove Emoji**

In `apps/backend/src/EnterpriseAgentOs.Application/Services/Skills/SkillService.cs`, in the `ToDto` method (line 253-263), remove `Emoji: skill.Emoji,` from the SkillDto constructor call.

- [ ] **Step 10: Replace SkillDashboardDto — remove Emoji/SourceCodeUrl, add Logo/Installed/SourceCodeUrl(computed)**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs`, replace the SkillDashboardDto record:

```csharp
[GraphQLName("Skill")]
public sealed record SkillDashboardDto(
    Guid Id,
    string Name,
    string Title,
    string Description,
    string? Doc,
    string Status,
    string Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);
```

- [ ] **Step 11: Add Logo, Installed, SourceCodeUrl resolvers**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs`, add these methods to `SkillDashboardResolvers`:

```csharp
    public async Task<string?> GetLogo(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillCatalogRepository catalog,
        CancellationToken ct)
    {
        var record = await catalog.GetByNameAsync(skill.Name, ct);
        if (record is null || string.IsNullOrWhiteSpace(record.ManifestJson)) return null;
        try
        {
            var manifest = JsonSerializer.Deserialize<RuntimeManifest>(record.ManifestJson, ManifestJsonOptions);
            return manifest?.Logo;
        }
        catch { return null; }
    }

    public async Task<bool> GetInstalled(
        [Parent] SkillDashboardDto skill,
        [Service] ISkillRepository repo,
        CancellationToken ct)
    {
        var row = await repo.GetByNameAsync(skill.Name, ct);
        return row?.Enabled == true;
    }

    public string GetSourceCodeUrl([Parent] SkillDashboardDto skill)
        => $"https://github.com/officeos/integrations/tree/main/packages/{skill.Name}";
```

Note: `ISkillRepository` is the install-state repository (`SkillCredentialRecord`). You'll need to add a `using` or ensure it's available. Check existing imports.

- [ ] **Step 12: Update SkillDashboardMapper.ToDto — remove Emoji/SourceCodeUrl**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/SkillTypes.cs`, update the mapper:

```csharp
    public static SkillDashboardDto ToDto(SkillRecord r) =>
        new(r.Id, r.Name, r.Title, r.Description, r.Doc,
            r.Status, r.Version, r.CreatedAt, r.UpdatedAt);
```

- [ ] **Step 13: Remove SetSkillSourceCodeUrl mutation**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Mutations/SkillMutations.cs`, delete the entire `SetSkillSourceCodeUrl` method (lines 173-187).

- [ ] **Step 14: Add Logo to ChannelTypeDefinition**

In `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Channels/ChannelDto.cs`, update the record:

```csharp
public sealed record ChannelTypeDefinition(
    string Type,
    string DisplayName,
    string Description,
    string Logo,
    IReadOnlyList<ChannelConfigField> ConfigFields);
```

- [ ] **Step 15: Add Logo SVGs to all 7 channel type definitions**

In `apps/backend/src/EnterpriseAgentOs.Application/DTOs/Channels/ChannelDto.cs`, update each `ChannelTypeDefinition` in `ChannelTypes.All` to include a `Logo` parameter. Fetch SVGs from thesvg.org for each channel. Example for Slack:

```csharp
new ChannelTypeDefinition("slack", "Slack", "Connect a Slack workspace",
    "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M5.04 15.16a2.53 2.53 0 0 1-2.52 2.53A2.53 2.53 0 0 1 0 15.16a2.53 2.53 0 0 1 2.52-2.52h2.52v2.52zm1.27 0a2.53 2.53 0 0 1 2.52-2.52 2.53 2.53 0 0 1 2.52 2.52v6.32A2.53 2.53 0 0 1 8.83 24a2.53 2.53 0 0 1-2.52-2.52v-6.32zM8.83 5.04a2.53 2.53 0 0 1-2.52-2.52A2.53 2.53 0 0 1 8.83 0a2.53 2.53 0 0 1 2.52 2.52v2.52H8.83zm0 1.27a2.53 2.53 0 0 1 2.52 2.52 2.53 2.53 0 0 1-2.52 2.52H2.52A2.53 2.53 0 0 1 0 8.83a2.53 2.53 0 0 1 2.52-2.52h6.31zm10.13 2.52a2.53 2.53 0 0 1 2.52-2.52A2.53 2.53 0 0 1 24 8.83a2.53 2.53 0 0 1-2.52 2.52h-2.52V8.83zm-1.27 0a2.53 2.53 0 0 1-2.52 2.52 2.53 2.53 0 0 1-2.52-2.52V2.52A2.53 2.53 0 0 1 15.17 0a2.53 2.53 0 0 1 2.52 2.52v6.31zm-2.52 10.13a2.53 2.53 0 0 1 2.52 2.52A2.53 2.53 0 0 1 15.17 24a2.53 2.53 0 0 1-2.52-2.52v-2.52h2.52zm0-1.27a2.53 2.53 0 0 1-2.52-2.52 2.53 2.53 0 0 1 2.52-2.52h6.31A2.53 2.53 0 0 1 24 15.17a2.53 2.53 0 0 1-2.52 2.52h-6.31z\"/></svg>",
    new[] { ... config fields ... }),
```

Do this for all 7 channels. For `webchat`, use a generic chat bubble SVG:
```
<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="M20 2H4c-1.1 0-2 .9-2 2v18l4-4h14c1.1 0 2-.9 2-2V4c0-1.1-.9-2-2-2zm0 14H6l-2 2V4h16v12z"/></svg>
```

Fetch the actual SVGs from thesvg.org for: slack, telegram, discord, whatsapp, microsoft-teams, google-chat.

- [ ] **Step 16: Add Logo to ChannelTypeGqlDto**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Types/ChannelTypes.cs`:

```csharp
public sealed record ChannelTypeGqlDto(
    string Type,
    string DisplayName,
    string Description,
    string Logo);
```

- [ ] **Step 17: Update ChannelQueries.GetChannelTypes to include Logo**

In `apps/backend/src/EnterpriseAgentOs.Api/GraphQL/Queries/ChannelQueries.cs`, update the mapping:

```csharp
    return ChannelTypes.All
        .Select(t => new Types.ChannelTypeGqlDto(t.Type, t.DisplayName, t.Description, t.Logo))
        .ToList();
```

- [ ] **Step 18: Create EF Core migration for Emoji/SourceCodeUrl removal**

Run:
```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend
dotnet ef migrations add RemoveEmojiAndSourceCodeUrl --project src/EnterpriseAgentOs.Infrastructure --startup-project .
```

- [ ] **Step 19: Update existing test SeedNotionManifest to include logo instead of emoji**

In `apps/backend/EnterpriseAgentOs.Api.Tests/SkillExecutionTests.cs`, update `SeedNotionManifest()`:

Replace `"emoji": "N",` with `"logo": "<svg viewBox=\"0 0 24 24\"><path d=\"M0 0h24v24H0z\"/></svg>",`

- [ ] **Step 20: Build and run all backend tests**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj`
Expected: ALL PASS including the new SkillDashboardTests

- [ ] **Step 21: Commit**

```bash
git add apps/backend/
git commit -m "feat: remove emoji, add logo/installed/sourceCodeUrl to skill and channel GraphQL types"
```

---

### Task 5: Dashboard — Update GraphQL operations and types

**Files:**
- Modify: `apps/dashboard/src/lib/graphql/operations/integrations.graphql`
- Modify: `apps/dashboard/src/lib/graphql/operations/channels.graphql`
- Modify: `apps/dashboard/src/features/agents/data/integrations.ts` (keep types, delete mock data)
- Modify: `apps/dashboard/src/features/agents/data/channels.ts` (keep types, delete mock data)

- [ ] **Step 1: Update integrations.graphql — replace emoji with logo, add installed**

Replace `apps/dashboard/src/lib/graphql/operations/integrations.graphql` with:

```graphql
query Skills {
  skills {
    id
    name
    title
    description
    logo
    sourceCodeUrl
    doc
    status
    installed
    likes
    likedByMe
    commentsCount
    tools {
      name
      description
    }
  }
}

query SkillComments($skillId: UUID!) {
  skillComments(skillId: $skillId) {
    id
    body
    createdAt
    author {
      id
      name
      avatarUrl
    }
  }
}

mutation InstallSkill($name: String!) {
  installSkill(name: $name)
}

mutation UninstallSkill($name: String!) {
  uninstallSkill(name: $name)
}

mutation LikeSkill($skillId: UUID!) {
  likeSkill(skillId: $skillId) {
    id
    likes
    likedByMe
  }
}

mutation UnlikeSkill($skillId: UUID!) {
  unlikeSkill(skillId: $skillId) {
    id
    likes
    likedByMe
  }
}

mutation CommentOnSkill($skillId: UUID!, $body: String!) {
  commentOnSkill(skillId: $skillId, body: $body) {
    id
    body
    createdAt
    author {
      id
      name
      avatarUrl
    }
  }
}

mutation DeleteSkillComment($commentId: UUID!) {
  deleteSkillComment(commentId: $commentId)
}

mutation SetSkillCredentials($name: String!, $credentials: [SkillCredentialEntryInput!]!) {
  setSkillCredentials(name: $name, credentials: $credentials)
}
```

- [ ] **Step 2: Update channels.graphql — add logo to channelTypes**

In `apps/dashboard/src/lib/graphql/operations/channels.graphql`, add `logo` to the channelTypes query:

```graphql
query ChannelsAndTypes {
  channelTypes {
    type
    displayName
    description
    logo
  }
  channelConnections {
    id
    channelType
    displayName
    enabled
    createdAt
  }
}
```

(Keep existing mutations unchanged.)

- [ ] **Step 3: Update Integration type — remove mock-only fields, align with backend**

Replace `apps/dashboard/src/features/agents/data/integrations.ts` with:

```typescript
export type Tool = {
  name: string
  description: string
}

export type CredentialField = {
  key: string
  label: string
  type: "password" | "text"
  placeholder: string
}

export type Integration = {
  id: string
  name: string
  slug: string
  logo: string
  description: string
  likes: number
  likedByMe: boolean
  commentsCount: number
  tools: Tool[]
  installed: boolean
  doc: string
  sourceCodeUrl: string
}

export const builtInTools: Tool[] = [
  { name: "bash", description: "Execute bash commands" },
  { name: "read", description: "Read files" },
  { name: "write", description: "Write files" },
  { name: "edit", description: "String replacement in files" },
  { name: "glob", description: "File pattern matching" },
  { name: "grep", description: "Text search with regex" },
  { name: "web_fetch", description: "Fetch URL content" },
  { name: "web_search", description: "Search the web" },
]
```

- [ ] **Step 4: Update Channel type — remove mock-only fields, align with backend**

Replace `apps/dashboard/src/features/agents/data/channels.ts` with:

```typescript
export type ChannelPermissions = {
  receive: "allow" | "ask" | "deny"
  send: "allow" | "ask" | "deny"
  initiate: "allow" | "ask" | "deny"
}

export type OnboardingStep = {
  title: string
  description: string
  action: "url" | "qr" | "input" | "copy"
  value?: string
  inputKey?: string
  inputLabel?: string
  inputPlaceholder?: string
}

export type Channel = {
  name: string
  slug: string
  logo: string
  description: string
  protocol: string
  capabilities: string[]
  defaultPermissions: ChannelPermissions
  added: boolean
  onboarding: OnboardingStep[]
}
```

- [ ] **Step 5: Commit**

```bash
git add apps/dashboard/src/lib/graphql/operations/ apps/dashboard/src/features/agents/data/
git commit -m "refactor: update GraphQL operations and types for logo/installed, delete mock data"
```

---

### Task 6: Dashboard — Rewrite useIntegrations hook to use real backend data

**Files:**
- Modify: `apps/dashboard/src/features/agents/api/useIntegrations.ts`

- [ ] **Step 1: Rewrite useIntegrations**

Replace `apps/dashboard/src/features/agents/api/useIntegrations.ts` with:

```typescript
"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import type { Integration } from "../data/integrations"

const SKILLS_QUERY = gql`
  query Skills {
    skills {
      id
      name
      title
      description
      logo
      sourceCodeUrl
      doc
      status
      installed
      likes
      likedByMe
      commentsCount
      tools {
        name
        description
      }
    }
  }
`

const SKILL_COMMENTS_QUERY = gql`
  query SkillComments($skillId: UUID!) {
    skillComments(skillId: $skillId) {
      id
      body
      createdAt
      author {
        id
        name
        avatarUrl
      }
    }
  }
`

const INSTALL_SKILL = gql`
  mutation InstallSkill($name: String!) {
    installSkill(name: $name)
  }
`

const UNINSTALL_SKILL = gql`
  mutation UninstallSkill($name: String!) {
    uninstallSkill(name: $name)
  }
`

const SET_SKILL_CREDENTIALS = gql`
  mutation SetSkillCredentials($name: String!, $credentials: [SkillCredentialEntryInput!]!) {
    setSkillCredentials(name: $name, credentials: $credentials)
  }
`

const LIKE_SKILL = gql`
  mutation LikeSkill($skillId: UUID!) {
    likeSkill(skillId: $skillId) { id likes likedByMe }
  }
`

const UNLIKE_SKILL = gql`
  mutation UnlikeSkill($skillId: UUID!) {
    unlikeSkill(skillId: $skillId) { id likes likedByMe }
  }
`

const COMMENT_ON_SKILL = gql`
  mutation CommentOnSkill($skillId: UUID!, $body: String!) {
    commentOnSkill(skillId: $skillId, body: $body) {
      id
      body
      createdAt
      author { id name avatarUrl }
    }
  }
`

const DELETE_SKILL_COMMENT = gql`
  mutation DeleteSkillComment($commentId: UUID!) {
    deleteSkillComment(commentId: $commentId)
  }
`

export function useIntegrations(): {
  integrations: Integration[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(SKILLS_QUERY)
  const raw: Array<{
    id: string
    name: string
    title: string | null
    description: string | null
    logo: string | null
    doc: string | null
    sourceCodeUrl: string | null
    installed: boolean
    likes: number
    likedByMe: boolean
    commentsCount: number
    tools: Array<{ name: string; description: string }> | null
  }> = data?.skills ?? []

  const integrations: Integration[] = raw.map((s) => ({
    id: s.id,
    name: s.title ?? s.name,
    slug: s.name,
    logo: s.logo ?? "",
    description: s.description ?? "",
    likes: s.likes,
    likedByMe: s.likedByMe,
    commentsCount: s.commentsCount,
    tools: (s.tools ?? []).map((t) => ({ name: t.name, description: t.description })),
    installed: s.installed,
    doc: s.doc ?? "",
    sourceCodeUrl: s.sourceCodeUrl ?? "",
  }))

  return { integrations, loading, error: error ?? undefined }
}

export type SkillComment = {
  id: string
  body: string
  createdAt: string
  author: { id: string; name: string | null; avatarUrl: string | null }
}

export function useSkillComments(skillId: string): {
  comments: SkillComment[]
  loading: boolean
  error?: Error
  refetch: () => void
} {
  const { data, loading, error, refetch } = useQuery(SKILL_COMMENTS_QUERY, {
    variables: { skillId },
    skip: !skillId,
  })
  const raw: Array<{
    id: string
    body: string
    createdAt: string
    author?: { id: string; name: string | null; avatarUrl: string | null } | null
  }> = data?.skillComments ?? []

  const comments: SkillComment[] = raw.map((c) => ({
    id: c.id,
    body: c.body,
    createdAt: c.createdAt,
    author: {
      id: c.author?.id ?? "",
      name: c.author?.name ?? "Unknown",
      avatarUrl: c.author?.avatarUrl ?? null,
    },
  }))

  return { comments, loading, error: error ?? undefined, refetch }
}

export function useInstallSkill() {
  const [fn] = useMutation(INSTALL_SKILL, { refetchQueries: ["Skills"] })
  return async (name: string) => {
    await fn({ variables: { name } })
  }
}

export function useUninstallSkill() {
  const [fn] = useMutation(UNINSTALL_SKILL, { refetchQueries: ["Skills"] })
  return async (name: string) => {
    await fn({ variables: { name } })
  }
}

export function useSetSkillCredentials() {
  const [fn] = useMutation(SET_SKILL_CREDENTIALS, { refetchQueries: ["Skills"] })
  return async (name: string, credentials: Record<string, string>) => {
    const entries = Object.entries(credentials).map(([key, value]) => ({ key, value }))
    await fn({ variables: { name, credentials: entries } })
  }
}

export function useLikeSkill() {
  const [likeFn] = useMutation(LIKE_SKILL)
  const [unlikeFn] = useMutation(UNLIKE_SKILL)
  return async (skillId: string, liked: boolean): Promise<void> => {
    if (liked) await likeFn({ variables: { skillId } })
    else await unlikeFn({ variables: { skillId } })
  }
}

export function useCommentOnSkill() {
  const [fn, state] = useMutation(COMMENT_ON_SKILL)
  return {
    commentOnSkill: async (skillId: string, body: string) => {
      const { data } = await fn({ variables: { skillId, body } })
      return data?.commentOnSkill as SkillComment
    },
    ...state,
  }
}

export function useDeleteSkillComment() {
  const [fn] = useMutation(DELETE_SKILL_COMMENT)
  return async (commentId: string) => {
    await fn({ variables: { commentId } })
  }
}
```

- [ ] **Step 2: Type-check**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && npx tsc --noEmit`
Expected: May fail — pages still reference old types. Fix in next tasks.

- [ ] **Step 3: Commit**

```bash
git add apps/dashboard/src/features/agents/api/useIntegrations.ts
git commit -m "feat: rewrite useIntegrations hook to use real backend GraphQL data"
```

---

### Task 7: Dashboard — Rewrite useChannels hook to use backend logos

**Files:**
- Modify: `apps/dashboard/src/features/agents/api/useChannels.ts`

- [ ] **Step 1: Rewrite useChannels to get logo from backend**

Replace `apps/dashboard/src/features/agents/api/useChannels.ts` with:

```typescript
"use client"

import { gql, useMutation, useQuery } from "@apollo/client"
import type { Channel } from "../data/channels"

const CHANNELS_QUERY = gql`
  query ChannelsAndTypes {
    channelTypes {
      type
      displayName
      description
      logo
    }
    channelConnections {
      id
      channelType
      displayName
      enabled
      createdAt
    }
  }
`

const CREATE_CONNECTION = gql`
  mutation CreateChannelConnection($input: CreateChannelConnectionInput!) {
    createChannelConnection(input: $input) {
      id
      channelType
    }
  }
`

const DELETE_CONNECTION = gql`
  mutation DeleteChannelConnection($id: UUID!) {
    deleteChannelConnection(id: $id)
  }
`

const BIND_CHANNEL = gql`
  mutation BindChannelToAgent($agentId: UUID!, $channelConnectionId: UUID!) {
    bindChannelToAgent(agentId: $agentId, channelConnectionId: $channelConnectionId) {
      id
      agentId
      channelConnectionId
    }
  }
`

export function useChannels(): {
  channels: Channel[]
  loading: boolean
  error?: Error
} {
  const { data, loading, error } = useQuery(CHANNELS_QUERY)

  const types: Array<{
    type: string
    displayName: string
    description: string | null
    logo: string | null
  }> = data?.channelTypes ?? []
  const connections: Array<{ channelType: string }> = data?.channelConnections ?? []
  const connectedSlugs = new Set(connections.map((c) => c.channelType))

  const channels: Channel[] = types.map((t) => ({
    name: t.displayName,
    slug: t.type,
    logo: t.logo ?? "",
    description: t.description ?? "",
    protocol: "",
    capabilities: [],
    defaultPermissions: { receive: "ask" as const, send: "ask" as const, initiate: "ask" as const },
    added: connectedSlugs.has(t.type),
    onboarding: [],
  }))

  return { channels, loading, error: error ?? undefined }
}

export function useCreateChannelConnection() {
  const [fn, state] = useMutation(CREATE_CONNECTION, { refetchQueries: ["ChannelsAndTypes"] })
  return {
    createChannelConnection: async (input: {
      channelType: string
      credentials: Record<string, string>
    }) => {
      const { data } = await fn({ variables: { input } })
      return data?.createChannelConnection as { id: string; channelType: string }
    },
    ...state,
  }
}

export function useDeleteChannelConnection() {
  const [fn, state] = useMutation(DELETE_CONNECTION, { refetchQueries: ["ChannelsAndTypes"] })
  return {
    deleteChannelConnection: async (id: string) => {
      const { data } = await fn({ variables: { id } })
      return Boolean(data?.deleteChannelConnection)
    },
    ...state,
  }
}

export function useBindChannelToAgent() {
  const [fn, state] = useMutation(BIND_CHANNEL)
  return {
    bindChannelToAgent: async (connectionId: string, agentId: string) => {
      const { data } = await fn({ variables: { agentId, channelConnectionId: connectionId } })
      return Boolean(data?.bindChannelToAgent)
    },
    ...state,
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/dashboard/src/features/agents/api/useChannels.ts
git commit -m "feat: rewrite useChannels hook to use backend logo data"
```

---

### Task 8: Dashboard — Rewrite integrations list page

**Files:**
- Modify: `apps/dashboard/src/app/(dashboard)/integrations/page.tsx`

- [ ] **Step 1: Rewrite integrations page to use real data**

Replace `apps/dashboard/src/app/(dashboard)/integrations/page.tsx` with:

```tsx
"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { CredentialDialog } from "@/features/agents"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { useIntegrations, useInstallSkill, useUninstallSkill } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { SearchIcon, HeartIcon, PlusIcon, CheckIcon, KeyRoundIcon } from "lucide-react"

type View = "all" | "installed" | "explore"

export default function IntegrationsPage() {
  const router = useRouter()
  const { integrations, loading } = useIntegrations()
  const installSkill = useInstallSkill()
  const uninstallSkill = useUninstallSkill()
  const { trackSkillInstalled } = useAnalytics()
  const [search, setSearch] = useState("")
  const [view, setView] = useState<View>("all")
  const [credDialogSlug, setCredDialogSlug] = useState<string | null>(null)

  const filtered = integrations.filter((i) => {
    if (search && !i.name.toLowerCase().includes(search.toLowerCase())) return false
    if (view === "installed" && !i.installed) return false
    if (view === "explore" && i.installed) return false
    return true
  })

  const installedCount = integrations.filter((i) => i.installed).length
  const credDialogIntegration = credDialogSlug ? integrations.find((i) => i.slug === credDialogSlug) : null

  async function handleAdd(slug: string, e: React.MouseEvent) {
    e.stopPropagation()
    await installSkill(slug)
    trackSkillInstalled(slug)
  }

  return (
    <>
      <PageHeader group="Managed Agents" page="Integrations" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="relative flex-1 max-w-sm">
            <SearchIcon className="absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
            <Input placeholder="Search integrations..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-8" />
          </div>
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["installed", `Installed (${installedCount})`], ["explore", "Explore"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => setView(key)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "explore" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="py-8 text-center text-sm text-muted-foreground">Loading integrations...</div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((integration) => (
              <button
                key={integration.slug}
                type="button"
                onClick={() => router.push(`/integrations/${integration.slug}`)}
                className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <div className="size-8 shrink-0 [&>svg]:size-8" dangerouslySetInnerHTML={{ __html: integration.logo }} />
                  <div className="min-w-0 flex-1">
                    <span className="font-medium text-sm">{integration.name}</span>
                  </div>
                  {integration.installed ? (
                    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
                      <CheckIcon className="size-3" /> Installed
                    </span>
                  ) : (
                    <Button size="sm" variant="outline" className="h-7 text-xs" onClick={(e) => handleAdd(integration.slug, e)}>
                      <PlusIcon className="size-3" /> Add
                    </Button>
                  )}
                </div>
                <p className="text-sm line-clamp-2 text-muted-foreground">{integration.description}</p>
                <div className="flex items-center gap-3 text-xs text-muted-foreground">
                  <span>{integration.tools.length} tools</span>
                  <span>·</span>
                  <span className="flex items-center gap-1"><HeartIcon className="size-3" />{integration.likes}</span>
                </div>
              </button>
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No integrations found.</div>
        )}
      </div>
    </>
  )
}
```

- [ ] **Step 2: Type-check**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && npx tsc --noEmit`
Expected: May still fail from detail page — fix in next task.

- [ ] **Step 3: Commit**

```bash
git add apps/dashboard/src/app/\(dashboard\)/integrations/page.tsx
git commit -m "feat: rewrite integrations list page to use real backend data with SVG logos"
```

---

### Task 9: Dashboard — Rewrite integration detail page with likes and comments

**Files:**
- Modify: `apps/dashboard/src/app/(dashboard)/integrations/[slug]/page.tsx`

- [ ] **Step 1: Rewrite detail page with like button and comments**

Replace `apps/dashboard/src/app/(dashboard)/integrations/[slug]/page.tsx` with:

```tsx
"use client"

import { use, useState } from "react"
import { notFound } from "next/navigation"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import {
  useIntegrations,
  useSkillComments,
  useLikeSkill,
  useCommentOnSkill,
  useDeleteSkillComment,
  useInstallSkill,
  useUninstallSkill,
} from "@/features/agents"
import {
  ExternalLinkIcon,
  HeartIcon,
  DownloadIcon,
  XIcon,
  SendIcon,
  Trash2Icon,
} from "lucide-react"

export default function IntegrationDetailPage({
  params,
}: {
  params: Promise<{ slug: string }>
}) {
  const { slug } = use(params)
  const { integrations, loading } = useIntegrations()
  const integration = integrations.find((i) => i.slug === slug)
  const likeSkill = useLikeSkill()
  const { commentOnSkill } = useCommentOnSkill()
  const deleteComment = useDeleteSkillComment()
  const installSkill = useInstallSkill()
  const uninstallSkill = useUninstallSkill()
  const { comments, refetch: refetchComments } = useSkillComments(integration?.id ?? "")
  const [commentBody, setCommentBody] = useState("")

  if (!integration) {
    if (loading) return null
    return notFound()
  }

  async function handleLike() {
    if (!integration) return
    await likeSkill(integration.id, !integration.likedByMe)
  }

  async function handleComment() {
    if (!integration || !commentBody.trim()) return
    await commentOnSkill(integration.id, commentBody.trim())
    setCommentBody("")
    refetchComments()
  }

  async function handleDeleteComment(commentId: string) {
    await deleteComment(commentId)
    refetchComments()
  }

  return (
    <>
      <PageHeader
        group="Integrations"
        page={integration.name}
        action={
          integration.installed ? (
            <Button size="sm" variant="outline" onClick={() => uninstallSkill(integration.slug)}>
              <XIcon className="size-4" />
              Uninstall
            </Button>
          ) : (
            <Button size="sm" onClick={() => installSkill(integration.slug)}>
              <DownloadIcon className="size-4" />
              Install
            </Button>
          )
        }
      />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        {/* Header */}
        <div className="flex items-start gap-4">
          <div className="size-12 shrink-0 rounded-xl [&>svg]:size-12" dangerouslySetInnerHTML={{ __html: integration.logo }} />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 mb-1">
              <h1 className="text-lg font-semibold">{integration.name}</h1>
            </div>
            <p className="text-sm text-muted-foreground">{integration.description}</p>
            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
              <button
                type="button"
                onClick={handleLike}
                className={`flex items-center gap-1 transition-colors hover:text-foreground ${integration.likedByMe ? "text-red-500" : ""}`}
              >
                <HeartIcon className={`size-3 ${integration.likedByMe ? "fill-current" : ""}`} />
                {integration.likes}
              </button>
              <span>{integration.tools.length} tools</span>
              <span>{integration.commentsCount} comments</span>
              <a
                href={integration.sourceCodeUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-1 hover:text-foreground transition-colors"
              >
                <ExternalLinkIcon className="size-3" />
                Source
              </a>
            </div>
          </div>
        </div>

        {/* Tools card */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Tools</span>
            <span className="ml-2 text-xs text-muted-foreground">{integration.tools.length}</span>
          </div>
          {integration.tools.map((tool, i) => (
            <div
              key={tool.name}
              className={`flex items-center gap-4 px-4 py-3 ${
                i < integration.tools.length - 1 ? "border-b border-border" : ""
              }`}
            >
              <code className="rounded bg-muted px-2 py-1 font-mono text-xs">{tool.name}</code>
              <span className="text-sm text-muted-foreground">{tool.description}</span>
            </div>
          ))}
        </div>

        {/* Documentation card */}
        {integration.doc && (
          <div className="rounded-xl border border-border bg-card">
            <div className="px-4 py-3 border-b border-border">
              <span className="text-sm font-medium">Documentation</span>
            </div>
            <div className="p-6">
              <div className="prose prose-sm max-w-none
                prose-headings:font-semibold prose-headings:text-foreground
                prose-h1:text-lg prose-h1:mt-0 prose-h1:mb-3
                prose-h2:text-sm prose-h2:mt-6 prose-h2:mb-3
                prose-h3:text-sm prose-h3:font-mono prose-h3:mt-4 prose-h3:mb-2 prose-h3:text-foreground
                prose-p:text-sm prose-p:leading-relaxed prose-p:text-muted-foreground
                prose-strong:text-foreground prose-strong:font-medium
                prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-xs prose-code:font-mono prose-code:text-foreground prose-code:before:content-none prose-code:after:content-none
                prose-pre:bg-zinc-950 prose-pre:text-zinc-300 prose-pre:rounded-lg prose-pre:text-xs prose-pre:leading-relaxed
                prose-table:text-sm prose-table:w-full
                prose-th:text-left prose-th:font-medium prose-th:text-muted-foreground prose-th:py-2 prose-th:px-3 prose-th:border-b prose-th:border-border
                prose-td:py-2 prose-td:px-3 prose-td:border-b prose-td:border-border prose-td:text-muted-foreground
                prose-li:text-sm prose-li:text-muted-foreground
                prose-ol:text-sm
                prose-a:text-foreground prose-a:underline prose-a:underline-offset-2
              ">
                <ReactMarkdown remarkPlugins={[remarkGfm]}>
                  {integration.doc}
                </ReactMarkdown>
              </div>
            </div>
          </div>
        )}

        {/* Comments section */}
        <div className="rounded-xl border border-border bg-card">
          <div className="px-4 py-3 border-b border-border">
            <span className="text-sm font-medium">Comments</span>
            <span className="ml-2 text-xs text-muted-foreground">{comments.length}</span>
          </div>
          <div className="p-4 space-y-4">
            {/* Comment input */}
            <div className="flex gap-2">
              <Textarea
                placeholder="Write a comment..."
                value={commentBody}
                onChange={(e) => setCommentBody(e.target.value)}
                className="min-h-[60px] text-sm"
              />
              <Button
                size="sm"
                onClick={handleComment}
                disabled={!commentBody.trim()}
                className="self-end"
              >
                <SendIcon className="size-4" />
              </Button>
            </div>

            {/* Comment list */}
            {comments.map((comment) => (
              <div key={comment.id} className="flex gap-3 border-t border-border pt-3">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <span className="text-sm font-medium">{comment.author.name}</span>
                    <span className="text-xs text-muted-foreground">
                      {new Date(comment.createdAt).toLocaleDateString()}
                    </span>
                  </div>
                  <p className="text-sm text-muted-foreground whitespace-pre-wrap">{comment.body}</p>
                </div>
                <button
                  type="button"
                  onClick={() => handleDeleteComment(comment.id)}
                  className="text-muted-foreground hover:text-foreground transition-colors self-start"
                >
                  <Trash2Icon className="size-3" />
                </button>
              </div>
            ))}

            {comments.length === 0 && (
              <p className="text-sm text-muted-foreground text-center py-2">No comments yet.</p>
            )}
          </div>
        </div>
      </div>
    </>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/dashboard/src/app/\(dashboard\)/integrations/\[slug\]/page.tsx
git commit -m "feat: add like button and comments section to integration detail page"
```

---

### Task 10: Dashboard — Rewrite channels page with backend logos

**Files:**
- Modify: `apps/dashboard/src/app/(dashboard)/channels/page.tsx`

- [ ] **Step 1: Rewrite channels page to use SVG logos from backend**

Replace `apps/dashboard/src/app/(dashboard)/channels/page.tsx` with:

```tsx
"use client"

import { useState } from "react"
import { useRouter } from "next/navigation"
import { PageHeader } from "@/components/page-header"
import { ChannelOnboardingDialog } from "@/features/agents"
import { Button } from "@/components/ui/button"
import { type Channel } from "@/features/agents/data/channels"
import { useChannels, useCreateChannelConnection, useBindChannelToAgent } from "@/features/agents"
import { useAnalytics } from "@/features/analytics"
import { PlusIcon, RadioIcon } from "lucide-react"

type View = "all" | "connected" | "available"

export default function ChannelsPage() {
  const router = useRouter()
  const { channels, loading } = useChannels()
  const { createChannelConnection } = useCreateChannelConnection()
  const { bindChannelToAgent } = useBindChannelToAgent()
  const { trackChannelConnected } = useAnalytics()
  void createChannelConnection
  void bindChannelToAgent
  const [view, setView] = useState<View>("all")
  const [onboardingChannel, setOnboardingChannel] = useState<Channel | null>(null)

  const filtered = channels.filter((c) => {
    if (view === "connected" && !c.added) return false
    if (view === "available" && c.added) return false
    return true
  })

  const connectedCount = channels.filter((c) => c.added).length

  return (
    <>
      <PageHeader group="Managed Agents" page="Channels" />
      <div className="flex flex-1 flex-col gap-4 p-4 pt-0">
        <div className="flex items-center gap-2">
          <div className="flex items-center rounded-lg border border-border">
            {([["all", "All"], ["connected", `Connected (${connectedCount})`], ["available", "Available"]] as const).map(([key, label]) => (
              <button key={key} type="button" onClick={() => setView(key)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${view === key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:text-foreground"} ${key === "all" ? "rounded-l-md" : ""} ${key === "available" ? "rounded-r-md" : ""}`}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="py-8 text-center text-sm text-muted-foreground">Loading channels...</div>
        ) : (
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((channel) => (
              <button
                key={channel.slug}
                type="button"
                onClick={() => router.push(`/channels/${channel.slug}`)}
                className="flex flex-col gap-3 rounded-xl border border-border p-4 text-left transition-colors hover:bg-muted/50 cursor-pointer"
              >
                <div className="flex items-start gap-3">
                  <div className="size-8 shrink-0 [&>svg]:size-8" dangerouslySetInnerHTML={{ __html: channel.logo }} />
                  <div className="min-w-0 flex-1">
                    <div className="font-medium text-sm">{channel.name}</div>
                  </div>
                  {channel.added ? (
                    <span className="flex items-center gap-1 rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-widest text-emerald-700">
                      <RadioIcon className="size-3" /> Live
                    </span>
                  ) : (
                    <Button size="sm" variant="outline" className="h-7 text-xs" onClick={(e) => { e.stopPropagation(); setOnboardingChannel(channel) }}>
                      <PlusIcon className="size-3" /> Connect
                    </Button>
                  )}
                </div>
                <p className="text-sm line-clamp-2 text-muted-foreground">{channel.description}</p>
              </button>
            ))}
          </div>
        )}

        {!loading && filtered.length === 0 && (
          <div className="py-8 text-center text-sm text-muted-foreground">No channels found.</div>
        )}
      </div>

      {/* Onboarding overlay */}
      {onboardingChannel && (
        <ChannelOnboardingDialog
          open={!!onboardingChannel}
          onOpenChange={(open) => { if (!open) setOnboardingChannel(null) }}
          channel={onboardingChannel}
          onComplete={() => {
            trackChannelConnected(onboardingChannel.slug)
            setOnboardingChannel(null)
          }}
        />
      )}
    </>
  )
}
```

- [ ] **Step 2: Commit**

```bash
git add apps/dashboard/src/app/\(dashboard\)/channels/page.tsx
git commit -m "feat: rewrite channels page to use backend SVG logos"
```

---

### Task 11: Dashboard — Update barrel exports and clean up dead imports

**Files:**
- Modify: `apps/dashboard/src/features/agents/index.ts`
- Delete: `apps/dashboard/public/logos/*.svg` (static logo files no longer needed)

- [ ] **Step 1: Update barrel exports**

In `apps/dashboard/src/features/agents/index.ts`, add the new exports:

```typescript
export * from "./api/useAgents";
export * from "./api/useIntegrations";
export * from "./api/useChannels";
export * from "./api/useAgentTemplates";
export * from "./api/useSendAgentMessage";
export * from "./api/useProviders";
export * from "./api/useModels";
export * from "./components/credential-dialog";
export * from "./components/channel-onboarding-dialog";
```

This file should already be correct since we're exporting from the same API files. Verify no dead imports.

- [ ] **Step 2: Delete static logo SVG files**

Run: `ls /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard/public/logos/`

If files exist, delete them:
```bash
rm -rf /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard/public/logos/
```

- [ ] **Step 3: Type-check entire dashboard**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && npx tsc --noEmit`
Expected: PASS

- [ ] **Step 4: Build dashboard**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && bun run build`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add -A apps/dashboard/
git commit -m "chore: clean up dead imports, delete static logo SVGs"
```

---

### Task 12: Update CLAUDE.md files

**Files:**
- Modify: `apps/backend/CLAUDE.md` (if needed)
- Modify: `apps/dashboard/CLAUDE.md`
- Modify: `packages/skill-sdk/CLAUDE.md`

- [ ] **Step 1: Update skill-sdk CLAUDE.md — remove emoji mentions**

In `packages/skill-sdk/CLAUDE.md`, remove the line about `SkillDefinition.emoji` being a deprecated fallback.

- [ ] **Step 2: Update dashboard CLAUDE.md — update hooks table and data model**

In `apps/dashboard/CLAUDE.md`:
- Update the `useIntegrations` hook description to note it queries real GraphQL (no mock fallback)
- Update the `useChannels` hook description similarly
- Remove mention of `NEXT_PUBLIC_USE_MOCKS` for integrations/channels (no longer mock)
- Update the Integration data model to reflect new fields (id, logo as SVG, installed, likedByMe, commentsCount, doc, sourceCodeUrl) and removed fields (likes mock count, updatedAgo, credentials, added, skillMd)
- Update Channel data model similarly

- [ ] **Step 3: Commit**

```bash
git add packages/skill-sdk/CLAUDE.md apps/dashboard/CLAUDE.md
git commit -m "docs: update CLAUDE.md files to reflect emoji removal and real backend wiring"
```

---

### Task 13: Final verification

- [ ] **Step 1: Run backend tests**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj`
Expected: ALL PASS

- [ ] **Step 2: Run dashboard type check**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && npx tsc --noEmit`
Expected: PASS

- [ ] **Step 3: Run dashboard build**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/apps/dashboard && bun run build`
Expected: PASS

- [ ] **Step 4: Run skill-runtime build**

Run: `cd /Users/harrokrog/Desktop/EnterpriseAgentOs/packages/skill-runtime && npm run build`
Expected: PASS

- [ ] **Step 5: Verify no emoji references remain in entire codebase**

Run: `grep -rn 'emoji' packages/skills/ packages/skill-sdk/src/ packages/skill-runtime/src/ apps/backend/src/ apps/dashboard/src/features/agents/ apps/dashboard/src/app/\(dashboard\)/integrations/ apps/dashboard/src/app/\(dashboard\)/channels/ apps/dashboard/src/lib/graphql/operations/ --include='*.ts' --include='*.tsx' --include='*.cs' --include='*.graphql'`
Expected: No matches (zero references to emoji)
