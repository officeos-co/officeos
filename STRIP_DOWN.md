# Strip-Down Refactor — `packages/zeroclaw-core`

A focused, multi-commit reduction of the inherited `zeroclaw-core` Rust crate from a sprawling, multi-tenant agent runtime into a lean K8s-native core for Office OS.

## Summary

- **`src/*.rs` total LOC**: ~292,000 → ~165,000 (**−127,000, −44%**)
- **Production-only LOC** (excluding extracted tests): ~292,000 → **~97,200** (**−194,800, −67%**)
- **Channels**: 45 → **2** (telegram, webhook)
- **Providers**: 17 → **6 + compatible wrapper** (anthropic, openai, ollama, openrouter, reliable, router)
- **Tools**: ~92 → **~30** (shell, file*\*, glob_search, content_search, memory*_, http*request, web*_, mcp*\*, skill*_, canvas, sessions\__, ask_user, escalate, delegate, …)
- **Workspace members**: 3 → **1**
- **Top-level dirs deleted**: 8 (apps/, benches/, crates/, dev/, fuzz/, scripts/, src/tunnel/, src/peripherals/, …)
- **Dockerfiles**: 4 → **1**
- **Locale files** (`tool_descriptions/`): 31 → **1** (en)
- **Inline test files extracted to siblings**: 0 → **188**
- **Tests passing**: build broken → **7,392 / 0 failing**
- **Phase 3 (Obsidian vault as source of truth)**: complete — identity loader unified, `src/identity.rs` + `vault_sync.rs` deleted, per-agent CouchDB vault provisioning + K8s ConfigMap mount in dashboard backend.
- **Phase 4 (orphan sweep)**: complete — 7 orphan config structs deleted, 14 dead Cargo features + ~17 deps deleted (Cargo.lock −6,000 lines), bare-metal `dist/`/`install.sh`/`setup.bat`/`flake.nix` deleted, `docs/hardware/` deleted.

The strip-down phases 1–4 are complete. The codebase builds clean (`cargo check --tests`), every test target passes, and the production source is one third of its original size.

### Architectural framing

Strip-down was guided by the **Office OS deployment model**:

- **K8s-native** — pods are self-contained; no OS-service installer, no tunneling layer (`Service`/`Ingress` handles ingress), no bare-metal install scripts.
- **Single-tenant intent** — a personal/internal Office OS instance, not a multi-tenant SaaS. SaaS-only integrations (~50 of them) and "browse 50+ integrations" UX go away.
- **Server-side** — no embedded firmware, no peripherals (STM32/RPi GPIO/Arduino/ESP32), no robot kits, no desktop wrapper (Tauri).
- **Telegram + dashboard primary UX** — kept telegram + webhook channels, deleted the other 43.
- **Six providers + one wrapper** — anthropic, openai, ollama, openrouter, reliable, router, plus `OpenAiCompatibleProvider` covering ~30 community OpenAI-compatible endpoints (Groq, Mistral, xAI, DeepSeek, Together, Fireworks, Cohere, Perplexity, LM Studio, llama.cpp, Z.AI, GLM, MiniMax, Qwen, …).
- **Core tool surface** — kept the ~30 tools an autonomous agent actually needs (shell, filesystem, search, memory, web, MCP, sessions, skill discovery), deleted the dozens of feature-specific integrations (cron CLI tools, SOP execution, image gen, screenshot, weather, jira, linkedin, etc.).

### What's still left (not blocking — quality work)

- **Phase 5 — documentation pass**: 5-line module-doc comments on the surviving modules.
- **Template sync retarget** (deferred from Phase 3): plumb `_seed_agent_vault` into the existing `POST /api/v1/gateways/{id}/templates/sync` flow so template updates propagate to the per-agent vault and ConfigMap of existing agents, not only on fresh create.
- **Alembic head merge** (pre-existing): the dashboard backend has three unrelated Alembic heads. Phase 3's new migration chained from one of them without attempting a merge.

---

## Per-commit recap

Commits are in chronological order. All changes scoped to `packages/zeroclaw-core/`. Every commit was build-verified (later commits with `cargo check --tests` after the discovery in 2.8 that plain `cargo check` skips inline test modules).

