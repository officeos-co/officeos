# skill-sdk — @harro/skill-sdk

TypeScript SDK published to npm. Skills import from this to get `defineSkill`, re-exported Zod, and the core type interfaces.

## Commands

```bash
npm install
npm run build        # Compile to dist/
npm run dev          # Watch mode
```

## Project structure

```
src/
  index.ts      Re-exports: defineSkill(), z (Zod), all types
  types.ts      Core interfaces: SkillDefinition, ActionDefinition, SkillContext, CredentialFieldDefinition
  context.ts    SkillContext shape — what gets injected into every execute() call
```

## What this package exports

**`defineSkill(def: SkillDefinition): SkillDefinition`** — identity function that provides TypeScript inference. Skills call this to get typed autocomplete.

**`SkillDefinition.logo`** — **required** raw inline SVG markup (e.g. `<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path d="..."/></svg>`) used as the dashboard logo. Must be inline SVG — not a URL, not a file path, not an emoji. Typically sourced from simpleicons.org.

**`SkillDefinition.emoji`** — optional deprecated fallback. Kept for backwards compatibility; new skills should rely on `logo` only.

**`z`** — Zod re-export. Skills use this for param schemas. Every param must have `.describe()`.

**`SkillContext`** — runtime context injected by skill-runtime into every `execute()` call:
- `credentials: Record<string, string>` — decrypted credential values, keys match the skill's credential schema
- `fetch: typeof globalThis.fetch` — sandboxed fetch, scoped to allowed origins in production
- `log: (...args) => void` — structured logger
- `page?: unknown` — Playwright Page object, only injected for browser skills

**`ActionDefinition<T>`** — one action: `description`, `params` (Zod schema), `returns` (optional Zod schema for GraphQL type generation), `execute(params, ctx)`

**`CredentialFieldDefinition`** — UI metadata for dashboard credential forms: `label`, `kind` (`password`|`text`|`textarea`), `placeholder`, `help`, `required`

## Key rules

- **`returns` on `ActionDefinition` drives GraphQL type generation.** The backend `SkillTypeModule` reads it to build the agent's introspectable schema. Always define it.
- **Every Zod param must have `.describe()`.** The description is what the agent sees in `--help`. Missing descriptions make params invisible to the agent.
- **Breaking `SkillContext` breaks all skills.** Any change to `SkillContext` must be backward compatible or all first-party skills in `packages/skills/` must be updated in the same PR.

## Anti-patterns

- Do not add runtime logic here (fetching, executing, sandboxing). That belongs in `packages/skill-runtime/`.
- Do not add skill implementations here. Those live in `packages/skills/`.
- Do not add Zod validation of credentials here — credentials are validated by the backend before reaching the runtime.
- Do not remove or rename `SkillContext` fields without updating every skill that uses them.
