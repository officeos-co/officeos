# ZeroClaw Strip-Down Plan

**Goal:** Reduce zeroclaw-core to the minimum surface required for the Office OS
vision (K8s-native agent runtime, Obsidian/CouchDB memory, AIEOS identity from
config). Remove inherited legacy from the OpenClaw → RustyClaw → ZeroClaw fork
chain that we don't use, can't read, or that duplicates newer subsystems.

**Non-goals:** Redesign. This document covers *removal* and *documentation* only.
Architectural redesigns happen on the cleaned-up base in separate PRs.

**Baseline:** ~292,000 lines of Rust across 36 src modules, 2 crates,
~45 channels, ~17 providers, ~95 tools. Target: <60,000 lines.

## Legend

- **DELETE** — remove entirely; not used in Office OS, not aligned with vision
- **KEEP** — load-bearing for Office OS; survives strip-down
- **COLLAPSE** — keep behavior, reduce abstraction (inline single-impl traits, etc.)
- **DECIDE** — needs a deliberate yes/no before phase 2

## Top-level repo

| Path | LOC | Verdict | Reason |
|---|---|---|---|
| `crates/robot-kit` | ~3k | **DELETE** | Toy Pi robot demo. Not registered in core. Office OS is server-side. |
| `crates/aardvark-sys` | ? | **DECIDE** | Unknown purpose; check if any src module imports it. |
| `apps/` | ? | **DECIDE** | Inspect — likely demo apps. |
| `firmware/` | ? | **DELETE** | Embedded firmware, unrelated to server agents. |
| `benches/` | ? | **DECIDE** | Keep if benchmarks cover modules we keep; delete otherwise. |
| `fuzz/` | ? | **DECIDE** | Same logic as benches. |
| `tool_descriptions/` | ? | **DECIDE** | Probably tied to tools we'll delete. |
| `marketplace/` | ? | **DELETE** | Placeholder for ClawHub-that-doesn't-exist-yet. |
| `web/` | ? | **DECIDE** | If it's the dashboard, keep; if a separate web client, decide. |
| `docs/i18n/` | ~30 langs | **DELETE** | 30 stale translations of one paragraph. Re-add when there are non-English users. |
| `Dockerfile.debian`, `.ci` variants | — | **DECIDE** | Pick one Dockerfile, delete the rest. |
| `setup.bat`, `install.sh` (Win bits) | — | **DECIDE** | Office OS is K8s; Windows bootstrap is dead weight. |

## src/ modules (sorted by LOC)

| Module | LOC | Verdict | Reason |
|---|---|---|---|
| `channels/` | 60,570 | **DELETE most** | 45 channels — keep only Telegram + REST/webhook for MVP. Delete the other ~43. |
| `tools/` | 58,019 | **DELETE most** | 95 tools — keep memory_*, file_*, shell, http_request, mcp_*, schema, traits, mod. Delete linkedin, notion, jira, google_workspace, microsoft365, weather, twitter, etc. |
| `providers/` | 27,666 | **DELETE most** | 17 providers — keep anthropic + ollama + openai + reliable + router + traits + mod. Delete bedrock, azure, gemini, glm, kilocli, telnyx, openrouter, copilot, claude_code, codex, gemini_cli, compatible. |
| `agent/` | 18,381 | **KEEP** | Core orchestration loop. Read pass needed. |
| `config/` | 16,569 | **KEEP** | Config schema. Will shrink as features get deleted. |
| `security/` | 13,508 | **KEEP** | Policy, pairing, secret store. Office OS needs this. |
| `memory/` | 12,223 | **KEEP, prune** | Keep `obsidian` + `none`. Delete sqlite, markdown, lucid, qdrant if unused. |
| `gateway/` | 9,911 | **KEEP** | REST/WebSocket gateway. Office OS dashboard depends on it. |
| `onboard/` | 7,975 | **DECIDE** | Likely interactive wizard. Office OS is config-driven via K8s ConfigMap — wizard may be dead. |
| `hardware/` | 7,618 | **DELETE** | STM32, RPi, etc. Server-side only. |
| `sop/` | 6,530 | **DECIDE** | Standard operating procedures. Could be a building block for the n8n-fork-as-deterministic-engine, or could be dead. |
| `skills/` | 5,114 | **KEEP** | Obsidian skill is injected here. Load-bearing. |
| `cron/` | 4,831 | **KEEP** | Scheduled jobs. Useful for agents. |
| `tui/` | 4,048 | **DELETE** | Terminal UI. Office OS is web dashboard. |
| `observability/` | 3,575 | **KEEP** | Metrics/logs/traces — needed for K8s ops. |
| `auth/` | 2,599 | **KEEP** | Required for multi-tenant agents-owned-by-people. |
| `verifiable_intent/` | 2,120 | **DECIDE** | Unknown. Read first. |
| `peripherals/` | 1,921 | **DELETE** | Hardware boards. Same as `hardware/`. |
| `tunnel/` | 1,743 | **DECIDE** | Probably ngrok-style tunneling for local dev. K8s doesn't need it. |
| `service/` | 1,712 | **DECIDE** | Systemd/launchd service install. K8s doesn't need it. |
| `integrations/` | 1,330 | **DECIDE** | Inspect contents. |
| `doctor/` | 1,324 | **DECIDE** | Health-check CLI. Useful or dead? |
| `runtime/` | 1,293 | **COLLAPSE** | RuntimeAdapter trait + native impl only → inline. |
| `hooks/` | 1,274 | **DECIDE** | Inspect — could be lifecycle hooks (keep) or pre-commit hooks (delete). |
| `heartbeat/` | 1,193 | **KEEP** | K8s liveness/readiness signal. |
| `daemon/` | 1,176 | **DECIDE** | Probably overlaps with `service/`. |
| `plugins/` | 1,136 | **DECIDE** | Inspect — could conflict with skills system. |
| `skillforge/` | 1,118 | **DECIDE** | Unknown. |
| `commands/` | 882 | **KEEP** | CLI command routing. Will shrink as subcommands get deleted. |
| `trust/` | 859 | **DECIDE** | Inspect. |
| `cost/` | 767 | **KEEP** | Token/USD accounting. Useful for K8s multi-tenant. |
| `routines/` | 660 | **DECIDE** | Inspect. |
| `approval/` | 611 | **KEEP** | Human-in-the-loop gating. Useful for the rights/RBAC story. |
| `hands/` | 574 | **DECIDE** | Unknown name. Inspect. |
| `rag/` | 395 | **DECIDE** | Probably vector retrieval — may overlap with vault. |
| `nodes/` | 238 | **DECIDE** | Unknown. |
| `health/` | 184 | **KEEP** | K8s health checks. |
| `identity.rs` | — | **KEEP** | AIEOS — wins over personality.rs. |
| `agent/personality.rs` | ~350 | **DELETE** (Phase 3) | Legacy markdown personality loader. AIEOS replaces it. |
| `agent/vault_sync.rs` Tier 0 path | — | **DELETE** (Phase 3) | Identity files don't belong in vault. |
| `migration.rs`, `multimodal.rs`, `i18n.rs`, `cli_input.rs`, `util.rs` | — | **DECIDE** | Top-level files; inspect each. |

