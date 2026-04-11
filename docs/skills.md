# Skill System

> How agents discover and use external tools — without touching credentials.

## Overview

Skills connect agents to external services (Notion, GitHub, Google, etc.). The key design constraint: **credentials never leave the backend**. Agents don't know API keys exist.

```
Agent sees:          skill_exec("notion search --query meetings")
                              ↓
                     CLI parser (Rust, deterministic)
                              ↓
                     POST /api/agents/me/skill-exec (Bearer: agent-uuid)
                              ↓
                     Backend decrypts credentials from Postgres
                              ↓
                     POST /execute → skill-runtime (Node.js)
                              ↓
                     Notion REST API (with injected credentials)
                              ↓
                     Structured result back to agent
```

## Architecture

### Three layers

1. **Skill packages** (`packages/skills/{name}/`) — TypeScript modules using `@harro/skill-sdk`. Each skill is fully self-contained: title, emoji, description, doc, credential field definitions, and action implementations all live in a single `skill.ts` file alongside a `SKILL.md` documentation file.

2. **Skill runtime** (`packages/skill-runtime/`) — Node.js service that bundles all skills via esbuild, exposes `/manifests` (skill metadata) and `/execute` (action dispatch). Receives credentials per-request from the backend — never stores them.

3. **Backend gateway** (`apps/v2-backend/Entities/Skills/`) — Proxies execution requests, manages credential encryption/storage, and serves the dashboard API. The backend has zero hardcoded skill knowledge — everything comes from the runtime's `/manifests` endpoint.

4. **`skill_exec` tool** (`packages/zeroclaw-core/src/tools/skill_exec/`) — Rust tool registered on every agent. Parses CLI-style commands, discovers available skills via the capabilities endpoint, and dispatches execution through the backend.

### Discovery flow

1. Agent boots with `ZEROCLAW_AGENT_ID`.
2. On first turn, fetches `GET /api/agents/me/capabilities` (forced, ignores TTL).
3. Response includes tool specs and SKILL.md docs for all installed+configured skills.
4. Skill docs are injected into the system prompt as XML sections so the LLM knows how to use each skill without needing `--help`.

### Refresh behavior

The agent polls for capability changes — no event-driven push, no pod restart needed.

```
skill-runtime (source of truth)
    ↓ GET /manifests (cached 30s by backend)
backend SkillRuntimeClient
    ↓ GET /api/agents/me/capabilities (polled every 30s by agent)
zeroclaw CapabilityCache
    ↓ hash-compare response
    ↓ if changed → swap tools + rebuild system prompt
agent LLM context (up to date)
```

- **Agent polling**: every 30 seconds (configurable via `backend_refresh_seconds` in gateway bootstrap).
- **Backend caching**: skill-runtime manifests cached for 30 seconds in `SkillRuntimeClient`.
- **Change detection**: agent hash-compares each capabilities response. If unchanged, no work is done. If changed, tools are swapped and the system prompt is rebuilt with new SKILL.md docs.
- **Worst case latency**: ~60 seconds between a skill-runtime update and the agent picking it up (30s backend cache + 30s agent poll).
- **Failure mode**: network errors fail open — the agent reuses cached tools and logs a warning.

### Credential isolation

```
Dashboard admin configures Notion API key
  → encrypted with DataProtection, stored in Postgres SkillCredentials table
  → never sent to agent pods
  → decrypted per-request in SkillService
  → injected into skill-runtime /execute call
  → used to call Notion API inside the skill
  → result returned to agent (no key in the response)
```

## Skill SDK

Skills are defined with `defineSkill()` from `@harro/skill-sdk`. A skill definition includes:

| Field | Type | Description |
|-------|------|-------------|
| `name` | `string` | Unique identifier (lowercase) |
| `title` | `string` | Human-readable title for the dashboard |
| `emoji` | `string` | Icon for dashboard display |
| `description` | `string` | Short description of the skill's purpose |
| `doc` | `string` | Markdown documentation (imported from SKILL.md) |
| `credentials` | `Record<string, CredentialFieldDefinition>` | Credential fields with UI metadata |
| `actions` | `Record<string, ActionDefinition>` | Map of action name → implementation |

All fields are required. Credential fields specify `label`, `kind` (password/text/textarea), `required`, `placeholder`, and `help` — the dashboard renders forms directly from this metadata.

## Current skills

| Skill  | Actions                                                                                              | Vendor API                   |
|--------|------------------------------------------------------------------------------------------------------|------------------------------|
| Notion | search, read_page, create_page, list_blocks, add_block, update_block, delete_block, add_todo, update_todo, query_database | Notion REST API v1           |
| GitHub | list_repos, list_issues, list_prs                                                                    | GitHub REST API v3           |
| Google | drive_search, calendar_upcoming                                                                      | Google Drive + Calendar APIs |

## Adding a new skill

### 1. Skill package

Create `packages/skills/jira/skill.ts`:

```typescript
import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

export default defineSkill({
  name: "jira",
  title: "Jira",
  emoji: "🎯",
  description: "Search and manage Jira issues.",
  doc,

  credentials: {
    api_token: {
      label: "API Token",
      kind: "password",
      placeholder: "ATATT3x...",
      help: "Create an API token at https://id.atlassian.com/manage-profile/security/api-tokens",
    },
    domain: {
      label: "Atlassian Domain",
      kind: "text",
      placeholder: "company.atlassian.net",
    },
  },

  actions: {
    search: {
      description: "Search Jira issues by JQL query.",
      params: z.object({
        jql: z.string().describe("JQL query string"),
        max_results: z.number().min(1).max(100).default(20),
      }),
      returns: z.array(z.object({
        key: z.string(),
        summary: z.string(),
        status: z.string(),
        assignee: z.string().nullable(),
      })),
      execute: async (params, ctx) => {
        // Call Jira REST API using ctx.credentials and ctx.fetch
      },
    },
  },
});
```

