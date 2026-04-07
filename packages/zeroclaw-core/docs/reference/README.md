# Reference Catalogs

Structured reference index for commands, providers, channels, and config.

## Core References

- Commands by workflow: [cli/commands-reference.md](cli/commands-reference.md)
- Provider IDs / aliases / env vars: [api/providers-reference.md](api/providers-reference.md)
- Channel setup + allowlists: [api/channels-reference.md](api/channels-reference.md)
- Config defaults and keys: [api/config-reference.md](api/config-reference.md)

## Usage

Use this collection when you need precise CLI/config details or provider
integration patterns rather than step-by-step tutorials.

The ground truth for this reference is the source tree under `src/`:

- `src/main.rs` — CLI command surface
- `src/providers/mod.rs` — provider factory and aliases
- `src/channels/` — channel implementations
- `src/config/schema.rs` — full `Config` struct and defaults
- `src/tools/mod.rs` — `all_tools_with_runtime()` tool registry

If a doc here disagrees with the source, the source wins — please open a
docs fix.
