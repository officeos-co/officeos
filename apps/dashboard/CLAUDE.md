# dashboard — Next.js 16 + React 19

Mission Control dashboard for managing agents, providers, and skills.

## Commands

```bash
npm install
npm run dev          # Dev server on :3000
npx tsc --noEmit     # Type check
```

## Project structure

```
src/
  app/
    layout.tsx              Root layout (sidebar + main area)
    page.tsx                Dashboard home
    agents/
      page.tsx              Agent list (grid, polling, delete)
      [id]/page.tsx         Agent detail (tabs, chat, logs, memory, skills)
    skills/
      page.tsx              Skill grid (install/configure)
      [name]/page.tsx       Skill detail (docs, credentials, tools)
    providers/
      page.tsx              Provider config (API keys)
  components/
    TopBar.tsx              Page header (title, subtitle, actions)
    StatusBadge.tsx         Color-coded status badges
    AgentDetailTabs.tsx     Tab router for agent detail page
    AgentChatPanel.tsx      WebSocket chat with reconnect
    AgentLogsPanel.tsx      SSE log streaming with reconnect
    AgentMemoryPanel.tsx    CouchDB vault file editor
    AgentSkillsListPanel.tsx  Per-agent skill list (backend /api/skills)
    SkillGridCard.tsx       Skill card for the grid page
    SkillCredentialsForm.tsx  Credential input form (reused)
    NewAgentOverlay.tsx     Agent creation modal (provider + model select)
    Modal.tsx               Generic modal wrapper
  hooks/
    useAgents.ts            Agent CRUD + 10s polling + pub-sub cache
    useSkills.ts            Skill CRUD (install/uninstall/credentials)
    useProviders.ts         Provider list + configure
    useProviderModels.ts    Per-provider model list
    useApi.ts               Generic fetch wrapper
  lib/
    agentProxy.ts           Pod proxy helpers (REST + WebSocket URLs)
    chatSession.ts          Chat session persistence (localStorage + pod hydration)
```

## Key rules

- **Data fetching via hooks.** Each entity has a `use*` hook in `hooks/`. Components don't call `fetch` directly — they use `apiFetch` or `agentFetch`.
- **Agent data from backend, not pod.** The Skills tab fetches from `/api/skills` (backend), not the pod's `/api/integrations`. Only Chat, Logs, Sessions, and Doctor fetch from the pod via `agentFetch`.
- **Polling via `useAgents`.** 10s interval with visibility check. The hook publishes to all listeners via a shared cache.
- **Reconnect on WS/SSE.** Chat and Logs panels have exponential backoff reconnect (1s → 30s cap, 10 retries). Three-state indicator: connected / reconnecting / disconnected.
- **`"use client"` on interactive components.** Pages and components that use hooks, state, or event handlers must have `"use client"` directive.
- **Color scheme via CSS variables.** `--eaos-bg`, `--eaos-panel`, `--eaos-border`, `--eaos-text-muted`. Do not hardcode colors — use the variables.
- **StatusBadge for all status displays.** Agent status, skill status, connection status — all go through `StatusBadge.tsx` for consistent color coding.

## Anti-patterns

- Do not fetch from agent pods for data that the backend owns (skills, providers, agent records).
- Do not add `error.tsx` files without the `"use client"` directive — Next.js error boundaries must be client components.
- Do not use `Link` for programmatic navigation — use `useRouter().push()`.
- Do not hardcode `ws://` or `wss://` URLs — use `agentWsUrl()` which handles localhost vs production.
