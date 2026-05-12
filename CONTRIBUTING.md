# Contributing

Thanks for helping improve OfficeOS. This repository contains the control plane, backend agent loop, pod executor, dashboard, docs, deployment manifests, and channel sidecars for self-hosted AI agent infrastructure.

## Before You Start

- Use an issue for bugs, feature requests, and larger design changes.
- Keep pull requests focused on one behavior or architectural change.
- Prefer MCP-based integrations for new external tools.
- Do not preserve legacy integrations by default when replacing a system. Remove obsolete paths cleanly.
- Do not commit secrets, provider keys, kubeconfigs, local database dumps, or generated build output.

## Architecture Guidelines

Backend code uses clean architecture:

- `Api` handles transport and request boundaries.
- `Application` owns use cases, orchestration, and MediatR handlers.
- `Domain` owns rich domain records, invariants, and domain events.
- `Infrastructure` owns persistence, providers, and external services.

Database entities must stay decoupled from domain models. Repositories should map persistence entities to domain records. Prefer domain events for cross-boundary behavior instead of direct coupling.

Dashboard code is separated by domain under `apps/dashboard/src/features`. Keep agent, analytics, and manage concerns in their own `api`, `types`, and `components` areas. Tabs should use URL parameters rather than local JavaScript state.

## Development Workflow

1. Fork or branch from `main`.
2. Copy `.env.example` to `.env` only for local development.
3. Make the smallest coherent change.
4. Add or update tests when behavior changes.
5. Update docs when configuration, APIs, deployment steps, or contributor workflows change.
6. Open a pull request using the template.

Do not start the full application just to validate a contribution. Build, lint, or run focused tests for the code you changed.

Useful checks:

```bash
# Backend
cd apps/backend
dotnet build OffceOs.sln
dotnet test OffceOs.sln

# Dashboard
cd apps/dashboard
bun run lint
bun run build

# Pod executor
cd packages/pod-executor
go test ./...

# Channel sidecar
cd packages/channels
npm run build
```

## Pull Request Expectations

Pull requests should include:

- A short summary of the behavior or documentation changed.
- Any architecture or migration notes reviewers need.
- The checks you ran, or a clear reason a check was not run.
- Screenshots for dashboard or website UI changes.
- Security notes when the change touches credentials, execution, networking, auth, tenant isolation, or logging.

Review focuses on correctness, operational safety, maintainability, and consistency with the existing architecture.

## Reporting Security Issues

Do not open public issues for vulnerabilities. Follow [SECURITY.md](SECURITY.md) for private reporting.
