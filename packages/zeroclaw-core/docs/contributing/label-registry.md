# Label Registry

Single reference for every label used on PRs and issues. Labels are grouped by category. Each entry lists the label name, definition, and how it is applied.

Sources consolidated here:

- `.github/labeler.yml` (path-label config for `actions/labeler`)
- `.github/label-policy.json` (contributor tier thresholds)
- `docs/contributing/pr-workflow.md` (size, risk, and triage label definitions)
- `docs/contributing/ci-map.md` (automation behavior and high-risk path heuristics)

Note: The CI was simplified to 4 workflows (`ci.yml`, `release.yml`, `ci-full.yml`, `promote-release.yml`). Workflows that previously automated size, risk, contributor tier, and triage labels were removed. Only path labels via `pr-path-labeler.yml` are currently automated.

---

## Path labels

Applied automatically by `pr-path-labeler.yml` using `actions/labeler`. Matches changed files against glob patterns in `.github/labeler.yml`.

### Base scope labels

| Label | Matches |
|---|---|
| `docs` | `docs/**`, `**/*.md`, `**/*.mdx`, `LICENSE`, `.markdownlint-cli2.yaml` |
| `dependencies` | `Cargo.toml`, `Cargo.lock`, `deny.toml`, `.github/dependabot.yml` |
| `ci` | `.github/**`, `.githooks/**` |
| `core` | `src/*.rs` |
| `agent` | `src/agent/**` |
| `channel` | `src/channels/**` |
| `gateway` | `src/gateway/**` |
| `config` | `src/config/**` |
| `daemon` | `src/daemon/**` |
| `doctor` | `src/doctor/**` |
| `health` | `src/health/**` |
| `heartbeat` | `src/heartbeat/**` |
| `memory` | `src/memory/**` |
| `security` | `src/security/**` |
| `runtime` | `src/runtime/**` |
| `onboard` | `src/onboard/**` |
| `provider` | `src/providers/**` |
| `service` | `src/service/**` |
| `skillforge` | `src/skillforge/**` |
| `skills` | `src/skills/**` |
| `tool` | `src/tools/**` |
| `observability` | `src/observability/**` |
| `tests` | `tests/**` |

### Per-component channel labels

Only the surviving channels are labeled. All previously-listed community channel labels were removed in Phase 2.4.

| Label | Matches |
|---|---|
| `channel:cli` | `cli.rs` |
| `channel:telegram` | `telegram.rs` |
| `channel:webhook` | `webhook.rs` |

### Per-component provider labels

Only surviving providers are labeled. Deleted provider labels (azure-openai, bedrock, claude-code, copilot, gemini, glm, kilocli, openai-codex, telnyx) were removed in Phase 2.

| Label | Matches |
|---|---|
| `provider:anthropic` | `anthropic.rs` |
| `provider:compatible` | `compatible.rs` |
| `provider:ollama` | `ollama.rs` |
| `provider:openai` | `openai.rs` |
| `provider:openrouter` | `openrouter.rs` |
| `provider:reliable` | `reliable.rs` |
| `provider:router` | `router.rs` |

### Per-group tool labels

Tools are grouped by logical function. Labels for deleted tool groups were removed in Phase 2.6.

| Label | Matches |
|---|---|
| `tool:file` | `file_edit.rs`, `file_read.rs`, `file_write.rs`, `glob_search.rs`, `content_search.rs` |
| `tool:mcp` | `mcp_client.rs`, `mcp_deferred.rs`, `mcp_protocol.rs`, `mcp_tool.rs`, `mcp_transport.rs` |
| `tool:memory` | `memory_forget.rs`, `memory_recall.rs`, `memory_store.rs` |
| `tool:shell` | `shell.rs` |
| `tool:web` | `web_fetch.rs`, `web_search_tool.rs`, `http_request.rs` |
| `tool:skill` | `skill_*.rs`, `read_skill.rs` |
| `tool:session` | `sessions_*.rs` |
| `tool:interaction` | `ask_user.rs`, `escalate.rs`, `delegate.rs`, `poll.rs`, `reaction.rs`, `tool_search.rs`, `canvas.rs` |

---

## Size labels

Defined in `pr-workflow.md` §6.1. Based on effective changed line count, normalized for docs-only and lockfile-heavy PRs.

| Label | Threshold |
|---|---|
| `size: XS` | <= 80 lines |
| `size: S` | <= 250 lines |
| `size: M` | <= 500 lines |
| `size: L` | <= 1000 lines |
| `size: XL` | > 1000 lines |

**Applied by:** manual.

---

## Risk labels

Defined in `pr-workflow.md` §13.2 and `ci-map.md`. Based on a heuristic combining touched paths and change size.

| Label | Meaning |
|---|---|
| `risk: low` | No high-risk paths touched, small change |
| `risk: medium` | Behavioral `src/**` changes without boundary/security impact |
| `risk: high` | Touches high-risk paths (see below) or large security-adjacent change |
| `risk: manual` | Maintainer override that freezes automated risk recalculation |

High-risk paths: `src/security/**`, `src/runtime/**`, `src/gateway/**`, `src/tools/**`, `.github/workflows/**`.

**Applied by:** manual.

---

## Contributor tier labels

Defined in `.github/label-policy.json`. Based on the author's merged PR count.

| Label | Minimum merged PRs |
|---|---|
| `trusted contributor` | 5 |
| `experienced contributor` | 10 |
| `principal contributor` | 20 |
| `distinguished contributor` | 50 |

**Applied by:** manual.

---

## Response and triage labels

Defined in `pr-workflow.md` §8. Applied manually.

| Label | Purpose | Applied by |
|---|---|---|
| `r:needs-repro` | Incomplete bug report; request deterministic repro | Manual |
| `r:support` | Usage/help item better handled outside bug backlog | Manual |
| `invalid` | Not a valid bug/feature request | Manual |
| `duplicate` | Duplicate of existing issue | Manual |
| `stale-candidate` | Dormant PR/issue; candidate for closing | Manual |
| `superseded` | Replaced by a newer PR | Manual |
| `no-stale` | Exempt from stale automation; accepted but blocked work | Manual |

---

## Maintenance

- **Owner:** maintainers responsible for label policy and PR triage automation.
- **Update trigger:** new channels, providers, or tools added to the source tree; label policy changes; triage workflow changes.
- **Source of truth:** this document consolidates definitions from the source files listed at the top. When definitions conflict, update the source file first, then sync this registry.