| #   | Commit    | Title                                                                                                 | Files | Net Δ                     |
| --- | --------- | ----------------------------------------------------------------------------------------------------- | ----- | ------------------------- |
| 1   | `7f49a16` | docs: add `STRIP_DOWN_PLAN.md` (phase 1 audit)                                                        | 1     | +158                      |
| 2   | `c140934` | strip phase 2.1 — robot-kit, firmware, i18n, marketplace                                              | 231   | **−51,031**               |
| 3   | `48fe3f3` | strip phase 2.2 — peripherals module                                                                  | 15    | −2,043                    |
| 4   | `4739acd` | strip phase 2.3 — hardware, onboard, tui, rag                                                         | 32    | **−21,269**               |
| 5   | `2df622e` | strip phase 2.4 — channels (43 of 45 deleted)                                                         | 39    | **−40,056**               |
| 6   | `e93be83` | strip phase 2.5 — providers (10 of 17 deleted)                                                        | 13    | −8,961                    |
| 7   | `7018648` | strip phase 2.6 — tools (~57 of ~92 deleted)                                                          | 67    | **−36,431**               |
| 8   | `2bfb481` | strip phase 2.7a — orphan modules (sop, verifiable_intent, trust, hands, nodes, routines, skillforge) | 30    | −12,103                   |
| 9   | `006d2c4` | strip phase 2.7b — service + integrations modules                                                     | 7     | −3,153                    |
| 10  | `86670dc` | strip phase 2.8 — repair test build after deletions                                                   | 15    | −2,870                    |
| 11  | `7aa1ad3` | extract god-file inline tests into sibling files                                                      | 9     | +4 (file split, no net)   |
| 12  | `e7d14d1` | extract `agent.rs` inline tests too                                                                   | 2     | +1 (file split)           |
| 13  | `cf564a8` | extract all inline test modules into sibling `.test.rs` files                                         | 364   | +182 (file split, no net) |
| 14  | `bfff879` | strip — delete tunnel module                                                                          | 19    | −1,774                    |
| 15  | `66a6d93` | strip top-level cruft (apps/tauri, non-en locales, dockerfile variants)                               | 73    | **−11,249**               |
| 16  | `779a54b` | delete `dev/` — unused local-dev harness scripts                                                      | 15    | −1,750                    |
| 17  | `b726cf5` | delete `scripts/`, `crates/`, `benches/`                                                              | 24    | −4,047                    |
| 18  | `da7fa1d` | delete `fuzz/` — unused cargo-fuzz harness                                                            | 6     | −90                       |
| 19  | `6e6ca70` | delete dead tests for stripped providers/tools/channels                                               | 5     | −351                      |

---

### Commit notes

#### `7f49a16` — Phase 1 audit (`STRIP_DOWN_PLAN.md`)

Inventoried the 292k-line surface and classified every module/crate/channel/provider/tool as **keep / delete / collapse / decide**. Defined the 5-phase sequence. No code changes — the map for everything that follows.

#### `c140934` — Phase 2.1: zero-entanglement bulk

Four sibling directories that were never wired into core runtime:

- `crates/robot-kit/` — Pi toy robot demo
- `firmware/` — embedded server firmware
- `docs/i18n/` — 30 stale translations of one paragraph
- `marketplace/` — placeholder for ClawHub-that-doesn't-exist-yet

Removed `robot-kit` from `Cargo.toml` workspace members. **No `src/` touched.** Single biggest LOC delete (51k) for almost zero risk.

#### `48fe3f3` — Phase 2.2: `src/peripherals/`

STM32, RPi GPIO, Arduino, ESP32 board support. Office OS is server-side; embedded peripherals are out of scope. Cleaned up the CLI: removed `Peripheral` subcommand, `--peripheral` flag on the `agent` command, `peripheral_overrides` parameter (and updated all 4 callers of `agent::loop_::run`), and the peripheral-tools registration in the agent loop.

`PeripheralsConfig` left as orphan in `schema.rs` for phase 4.

#### `4739acd` — Phase 2.3: hardware + onboard + tui + rag

Bundled deletion because these four modules are tightly coupled (`tui` exists only for the onboard wizard; `onboard/wizard` imports `hardware`; `rag` is solely the hardware datasheet RAG). Have to go together to keep the build green.

- `src/hardware/` — board support
- `src/onboard/` — interactive setup wizard (Office OS uses K8s ConfigMap-driven config, not interactive setup)
- `src/tui/` — ratatui terminal UI (only used by the wizard)
- `src/rag/` — hardware datasheet RAG (only consumer was hardware tools)
- `src/gateway/hardware_context.rs` — orphan handler

CLI surface: removed `Onboard`, `Hardware`, `Models` variants, `ModelCommands` enum, `DoctorCommands::Models` variant, the `Commands::Onboard` runtime handler block (~140 lines).

Agent loop: deleted `build_hardware_context()`, the hardware_rag + board_names blocks, the conditional gpio/hardware/arduino tool description blocks, and replaced 3 `hw_context` call sites with plain `mem_context`.

