# ZeroClaw Commands Reference

This reference is derived from the `Commands` enum in `src/main.rs`.
If a flag or subcommand disagrees with `zeroclaw --help`, the binary is
the source of truth — please open a docs fix.

## Top-Level Commands

| Command       | Purpose |
|---------------|---------|
| `agent`       | Run interactive chat or single-message mode |
| `gateway`     | Start/manage the webhook/websocket gateway |
| `daemon`      | Start the supervised runtime (gateway + channels + heartbeat + scheduler) |
| `doctor`      | Diagnostics and runtime trace queries |
| `status`      | Print current configuration and system summary |
| `estop`       | Engage/resume emergency-stop levels and inspect estop state |
| `cron`        | Manage scheduled tasks |
| `providers`   | List supported AI providers |
| `channel`     | Manage channels (list/start/doctor/bind/add/remove) |
| `skills`      | List/audit/install/remove skills |
| `migrate`     | Import data from other agent runtimes |
| `auth`        | Manage provider subscription auth profiles |
| `memory`      | List/get/stats/clear agent memory entries |
| `config`      | Export machine-readable config schema |
| `update`      | Check for and apply ZeroClaw updates |
| `self-test`   | Run diagnostic self-tests |
| `completions` | Generate shell completion scripts to stdout |
| `desktop`     | Launch or install the companion desktop app |

## Command Groups

### `agent`

- `zeroclaw agent`
- `zeroclaw agent -m "Hello"`
- `zeroclaw agent --provider <ID> --model <MODEL> --temperature <0.0-2.0>`
- `zeroclaw agent --session-state-file <PATH>`

Interactive by default; use `-m/--message` for single-shot queries.

### `gateway` / `daemon`

- `zeroclaw gateway [start|restart|get-paircode|...]`
- `zeroclaw daemon [--host <HOST>] [--port <PORT>]`

`daemon` launches the full runtime: gateway server, all configured
channels, heartbeat monitor, and the cron scheduler.

### `doctor`

- `zeroclaw doctor`
- `zeroclaw doctor traces [--limit <N>] [--event <TYPE>] [--contains <TEXT>]`
- `zeroclaw doctor traces --id <TRACE_ID>`

`doctor traces` reads runtime tool/model diagnostics from
`observability.runtime_trace_path`.

### `status`

- `zeroclaw status`
- `zeroclaw status --format exit-code` — exits `0` if healthy, `1`
  otherwise. Intended for Docker `HEALTHCHECK`.

### `estop`

- `zeroclaw estop` (engage `kill-all`)
- `zeroclaw estop --level network-kill`
- `zeroclaw estop --level domain-block --domain "*.chase.com"`
- `zeroclaw estop --level tool-freeze --tool shell --tool browser`
- `zeroclaw estop status`
- `zeroclaw estop resume [--network] [--domain <pat>] [--tool <name>] [--otp <code>]`

Notes:

- `estop` requires `[security.estop].enabled = true`.
- When `[security.estop].require_otp_to_resume = true`, `resume`
  prompts for OTP if `--otp` is omitted.

### `cron`

- `zeroclaw cron list`
- `zeroclaw cron add <expr> [--tz <IANA_TZ>] <command>`
- `zeroclaw cron add-at <rfc3339_timestamp> <command>`
- `zeroclaw cron add-every <every_ms> <command>`
- `zeroclaw cron once <delay> <command>`
- `zeroclaw cron remove <id>`
- `zeroclaw cron pause <id>`
- `zeroclaw cron resume <id>`
- `zeroclaw cron update <id> [--expression <expr>] [--tz <tz>]`

Notes:

- Mutating schedule actions require `cron.enabled = true`.
- Shell command payloads are validated by security command policy
  before job persistence.

### `providers`

- `zeroclaw providers`

Lists supported provider IDs and the active provider from config.

### `channel`

- `zeroclaw channel list`
- `zeroclaw channel start`
- `zeroclaw channel doctor`
- `zeroclaw channel bind-telegram <IDENTITY>`
- `zeroclaw channel add <type> <json>`
- `zeroclaw channel remove <name>`
- `zeroclaw channel send <text> --channel-id <id> --recipient <id>`

Runtime in-chat commands available while `channel start` or `daemon` is
running:

- `/models` — show available providers and current selection
- `/models <provider>` — switch provider for this sender session
- `/model` — show current model
- `/model <model-id>` — switch model for this sender session
- `/new` — clear the sender's conversation history

`channel start` hot-applies updates to `default_provider`,
`default_model`, `default_temperature`, `api_key`, `api_url`, and
`reliability.*` from `config.toml` on the next inbound message.

### `skills`

- `zeroclaw skills list`
- `zeroclaw skills audit <source_or_name>`
- `zeroclaw skills install <source>`
- `zeroclaw skills remove <name>`

`<source>` accepts git remotes (`https://...`, `http://...`,
`ssh://...`, `git@host:owner/repo.git`) or a local filesystem path.
`skills install` always runs a built-in static security audit before
accepting a skill.

### `migrate`

- `zeroclaw migrate openclaw [--source <path>] [--dry-run]`

### `auth`

- `zeroclaw auth login --provider <id> [--profile <name>] [--device-code] [--import <path>]`
- `zeroclaw auth paste-redirect --provider <id> [--profile <name>] [--input <url_or_code>]`
- `zeroclaw auth paste-token --provider <id> [--profile <name>] [--token <t>] [--auth-kind <k>]`
- `zeroclaw auth setup-token --provider <id> [--profile <name>]`
- `zeroclaw auth refresh --provider <id> [--profile <id>]`
- `zeroclaw auth logout --provider <id> [--profile <name>]`
- `zeroclaw auth use --provider <id> --profile <name>`
- `zeroclaw auth list`
- `zeroclaw auth status`

### `memory`

- `zeroclaw memory stats`
- `zeroclaw memory list [--category <c>] [--session <s>] [--limit <n>] [--offset <n>]`
- `zeroclaw memory get <key>`
- `zeroclaw memory clear [--category <c>] --yes`

### `config`

- `zeroclaw config schema`

Prints a JSON Schema (draft 2020-12) for the full `config.toml`
contract to stdout.

### `update`

- `zeroclaw update [--check] [--force] [--version <version>]`

Downloads and installs the latest release with a 6-phase pipeline:
preflight, download, backup, validate, swap, smoke test. Automatic
rollback on failure.

### `self-test`

- `zeroclaw self-test`
- `zeroclaw self-test --quick`

`--quick` skips network-dependent checks for faster offline validation.

### `completions`

- `zeroclaw completions bash`
- `zeroclaw completions fish`
- `zeroclaw completions zsh`
- `zeroclaw completions powershell`
- `zeroclaw completions elvish`

`completions` is stdout-only by design so scripts can be sourced
directly without log/warning contamination.

### `desktop`

- `zeroclaw desktop` — launch the companion menu-bar app
- `zeroclaw desktop --install` — download and install the companion app

## Global Flags

- `--config-dir <PATH>` — override the config directory for this
  invocation (applies to every subcommand).

## Validation Tip

To verify docs against your current binary quickly:

```bash
zeroclaw --help
zeroclaw <command> --help
```
