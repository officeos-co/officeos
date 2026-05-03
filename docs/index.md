# EnterpriseAgentOS

EnterpriseAgentOS is open-source, self-hosted, model-agnostic agent infrastructure for companies that want to run AI agents on their own cloud.

## Architecture

User opens the dashboard and creates an agent. The backend creates a Kubernetes pod running the agent runtime and stores the agent record in Postgres. The pod boots with a minimal environment, fetches its bootstrap payload from the backend, mounts its workspace, discovers available tools, and serves a WebSocket chat gateway.

All LLM calls route through the backend proxy so credentials stay in the backend. External integrations are moving toward MCP as the universal tool standard.

## Repository Layout

| Directory | Description |
| --- | --- |
| `apps/backend/` | C# ASP.NET Core backend for agent lifecycle, LLM proxy, tool execution, and Kubernetes orchestration |
| `apps/dashboard/` | Next.js operator dashboard |
| `apps/docs/` | Fumadocs documentation app |
| `packages/pod-executor/` | Shell-like execution environment for agents, deployed as Kubernetes pods |
| `packages/channels/` | TypeScript sidecars for Telegram, WhatsApp, Slack, and Teams |
