# Maintainers

Docs for repository maintainers. Smaller than it used to be — the project-triage snapshot, docs inventory, repo map, i18n coverage map, and refactor-candidates list were all deleted during the Phase 4 docs-alignment pass because they tracked historical state that no longer corresponds to the codebase.

## Contents

- [`trademark.md`](trademark.md) — ZeroClaw trademark and brand usage policy.

## Historical notes

The strip-down sequence is documented in `STRIP_DOWN.md` at the repo root. That file is the authoritative record of what was deleted and why; it replaces the previous "project triage snapshot" + "docs inventory" + "refactor candidates" trio with a single per-phase log.

For current repository structure, the source tree itself is the reference. `src/` under `packages/zeroclaw-core/` is now small enough (~97k production LOC, down from ~292k) that a human can read through the module tree directly. There is no longer a separate repo-map doc to keep in sync.
