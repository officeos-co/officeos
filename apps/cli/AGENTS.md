# EAOS CLI Agent Instructions

The CLI is a Bun/TypeScript command-line client for EnterpriseAgentOs. Keep command wiring, feature behavior, shared transport/config, and terminal output in separate lanes.

## Project Layout

- `src/app`: command entrypoint and top-level routing.
- `src/features/auth`: login and identity commands plus auth-specific API calls.
- `src/features/manifests`: declarative YAML validate/diff/apply/export commands.
- `src/lib`: cross-cutting API client, config store, environment defaults, and file helpers.
- `src/shell`: terminal output, errors, and small prompt/browser helpers.
- `src/__tests__`: focused CLI tests.

Feature roots may contain only `api`, `commands`, `data`, `types.ts`, and `index.ts`. Keep folders flat like the dashboard.
