# Dashboard Agent Instructions

This app is a small Next.js operator dashboard for OfficeOS resources. Keep it aligned with the CLI and VS Code extension resource model instead of rebuilding the old product dashboard.

## Scope

- `src/app/page.tsx`: main authenticated resource dashboard.
- `src/app/login`: OAuth sign-in surface.
- `src/app/cli/activate`: CLI device-code approval.
- `src/app/api/auth/[...path]`: auth proxy to the backend.
- `src/lib/resources.ts`: resource categories and display helpers.
- `src/lib/env.ts`: environment-derived backend URLs.

## Rules

- Resource categories should match the VS Code extension and CLI: agents, runs, channels, routines, browsers, memorystores, engines, providers, models.
- Use backend REST endpoints under `/api/v1`, not GraphQL.
- Keep UI dense and operational: lists, status, JSON details, logs.
- Do not reintroduce feature-specific dashboard domains unless the backend resource API requires them.
- Run `bun run lint` and `bun run build` after dashboard changes.
