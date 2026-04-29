# OfficeOS

# Local Development

| Service   | Port | Command                         |
| --------- | ---- | ------------------------------- |
| Dashboard | 3000 | `cd apps/dashboard && bun dev`  |
| Website   | 3001 | `cd apps/website && bun dev`    |
| Docs      | 3002 | `cd apps/docs && bun dev`       |
| Backend   | 5000 | `cd apps/backend && dotnet run` |

## Self-Hosting (Docker Compose)

```bash
docker compose up
```

This starts the full stack: postgres, redis, backend, dashboard, channel gateway, and skill runtime. The backend mounts the Docker socket to deploy agent containers dynamically — no Kubernetes required.

| Service       | Port | Notes                                                        |
| ------------- | ---- | ------------------------------------------------------------ |
| Dashboard     | 3000 | Next.js frontend                                             |
| Backend       | 8000 | .NET API, manages agent containers via Docker                |
| Skill Runtime | 3001 | Executes skills (bundled TypeScript actions)                  |
| Channels      | 3100 | Channel gateway (Slack, Discord, Teams, Telegram, WhatsApp)  |
| Postgres      | 5432 | Primary database                                             |
| Redis         | 6379 | Distributed cache                                            |

# Kubernetes Ingress Endpoints

## Production

| Domain                  | Service                                                     |
| ----------------------- | ----------------------------------------------------------- |
| `officeos.co`           | `http://eaos-website-prod.default.svc.cluster.local:3000`   |
| `dashboard.officeos.co` | `http://eaos-frontend-prod.default.svc.cluster.local:3000`  |
| `api.officeos.co`       | `http://eaos-backend-prod.default.svc.cluster.local:8000`   |
| `docs.officeos.co`      | `http://eaos-docs-prod.default.svc.cluster.local:3000`      |
| `changelog.officeos.co` | `http://eaos-changelog-prod.default.svc.cluster.local:3000` |

## Staging

| Domain                          | Service                                                       |
| ------------------------------- | ------------------------------------------------------------- |
| `staging.officeos.co`           | `http://eaos-website-staging.default.svc.cluster.local:3000`  |
| `staging-dashboard.officeos.co` | `http://eaos-frontend-staging.default.svc.cluster.local:3000` |
| `staging-api.officeos.co`       | `http://eaos-backend-staging.default.svc.cluster.local:8000`  |