## Channels — keep / delete

**KEEP (2):** `telegram.rs`, `webhook.rs` (+ `mod.rs`, `traits.rs`, `session_*`).
Telegram is the canonical channel. Webhook is the gateway integration point.

**DELETE (~43):** acp_server, bluesky, clawdtalk, cli, debounce, dingtalk,
discord, discord_history, email_channel, gmail_push, imessage, irc, lark,
link_enricher, linq, matrix, mattermost, media_pipeline, mochat, mqtt,
nextcloud_talk, nostr, notion, qq, reddit, signal, slack, stall_watchdog,
transcription, tts, twitter, voice_call, voice_wake, wati, wecom, whatsapp,
whatsapp_storage, whatsapp_web.

If any of these turn out to be load-bearing for the Office OS MVP (e.g.,
`nextcloud_talk` if you actually want first-party Nextcloud), promote it back
during phase 2 review.

## Providers — keep / delete

**KEEP (5):** `anthropic.rs`, `openai.rs`, `ollama.rs`, `reliable.rs`,
`router.rs` (+ `traits.rs`, `mod.rs`).

**DELETE (12):** azure_openai, bedrock, claude_code, compatible, copilot,
gemini, gemini_cli, glm, kilocli, openai_codex, openrouter, telnyx.

## Tools — keep / delete

**KEEP (~20):** memory_store, memory_recall, memory_forget, memory_purge,
memory_export, file_read, file_write, file_edit, glob_search, content_search,
shell, http_request, web_fetch, web_search_tool, mcp_client, mcp_protocol,
mcp_transport, mcp_tool, mcp_deferred, schema, traits, mod, wrappers,
ask_user, escalate, skill_tool, read_skill.

**DELETE (~70):** all SaaS-specific tools (linkedin*, notion_tool, jira_tool,
google_workspace, microsoft365/, discord_search, weather_tool, pushover),
all hardware tools (hardware_*), browser delegation (browser*, text_browser),
CLI delegation (claude_code*, codex_cli, gemini_cli, opencode_cli, kilocli,
delegate, swarm), report templates, image_gen/info, pdf_read, screenshot,
sop_*, cron_* (CLI commands; cron module itself stays), node_*, project_intel,
proxy_config, security_ops, sessions, model_*, poll, reaction, calculator,
canvas, cli_discovery, composio, data_management, cloud_*, backup_tool,
git_operations, llm_task, pipeline, schedule, verifiable_intent.rs,
web_search_provider_routing.

## Decision points blocking phase 2

These need a yes/no before deletion can proceed:

1. **`crates/aardvark-sys`** — what is it, who uses it?
2. **`apps/`, `web/`, `tool_descriptions/`** — inspect and classify.
3. **`onboard/`** — keep the wizard or delete in favor of K8s ConfigMap?
4. **`sop/`** — is this the future deterministic-workflow engine, or dead?
5. **`tunnel/`, `service/`, `daemon/`** — local-dev only? K8s deployments don't need any of these.
6. **`integrations/`, `hooks/`, `plugins/`, `skillforge/`, `verifiable_intent/`,
   `trust/`, `routines/`, `hands/`, `rag/`, `nodes/`** — read-and-classify pass.

## Phase order

1. **Phase 1 (this doc)** — audit, no deletions. ✅
2. **Phase 2** — delete robot-kit, hardware/peripherals, firmware, marketplace,
   docs/i18n, tui, unused channels (43), unused providers (12), unused tools
   (~70). Resolve the DECIDE points above as we encounter them.
3. **Phase 3** — kill personality.rs, fold its responsibilities into AIEOS
   (`identity.rs`). Document AIEOS properly.
4. **Phase 4** — collapse single-impl traits.
5. **Phase 5** — write 5-line module docs for everything that survived. If a
   module can't be documented, it gets deleted.

## Rules

- **Removal only.** No redesign, no rewrites, no "while I'm here" cleanups.
- **Commit per category.** One commit for "delete unused channels", another
  for "delete unused providers", etc. Easy to revert if something was wrong.
- **No upstream merges after this point.** This is a hard fork.
- **Tests stay green.** After each commit: `cargo build` must succeed. Tests
  must compile. Failing tests for deleted modules get deleted with them.
