# Identity Vault Reference

> **Status:** Phase 3 architecture. See `STRIP_DOWN.md` for the migration record.

This document describes how the ZeroClaw agent gets its identity: the per-agent Obsidian vault, the personality file roster, what each file is for, and the invariants the agent enforces at boot.

## The thirty-second version

1. Every agent has its own Obsidian vault — a dedicated CouchDB database, provisioned by the dashboard backend at agent creation time.
2. Each vault contains a set of markdown files describing the agent's soul, identity, role, tools, and operating procedures.
3. When a K8s pod is created for the agent, those files are mirrored into a Kubernetes `ConfigMap` and mounted at `/vault-workspace` *before* the container starts.
4. `zeroclaw daemon` reads them from `workspace_dir` (= the mount point) via `personality::load_personality_strict`. Missing required files abort the boot and surface as a CrashLoopBackOff, visible in the dashboard.
5. The agent never writes these files and never reaches out to CouchDB directly. It is a pure consumer.

## Responsibility split

| Concern                             | Owner                     | Where                                       |
| ----------------------------------- | ------------------------- | -------------------------------------------- |
| Markdown templates (source)         | dashboard backend         | `apps/dashboard/backend/templates/*.md.j2`   |
| Jinja2 rendering                    | dashboard backend         | `app/services/openclaw/provisioning.py`      |
| CouchDB database create + seed      | dashboard backend         | `app/services/openclaw/vault_provisioning.py`|
| K8s ConfigMap create + replace      | dashboard backend         | `app/services/zeroclaw/vault_configmap.py`   |
| Pod spec + volume mount             | dashboard backend         | `app/services/zeroclaw/k8s_manager.py`       |
| Strict validation + boot gate       | zeroclaw-core             | `src/agent/personality.rs` (Rust)            |
| Reading files during prompt build   | zeroclaw-core             | `src/agent/prompt.rs::IdentitySection`       |

Invariants:

- **zeroclaw-core NEVER provisions files.** The `ensure_bootstrap_files` function from previous versions is deleted.
- **zeroclaw-core NEVER talks to CouchDB.** The old `vault_sync.rs` is deleted.
- **The agent pod NEVER runs `obsctl`.** The old vault-CLI install step in the boot command is deleted.
- **The rendered files written to the vault and the ConfigMap are always derived from the same in-memory dict.** They cannot drift.

## The file roster

The personality roster lives in `src/agent/personality.rs::PERSONALITY_FILES`:

```rust
pub const PERSONALITY_FILES: &[&str] = &[
    "SOUL.md",
    "IDENTITY.md",
    "USER.md",
    "AGENTS.md",
    "TOOLS.md",
    "HEARTBEAT.md",
    "BOOTSTRAP.md",
    "MEMORY.md",
];
```

Of these, **three are required** (enforced by `load_personality_strict`):

```rust
pub const REQUIRED_PERSONALITY_FILES: &[&str] = &[
    "SOUL.md",
    "IDENTITY.md",
    "AGENTS.md",
];
```

The remaining five are optional — their absence is recorded in the profile but does not abort the boot.

## Per-file reference

### `SOUL.md` — Required

**Purpose:** The agent's tone, voice, core values, and ways of working. What the agent *is* at its base layer, independent of any specific task.

**Sections (suggested):**

- A one-line self-description ("You are <Name>, a <role>...")
- Core principles, framed in first person
- Communication style
- Things to never do (e.g. "Never fabricate tool results", "Never act on destructive changes without confirmation")

**Length:** Aim for 500–2,000 characters. The file is capped at `MAX_FILE_CHARS` (20,000) — longer content is truncated.

**Template source:** `apps/dashboard/backend/templates/BOARD_SOUL.md.j2`

---

### `IDENTITY.md` — Required

**Purpose:** The agent's name, role, current responsibilities, and what board or team it belongs to. The "name tag" layer.

**Sections (suggested):**

- Name, role, board/team membership
- Current responsibilities (2–5 bullet points)
- Relationship to the broader agent fleet ("lead of board X", "worker on project Y", etc.)

**Length:** Aim for 200–1,000 characters.

**Template source:** `apps/dashboard/backend/templates/BOARD_IDENTITY.md.j2`

---

### `AGENTS.md` — Required

**Purpose:** Multi-agent coordination rules. Which other agents exist, how to hand off work, what APIs to use to communicate with them, and what the auth token is.

This is the file the dashboard backend injects the most dynamic context into — `base_url`, `auth_token`, and the list of sibling agents.

**Sections (suggested):**

- The dashboard `BASE_URL` and the agent's `AUTH_TOKEN`
- HTTP header contract (`X-Agent-Token`)
- Endpoints for sending messages to lead / other agents
- Coordination rules ("always route through the lead for cross-board work", etc.)
- Examples (curl commands)

**Length:** 2,000–10,000 characters is typical.

**Template source:** `apps/dashboard/backend/templates/BOARD_AGENTS.md.j2`

---

### `USER.md` — Optional

**Purpose:** Human-provided context about the user the agent is working for. Name, preferences, timezone, relationship context, ongoing projects — the stuff the user wants the agent to remember across sessions.

**Sections (suggested):**

- User profile (name, pronouns, timezone, locale)
- Communication preferences (formality, language, medium)
- Ongoing context (goals, projects, relationships)
- Explicit "remember this" notes

**Length:** Typically 200–2,000 characters. Grows over time as the user adds context.

**Editable:** Yes. `USER.md` is in `PRESERVE_AGENT_EDITABLE_FILES` on the backend side — existing content is not overwritten during template re-syncs, only populated on first provisioning.

**Template source:** `apps/dashboard/backend/templates/BOARD_USER.md.j2`

