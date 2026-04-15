---
title: "Skill SDK and Runtime Overhaul"
date: "2026-04-01"
tags: ["Skills", "SDK", "Runtime"]
version: "0.5"
---

Major improvements to how skills are defined, validated, and executed.

## Skill SDK (`@harro/skill-sdk`)

- **`defineSkill` API** — single function to declare a skill with name, description, parameters (Zod schema), and action handler
- **Type-safe parameters** — Zod schemas provide runtime validation and TypeScript inference
- **Credential injection** — skills receive decrypted credentials via context, never handle raw API keys
- **Sandboxed fetch** — skills get a scoped `fetch` function that respects rate limits and timeouts

## Skill Runtime

- **Dynamic schema generation** — backend reads skill manifests and generates HotChocolate GraphQL types at runtime, no hardcoded skill knowledge in C#
- **Manifest seeding** — CI pushes skill manifests to the backend via `POST /api/internal/seed-manifests` on every deploy
- **Improved error handling** — skill execution errors now include structured error codes and messages returned to the agent

## First-party skills added

- Google Calendar (read, create, update events)
- Gmail (read, search, send emails)
- Linear (issues, projects, cycles)
- Web search and page fetch