Doctor module: deleted `run_models()`, `ModelProbeOutcome`, `classify_model_probe_error`, `model_probe_status_label`, `doctor_model_targets`, `format_error_chain`.

#### `2df622e` — Phase 2.4: 31 channels deleted (43 of 45)

Massive channel deletion. **Kept only telegram + webhook + cli + shared infra** (debounce, link*enricher, media_pipeline, session*\*, stall_watchdog, traits, transcription, tts).

Deleted: acp_server, bluesky, clawdtalk, dingtalk, discord, discord_history, email_channel, gmail_push, imessage, irc, lark, linq, matrix, mattermost, mochat, mqtt, nextcloud_talk, nostr, notion, qq, reddit, signal, slack, twitter, voice_call, voice_wake, wati, wecom, whatsapp, whatsapp_storage, whatsapp_web.

Knock-on cleanups:

- `src/channels/mod.rs` (11k → much smaller): gutted `build_channel_by_id()` and `collect_configured_channels()` to keep only telegram + webhook
- `src/cron/scheduler.rs`: removed all per-channel delivery match arms from cron job dispatch
- `src/gateway/mod.rs`: removed 8 `AppState` fields (`whatsapp`, `whatsapp_app_secret`, `linq`, `linq_signing_secret`, `nextcloud_talk`, `nextcloud_talk_webhook_secret`, `wati`, `gmail_push`), 6 channel construction blocks (~140 lines), 5 routes, 8 handler functions (~590 lines), all WhatsApp/Nextcloud Talk tests + helpers (~325 lines)
- `src/gateway/api.rs`: removed `mask_sensitive_fields` / `restore_masked_sensitive_fields` handlers for clawdtalk + email
- `src/integrations/registry.rs`: removed Email IntegrationEntry
- `src/config/schema.rs`: removed `channels_config` fields email/gmail_push/clawdtalk/voice_call + corresponding ConfigWrapper entries + encrypt/decrypt helper blocks
- `src/daemon/mod.rs`: stubbed `run_mqtt_sop_listener()` to bail
- `src/main.rs`: removed `Acp` CLI subcommand

#### `e93be83` — Phase 2.5: 10 providers deleted

Deleted: `azure_openai`, `bedrock`, `claude_code`, `copilot`, `gemini`, `gemini_cli`, `glm` (orphan), `kilocli`, `openai_codex`, `telnyx`.

**Kept 6**: anthropic, openai, ollama, openrouter, reliable, router (+ traits, mod). Also kept `compatible.rs` as shared infra — it backs **~30 community providers** (Groq, Mistral, xAI, DeepSeek, Together, Fireworks, Cohere, Perplexity, LM Studio, llama.cpp, Z.AI, GLM, MiniMax, Qwen, etc.) via the `OpenAiCompatibleProvider` wrapper through generic match arms in `mod.rs`.

`src/providers/mod.rs` factory: removed dedicated match arms for openai-codex/codex, gemini/google/google-gemini (with AuthService wiring), telnyx, azure_openai/azure-openai/azure, bedrock/aws-bedrock, copilot/github-copilot, claude-code, gemini-cli.

#### `7018648` — Phase 2.6: ~57 tools deleted

The biggest surgical strike. Kept ~30 core tools an autonomous agent actually needs:

- shell, file\_\*, glob_search, content_search
- memory\_\*, http_request, web_fetch, web_search_tool
- mcp*\*, skill*\*, read_skill
- schema, traits, wrappers
- ask_user, escalate, canvas
- sessions\_\*, poll, reaction, delegate, tool_search

Deleted ~57 tool files: backup*tool, browser, browser_delegate, browser_open, calculator, claude_code, claude_code_runner, cli_discovery, cloud_ops, cloud_patterns, codex_cli, composio, cron_add/list/remove/run/runs/update (6 cron CLI tools), data_management, discord_search, gemini_cli, git_operations, google_workspace, hardware*_, image_gen, image_info, jira_tool, linkedin_, llm_task, microsoft365, model_routing_config, model_switch, node_capabilities, node_tool, notion_tool, opencode_cli, pdf_read, pipeline, project_intel, proxy_config, pushover, report_template_tool, report_templates, schedule, screenshot, security_ops, sop_advance/approve/execute/list/status (5 SOP tools), swarm, text_browser, verifiable_intent, weather_tool, workspace_tool.

`src/tools/mod.rs`: replaced ~635-line `all_tools_with_runtime()` factory body with a minimal version registering only the kept tools.

`src/config/schema.rs`: introduced stub `BrowserDelegateConfig {}` so the field on `Config` still compiles. Phase 4 sweep candidate.

`src/doctor/mod.rs`: stubbed `check_cli_tools()`.

