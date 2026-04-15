# Skills Package

## Skill directory structure

Every skill must follow this structure. Separate **core logic** from **command definitions** — same pattern as [obsctl](https://github.com/HarKro753/obsctl/tree/main/packages/cli/vault_cli).

```
packages/skills/{name}/
├── package.json          # Self-contained: bun test, @harro/skill-sdk dep
├── SKILL.md              # CLI-style spec for agents (what the agent sees)
├── REFERENCES.md         # Source OSS repo, license, npm package, docs link
├── skill.ts              # Entry point — defineSkill(), imports commands from cli/
├── core/                 # Business logic — no skill-sdk knowledge
│   ├── client.ts         # API client: auth, fetch wrappers, base URLs
│   ├── types.ts          # Shared types and interfaces
│   └── {domain}.ts       # Domain logic: parsing, transforms, pagination, etc.
├── cli/                  # Thin command layer — imports core, wires to skill actions
│   ├── {category}.ts     # Action definitions grouped by domain
│   └── ...               # Each exports Record<string, ActionDefinition>
└── __tests__/
    ├── {category}.test.ts  # Tests per cli/ file, using bun:test
    └── ...
```

### Why core/ and cli/ separation

`core/` contains the real logic: API clients, response parsing, pagination, error handling, data transforms. It has **no dependency on @harro/skill-sdk** — it's pure TypeScript that could be reused anywhere.

`cli/` is thin glue: Zod schemas, descriptions, and execute functions that call into core. If the Zod schema or skill SDK changes, only cli/ changes. If the API changes, only core/ changes.

### Entry point (`skill.ts`) — ~30 lines max

Only the `defineSkill()` call. Imports and spreads command groups from cli/:

```typescript
import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { customers } from "./cli/customers.ts";
import { payments } from "./cli/payments.ts";
import { subscriptions } from "./cli/subscriptions.ts";

export default defineSkill({
  name: "stripe",
  title: "Stripe",
  emoji: "💳",
  description: "Stripe payments — charges, subscriptions, customers, and invoices.",
  doc,
  credentials: {
    secret_key: { label: "Secret Key", kind: "password", placeholder: "sk_...", help: "..." },
  },
  actions: { ...customers, ...payments, ...subscriptions },
});
```

### Core (`core/`) — business logic

No skill-sdk imports. Pure functions that take typed inputs and return typed outputs.

```typescript
// core/client.ts
export const BASE = "https://api.stripe.com/v1";

export async function stripeGet(fetch: typeof globalThis.fetch, key: string, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await fetch(`${BASE}${path}${qs}`, {
    headers: { Authorization: `Bearer ${key}` },
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(`Stripe ${res.status}: ${err?.error?.message ?? res.statusText}`);
  }
  return res.json();
}

// core/pagination.ts
export async function autopaginate<T>(fetchPage: (cursor?: string) => Promise<{ data: T[]; has_more: boolean; next_cursor?: string }>) {
  const all: T[] = [];
  let cursor: string | undefined;
  do {
    const page = await fetchPage(cursor);
    all.push(...page.data);
    cursor = page.has_more ? page.next_cursor : undefined;
  } while (cursor);
  return all;
}
```

### CLI (`cli/{category}.ts`) — thin command glue

Imports core, defines Zod schemas and execute functions. Each file exports a record matching SKILL.md sections.

```typescript
// cli/customers.ts
import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { stripeGet, stripePost } from "../core/client.ts";

export const customers: Record<string, ActionDefinition> = {
  list_customers: {
    description: "List customers.",
    params: z.object({ limit: z.number().default(10).describe("Max results") }),
    returns: z.array(z.object({ id: z.string(), email: z.string().nullable() })),
    execute: async (params, ctx) => {
      const data = await stripeGet(ctx.fetch, ctx.credentials.secret_key, "/customers", { limit: String(params.limit) });
      return data.data;
    },
  },
};
```

### Tests (`__tests__/{category}.test.ts`)

One test file per cli/ file. Use `bun:test`. Test both core logic and cli wiring.

```typescript
import { describe, it, expect } from "bun:test";

describe("customers", () => {
  describe("list_customers", () => {
    it.todo("should call /v1/customers with limit param");
    it.todo("should return mapped customer array");
    it.todo("should parse Stripe error response on failure");
  });
});
```

### File size guidelines

- `skill.ts`: ~30 lines (entry point only)
- `core/client.ts`: ~50-100 lines (API helpers)
- `core/{domain}.ts`: ~50-200 lines (domain logic)
- `cli/{category}.ts`: ~80-150 lines (action definitions)
- `__tests__/{category}.test.ts`: ~50-200 lines (tests)
- If any file exceeds 200 LOC, split it further

## Known issues

None currently.

## Rules

- Every skill must wrap a real CLI, SDK, or API — no prompt-only skills
- No channel-specific skills (Slack, Discord, Teams, Telegram, etc.) — native channel integrations handle messaging
- Spec-driven workflow: (1) SKILL.md spec, (2) tests, (3) implement
- Use `bun test` for testing, not vitest/jest
- Use `ctx.fetch` for all HTTP calls — never import http clients directly
- Use `ctx.credentials` for auth — never hardcode keys
- Every Zod param must have `.describe()` for agent discoverability
- Import doc from `"./SKILL.md"` — the SKILL.md is the agent-facing documentation
- `core/` must not import from `@harro/skill-sdk` — keep it pure TypeScript
- `cli/` is the only layer that touches the skill SDK
