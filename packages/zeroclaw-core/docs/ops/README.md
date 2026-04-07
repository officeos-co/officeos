# Operations Docs

Operational guides for running ZeroClaw agents in the Kubernetes-based
deployment model.

## Deployment Model

ZeroClaw agents run as Kubernetes Pods managed by the dashboard backend
(`apps/dashboard/backend`). Each agent is a Pod + Service + ConfigMap
(identity vault files) + PVC (sqlite memory) provisioned by the backend's
`K8sManager`. There is no bare-metal install path — `zeroclaw-core` is a
library plus a CLI binary that runs as the pod's main process.

For the provisioning flow, see the dashboard backend
`app/services/zeroclaw/k8s_manager.py`.

## Core Operations

- Day-2 runbook: [./operations-runbook.md](./operations-runbook.md)
- Troubleshooting: [./troubleshooting.md](./troubleshooting.md)
- Proxy configuration: [./proxy-agent-playbook.md](./proxy-agent-playbook.md)
- Resource limits and quotas: [./resource-limits.md](./resource-limits.md)

## Common Flow

1. Validate runtime inside the pod (`zeroclaw status`, `zeroclaw doctor`)
2. Apply one config change at a time (usually by updating the pod spec
   or identity ConfigMap through the dashboard backend)
3. Restart the agent pod
4. Verify provider and gateway health
5. Roll back quickly if behavior regresses

## Related

- Config reference: [../reference/api/config-reference.md](../reference/api/config-reference.md)
- Identity vault: [../reference/identity-vault.md](../reference/identity-vault.md)
- Future memory service: [../reference/memory-future.md](../reference/memory-future.md)
