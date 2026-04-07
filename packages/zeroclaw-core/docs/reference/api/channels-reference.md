# Channels Reference

This document is the canonical reference for channel configuration in
ZeroClaw.

ZeroClaw ships two real messaging channels plus the built-in CLI:

| Channel   | Kind                            | Public inbound port? |
|-----------|---------------------------------|----------------------|
| `cli`     | local stdin/stdout              | No                   |
| `telegram`| Bot API long-polling            | No                   |
| `webhook` | gateway HTTP endpoint           | Yes                  |

Anything else you may see referenced in historical docs or config schema
fields has no backing implementation and is ignored at runtime.

Verify the set yourself with:

```bash
ls src/channels/*.rs | grep -v test.rs | grep -v mod.rs
```

## 1. Configuration Namespace

All channel settings live under `channels_config` in `~/.zeroclaw/config.toml`.

```toml
[channels_config]
cli = true                       # enable the interactive CLI channel
message_timeout_secs = 300       # base per-message LLM+tool budget
ack_reactions = true             # add 👀 / ✅ / ⚠️ receipts
show_tool_calls = false          # forward tool-call notes to the channel
session_persistence = true       # persist per-session history to disk
session_backend = "sqlite"       # "sqlite" (default) or "jsonl" (legacy)
session_ttl_hours = 0            # 0 = never auto-archive
debounce_ms = 0                  # accumulate burst messages before dispatch
```

Each channel is enabled by creating its sub-table (for example
`[channels_config.telegram]`).

## 2. Allowlist Semantics

For channels with inbound sender allowlists:

- Empty allowlist: deny all inbound messages.
- `"*"`: allow all inbound senders (use for temporary verification only).
- Explicit list: allow only listed senders.

## 3. Per-Channel Configuration

### 3.1 CLI

The CLI channel has no config other than the toggle:

```toml
[channels_config]
cli = true
```

It is used by `zeroclaw agent` for interactive/single-shot prompts and by
the internal test harness. It never touches the network.

### 3.2 Telegram

```toml
[channels_config.telegram]
bot_token = "123456:telegram-token"
allowed_users = ["*"]
stream_mode = "off"               # optional: off | partial
draft_update_interval_ms = 1000   # optional: edit throttle for partial streaming
mention_only = false              # optional: require @mention in groups
interrupt_on_new_message = false  # optional: cancel in-flight same-sender request
```

Notes:

- Delivery mode is Telegram Bot API long-polling, so no inbound public
  port is required.
- `interrupt_on_new_message = true` preserves the interrupted user turn
  in conversation history, then restarts generation on the newest message.
  The scope is strict: same sender in the same chat.
- `stream_mode = "partial"` sends an editable draft message that updates
  as the LLM streams tokens; `draft_update_interval_ms` controls the edit
  throttle.

### 3.3 Webhook

`channels_config.webhook` enables a generic inbound webhook handled by
the gateway. Point your custom integration at the gateway's
`POST /webhook` endpoint.

```toml
[channels_config.webhook]
port = 8080
secret = "optional-shared-secret"
```

Notes:

- Run with `zeroclaw gateway` or `zeroclaw daemon` and verify `/health`.
- When `secret` is set, requests must send a matching
  `X-Webhook-Secret` header; unmatched requests are rejected.
- Gateway pairing rules (`[gateway].require_pairing`) still apply.

## 4. Inbound Image Marker Protocol

ZeroClaw supports multimodal input through inline message markers:

- Syntax: `` [IMAGE:<source>] ``
- `<source>` can be:
  - Local file path
  - Data URI (`data:image/...;base64,...`)
  - Remote URL only when `[multimodal].allow_remote_fetch = true`

Operational notes:

- Marker parsing applies to user-role messages before provider calls.
- Provider capability is enforced at runtime: if the selected provider
  does not support vision, the request fails with a structured
  capability error (`capability=vision`).

## 5. Validation Workflow

1. Configure the channel with a permissive allowlist (`"*"`) for initial
   verification.
2. Run:

   ```bash
   zeroclaw daemon
   ```

3. Send a message from an expected sender.
4. Confirm a reply arrives.
5. Tighten the allowlist from `"*"` to explicit IDs.

## 6. Troubleshooting Checklist

If a channel appears connected but does not respond:

1. Confirm the sender is allowed by the channel's allowlist field.
2. Confirm bot tokens/secrets are valid and not revoked.
3. Confirm transport assumptions:
   - Telegram polls outbound, so no inbound port is needed.
   - Webhook requires a reachable HTTPS callback from your integration.
4. Restart `zeroclaw daemon` after config changes.

## 7. Runtime Supervisor

If a channel task crashes or exits, the channel supervisor in
`src/channels/mod.rs` restarts it automatically with exponential backoff
(`[reliability].channel_initial_backoff_secs` →
`channel_max_backoff_secs`) and emits:

- `Channel <name> exited unexpectedly; restarting`
- `Channel <name> error: ...; restarting`
- `Channel message worker crashed:`

Inspect preceding log lines for the root cause.