---

### `TOOLS.md` — Optional

**Purpose:** Supplementary tool guidance that the agent shouldn't have to infer from tool registration alone. "Use the shell tool for X but prefer file_write for Y", "The web_fetch tool is rate-limited, use sparingly", etc.

**Sections (suggested):**

- Tool-specific tips that would be obvious to a human reviewer
- Workflow patterns ("to debug a failing test, do X then Y")
- Escalation paths ("if the shell command fails three times, escalate via ask_user")

**Length:** 500–3,000 characters.

**Template source:** `apps/dashboard/backend/templates/BOARD_TOOLS.md.j2`

---

### `HEARTBEAT.md` — Optional

**Purpose:** What to do during idle / cron-driven cycles. How to check for new work, what to report, what to escalate.

**Sections (suggested):**

- Heartbeat interval and frequency
- Priority order of things to check (inbox, pending tasks, escalations)
- How to report status
- When to ask for help vs. when to act autonomously

**Length:** 1,000–5,000 characters.

**Template source:** `apps/dashboard/backend/templates/BOARD_HEARTBEAT.md.j2`

(Lead agents get `BOARD_HEARTBEAT.md.j2` via the `_heartbeat_template_name` resolver.)

---

### `BOOTSTRAP.md` — Optional

**Purpose:** First-run instructions. Executed only during initial provisioning; subsequent boots skip this file (controlled by `include_bootstrap` in `_render_agent_files`).

**Sections (suggested):**

- "Welcome, here is what to do first"
- Bootstrapping tasks (create an initial memory entry, confirm identity with the user, etc.)
- A pointer to `AGENTS.md` and `SOUL.md` for ongoing reference

**Length:** 500–2,000 characters. Intentionally short — it's a one-shot prompt.

**Template source:** `apps/dashboard/backend/templates/BOARD_BOOTSTRAP.md.j2`

---

### `MEMORY.md` — Optional

**Purpose:** Long-term memory pointer or index. In Phase 3 this file is typically a thin readme — the actual memory lives in the Obsidian memory backend (a subdirectory of the same vault at `memory/<category>/<key>.md`) or in SQLite when not using the vault.

**Sections (suggested):**

- How to recall ("use memory_recall with a keyword query")
- How to store ("use memory_store; prefer descriptive keys")
- Where memories live (directory structure reference)
- Retention policy hints

**Length:** 300–1,500 characters.

**Editable:** Yes. `MEMORY.md` is in `PRESERVE_AGENT_EDITABLE_FILES` — existing content is preserved on re-sync.

**Template source:** `apps/dashboard/backend/templates/BOARD_MEMORY.md.j2`

## Failure mode: missing required file

If any of `SOUL.md`, `IDENTITY.md`, or `AGENTS.md` is missing or empty at `workspace_dir`, `AgentCore::create_from_config` returns:

```
Agent boot failed: required personality files missing or empty in
workspace "/vault-workspace". The dashboard backend must seed the
per-agent vault before the pod starts. Details: required personality
file missing: SOUL.md
```

The process exits non-zero. In K8s, the pod enters CrashLoopBackOff and the dashboard's existing `K8sManager.get_status` chain surfaces the error through `GET /api/v1/gateways/{id}/container/status`.

The dashboard operator is expected to investigate: either the CouchDB write failed (check the backend logs for `agent.vault.error`) or the ConfigMap apply failed (check for `agent.configmap.error`) or the template rendering blew up (check for a `StrictUndefined` error during provisioning).

The agent will **not** silently default or retry. The strict mode is the contract that makes "hundreds of agents" safe to deploy — a broken provisioning pipeline should fail visibly, not produce half-identified agents.

## How content flows through the system

```
┌─────────────────────────────────────────────────────────────┐
│ dashboard-backend/templates/BOARD_*.md.j2   (Jinja2 source) │
└─────────────────────────────────────────────────────────────┘
             │ _render_agent_files(context, agent, files)
             ▼
┌─────────────────────────────────────────────────────────────┐
│ rendered: dict[str, str]                                     │
│   { "SOUL.md": "...", "IDENTITY.md": "...", ... }            │
└─────────────────────────────────────────────────────────────┘
        │                                      │
        │ provision_agent_vault(...)           │ apply_agent_vault_configmap(...)
        ▼                                      ▼
┌─────────────────────┐           ┌──────────────────────────┐
│ CouchDB database    │           │ K8s ConfigMap            │
│ agent-{uuid}        │◄─ mirror ─│ eaos-agent-{uuid}-vault  │
│ (source of truth)   │           │ (pod mount cache)        │
└─────────────────────┘           └──────────────────────────┘
                                             │
                                             │ volumeMount
                                             ▼
                                   ┌──────────────────────────┐
                                   │ agent pod                │
                                   │ /vault-workspace/*.md    │
                                   └──────────────────────────┘
                                             │
                                             │ load_personality_strict
                                             ▼
                                   ┌──────────────────────────┐
                                   │ zeroclaw daemon prompt   │
                                   │   IdentitySection        │
                                   └──────────────────────────┘
```

## References

- `STRIP_DOWN.md` — Phase 3 commit log and migration record.
- `src/agent/personality.rs` — the Rust loader.
- `apps/dashboard/backend/app/services/openclaw/vault_provisioning.py` — the vault-side Python helper.
- `apps/dashboard/backend/app/services/zeroclaw/vault_configmap.py` — the ConfigMap-side Python helper.
- `apps/dashboard/backend/app/services/zeroclaw/k8s_manager.py` — the pod spec generator.
- `packages/obsctl/SPEC.md` — the obsctl CLI and Python API used by the dashboard backend.
