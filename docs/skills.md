# Skill System

> How agents discover and use external tools — without touching credentials.

## Overview

Skills connect agents to external services (Notion, GitHub, Google, etc.). The key design constraint: **credentials never leave the backend**. Agents don't know API keys exist.

```
Agent sees:          skill_exec("notion search --query meetings")
                              ↓
                     CLI parser (Rust, deterministic)
                              ↓
GraphQL query:       { notionSearch(query: "meetings") { id title url } }
                              ↓
                     POST /api/graphql (Bearer: agent-uuid)
                              ↓
                     HotChocolate resolver → NotionSkill.SearchAsync()
                              ↓
                     Notion REST API (with decrypted credentials)
                              ↓
                     Structured result back to agent
```

## Architecture

### Three layers

1. **Skill implementations** (`apps/v2-backend/Entities/Skills/Implementations/`) — C# classes that call vendor APIs. Each takes a request DTO + decrypted credentials + CancellationToken, returns typed result objects.

2. **GraphQL gateway** (`apps/v2-backend/Entities/Skills/GraphQL/`) — HotChocolate query types that wrap the implementations. Each resolver fetches decrypted credentials from the DB and calls the skill method. Exposed at `POST /api/graphql` with agent-token auth.

3. **`skill_exec` tool** (`packages/zeroclaw-core/src/tools/skill_exec/`) — Rust tool registered on every agent. Parses CLI-style commands, caches the GraphQL schema via introspection, builds queries, and sends them to the backend.

### Discovery flow

1. Agent boots with `ZEROCLAW_AGENT_ID`.
2. On first `skill_exec` call, the tool fires a GraphQL introspection query against `/api/graphql`.
3. The schema is cached for 5 minutes. `--help` at any level reads from cache (no HTTP).
4. When a skill is installed/configured on the dashboard, the schema changes. The agent picks it up on the next cache refresh.

### Credential isolation

```
Dashboard admin configures Notion API key
  → encrypted with DataProtection, stored in Postgres SkillCredentials table
  → never sent to agent pods
  → decrypted per-request inside GraphQL resolvers
  → used to call Notion API server-side
  → result returned to agent (no key in the response)
```

## Current skills

| Skill  | Tools                                             | Vendor API                   |
| ------ | ------------------------------------------------- | ---------------------------- |
| Notion | `notion search`, `notion read_page`               | Notion REST API v1           |
| GitHub | `github repos`, `github issues`, `github prs`     | GitHub REST API v3           |
| Google | `google drive_search`, `google calendar_upcoming` | Google Drive + Calendar APIs |

## Adding a new skill

### 1. Typed return objects

Create `Entities/Skills/GraphQL/Types/JiraTypes.cs`:

```csharp
public class JiraIssue
{
    public string? Key { get; init; }
    public string? Summary { get; init; }
    public string? Status { get; init; }
    public string? Assignee { get; init; }
}
```

### 2. Skill implementation

Create `Entities/Skills/Implementations/JiraSkill.cs`:

```csharp
public sealed class JiraSkill
{
    private readonly HttpClient _http;
    public JiraSkill(HttpClient http) { _http = http; }

    public async Task<List<JiraIssue>> SearchAsync(
        JiraSearchRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var token = creds["api_token"];
        var domain = creds["domain"];
        // Call Jira REST API, parse, return typed objects
    }
}
```

### 3. Manifest

Add to `SkillManifests.cs`:

```csharp
["jira"] = new SkillManifest(
    Name: "jira",
    Title: "Jira",
    Description: "Search and manage Jira issues.",
    Emoji: "🎯",
    CredentialFields: new[] {
        new CredentialField(Key: "api_token", Label: "API Token", Kind: "password"),
        new CredentialField(Key: "domain", Label: "Domain", Kind: "text", Placeholder: "company.atlassian.net"),
    },
    LlmTools: new[] { /* kept for admin UI display */ }),
```

### 4. GraphQL resolver

Create `Entities/Skills/GraphQL/JiraQueries.cs`:

```csharp
[ExtendObjectType("Query")]
public class JiraQueries
{
    [GraphQLDescription("Search Jira issues")]
    public async Task<List<JiraIssue>> JiraSearch(
        [Service] JiraSkill jira,
        [Service] ISkillService skills,
        string jql,
        int maxResults = 20,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("jira", ct)
            ?? throw new GraphQLException("Jira not configured");
        return await jira.SearchAsync(new(jql, maxResults), creds, ct);
    }
}
```

### 5. Documentation

Create `Entities/Skills/Docs/jira.md` with tool descriptions, parameter details, and usage patterns.

### 6. Registration

In `Program.cs`:

```csharp
builder.Services.AddHttpClient<JiraSkill>();
// In AddGraphQLServer():
.AddTypeExtension<JiraQueries>()
```

### 7. Done

No DB migration. No zeroclaw changes. The agent discovers the new skill automatically via GraphQL introspection within 5 minutes (or on next `skill_exec("--help")` after cache expiry).

## Agent CLI experience

```
skill_exec("--help")
→ Available skills:
    notion     actions: search, read_page
    github     actions: repos, issues, prs
    google     actions: drive_search, calendar_upcoming

skill_exec("notion --help")
→ notion search — Search the Notion workspace for pages
    --query STRING        (required) Free-text search query
    --page_size INT       Max results (1-100), default 10

  notion read_page — Read a page's content as plain text
    --page_id STRING      (required) Page UUID from search results

skill_exec("notion search --query 'project plan' --page_size 5")
→ { "notionSearch": { "results": [ { "id": "abc", "title": "Q2 Project Plan", ... } ] } }
```

## Global vs per-agent

Skills are currently **global** — all agents see all configured skills. There's no per-agent scoping. The `SkillCredentials` table has no `AgentId` column.

To add per-agent skills in the future: add `AgentId` FK to `SkillCredentialRecord`, filter in the GraphQL auth interceptor, and add a per-agent skill config UI on the agent detail page.
