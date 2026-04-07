# ZeroClaw Troubleshooting

Common failures and fast resolution paths for ZeroClaw agents.

## Build Issues (Developers)

### `cargo` not found

Install the Rust toolchain from <https://rustup.rs/>.

### Missing system build dependencies

Install a C toolchain and `pkg-config` via your OS package manager.
`rusqlite` with bundled SQLite compiles C locally, so a working C
compiler is required.

### Build fails on low-RAM / low-disk hosts

Symptoms:

- `cargo build --release` killed by the OOM killer (`signal: 9`)
- Build crashes with `cannot allocate memory`

Why this happens:

- Compile-time memory is much higher than runtime memory. A full release
  build can require **~2 GB RAM + swap** and **several GB free disk**.
- TLS/crypto native build scripts (`aws-lc-sys`, `ring`) add real cost.
- Bundled sqlite compiles C locally.

Mitigations:

```bash
# Limit cargo parallelism
CARGO_BUILD_JOBS=1 cargo build --release --locked
```

Or cross-compile on a stronger machine and copy the binary.

### Build is slow or appears stuck

Fast checks:

```bash
cargo check --timings
cargo tree -d
```

The timing report is written to `target/cargo-timings/cargo-timing.html`.

Avoid running multiple cargo jobs in parallel worktrees — cargo's
package/build-directory locks will serialize them anyway.

### Available Cargo features

Current features in `Cargo.toml`:

| Feature | Default | Purpose |
|---|---|---|
| `observability-prometheus` | yes | Prometheus metrics exporter |
| `skill-creation` | yes | Autonomous skill creation from successful tasks |
| `observability-otel` | no | OpenTelemetry OTLP exporter |
| `sandbox-landlock` | no | Linux Landlock sandbox |
| `rag-pdf` | no | PDF ingestion for datasheet RAG |

Build the full CI matrix with the `ci-all` meta-feature:

```bash
cargo check --features ci-all
```

## Runtime / Gateway

### Gateway unreachable

Checks from inside the agent pod:

```bash
zeroclaw status
zeroclaw doctor
curl -sf http://127.0.0.1:<gateway-port>/health
```

Verify config:

- `[gateway].host` (default `127.0.0.1`)
- `[gateway].port` (default `42617`)
- `allow_public_bind` only when intentionally exposing beyond the pod

Also verify the backing Kubernetes Service is routing traffic to the
pod's gateway port.

### Pairing / auth failures on webhook

1. Ensure pairing completed (`/pair` flow)
2. Ensure bearer token is current
3. Re-run `zeroclaw doctor`

## Channel Issues

### Telegram conflict: `terminated by other getUpdates request`

Cause: multiple pollers using the same bot token.

Fix: ensure only one agent pod (or local debug process) is using that
token at a time.

### Channel unhealthy

Inspect runtime state:

```bash
zeroclaw status
zeroclaw doctor
```

Then verify channel credentials and allowlist fields in the agent's
config (managed through the dashboard backend).

## Pod Issues

### Pod is CrashLoopBackOff

```bash
kubectl logs --previous <agent-pod>
kubectl describe pod <agent-pod>
```

Common causes:

- Missing or malformed identity vault files in the mounted ConfigMap.
  Required files: `SOUL.md`, `IDENTITY.md`, `AGENTS.md`. See
  [../reference/identity-vault.md](../reference/identity-vault.md).
- PVC not bound (sqlite memory file cannot be opened).
- Invalid provider credentials.

### Memory file corruption

Per-agent sqlite memory lives on the pod's PVC. If corrupted, stop the
pod, snapshot the PVC, and restore from backup. See
[../reference/memory-future.md](../reference/memory-future.md) for the
planned centralized memory service.

## Still Stuck?

Collect and include these outputs when filing an issue:

```bash
zeroclaw --version
zeroclaw status
zeroclaw doctor
kubectl logs --tail=500 <agent-pod>
```

Include sanitized config snippets (no secrets, no PII).

## Related Docs

- [operations-runbook.md](./operations-runbook.md)
- [resource-limits.md](./resource-limits.md)
- [config-reference.md](../reference/api/config-reference.md)
