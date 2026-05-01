<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="apps/website/public/logo-white.svg" />
    <source media="(prefers-color-scheme: light)" srcset="apps/website/public/logo.svg" />
    <img src="apps/website/public/logo.svg" height="80" alt="OfficeOS logo" />
  </picture>
</p>

<h1 align="center">The AI workforce for your company</h1>

<p align="center">
Employees that work 24/7, know everything about your company, and never need onboarding.
<a href="https://officeos.co">Website</a> · <a href="https://docs.officeos.co">Docs</a> · <a href="https://dashboard.officeos.co">Cloud</a> · <a href="https://docs.officeos.co/quickstart">Getting Started</a> · <a href="https://github.com/HarKro753/EnterpriseAgentOs/issues">Issues</a>
</p>

<br/>

OfficeOS deploys autonomous AI agents across your company. Each agent has persistent memory, enterprise knowledge, custom skills, and responds in the channels your team already uses — Slack, Teams, WhatsApp, Telegram, Discord, email.

The dashboard is the control plane. The product is the agent.

Self-host the full stack with Docker Compose or Kubernetes, or use [OfficeOS Cloud](https://dashboard.officeos.co).

## Highlights

- **Agents, not chatbots** — persistent agents with memory, knowledge graphs, and their own container runtime
- **Multi-channel** — agents respond in Slack, Discord, Teams, Telegram, WhatsApp, email
- **Custom skills in TypeScript** — email, calendar, browser automation, CRM sync, contract review — attach to any agent
- **Works with your stack** — Notion, GitHub, Salesforce, Google Workspace, Jira, HubSpot, and dozens more
- **Automated schedules** — cron-based agent tasks: competitor scans, CRM syncs, weekly reports, briefings
- **Self-hosted** — runs on your infrastructure, data never leaves your network. Or use our cloud
- **BYOK** — bring your own LLM keys (Anthropic, OpenAI, Google, xAI) or use platform-managed keys

## Quick Start

```bash
git clone https://github.com/HarKro753/EnterpriseAgentOs.git
cd EnterpriseAgentOs

cp apps/backend/.env.example .env
# Add at least one LLM provider key

docker compose up
```

## Kubernetes

OfficeOS is built for self-hosters who want to run agent infrastructure on their own cloud. Kubernetes manifests are available under `k8s/` for production and staging deployments.

```bash
kubectl apply -f k8s/prod/
```

## Development

```bash
# Dashboard
cd apps/dashboard && bun dev

# Backend
cd apps/backend && dotnet run --project src/EnterpriseAgentOs.Api

# Website
cd apps/website && bun dev

# Docs
cd apps/docs && bun dev
```
