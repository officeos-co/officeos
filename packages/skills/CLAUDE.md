# skills — first-party skill packages

Each subdirectory is one TypeScript skill built with `@harro/skill-sdk`. Skills wrap real CLIs, SDKs, or APIs — never prompts or templates.

## Commands

```bash
# Per skill:
cd packages/skills/{name}
npm install
bun test             # Run tests
bun test --watch     # Watch mode

# Build all (handled by CI via skill-runtime):
cd packages/skill-runtime && npm run build
```

## Skill directory structure

Every skill must follow this exact layout:

```
packages/skills/{name}/
  skill.json            Marketplace manifest — single source of truth for metadata
  skill.ts              Entry point — defineSkill() call, imports from skill.json, ~30 lines max
  SKILL.md              Agent-facing documentation (runtime injects into agent context)
  README.md             Human-facing marketplace listing
  CHANGELOG.md          Version history
  LICENSE               License file (MIT for first-party)
  package.json          npm dependencies only — no marketplace metadata
  core/
    client.ts           API client: auth, fetch wrappers, base URLs
    types.ts            Shared TypeScript types
    {domain}.ts         Domain logic: parsing, pagination, transforms
  cli/
    {category}.ts       Thin command layer — Zod schemas + execute functions calling core/
  __tests__/
    {category}.test.ts  Tests using bun:test
```

## skill.json — marketplace manifest

Single source of truth for all skill metadata. The `skill.ts` entry point imports this file and spreads it into `defineSkill()`.

**Fields:**

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | string | yes | Unique skill identifier (kebab-case) |
| `title` | string | yes | Human-readable display name |
| `version` | string | yes | Semver version (e.g. `"1.0.0"`) |
| `description` | string | yes | One-line description for marketplace listing |
| `logo` | string | yes | Inline SVG markup |
| `categories` | string[] | yes | 1–3 from the fixed category list below |
| `keywords` | string[] | no | Freeform search terms, max 30 |
| `author` | `{name, url}` | yes | Primary author |
| `license` | string | yes | SPDX identifier (e.g. `"MIT"`) |
| `repository` | string | no | GitHub URL |
| `contributors` | `[{name, url}]` | no | Additional contributors |
| `credentials` | object | yes | Credential field definitions for the dashboard |

**Fixed categories (use only these):**

Developer Tools, Version Control, Project Management, CRM, Communication, Productivity, Data & Analytics, Cloud Infrastructure, Marketing, Finance, Support, Security, Monitoring, Database, Documents, AI & ML, E-commerce, HR

## Architecture pattern — core/cli separation

`core/` contains real logic: API clients, response parsing, pagination, error handling. It has **no dependency on `@harro/skill-sdk`** — pure TypeScript that could run anywhere.

`cli/` is thin glue: Zod param schemas and `execute` functions that call `core/`. If the skill SDK changes, only `cli/` changes. If the API changes, only `core/` changes.

`skill.ts` only calls `defineSkill()` and spreads action groups from `cli/`. It must stay under ~30 lines.

## skill.ts pattern

```typescript
import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { customers } from "./cli/customers.ts";
import { payments } from "./cli/payments.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...customers, ...payments },
});
```

All metadata (name, title, logo, description, version, categories, credentials, etc.) lives in `skill.json` and is spread into `defineSkill()`. The `skill.ts` file only adds `doc` and `actions`.

## cli/{category}.ts pattern

```typescript
import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet } from "../core/client.ts";

export const customers: Record<string, ActionDefinition> = {
  list_customers: {
    description: "List customers.",
    params: z.object({ limit: z.number().default(10).describe("Max results") }),
    returns: z.array(z.object({ id: z.string(), email: z.string().nullable() })),
    execute: async (params, ctx) => {
      return stripeGet(ctx.fetch, ctx.credentials.secret_key, "/customers", { limit: String(params.limit) });
    },
  },
};
```

## Key rules

- **Every skill wraps a real API, CLI, or SDK.** No prompt-only skills.
- **No channel skills.** Slack, Discord, Teams, Telegram, WhatsApp, Twilio, iMessage — never. Channel integrations are handled elsewhere.
- **`core/` has zero skill-sdk imports.** Only `cli/` touches the SDK. If `core/` imports from `@harro/skill-sdk`, it is wrong.
- **Use `ctx.fetch` for all HTTP.** Never import `node-fetch`, `axios`, or similar — the runtime injects a sandboxed fetch.
- **Use `ctx.credentials` for auth.** Never hardcode keys or read from env vars.
- **Every Zod param must have `.describe()`.** The description is what the agent sees in `--help`. Without it, params are invisible to the agent.
- **`logo` is required inline SVG.** Every `defineSkill` call must include a `logo` field containing raw `<svg>...</svg>` markup (typically sourced from simpleicons.org). Do not use a URL or file path. `emoji` remains as an optional fallback but is deprecated.
- **Spec-driven order:** (1) write `SKILL.md`, (2) write tests, (3) implement `core/`, (4) wire `cli/`.

## File size limits

- `skill.ts`: ~30 lines
- `core/client.ts`: 50–100 lines
- `core/{domain}.ts`: 50–200 lines
- `cli/{category}.ts`: 80–150 lines
- `__tests__/{category}.test.ts`: 50–200 lines

If any file exceeds 200 lines, split it.

## Anti-patterns

- Do not add skill logic to `skill.ts` beyond the `defineSkill()` call. Logic belongs in `core/`.
- Do not import `@harro/skill-sdk` in `core/` — that breaks the separation and prevents reuse.
- Do not create skills that only wrap prompts or instructions. If there is no real API call, it is not a skill.
- Do not use `ctx.credentials` in `core/` — credentials are injected by the runtime into `ctx` in `cli/execute()` and passed explicitly to `core/` functions.
- Do not skip `returns` on action definitions. The backend `SkillTypeModule` uses it to generate GraphQL return types for agent introspection.
- Do not write `.describe("")` with an empty string. If you cannot describe the param, the param design is wrong.