### 2. Documentation

Create `packages/skills/jira/SKILL.md` with CLI usage examples, parameter tables, workflow guidance, and safety notes. This file is injected into the agent's context.

### 3. Package manifest

Create `packages/skills/jira/package.json`:

```json
{
  "name": "@harro/skill-jira",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "main": "skill.ts",
  "dependencies": {
    "@harro/skill-sdk": "file:../../skill-sdk"
  }
}
```

### 4. Build

```bash
cd packages/skill-runtime && npm run build
```

The build script auto-discovers skill packages and bundles them. No backend code changes needed — the runtime's `/manifests` endpoint exposes the new skill, and the backend's `SkillService` picks it up automatically.

### 5. Done

No C# code. No DB migration. No GraphQL resolvers. The dashboard shows the new skill with its credential form, and agents discover it via the capabilities endpoint.

## Backend structure (`Entities/Skills/`)

| File | Responsibility |
|------|---------------|
| `SkillsController.cs` | Dashboard REST API (`/api/skills`) — list, get, install, uninstall, credentials, doc |
| `AgentSkillsController.cs` | Agent pod API (`/api/agents/me`) — capabilities, skill-exec |
| `ISkillService.cs` / `SkillService.cs` | Business logic — merges runtime manifests with DB state |
| `ISkillCredentialRepository.cs` / `SkillCredentialRepository.cs` | Postgres CRUD for encrypted credentials |
| `SkillCredentialProtector.cs` | DataProtection wrapper for credential encryption |
| `SkillRuntimeClient.cs` | HTTP client to the skill-runtime service |
| `SkillRuntimeModels.cs` | Deserialization models for runtime responses |
| `SkillDto.cs` | API DTOs and request/response records |
| `AgentTokenAuthAttribute.cs` | Agent pod auth (Bearer UUID) |
| `AgentBackendTokenProtector.cs` | Backend token encryption |
| `GraphQL/` | Dynamic GraphQL schema from runtime manifests (SkillTypeModule) |

## Custom skills

Users can add their own skills via the dashboard — either by uploading a `.zip` or connecting a GitHub repo.

### Upload flow

```
Dashboard "Upload Skill" button
    ↓
POST /api/custom-skills/upload (multipart .zip)
    ↓
Backend validates: zip must contain skill.ts
    ↓
Stores zip in MinIO (s3://skills/{name}/{name}.zip)
    ↓
Sends source files to skill-runtime POST /build
    ↓
skill-runtime runs esbuild, hot-loads the skill
    ↓
Skill appears in /manifests → agents discover it
```

### GitHub flow

```
Dashboard "Connect GitHub Repo" button
    ↓
POST /api/custom-skills/github { repoUrl, branch }
    ↓
Backend stores config in CustomSkills table
    ↓
Build status: "pending" (clone + build is TODO)
```

### Storage

Custom skill source archives are stored in **MinIO** (S3-compatible) in the `skills` bucket. Config in `appsettings.json`:

| Key | Description |
|-----|-------------|
| `MinioEndpoint` | S3 endpoint URL (e.g. `http://eaos-minio:9000`) |
| `MinioAccessKey` | Access key |
| `MinioSecretKey` | Secret key |
| `MinioBucket` | Bucket name (default: `skills`) |

### Build endpoints (skill-runtime)

| Method | Path | Description |
|--------|------|-------------|
| `POST /build` | `{ name, files: [{ path, content }] }` | Build a skill from source, hot-load it |
| `POST /reload/{name}` | — | Re-load an existing built skill from disk |

### Database model (`CustomSkillRecord`)

| Column | Description |
|--------|-------------|
| `Id` | Primary key |
| `OwnerId` | FK to UserRecord |
| `Name` | Skill name (unique) |
| `Source` | `"upload"` or `"github"` |
| `GitHubRepoUrl` | Repository URL (github source) |
| `BundlePath` | S3 path to stored zip |
| `BuildStatus` | `pending`, `building`, `ready`, `failed` |

## Runner dispatch

When an agent calls `skill_exec` for a skill that isn't installed locally, the backend checks for an online runner. If one exists, the job is dispatched to the runner instead of the local skill-runtime. See [runners.md](runners.md) for the full architecture.

```
Agent calls skill_exec
    ↓
AgentSkillsController.SkillExec:
  1. Check local skill credentials → if configured, use skill-runtime (normal path)
  2. If not configured → check for online runners
  3. If runner available → create RunnerJobRecord, await via RunnerJobWaiter (30s timeout)
  4. Runner polls, executes, posts result → waiter completes → response to agent
  5. If no runner → return 409 "not installed or not configured"
```

## Global vs per-agent

Skills are currently **global** — all agents see all configured skills. There's no per-agent scoping. The `SkillCredentials` table has no `AgentId` column.

To add per-agent skills in the future: add `AgentId` FK to `SkillCredentialRecord`, filter in the capabilities endpoint, and add a per-agent skill config UI on the agent detail page.
