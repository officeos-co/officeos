<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="apps/website/public/logo-white.svg" />
    <source media="(prefers-color-scheme: light)" srcset="apps/website/public/logo.svg" />
    <img src="apps/website/public/logo.svg" height="80" alt="OfficeOS" />
  </picture>
</p>

<h1 align="center">The AI workforce for your company</h1>

<p align="center">
Employees that work 24/7, know everything about your company, and never need onboarding.
<br/><br/>
<a href="https://officeos.co">Website</a> · <a href="https://docs.officeos.co">Docs</a> · <a href="https://dashboard.officeos.co">Cloud</a> · <a href="https://docs.officeos.co/quickstart">Getting Started</a> · <a href="https://github.com/HarKro753/EnterpriseAgentOs/issues">Issues</a>
</p>

<br/>

OfficeOS deploys autonomous AI agents across your company. Each agent has persistent memory, enterprise knowledge, custom skills, and responds in the channels your team already uses — Slack, Teams, WhatsApp, Telegram, Discord, email.

The dashboard is the control plane. The product is the agent.

Self-host the full stack with `docker compose up` or use [OfficeOS Cloud](https://dashboard.officeos.co).

<br/>

## Highlights

- **Agents, not chatbots** — persistent agents with memory, knowledge graphs, and their own container runtime
- **Multi-channel** — agents respond in Slack, Discord, Teams, Telegram, WhatsApp, email
- **Custom skills in TypeScript** — email, calendar, browser automation, CRM sync, contract review — attach to any agent
- **Works with your stack** — Notion, GitHub, Salesforce, Google Workspace, Jira, HubSpot, and dozens more
- **Automated schedules** — cron-based agent tasks: competitor scans, CRM syncs, weekly reports, briefings
- **Self-hosted** — runs on your infrastructure, data never leaves your network. Or use our cloud
- **BYOK** — bring your own LLM keys (Anthropic, OpenAI, Google, xAI) or use platform-managed keys

<br/>

## Recognized by

<p align="center">
  <img src="apps/website/public/logos/microsoft.png" height="28" alt="Microsoft" />&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/dtu-skylab.png" height="28" alt="DTU Skylab" />&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/siteimprove.png" height="28" alt="Siteimprove" />&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/techbbq.png" height="28" alt="TechBBQ" />&nbsp;&nbsp;&nbsp;&nbsp;
  <img src="apps/website/public/logos/elsass-fonden.png" height="28" alt="Elsass Fonden" />
</p>

<br/>

## Quick Start

```bash
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs

cp apps/backend/.env.example .env
# Add at least one LLM provider key

docker compose up
```

Open [localhost:3000](http://localhost:3000) and sign in with Google. That's it.

Postgres, Redis, backend, dashboard, skill runtime, and channel gateway all start automatically. Agents run as Docker containers — no Kubernetes needed.

<br/>

## Architecture

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

| Service       | Port | Stack                                       |
| ------------- | ---- | ------------------------------------------- |
| Dashboard     | 3000 | Next.js 16, React 19, Apollo GraphQL        |
| Backend       | 8000 | .NET 9, HotChocolate GraphQL, EF Core       |
| Skill Runtime | 3001 | Node.js, esbuild, bundled TypeScript skills |
| Channels      | 3100 | Node.js gateway for messaging platforms     |
| Postgres      | 5432 | Primary database                            |
| Redis         | 6379 | Distributed cache                           |

<br/>

## Configuration

Self-hosters only need a few environment variables. Everything else has sensible defaults.

```bash
CONNECTION_STRING=Host=localhost;Port=5432;Database=eaos;Username=eaos;Password=eaos
REDIS=localhost:6379
FRONTEND_ORIGIN=http://localhost:3000
PLATFORMKEYS__ANTHROPICAPIKEY=sk-ant-...
```

Google OAuth works out of the box on `localhost:3000`. Stripe, PostHog, and object storage are disabled in development.

Full guide: [docs/environment-management.md](docs/environment-management.md)

<br/>

## Development

| Service   | Command                                                             |
| --------- | ------------------------------------------------------------------- |
| Dashboard | `cd apps/dashboard && bun dev`                                      |
| Backend   | `cd apps/backend && dotnet run --project src/EnterpriseAgentOs.Api` |
| Website   | `cd apps/website && bun dev`                                        |
| Docs      | `cd apps/docs && bun dev`                                           |

Or run everything: `docker compose up`

<br/>

## Cloud

Don't want to self-host? [dashboard.officeos.co](https://dashboard.officeos.co) — managed agents with billing, analytics, team management, and platform LLM keys.

<br/>

## Docs

- [Getting Started](https://docs.officeos.co/quickstart)
- [Environment Management](docs/environment-management.md)
- [Agent Lifecycle](docs/agent-lifecycle.md)
- [Skills](docs/skills.md)
- [Runners](docs/runners.md)

<br/>
