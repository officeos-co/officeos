# zeroclaw-core Documentation

`zeroclaw-core` is a Rust library crate: the agent runtime that powers Office OS agent pods. It is consumed by the Python dashboard backend in `apps/dashboard/backend/`, which provisions agent pods on Kubernetes. There is no bare-metal installation path.

If you're looking for **how to run an agent**, start with the dashboard backend docs, not here. The docs in this directory describe the zeroclaw-core runtime, its configuration surface, its extension points, and how to contribute.

## Quick navigation

| I want to… | Read this |
| --- | --- |
| Understand the post-strip-down architecture | [`STRIP_DOWN.md`](../../../STRIP_DOWN.md) (at repo root) |
| Look up a CLI subcommand | [`reference/cli/commands-reference.md`](reference/cli/commands-reference.md) |
| Look up a config key | [`reference/api/config-reference.md`](reference/api/config-reference.md) |
| Look up a provider ID or alias | [`reference/api/providers-reference.md`](reference/api/providers-reference.md) |
| Look up a channel config | [`reference/api/channels-reference.md`](reference/api/channels-reference.md) |
| Understand the per-agent Obsidian vault (identity source of truth) | [`reference/identity-vault.md`](reference/identity-vault.md) |
| Read the planned centralized memory service | [`reference/memory-future.md`](reference/memory-future.md) |
| Operate a running agent pod (day-2) | [`ops/operations-runbook.md`](ops/operations-runbook.md) |
| Diagnose a failing agent pod | [`ops/troubleshooting.md`](ops/troubleshooting.md) |
| Add a new provider, channel, or tool | [`contributing/change-playbooks.md`](contributing/change-playbooks.md) |
| Open a PR against zeroclaw-core | [`contributing/pr-workflow.md`](contributing/pr-workflow.md) |
| Understand CI | [`contributing/ci-map.md`](contributing/ci-map.md) |
| Read security design proposals | [`security/README.md`](security/README.md) |

## Collections

- **Setup guides** — [`setup-guides/`](setup-guides/README.md): MCP server registration, Z.AI/GLM provider setup. Bare-metal install, Homebrew, Windows, and macOS update/uninstall guides were deleted — Office OS is K8s-only.
- **Reference catalogs** — [`reference/`](reference/README.md): authoritative CLI, config, provider, channel, and identity-vault docs.
- **Operations** — [`ops/`](ops/README.md): day-2 runbook, troubleshooting, resource limits, proxy playbook. All oriented at K8s-deployed agent pods.
- **Security** — [`security/README.md`](security/README.md): sandboxing, audit-logging, threat model. **Design proposals**, not always current behaviour.
- **Contributing** — [`contributing/`](contributing/README.md): PR discipline, extension points, testing.
- **Architecture** — [`architecture/`](architecture/): ADRs.
- **Maintainers** — [`maintainers/`](maintainers/README.md): repo governance and trademark.

## Not in these docs

The following topics used to live here but have been deleted along with the subsystems they described:

- **Bare-metal install** (`install.sh`, `setup.bat`, `flake.nix`, Docker Compose, Homebrew formula, Scoop, AUR) — Office OS is K8s-only.
- **Peripherals / hardware** (STM32, RPi GPIO, Arduino, ESP32, datasheet RAG) — Phase 2.2–2.3 deletion.
- **Tunnels** (Cloudflare, ngrok, Tailscale, Pinggy, OpenVPN) — Phase 4 deletion. The pod is exposed via K8s Service/Ingress.
- **Deleted channels** (Matrix, Discord, Slack, WhatsApp, Signal, Nostr, 38 others) — only Telegram + webhook remain.
- **Deleted providers** (Gemini, Bedrock, Copilot, Azure OpenAI, Claude Code, OpenAI Codex, Telnyx) — only anthropic, openai, ollama, openrouter, reliable, router, and the `compatible` wrapper remain.
- **SOP engine, Verifiable Intent, Trust scoring, Plugins (WASM), Voice wake, WebAuthn** — all deleted in Phase 2/4.
- **AIEOS JSON identity** — Phase 3 made markdown-only identity the source of truth; AIEOS was deleted.
- **Interactive onboard wizard** (`zeroclaw onboard`) — deleted Phase 2.3. Agent provisioning is handled by the dashboard backend.

See [`STRIP_DOWN.md`](../../../STRIP_DOWN.md) for the complete strip-down record.

## Contributing to these docs

Every doc in this directory should describe **current behaviour**, not historical state or aspirational design. Security proposals and the memory-future spec are the only exceptions, and they're clearly labelled.

When you change the surviving surface (add a provider, change a config key, add a CLI command), update the corresponding reference doc in the same PR. See [`contributing/docs-contract.md`](contributing/docs-contract.md).
