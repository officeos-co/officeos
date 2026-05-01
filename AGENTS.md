# EnterpriseAgentOs

Open-source, self-hosted, model-agnostic agent infrastructure. The obvious choice for companies that want to run AI agents on their own cloud. Not competing with Claude or ChatGPT — this is the Kubernetes of agents.

**Philosophy**: AI models will commoditize. The value is in the orchestration and integration layer. We use MCP (Model Context Protocol) as the universal standard for tool integrations — any community MCP server plugs in, and we also publish our own custom MCP servers for integrations that don't exist yet.

## Architecture

- **Pod executor** (`packages/pod-executor`) — shell-like environment for agents, deployed as K8s pods
- **Agentic loop** (`apps/backend/.../AgentTurnService.cs`) — the core reasoning loop, runs in the backend, max 25 iterations per turn
- **Tools** (`apps/backend/.../Tools/`) — built-in tools (shell, file_read, file_write, file_edit, content_search, glob_search, memory, http)
- **MCP integration** (in progress) — agents connect to MCP servers for external tool access. Replaces the old skill system.
- **Channels** (`apps/channels`) — TypeScript sidecar for Telegram, WhatsApp, Slack, Teams
- **Dashboard** (`apps/dashboard`) — Next.js operator UI

The backend uses strict structured logging — agent interactions are a sequence of typed log entries (message_in, tool_call, tool_result, message_out), not chat messages.

# Backend

The backend uses clean architecture: Api, Application, Domain, Infrastructure. Database entities are decoupled from domain models — each repository maps entities to rich domain records. We use event-driven architecture with MediatR domain events. This is critical — always use events where possible.

# Dashboard

Clean architecture with domain separation under `apps/dashboard/src/features` (agents, analytics, manage). Each domain has its own api, types, and components. Tabs use URL parameters, not JS state.

# Goals

- Agent becomes as good as claude code
- Full browser capabilities
- Full cloud coding capabilities
- MCP-native tool ecosystem with marketplace
- Model-agnostic — works with any LLM provider

<claude-mem-context>
# Memory Context

# [EnterpriseAgentOs] recent context, 2026-05-01 1:04pm GMT+2

No previous sessions found.
</claude-mem-context>
