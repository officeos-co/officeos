# EnterpriseAgentOS Skills

> First-party skill modules that give agents the ability to interact with external services.

## Highlights

- **Single-file skills.** Each skill is one `skill.ts` file using `defineSkill()` from [`@harro/skill-sdk`](../skill-sdk/).
- **Five built-in integrations.** Browser automation, GitHub, Google, Notion, and Obsidian — ready to install from the dashboard.
- **Self-documenting.** Every skill ships a `SKILL.md` that gets injected into the agent's context so it knows exactly how to use each tool.
- **No backend changes needed.** Adding a skill is purely TypeScript. The backend auto-discovers it via the runtime's manifest endpoint.

## Available skills

| Skill | Description | Credentials | Actions |
| --- | --- | --- | --- |
| **browser** | Headless browser automation via Playwright | None (system skill) | `open`, `navigate`, `click`, `fill`, `press`, `scroll`, `screenshot`, `snapshot`, `get_text`, `close` |
| **github** | GitHub repository and issue management | Personal Access Token | `list_repos`, `get_repo`, `list_issues`, `create_issue`, `list_pulls`, `get_pull`, `search_code`, `get_file`, `create_or_update_file` |
| **google** | Google Calendar and Gmail integration | OAuth credentials | `list_events`, `create_event`, `list_messages`, `send_message` |
| **notion** | Notion page and database operations | Integration Token | `search`, `read_page`, `create_page`, `list_blocks`, `add_block`, `update_block`, `delete_block`, `add_todo`, `update_todo`, `query_database` |
| **obsidian** | Obsidian vault operations via CouchDB | Vault URL + credentials | `search`, `read_note`, `create_note`, `update_note`, `list_folder` |

## Overview

Skills are the extension mechanism for EnterpriseAgentOS agents. Each skill exposes a set of actions that agents call through the `skill_exec` tool using a CLI-like syntax:

```
skill_exec("notion search --query 'meeting notes'")
skill_exec("github list_issues --repo my-org/my-repo --state open")
skill_exec("browser open --url 'https://example.com'")
```

The agent never calls skills directly. The flow is:

1. Agent sends a `skill_exec` command to the backend
2. Backend decrypts credentials and forwards the request to the skill runtime
3. Skill runtime validates parameters, creates a sandboxed context, and executes the action
4. Result flows back through the backend to the agent

### System skills

The `browser` skill is a **system skill** — it's always available without manual installation or credentials. The backend auto-includes it in every agent's capability set.

## Adding a new skill

1. Create `packages/skills/{name}/skill.ts`:

```typescript
import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md" with { type: "text" };

export default defineSkill({
  name: "my-service",
  title: "My Service",
  emoji: "🔧",
  description: "Interact with My Service.",
  doc,
  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      required: true,
      placeholder: "sk-...",
      help: "Generate at https://my-service.com/settings/tokens",
    },
  },
  actions: {
    list_items: {
      description: "List all items",
      params: z.object({
        limit: z.number().optional().default(10).describe("Max results"),
      }),
      returns: z.array(z.object({ id: z.string(), name: z.string() })),
      execute: async (params, ctx) => {
        const res = await ctx.fetch("https://api.my-service.com/items?limit=" + params.limit, {
          headers: { Authorization: `Bearer ${ctx.credentials.api_key}` },
        });
        return res.json();
      },
    },
  },
});
```

2. Create `packages/skills/{name}/SKILL.md` with CLI usage docs for the agent.

3. Create `packages/skills/{name}/package.json`:

```json
{
  "name": "@harro/skill-my-service",
  "private": true,
  "dependencies": {
    "@harro/skill-sdk": "file:../../skill-sdk"
  }
}
```

4. Add credential UI metadata to `apps/backend/Entities/Skills/SkillManifests.cs`.

5. Rebuild: `cd packages/skill-runtime && npm run build`

No database migration needed. The `SkillTypeModule` auto-generates GraphQL types from the manifest.

## Feedback and contributing

Found a bug or have a skill request? [Open an issue](https://github.com/harrokrog/EnterpriseAgentOs/issues) on the EnterpriseAgentOS repo.

For the SDK reference, see [`@harro/skill-sdk`](../skill-sdk/).
