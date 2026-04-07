# Resource Limits for ZeroClaw

> **Status: Operational guidance + roadmap.**
>
> Pod-level limits described below are the supported path today. Rust-side
> in-process limits are proposals. For current runtime behavior, see
> [config-reference.md](../reference/api/config-reference.md),
> [operations-runbook.md](./operations-runbook.md), and
> [troubleshooting.md](./troubleshooting.md).

## Problem

ZeroClaw has rate limiting (20 actions/hour) but no in-process resource
caps. A runaway agent could:

- Exhaust available memory
- Spin CPU at 100%
- Fill the PVC with logs/output

Because agents now run as Kubernetes pods, the pragmatic first line of
defense is pod-level resources and quotas, not custom Rust allocators.

---

## Pod-Level Limits (Recommended)

Set `resources.requests` and `resources.limits` on the agent pod spec.
The dashboard backend's `K8sManager` is the component that writes these
fields — adjust its defaults there, not by hand on live pods.

Example baseline for a single agent:

```yaml
resources:
  requests:
    cpu: "100m"
    memory: "256Mi"
  limits:
    cpu: "1000m"
    memory: "512Mi"
    ephemeral-storage: "1Gi"
```

PVC size (for the per-agent sqlite memory file) should be small by
default (e.g. 1–2 GiB) and grown only when memory-heavy agents need it.

## Namespace Quotas

For multi-agent deployments, enforce a `ResourceQuota` on the agent
namespace so one runaway pod cannot starve the rest:

```yaml
apiVersion: v1
kind: ResourceQuota
metadata:
  name: zeroclaw-agents
spec:
  hard:
    requests.cpu: "8"
    requests.memory: "16Gi"
    limits.cpu: "32"
    limits.memory: "64Gi"
    persistentvolumeclaims: "50"
```

Pair with a `LimitRange` to force every pod to declare requests/limits.

---

## In-Process Limits (Proposals)

These are not implemented yet. They remain on the roadmap for cases
where pod-level limits are not fine-grained enough (per-command CPU
budget, per-tool memory cap).

### Option 1: per-command CPU timeout

```rust
use tokio::time::{timeout, Duration};

pub async fn execute_with_timeout<F, T>(
    fut: F,
    cpu_time_limit: Duration,
) -> Result<T>
where
    F: Future<Output = Result<T>>,
{
    timeout(cpu_time_limit, fut).await?
}
```

### Option 2: heap accounting via a custom global allocator

Track heap usage and abort if over a configured limit. Intrusive and
platform-sensitive; use only if pod limits are insufficient.

---

## Proposed Config Schema

```toml
[resources]
# CPU limits
max_cpu_time_seconds = 60

# Disk I/O limits
max_log_size_mb = 100
max_temp_storage_mb = 500

# Process limits
max_subprocesses = 10
max_open_files = 100
```

---

## Implementation Priority

| Phase | Feature | Effort | Impact |
|-------|---------|--------|--------|
| **P0** | Pod `resources.limits` + namespace `ResourceQuota` | Low | High |
| **P1** | Per-command CPU timeout in core | Low | High |
| **P2** | Heap accounting allocator | Medium | Medium |
| **P3** | Per-tool disk I/O caps | Medium | Medium |
