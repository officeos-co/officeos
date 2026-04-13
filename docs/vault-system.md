# Vault System

> How agent identity files flow from templates → CouchDB → agent pod, and the knowledge graph philosophy behind it.

## Overview

Every agent has a **vault** — a set of personality files that define who the agent is, how it behaves, and what it knows. The vault lives in CouchDB as the source of truth, gets cached on the agent pod's PVC for fast boot, and is editable from the dashboard's Memory tab.

```
Templates (C# code)                    CouchDB                        Agent Pod PVC
   │                                     │                               │
   │  Agent created →                    │                               │
   │  Render SOUL.md, IDENTITY.md,       │                               │
   │  AGENTS.md with agent name/id       │                               │
   │ ──────────────────────────────────▶ │                               │
   │                                     │  PUT eaos-agent-{uuid}/       │
   │                                     │    SOUL.md                    │
   │                                     │    IDENTITY.md                │
   │                                     │    AGENTS.md                  │
   │                                     │                               │
   │                                     │  Pod boots →                  │
   │                                     │ ─────────────────────────────▶│
   │                                     │  vault_bootstrap hydrates     │
   │                                     │  /zeroclaw-data/workspace/    │
   │                                     │                               │
   │                                     │  Dashboard edits →            │
   │                                     │◀─── PUT {db}/{file} ─────────│
   │                                     │  (next pod restart picks up)  │
```

## Knowledge graph philosophy

The vault system is inspired by the Obsidian "second brain" approach. The core insight:

> **Knowledge is unstructured.** You can't organize information by rigid folders or categories. Knowledge is defined by its _relations_ to other knowledge — not by where you file it.

This translates to the agent vault design:

### Categories, not folders

Each vault file has a clear **identity** (SOUL = core values, IDENTITY = who I am, AGENTS = how I operate). These are categories, not a filesystem hierarchy. An agent's personality is defined by the combination of these files, not by directory structure.

### Tags over taxonomy

When agents eventually get richer vaults (memory files, daily notes, project context), the relationship between notes matters more than their folder. A note about "Q1 planning" might relate to both "work" and "strategy" — it gets tags, not a single folder.

### Graph-first discovery

The goal is for agents to navigate knowledge by following connections, not by browsing folders:

- `SOUL.md` references `MEMORY.md` for evolving preferences
- `AGENTS.md` references `IDENTITY.md` for role context
- Memory entries link to each other by topic and date

### Text first, structure later

Don't over-organize prematurely. Write first, structure when patterns emerge:

- Start with a plain note
- Only extract into a separate file when the concept is referenced from multiple places
- Only add tags when a clear category pattern exists across many notes
- A 500-line note is fine until you actually need to split it

### Planned vault structure

As the vault evolves beyond the three required personality files:

```
/workspace/
├── SOUL.md           Required. Stable core identity. Rarely changes.
├── IDENTITY.md       Required. Name, provider, purpose, personality.
├── AGENTS.md         Required. How I operate. Session checklist, safety rules.
├── MEMORY.md         Curated long-term memory. Distilled from daily notes.
├── USER.md           Who the human operator is. Preferences, context.
├── TOOLS.md          Environment-specific notes (hosts, paths, conventions).
├── HEARTBEAT.md      Heartbeat instructions for periodic check-ins.
├── memory/
│   └── YYYY-MM-DD.md Daily notes. Raw session logs. Text dump first.
└── (future)
    ├── projects/     Per-project context, linked by tags
    └── references/   External knowledge, not authored by the agent
```

**Categories** map to file types: `SOUL` is identity, `memory/` is temporal, `references/` is external. Each file gets a `category` header.

**Tags** are used sparingly and only when a note genuinely spans multiple categories. A tag should be added when you see the pattern recurring, not preemptively.

## CouchDB structure

Each agent gets its own CouchDB database: `eaos-agent-{uuid}` (lowercase).

Documents are simple:

```json
{
  "_id": "SOUL.md",
  "_rev": "3-abc123...",
  "content": "# Soul\n\nI am Agent-1, a helpful..."
}
```

The `content` field holds the raw markdown. CouchDB manages revisions via `_rev`. No Obsidian LiveSync chunking — plain JSON documents.

## Lifecycle

### Creation

`CouchDbVaultClient.CreateAgentVaultAsync`:

1. `PUT /{db}` — create database (412 = already exists, idempotent)
2. For each template file: `PUT /{db}/{filename}` with `{"content": "..."}`
3. Templates rendered from `Entities/Vault/Templates/*.md` with token substitution (`{{agent_name}}`, `{{agent_id}}`, etc.)

### Boot hydration

`vault_bootstrap::hydrate` (Rust):

1. Check if `ZEROCLAW_AGENT_ID` is set → fetch from backend memory proxy
2. `GET /api/agents/{id}/memory/SOUL.md` (etc.) — backend reads from CouchDB, returns raw text
3. Write to `/zeroclaw-data/workspace/SOUL.md`
4. **Cache**: if all required files already exist on the PVC, skip the fetch entirely

### Dashboard editing

Memory tab in agent detail page:

1. `GET /api/agents/{id}/memory` — lists all files (CouchDB `_all_docs`)
2. Click file → `GET /api/agents/{id}/memory/SOUL.md` — read content
3. Edit → `PUT /api/agents/{id}/memory/SOUL.md` — write back to CouchDB
4. Changes take effect on next pod restart (PVC cache is stale until then)

### Deletion

`CouchDbVaultClient.DeleteAgentVaultAsync`:

- `DELETE /{db}` — removes the entire per-agent database
- 404 is idempotent (already deleted)

## Cache invalidation

The PVC cache is a **boot-time optimization**, not a sync mechanism:

- **Cache hit**: all required files exist non-empty on PVC → skip CouchDB fetch, boot in <100ms
- **Cache miss**: any required file missing → fetch from CouchDB/backend, write to PVC
- **Stale cache**: dashboard edits update CouchDB but NOT the PVC. Pod must restart to pick up changes.

Future improvement: add a `_rev` check on boot — compare CouchDB rev with a cached rev file on PVC, re-fetch only if changed.

## Key files

| File                                                         | Purpose                                              |
| ------------------------------------------------------------ | ---------------------------------------------------- |
| `apps/backend/Entities/Vault/CouchDbVaultClient.cs`       | CouchDB CRUD (create DB, put/get/delete docs)        |
| `apps/backend/Entities/Vault/VaultPersonalityTemplate.cs` | Renders templates with token substitution            |
| `apps/backend/Entities/Vault/Templates/*.md`              | SOUL.md, IDENTITY.md, AGENTS.md templates            |
| `packages/zeroclaw-core/src/agent/vault_bootstrap.rs`        | Fetches vault from backend, caches on PVC            |
| `packages/zeroclaw-core/src/agent/personality.rs`            | Strict file loader (fails if required files missing) |
| `apps/dashboard/src/components/AgentMemoryPanel.tsx`       | Dashboard vault editor                               |
