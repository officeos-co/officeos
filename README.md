<p align="center">
  <img src="apps/website/public/logo.svg" height="80" alt="OfficeOS" />
</p>

<h3 align="center">Deploy autonomous AI agents across your company</h3>

<p align="center">
  Enterprise knowledge, custom skills, full infrastructure control.
  <br />
  Self-host in minutes or use <a href="https://dashboard.officeos.co">OfficeOS Cloud</a>.
</p>

<p align="center">
  <a href="https://officeos.co">Website</a> &middot;
  <a href="https://docs.officeos.co">Docs</a> &middot;
  <a href="https://dashboard.officeos.co">Dashboard</a> &middot;
  <a href="https://github.com/HarKro753/EnterpriseAgentOs/issues">Issues</a>
</p>

---

## Highlights

- **Managed agents** — deploy, monitor, and control AI agents from a single dashboard
- **Multi-channel** — connect agents to Slack, Discord, Teams, Telegram, WhatsApp
- **Custom skills** — extend agents with TypeScript skills (email, calendar, browser, APIs)
- **BYOK** — bring your own LLM keys (Anthropic, OpenAI, Google, xAI) or use platform keys
- **Self-hosted** — run the full stack with Docker Compose, no Kubernetes required
- **12-Factor config** — secrets via environment variables, zero hardcoded credentials

---

## Recognized by

<p align="center">
  <img src="apps/website/public/logos/microsoft.png" height="30" alt="Microsoft" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/dtu-skylab.png" height="30" alt="DTU Skylab" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/siteimprove.png" height="30" alt="Siteimprove" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/techbbq.png" height="30" alt="TechBBQ" />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/elsass-fonden.png" height="30" alt="Elsass Fonden" />
</p>

---

## Quick Start (Self-Hosted)

```bash
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs

cp apps/backend/.env.example .env
# Edit .env — add at least one LLM provider key

docker compose up
```

Open [localhost:3000](http://localhost:3000) and sign in with Google.

> **That's it.** Postgres, Redis, backend, dashboard, skill runtime, and channel gateway all start automatically. Agents run as Docker containers — no Kubernetes needed.

---

## Architecture

| Service           | Port | Stack                                                       |
| ----------------- | ---- | ----------------------------------------------------------- |
| **Dashboard**     | 3000 | Next.js 16, React 19, Apollo GraphQL                        |
| **Backend**       | 8000 | .NET 9, HotChocolate GraphQL, EF Core                       |
| **Skill Runtime** | 3001 | Node.js, esbuild, TypeScript skills                         |
| **Channels**      | 3100 | Node.js gateway (Slack, Discord, Teams, Telegram, WhatsApp) |
| **Postgres**      | 5432 | Primary database                                            |
| **Redis**         | 6379 | Distributed cache                                           |

```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│  Dashboard   │────▶│   Backend    │────▶│ Skill Runtime │
│  (Next.js)   │     │  (.NET API)  │     │  (TypeScript)  │
└─────────────┘     └──────┬───────┘     └───────────────┘
                           │
                    ┌──────┴───────┐
                    │   Channels   │
                    │  (Gateway)   │
                    └──────────────┘
```

---

## Environment Management

Secrets are injected via environment variables — never committed to the repo. See [docs/environment-management.md](docs/environment-management.md) for the full guide covering Doppler, Kubernetes secrets, and self-hosted configuration.

### What self-hosters need to configure

```bash
# Required
CONNECTION_STRING=Host=localhost;Port=5432;Database=eaos;Username=eaos;Password=eaos
REDIS=localhost:6379
FRONTEND_ORIGIN=http://localhost:3000

# At least one LLM key
PLATFORMKEYS__ANTHROPICAPIKEY=sk-ant-...
```

Google OAuth works out of the box for `localhost:3000`. Stripe, PostHog, and Minio are disabled in development — no config needed.

---

## Development

| Service   | Command                                                             |
| --------- | ------------------------------------------------------------------- |
| Dashboard | `cd apps/dashboard && bun dev`                                      |
| Backend   | `cd apps/backend && dotnet run --project src/EnterpriseAgentOs.Api` |
| Website   | `cd apps/website && bun dev`                                        |
| Docs      | `cd apps/docs && bun dev`                                           |

Or run everything with Docker:

```bash
docker compose up
```

---

## OfficeOS Cloud

Don't want to self-host? Use the managed platform at **[dashboard.officeos.co](https://dashboard.officeos.co)** — includes billing, usage analytics, team management, and platform LLM keys.

---

## Documentation

- [Environment Management](docs/environment-management.md) — secrets, Doppler, K8s operator
- [Agent Lifecycle](docs/agent-lifecycle.md) — how agents are created, deployed, and managed
- [Skills](docs/skills.md) — writing custom TypeScript skills
- [Runners](docs/runners.md) — agent execution infrastructure

---

## Contributing

Contributions are welcome. Open an issue first to discuss what you'd like to change.

```bash
# Clone and install
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs

# Backend
cd apps/backend && dotnet build EnterpriseAgentOs.sln

# Dashboard
cd apps/dashboard && bun install && bun dev
```

---

## License

MIT

---

<p align="center">
  Built by <a href="https://github.com/HarKro753">Harro Krog</a>
</p>
