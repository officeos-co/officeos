# @harro/skill-sdk

> TypeScript SDK for defining EnterpriseAgentOS skills — type-safe, declarative, zero boilerplate.

## Highlights

- **One function, one skill.** `defineSkill()` is the entire API. No classes, no decorators, no lifecycle hooks.
- **Schema-driven.** Parameters and return types are Zod schemas. The runtime auto-generates JSON Schema for GraphQL introspection and CLI `--help`.
- **Credentials injected, never stored.** Skills declare what credentials they need. The runtime decrypts and injects them at execution time.
- **Sandboxed execution.** Skills run in an isolated context with a scoped `fetch`, structured logger, and optional Playwright `page`.

## Overview

`@harro/skill-sdk` is the contract between skill authors and the EnterpriseAgentOS skill runtime. A skill is a single TypeScript file that exports a `defineSkill()` call. The runtime bundles it with esbuild, loads it at boot, and exposes its actions through a GraphQL gateway that agents discover via introspection.

Skills never talk to the database, never hold API keys, and never import Node.js internals. They receive a `SkillContext` with everything they need and return plain objects.

This package is part of [EnterpriseAgentOS](https://github.com/harrokrog/EnterpriseAgentOs).

## Usage

```typescript
import { defineSkill, z } from "@harro/skill-sdk";

export default defineSkill({
  name: "weather",
  title: "Weather",
  emoji: "🌤️",
  description: "Look up current weather for a location.",
  doc: `# Weather Skill

## Commands
- \`weather check --location "Berlin"\` — current conditions

## Notes
Uses the free wttr.in API. No API key required.`,

  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      required: false,
      placeholder: "Optional — uses free tier without it",
      help: "Premium API key for higher rate limits.",
    },
  },

  actions: {
    check: {
      description: "Get current weather for a location",
      params: z.object({
        location: z.string().describe("City name or coordinates"),
      }),
      returns: z.object({
        temperature: z.string(),
        condition: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `https://wttr.in/${encodeURIComponent(params.location)}?format=j1`
        );
        const data = await res.json();
        return {
          temperature: data.current_condition[0].temp_C + "°C",
          condition: data.current_condition[0].weatherDesc[0].value,
        };
      },
    },
  },
});
```

### Key types

| Type | Purpose |
| --- | --- |
| `SkillDefinition` | Top-level skill shape: name, title, emoji, description, doc, credentials, actions |
| `ActionDefinition<T>` | Single action: description, `params` (Zod), optional `returns` (Zod), `execute` |
| `SkillContext` | Injected runtime context: `credentials`, `fetch`, `log`, optional `page` |
| `CredentialFieldDefinition` | Dashboard form metadata: label, kind, required, placeholder, help |

### The `doc` field

The `doc` string is injected into the agent's system prompt. It's the primary way an agent learns *how* to use your skill. Write it as a CLI reference: list commands, show examples, note limitations. Markdown is supported.

## Installation

```bash
npm install @harro/skill-sdk
```

Requires Node.js 22+ and TypeScript 5.7+.

## Feedback and contributing

Found a bug or want a new feature? [Open an issue](https://github.com/harrokrog/EnterpriseAgentOs/issues) on the EnterpriseAgentOS repo.

To add a new skill, see the [Adding a new skill](https://github.com/harrokrog/EnterpriseAgentOs/blob/main/docs/skills.md) guide.
