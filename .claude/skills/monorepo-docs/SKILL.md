---
name: monorepo-docs
description: Style guide for writing CLAUDE.md documentation in complex monorepos — structure, rules, and anti-patterns for agent-facing docs
when_to_use: when writing, auditing, or improving CLAUDE.md files in a monorepo
---

You are writing documentation for an AI agent, not a human developer. The test is: can an agent read only these files and immediately work in any part of the codebase without reading source? If not, the docs have failed.

---

## What agent-facing docs are

Agent-facing docs (CLAUDE.md, AGENTS.md) are operational manuals. Every sentence is something the agent acts on — a command to run, a rule to follow, a pattern to use, a thing not to do. They are not:

- Marketing copy ("powerful platform for...")
- Origin stories ("we built this because...")
- Philosophy ("we believe clean code...")
- Tutorial prose ("first, let's understand...")
- Aspirations ("in the future we plan to...")

If a sentence does not change agent behavior, delete it.

---

## Documentation hierarchy

```
CLAUDE.md (root)
├── apps/dashboard/CLAUDE.md
├── apps/backend/CLAUDE.md
├── apps/website/CLAUDE.md
└── packages/*/CLAUDE.md
```

**Root** = the entry point. Tells the agent what exists and where to go. Does not duplicate sub-package detail.  
**Sub-package** = self-contained operational manual for that package. An agent working in `apps/backend/` should only need the root + `apps/backend/CLAUDE.md`.

No content should appear in two places. If it belongs in a sub-package, delete it from the root.

---

## Root CLAUDE.md — required sections

### 1. System mental model (required, max 20 lines)

A diagram or structured paragraph showing how data flows end-to-end. Must answer:
- Which app does the user interact with?
- Which service is the orchestrator?
- What does each piece own?
- How do the pieces connect?

Use a code block with ASCII arrows. Never prose alone — a diagram communicates faster.

### 2. Sub-CLAUDE index (required)

A table mapping every package to its CLAUDE.md. The agent uses this to know where to look. Format:

```
| Package | Role | CLAUDE.md |
|---------|------|-----------|
| apps/dashboard/ | Main product UI | apps/dashboard/CLAUDE.md |
```

Include a one-line role description so the agent can pick the right file without reading all of them.

### 3. CI/CD table (required)

Every workflow listed. Format:

```
| Workflow | Triggers on | Builds | Deploys to |
|----------|-------------|--------|-----------|
```

An agent touching any file must know whether that file triggers a deploy and what happens.

### 4. Conventions (required)

Only global conventions that apply across multiple packages:
- Commit style
- Branch naming
- Image registry and tag pattern
- Prod hostnames
- Schema migration rule
- Any cross-cutting must-do

Do not list conventions that only apply to one package — put those in the sub-package CLAUDE.md.

### 5. What NOT to include in root

- Commands for specific packages (goes in sub-package)
- File structure of specific packages (goes in sub-package)
- Rules that only apply to one package (goes in sub-package)
- Anything you would also write in a sub-package

---

## Sub-package CLAUDE.md — required sections

### 1. Header (required)

One sentence. Tech stack + what this package does in the system.  
`# backend — C# ASP.NET Core 9. Central orchestrator: agent lifecycle, LLM proxy, skill gateway, K8s control.`

### 2. Commands (required)

Every command the agent would run. Exact syntax, no placeholders. Copy-pasteable.

```bash
npm install
npm run dev          # port and what it starts
npx tsc --noEmit     # type check only
npm test             # test runner
```

If there is a build step required before test, show it. If there is a watch mode, show it.

### 3. Project structure (required)

A directory tree. Every non-obvious entry gets a one-line description on the same line. Obvious entries (`node_modules/`, `dist/`) can be omitted. The agent uses this to know where to put new code and where to find existing code.

```
src/
  app/
    layout.tsx          Root layout — always server component
    agents/
      page.tsx          Agent list with 10s polling
```

### 4. Architecture patterns (required for complex packages)

How data flows through this package. Which layer owns what. Where logic lives, where it must not live. This is the section that prevents misplaced code.

Examples of what belongs here:
- "All fetching through hooks — never raw fetch in components"
- "Agent data comes from backend, pod data comes from pod proxy. Do not mix."
- "SkillTypeModule generates the GraphQL schema dynamically — no hardcoded skill logic in C#"

### 5. Key rules (required, ≥3)

Positive obligations. What the agent must do. Use bold for the rule name, then explain why.

**Format:**  
`- **Rule name**: explanation`

Prefer rules that are non-obvious — things the agent would not naturally do correctly without being told.

### 6. Anti-patterns (required, ≥3)

Explicit prohibitions. Start every item with "Do not". Target the exact thing an LLM would naturally do wrong in this package.

**Good anti-pattern:** Specific to this package, targets a real failure mode.  
`- Do not fetch from agent pods for data the backend owns (skills, providers, agent records). The pod only owns chat, logs, memory, and health.`

**Bad anti-pattern:** Generic software advice not specific to this package.  
`- Do not use magic numbers.`  
`- Do not write long functions.`

---

## Writing anti-patterns

Anti-patterns are the most important section. They are explicit prohibitions targeting where an LLM would naturally fail.

Ask: "What would an LLM confidently do here that would be wrong?" That is your anti-pattern.

Common LLM failure modes to target:
- Putting logic in the wrong layer ("I'll add this validation to the component")
- Hardcoding things that should be injected ("I'll just put the API key in the config file")
- Reaching for external state libraries when the pattern is hooks-only
- Calling the wrong API ("I'll fetch skills from the pod since I'm in a pod context")
- Adding abstractions for one-use code ("I'll create a util for this")
- Breaking security invariants without realizing ("I'll add the credential to the env var")

---

## Guardrails for writing these docs

- Do not write marketing language in any CLAUDE.md. If the current file has it, delete it.
- Do not duplicate content across files. Pick one file and delete from the other.
- Do not write anti-patterns that are generic advice. Every "Do not" must be specific to this package and this codebase.
- Do not invent commands. Read `package.json`, `Cargo.toml`, `Makefile`, or the CI workflows to find real commands.
- Do not explain the why of the product in docs. Explain the why of the rule.
- Do not write "we" — write "the agent" or use imperative voice.
- Do not write partial docs. A section with one anti-pattern is worse than a honest note that anti-patterns are not yet documented.
- Do not put `docs/` content in CLAUDE.md files. `docs/` is for human-facing external documentation.