`src/gateway/api.rs`: stubbed `handle_api_cli_tools()`, deleted `handle_claude_code_hook()`.

#### `2bfb481` — Phase 2.7a: 7 orphan modules

Modules with **zero external code references** after the channel/tool/provider strip-down. Pure dead weight kept alive only by mod decls.

- `src/sop/` — Standard Operating Procedures engine
- `src/verifiable_intent/` — VI credential verification
- `src/trust/` — trust scoring (only ref was schema.rs config field)
- `src/hands/`
- `src/nodes/` — top-level nodes (distinct from `gateway::nodes`)
- `src/routines/`
- `src/skillforge/`

Defined local stub `TrustConfig {}` in `schema.rs` so the `trust:` field still compiles. `SopConfig` and `VerifiableIntentConfig` were already defined locally.

#### `006d2c4` — Phase 2.7b: service + integrations modules

- `src/service/` — systemd/launchd OS-service installer (Office OS is K8s-deployed; pods don't need OS service units)
- `src/integrations/` — "Browse 50+ integrations" listing (only catalogued SaaS integrations we already deleted)

Removed corresponding CLI subcommands, dispatch arms, `ServiceCommands`/`IntegrationCommands`/`SopCommands` enums, and `service::is_running()` from status output.

`src/gateway/api.rs`: stubbed `handle_api_integrations()` and `handle_api_integrations_settings()` to return empty data so the dashboard doesn't 404.

#### `86670dc` — Phase 2.8: repair test build

**Critical discovery**: phases 2.4–2.7 used `cargo check` to verify each commit, which only compiles the lib build — it does **not** compile inline `#[cfg(test)] mod tests` blocks. So inline tests that referenced deleted symbols silently rotted.

`cargo test --no-run` revealed 12+ compile errors in inline tests across `gateway/mod.rs`, `gateway/api.rs`, etc. (whatsapp_memory_key, channels_config.email, deleted AppState fields, etc.).

This commit:

- **Deleted 8 test files** for fully-removed features (`whatsapp_webhook_security`, `gemini_capabilities`, `gateway` (was whatsapp signature tests), `channel_matrix`, `email_attachments`, `report_template_tool_test`, `openai_codex_vision_e2e`, `gemini_fallback_oauth_refresh`)
- Updated `tests/{component,integration,live}/mod.rs` to drop the corresponding `mod` declarations
- Removed inline test fixtures referencing deleted AppState fields
- Removed `whatsapp_memory_key_*` test
- Removed `mask_sensitive_fields` email assertions

**Process change recorded as memory**: future strip-down work uses `cargo check --tests` (or `cargo test --no-run`) per commit, not plain `cargo check`.

#### `7aa1ad3` — Extract 4 god-file inline test modules

The first round of test extraction. The four largest god files had thousands of lines of inline tests crammed into the same file as production code.

| File                       | Before | After  | Tests extracted to                      |
| -------------------------- | ------ | ------ | --------------------------------------- |
| `src/channels/mod.rs`      | 11,058 | 5,007  | `src/channels/tests.rs` (6,052)         |
| `src/config/schema.rs`     | 15,982 | 10,938 | `src/config/schema.test.rs` (5,045)     |
| `src/agent/loop_.rs`       | 9,381  | 4,633  | `src/agent/loop_.test.rs` (4,749)       |
| `src/channels/telegram.rs` | 5,065  | 3,019  | `src/channels/telegram.test.rs` (2,047) |

Wiring pattern in each parent:

```rust
#[cfg(test)]
#[path = "<basename>.test.rs"]
mod tests;
```

Module path is preserved (still `super::tests`), so `use super::*;` inside the extracted files keeps working — zero behavior change, only file organization.

#### `e7d14d1` — Extract `agent.rs` inline tests

User-requested follow-up: even though `agent.rs` had only ~600 lines of inline tests (under the "way over 1000" threshold), the linter had pre-created an empty `agent.test.rs` placeholder signaling intent. Extracted 1,908 → 1,299 lines.

#### `cf564a8` — Extract everything else (188 sibling files)

Same `#[path]` pattern applied to **every remaining file** with a top-level inline `#[cfg(test)] mod tests` block, executed by **25 parallel subagents** (one per directory).

|                                                    | Before extraction | After       |
| -------------------------------------------------- | ----------------- | ----------- |
| Production source LOC (`src/*.rs` excluding tests) | ~165,000          | **~97,200** |
| Sibling test files                                 | 5                 | **188**     |

~68,000 lines of test code moved out of production files in this single batch — each parent file is now half its previous size on average.

Subagents skipped files where the test block didn't extend to EOF, the sibling already existed, or the file had a non-standard pattern (e.g. `src/skills/mod.rs` ends with `mod symlink_tests;`, `src/memory/vector.rs` has `#[allow(...)]` immediately after `#[cfg(test)]`).

#### `bfff879` — Delete `src/tunnel/`

Office OS is K8s-deployed; the gateway is exposed via Service/Ingress, not via per-process tunnel providers (ngrok, cloudflare, tailscale, pinggy, openvpn, custom).

Removed: 8 provider files + mod.rs + sibling `.test.rs` files. Removed mod decls in `lib.rs` and `main.rs`. Removed the tunnel startup block in `src/gateway/mod.rs` (`create_tunnel` call, `start()` call, `tunnel_url` print).

`TunnelConfig` and sub-types (`CloudflareTunnelConfig`, `NgrokTunnelConfig`, `PinggyTunnelConfig`, `OpenVpnTunnelConfig`) remain in `schema.rs` as orphans — phase 4 sweep alongside the other orphan configs.

#### `66a6d93` — Top-level cruft

Three deletions per the K8s deployment model:

1. **`apps/tauri/`** — desktop wrapper. `Cargo.toml` workspace `members` updated from `[".", "crates/aardvark-sys", "apps/tauri"]` to `[".", "crates/aardvark-sys"]`.
2. **`tool_descriptions/`** — deleted all 30 non-English locale files. Runtime falls back to English on missing locale (documented in `schema.rs:407`), so no code change needed.
3. **Dockerfiles** — collapsed 4 → 1. Kept the debian-based, multi-stage, build-from-source variant (renamed `Dockerfile.debian` → `Dockerfile`). Distroless ruled out because the agent's `shell` tool is in our kept core set.

#### `779a54b` — Delete `dev/`

User does all testing through the dashboard, not via dev/ scripts. Deleted 14 files: `ci.sh`, `cli.sh` (the source of the `compdef` zsh-completion error), `kill-port.py`, contributor tier scripts, docker-compose variants, test harnesses. Updated `CLAUDE.md` to drop the `./dev/ci.sh all` recommendation.

#### `b726cf5` — Delete `scripts/`, `crates/`, `benches/`

1. **`scripts/`** — 16 files of bare-metal RPi infra, CI gates, release-tag automation, and browser/VNC setup. Zero references from `src/`. Includes the `bump-version.sh` that referenced `apps/tauri` (already deleted) and `zeroclaw.service` (systemd unit, obsolete after deleting `src/service/`).
2. **`crates/aardvark-sys/`** — workspace member crate stubbing the Aardvark I2C/SPI/GPIO USB adapter SDK. Zero `aardvark` references in `src/*.rs` after the hardware/peripherals strip-down. Also: workspace `members = ["."]`, removed `aardvark-sys = { path = … }` dependency.
3. **`benches/`** — single criterion benchmark, never run. Also removed `[[bench]]` entry from `Cargo.toml` and `criterion = "0.8"` dev-dependency.

#### `da7fa1d` — Delete `fuzz/`

Self-contained cargo-fuzz package with 5 targets (config_parse, command_validation, provider_response, tool_params, webhook_payload). Not a workspace member, zero CI integration, never run as part of any dev workflow. Same reasoning as `dev/` and `benches/` — useful in theory, dead weight in practice.

#### `6e6ca70` — Delete dead tests

After running `cargo test` for the first time post-strip-down, **24 tests were failing** — all dead tests for providers/tools/channels/feature-flags deleted in phases 2.4–2.7. The test build _compiled_ because the factory functions still exist (they just no longer recognize the deleted provider names), but the tests panicked at runtime.

Removed 24 test functions across 5 files:

- `src/providers/tests.rs`: 11 `factory_*` tests for openai_codex, gemini, telnyx, bedrock, codex_oauth_aliases, copilot, claude_code, gemini_cli, kilocli, factory_all_providers_create_successfully, listed_providers_and_aliases_are_constructible
- `src/tools/tests.rs`: `all_tools_excludes_browser_when_disabled`, `all_tools_includes_browser_when_enabled`
- `src/channels/tests.rs`: `collect_configured_channels_includes_mattermost_when_configured`
- `src/cron/scheduler.test.rs`: both `deliver_if_configured_matrix_*` tests
- `tests/component/provider_resolution.rs`: 8 dead `factory_*` tests + their aliases (gemini, bedrock, copilot, openai_codex, google, google-gemini, aws-bedrock, github-copilot)

**Test results after cleanup** (first verified all-green run since phase 2 began):

```
lib              3,562 passed,  0 failed,  1 ignored
zeroclaw bin     3,572 passed,  0 failed,  1 ignored
component          161 passed,  0 failed
integration         91 passed,  0 failed
system               5 passed,  0 failed
live                 0 passed,  2 ignored (require live credentials)
doctests             1 passed,  2 ignored
─────────────────────────────────────────────────────
Total            7,392 passed,  0 failed
```

---

## Phase 3 — Obsidian vault as single source of truth

Phase 3 was originally scoped as "resolve the `personality.rs` / `identity.rs` legacy split." It grew into a multi-repo architectural change: every agent gets its own Obsidian vault, the dashboard backend owns vault provisioning, the agent pod only reads, and the agent fails loudly if required personality files are missing.

### Summary

- Identity formats: **2 → 1** (markdown only)
- zeroclaw-core LOC in phase 3: **−2,300**
- Boot gate: **none → strict** (`load_personality_strict`)
- Per-agent vault DB: **none → 1:1** (CouchDB per agent)
- Phase 3 backend tests: **+56 green**

Deletions in zeroclaw-core:

- **`src/identity.rs`** — 987 lines, the AIEOS JSON loader and its 8-section normalizers.
- **`src/agent/vault_sync.rs`** — 262 lines, the boot-time obsctl pull.
- **`IdentityConfig` struct** — 3 fields (`format`, `aieos_path`, `aieos_inline`) + its `identity` field on `Config` + 3 default sites.
- **`identity_config` plumbing** — 43 reference sites across `PromptContext`, `AgentCoreBuilder`, `build_system_prompt` wrappers, and every test fixture.
- **`ensure_bootstrap_files()`** — the workspace auto-seeder. Agent no longer writes files.

Additions:

- **`personality::load_personality_strict`** — new loader with `PersonalityError`, wired into `AgentCore::create_from_config` as the boot gate.
- **`obsctl.VaultClient.ensure_database`** — new Python API method (+65 LOC, 9 tests).
- **Per-agent CouchDB database** `agent-{uuid}`, provisioned by the dashboard backend at agent creation time.
- **Per-agent K8s ConfigMap** `eaos-agent-{uuid}-vault`, mounted read-only at `/vault-workspace`.
- **`Agent.vault_database` column** + Alembic migration in the dashboard backend.
- **`vault_provisioning.py`** and **`vault_configmap.py`** helper modules in `apps/dashboard/backend`.
- **Lockstep write path** in `AgentLifecycleService._seed_agent_vault`: one rendered dict → CouchDB + ConfigMap.

Pod boot sequence simplified: no more `pip install obsidian-vault-cli`, no more `vault config set` block, no more `zeroclaw onboard --quick`. The boot command is now just `zeroclaw daemon`; the K8s API guarantees the workspace files are present before the container starts.

After Phase 3, zeroclaw-core is a pure markdown reader. It does not create, sync, or write identity files. The dashboard backend (`apps/dashboard/backend`) owns every write path.

### Per-commit recap (Phase 3)

| #   | Commit    | Title                                                                                   | Repo              |
| --- | --------- | --------------------------------------------------------------------------------------- | ----------------- |
| 1   | `3f79da1` | feat(obsctl): add VaultClient.ensure_database()                                         | packages/obsctl   |
| 2   | `c044e22` | feat(zeroclaw-core): add personality::load_personality_strict + PersonalityError        | zeroclaw-core     |
| 3   | `78850af` | refactor(zeroclaw-core): remove AIEOS branch from prompt.rs IdentitySection             | zeroclaw-core     |
| 4   | `60343eb` | refactor(zeroclaw-core): remove AIEOS branch from channels::build_system_prompt         | zeroclaw-core     |
| 5   | `e6ad012` | chore(zeroclaw-core): delete src/identity.rs — AIEOS JSON loader                        | zeroclaw-core     |
| 6   | `9475ff5` | chore(zeroclaw-core): delete IdentityConfig + ensure_bootstrap_files                    | zeroclaw-core     |
| 7   | `9334b42` | chore(zeroclaw-core): delete vault_sync.rs + wire load_personality_strict in agent boot | zeroclaw-core     |
| 8   | `fa7b7dc` | feat(dashboard-backend): add Agent.vault_database column + migration                    | dashboard/backend |
| 9   | `30f2ae5` | feat(dashboard-backend): provision per-agent Obsidian vault on agent create             | dashboard/backend |
| 10  | `62fe0da` | feat(dashboard-backend): ConfigMap-mounted vault workspace in k8s_manager               | dashboard/backend |
| 11  | `386dcee` | feat(dashboard-backend): lockstep vault + ConfigMap in AgentLifecycleService            | dashboard/backend |
| 12  | `b3fd5d6` | docs(zeroclaw-core): identity-vault reference                                           | zeroclaw-core     |
| 13  | (this)    | docs(zeroclaw-core): STRIP_DOWN.md Phase 3 entry                                        | zeroclaw-core     |

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ apps/dashboard/backend (FastAPI, Python) — orchestrator          │
│                                                                  │
│ POST /api/v1/agents                                              │
│  1. Insert Agent row                                             │
│  2. Render Jinja2 templates (existing pipeline)                  │
│  3. VaultClient.ensure_database("agent-{uuid}")                  │
│  4. For each file: VaultClient.write_note(path, content)         │
│  5. apply_agent_vault_configmap(api, rendered_files)             │
│  6. K8sManager.create_container(agent_id=...)                    │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ K8s pod (spec generated by k8s_manager.py)                       │
│                                                                  │
│ spec.volumes: [configMap: eaos-agent-{uuid}-vault]               │
│ spec.containers[0]:                                              │
│   env: ZEROCLAW_WORKSPACE=/vault-workspace                       │
│   volumeMounts: vault-workspace → /vault-workspace               │
│                                                                  │
│ boot: `zeroclaw daemon` (no onboarding, no CLI install)          │
│                                                                  │
│ agent.rs::create_from_config:                                    │
│   personality::load_personality_strict(&workspace_dir)?          │
│   → fails LOUDLY if SOUL/IDENTITY/AGENTS missing                 │
│   → process exits → CrashLoopBackOff → dashboard status          │
└─────────────────────────────────────────────────────────────────┘
```

### Key decisions (recorded during the phase)

1. **Markdown only, no AIEOS.** `identity.rs` deleted entirely (987 LOC). The AIEOS JSON loader was parallel to the markdown loader with no benefit — standardizing on one format removed the whole `IdentityConfig { format, aieos_path, aieos_inline }` plumbing that threaded through `prompt.rs`, `channels/mod.rs`, and the Agent builder.

2. **Sync logic in Python, not Rust.** The old `vault_sync.rs` (262 LOC) pulled personality files from CouchDB via the `obsctl` CLI at agent boot. Deleted outright. The dashboard backend now imports `obsctl.VaultClient` as a Python library and writes files directly to the per-agent database. No CLI shell-out, no boot-time `pip install`, no sync-state file.

3. **ConfigMap as pod mount, not initContainer.** Rejected the initContainer approach (which would have required a Python runtime in every agent pod). The dashboard backend mirrors the rendered file set into a K8s `ConfigMap` and mounts it into the pod; the K8s API guarantees the files are present before the container starts. No pod-side tooling, no boot race.

4. **Strict boot gate.** `load_personality_strict` is called first thing in `AgentCore::create_from_config`. If `SOUL.md`, `IDENTITY.md`, or `AGENTS.md` is missing or empty, the process exits non-zero and the pod enters CrashLoopBackOff. The dashboard's existing `K8sManager.get_status` chain surfaces the error via `GET /api/v1/gateways/{id}/container/status`. No silent fallbacks, no default content. This is the contract that makes "hundreds of agents" safe to deploy — a broken provisioning pipeline fails visibly.

5. **Vault and ConfigMap derived from the same rendered dict.** `_seed_agent_vault` in the lifecycle service renders the templates once and passes the same in-memory `dict[str, str]` to both `provision_agent_vault` (CouchDB) and `apply_agent_vault_configmap` (K8s). They cannot drift.

### What's deferred

- **Template sync endpoint not yet retargeted.** `POST /api/v1/gateways/{id}/templates/sync` still writes through the OpenClaw gateway control plane and does not re-seed the vault or refresh the ConfigMap. A follow-up commit should plumb `_seed_agent_vault` into `_sync_one_agent` so template updates propagate to existing agents. For now, vault provisioning only engages on fresh agent creation.
- **Multi-head Alembic graph not merged.** The dashboard backend has three pre-existing Alembic heads from before Phase 3. Commit `fa7b7dc` chained its new migration off `b2c3d4e5f6a7` without attempting a merge. A future cleanup commit should merge all the heads.

---

## Phase 4 — Orphan sweep

Phase 4 removed the scaffolding Phase 2 had to leave behind: dead config structs, dead Cargo features, dead dependencies, and repo-root loose ends. Pure deletion work — no architectural change, no new code.

### Summary

- **7 orphan config structs deleted**: `BrowserDelegateConfig`, `TrustConfig`, `SopConfig`, `VerifiableIntentConfig`, `HardwareConfig` (+ `HardwareTransport` enum), `PeripheralsConfig` (+ `PeripheralBoardConfig`), and `TunnelConfig` with all 7 tunnel sub-types (`Cloudflare`, `Tailscale`, `Ngrok`, `OpenVpn`, `Pinggy`, `Custom`).
- **14 dead Cargo features deleted**: `channel-nostr`, `channel-matrix`, `channel-lark`, `channel-feishu`, `voice-wake`, `hardware`, `peripheral-rpi`, `browser-native` (+ `fantoccini` alias), `sandbox-bubblewrap`, `probe`, `whatsapp-web`, `plugins-wasm`, `webauthn`, plus the orphan `runtime-wasm` cfg that was never even in Cargo.toml.
- **~17 dead dependencies deleted** from Cargo.toml: `matrix-sdk`, `nostr-sdk`, `prost`, `cpal`, `extism`, `fantoccini`, `probe-rs`, `wa-rs` + 5 sub-crates, `qrcode`, `serde-big-array`, `tokio-serial`, `nusb`, `rppal`. Cargo.lock shrunk by ~6,000 lines of transitive deps.
- **2 orphan source files deleted**: `src/gateway/api_plugins.rs` (plugins-wasm), `src/runtime/wasm.rs` (runtime-wasm).
- **Gateway bare-metal security warning deleted**: the `is_public_bind(host) && config.tunnel.provider == "none"` warning in `run_gateway` was the only live reader of `TunnelConfig`. Office OS is K8s-deployed, so the concern is obsolete.
- **Repo-root loose ends deleted**: `dist/aur/`, `dist/scoop/`, `install.sh`, `setup.bat`, `flake.nix`, `flake.lock`, `docker-compose.yml`, `docs/hardware/` (8 files), `docs/browser-setup.md`.
- **MemoryLoader trait retained** with a design-note docstring after the Phase 4 trait collapse sweep found it has a second implementor (`StaticMemoryLoader` in `tests/support/helpers.rs`) used by 3 integration tests as a deterministic mock. All other traits in the crate are genuinely multi-impl; no trait collapse was needed.

### Per-commit recap (Phase 4)

| #   | Commit    | Title                                                                            |
| --- | --------- | -------------------------------------------------------------------------------- |
| 1   | `1e11a04` | delete 6 orphan config structs (−312 LOC)                                         |
| 2   | `13d93d8` | delete TunnelConfig + all sub-types (−221 LOC)                                    |
| 3   | `34fbd64` | delete 14 dead Cargo features + deps (−5,908 LOC net, mostly Cargo.lock)          |
| 4   | `a9c7089` | retain MemoryLoader trait with design note                                        |
| 5   | `c8fa6d6` | delete repo-root loose ends (dist, install.sh, setup.bat, flake, docker-compose, docs/hardware, docs/browser-setup.md) |
| 6   | (this)    | STRIP_DOWN.md Phase 4 entry                                                       |

### Process notes (Phase 4 specific)

- **Parallel subagents for mechanical deletions worked well.** Commit 3 used 3 parallel subagents with disjoint file lists to strip `#[cfg(feature = "X")]` blocks for dead features across 15+ files. Zero merge conflicts because each subagent had a precise file list.
- **Research subagents found the scope cleanly.** Before any edits, 3 parallel Explore agents mapped (a) orphan config structs with file:line references, (b) dead features + their gated code locations, (c) single-impl trait candidates. The maps made the execution phase near-mechanical.
- **"Test implementations don't count" is too broad.** The trait collapse research agent followed that heuristic and missed `StaticMemoryLoader` in `tests/support/helpers.rs`. A better rule: test implementations count when they're in `tests/support/` (shared across multiple integration tests) but don't count when they're in a `#[cfg(test)]` mod inline next to the trait itself.

---

## Process notes

Three things from this strip-down worth remembering for future bulk-deletion work:

1. **`cargo check` is not enough.** It only compiles the lib build and skips inline `#[cfg(test)] mod tests` blocks. After deleting code, always use `cargo check --tests` (or `cargo test --no-run`) per commit. Recorded as a feedback memory after phase 2.8.
2. **Type-checking ≠ runtime correctness.** The dead-test cleanup (`6e6ca70`) was a separate commit because tests can compile against factory functions that _type-check_ but reject the deleted provider names at runtime. Always run `cargo test` (not just `--no-run`) at least once per major deletion phase.
3. **Subagents per directory work well for mechanical extractions.** The 25-parallel-subagent run in `cf564a8` extracted 188 files in one batch with zero conflicts. Key was a precise procedure (find LAST `#[cfg(test)]`, verify file ends with `}`, skip if pattern doesn't match) and an explicit "do not run cargo check" instruction to avoid file-lock contention.
