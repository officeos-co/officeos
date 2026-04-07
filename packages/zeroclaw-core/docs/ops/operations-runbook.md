# ZeroClaw Operations Runbook

Day-2 operations guide for ZeroClaw agents running as Kubernetes pods.

## Deployment Context

In production, ZeroClaw agents are provisioned and supervised by the
dashboard backend (`apps/dashboard/backend`). Each agent is:

- a **Pod** running `zeroclaw daemon` as its main command
- a **Service** exposing the gateway port
- a **ConfigMap** holding the agent's identity vault files
  (`SOUL.md`, `IDENTITY.md`, `AGENTS.md`, etc.) mounted at `/vault-workspace`
- a **PVC** holding the per-agent sqlite memory file

Pod lifecycle (create / restart / delete) is driven by the backend's
`K8sManager` (`app/services/zeroclaw/k8s_manager.py`). Operators should
prefer backend-mediated actions over `kubectl` surgery except during
incident response.

For identity vault structure, see
[../reference/identity-vault.md](../reference/identity-vault.md).

## Scope

Use this document for:

- starting and supervising agent pods
- health checks and diagnostics
- safe rollout and rollback
- incident triage and recovery

## Runtime Modes

| Mode | Command | When to use |
|---|---|---|
| Agent daemon | `zeroclaw daemon` | default pod entrypoint — long-running agent loop |
| Gateway only | `zeroclaw gateway start` | debugging webhook / WebSocket surface |
| One-shot agent | `zeroclaw agent -m "…"` | scripted single-turn invocations |

## Local Development Mode (non-production)

For debugging changes locally, you can run the binary directly against a
workspace directory — no Kubernetes required. This is **not** a
supported production deployment path.

```bash
# Build
cargo build --release

# Point at a local workspace dir containing identity files
export ZEROCLAW_WORKSPACE="$PWD/.zeroclaw-dev/workspace"
mkdir -p "$ZEROCLAW_WORKSPACE"

# Run the daemon in the foreground
./target/release/zeroclaw daemon
```

A container image can also be run directly for reproducible local
debugging — mount a workspace directory and expose the gateway port. Do
not use this for production; the dashboard backend owns pod lifecycle.

## Baseline Operator Checklist

Run inside the agent pod (e.g. via `kubectl exec …`):

```bash
zeroclaw status
zeroclaw doctor
```

The daemon itself is started automatically as the pod's main process, so
you normally don't run `zeroclaw daemon` by hand — restart the pod
instead.

## Health and State Signals

| Signal | Source | Expected |
|---|---|---|
| Config validity | `zeroclaw doctor` | no critical errors |
| Runtime summary | `zeroclaw status` | expected provider/model/channels |
| Gateway liveness | `GET /health` on the pod's gateway port | 200 OK |
| Metrics | `GET /metrics` on the pod's gateway port | Prometheus scrape target |
| Pod state | `kubectl get pod <agent>` | `Running`, recent `READY` |

## Logs and Diagnostics

Agent stdout/stderr are captured by Kubernetes:

```bash
kubectl logs -f <agent-pod>
kubectl logs --previous <agent-pod>   # last crash
```

For deeper inspection, exec into the pod:

```bash
kubectl exec -it <agent-pod> -- zeroclaw status
kubectl exec -it <agent-pod> -- zeroclaw doctor
```

## Incident Triage Flow (Fast Path)

1. Snapshot state:

```bash
kubectl get pod <agent-pod> -o wide
kubectl logs --tail=200 <agent-pod>
kubectl exec <agent-pod> -- zeroclaw status
kubectl exec <agent-pod> -- zeroclaw doctor
```

2. Hit the gateway health endpoint:

```bash
kubectl exec <agent-pod> -- curl -sf http://127.0.0.1:<gateway-port>/health
```

3. If the pod is unhealthy, restart it:

```bash
kubectl delete pod <agent-pod>   # controller recreates
```

4. If provider or channel calls still fail, verify credentials and
   allowlists in the agent's config (managed through the dashboard
   backend) and check outbound network reachability from the pod.

5. If the gateway is involved, verify bind/auth settings (`[gateway]`)
   and that the backing Service routes traffic correctly.

## Safe Change Procedure

Before applying config changes:

1. snapshot the current agent config via the dashboard backend
2. apply one logical change at a time
3. roll the agent pod
4. run `zeroclaw doctor` inside the new pod
5. verify with `status` and `/health`

## Rollback Procedure

If a rollout regresses behavior:

1. restore the previous agent config through the dashboard backend
2. roll the pod
3. confirm recovery via `doctor` and `/health`
4. document root cause and mitigation

## Related Docs

- [troubleshooting.md](./troubleshooting.md)
- [resource-limits.md](./resource-limits.md)
- [proxy-agent-playbook.md](./proxy-agent-playbook.md)
- [config-reference.md](../reference/api/config-reference.md)
- [identity-vault.md](../reference/identity-vault.md)
