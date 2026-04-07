# Setup Guides

These are the small setup guides that still apply to `zeroclaw-core`. The bulk of setup (agent provisioning, K8s pod spec, vault creation, dashboard UI) lives in the dashboard backend docs, not here.

## Agent provisioning

Agent lifecycle is managed by the dashboard backend (`apps/dashboard/backend/`). To create a new agent:

1. Use the dashboard UI or `POST /api/v1/agents`.
2. The backend provisions a per-agent Obsidian vault, seeds it with template `.md` files, creates a Kubernetes `Pod` + `Service` + `ConfigMap` + `PVC`, and returns the agent ID.
3. The agent pod boots, reads its personality files from the mounted ConfigMap at `/vault-workspace`, and starts `zeroclaw daemon`.

See [`../reference/identity-vault.md`](../reference/identity-vault.md) for the full vault architecture.

## Guides in this directory

- [`mcp-setup.md`](mcp-setup.md) — register an MCP server for the agent's tool surface (stdio, SSE, HTTP transports).
- [`zai-glm-setup.md`](zai-glm-setup.md) — configure the Z.AI / GLM provider via the `compatible` wrapper.

## What's not here anymore

Deleted during the strip-down (Phases 2–4):

- `one-click-bootstrap.md` — referenced deleted `install.sh`.
- `windows-setup.md` — Windows bare-metal installation is no longer supported.
- `macos-update-uninstall.md` — referenced deleted `install.sh` and `zeroclaw service` commands.
- `mattermost-setup.md`, `nextcloud-talk-setup.md` — channels were deleted in Phase 2.4.

## Validating a running agent

Once a pod is running, validate it from within the pod (via `kubectl exec`) or the dashboard:

```bash
zeroclaw status       # current config summary + component health
zeroclaw doctor       # deeper diagnostic probes
```

Both commands read the mounted config and report on identity, memory, providers, and channels. See [`../reference/cli/commands-reference.md`](../reference/cli/commands-reference.md).
