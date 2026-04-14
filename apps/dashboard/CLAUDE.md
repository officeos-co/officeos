# dashboard — Next.js 16 + React 19

Mission Control dashboard for managing agents, providers, skills, runners, and system events.

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
    layout.tsx              Root layout (AppShell — sidebar + main area)
    page.tsx                Dashboard home
    agents/
      page.tsx              Agent list (grid, polling, delete)
      [id]/page.tsx         Agent detail (tabs: chat, logs, memory, skills, config, etc.)
    skills/
      page.tsx              Skill marketplace/registry (install/configure)
      [name]/page.tsx       Skill detail (docs, credentials, tools)
    providers/page.tsx      Provider configuration (API keys)
    runners/page.tsx        Runner management (create, monitor)
    settings/               Settings sub-routes (org, profile, api-keys, billing, channels, limits)
    system-events/page.tsx  SSE-streamed system event log
    (fullscreen)/           Auth pages (login, pricing) — skip sidebar
    api/auth/               OAuth callback routes (Google, me, logout)
    error.tsx               Global error boundary (must be "use client")
  components/
    shared/                 Layout & reusable UI (AppShell, AuthGuard, Sidebar, TopBar, Modal, etc.)
    agents/                 Agent domain components (chat, logs, memory, config, crons, etc.)
    skills/                 Skill domain components (cards, forms, overlays)
    providers/              Provider domain components
    runners/                Runner domain components
    ui/                     shadcn/ui primitives (button, input, dialog, table, etc.)
  hooks/
    useApi.ts               Generic apiFetch<T>(path, init?) wrapper
    useAuth.ts              Auth check + logout
    useAgents.ts            Agent CRUD + 10s polling + pub-sub cache
    useSkills.ts            Skill CRUD (install/uninstall/credentials)
    useSkillRegistry.ts     Registry metadata
    useCustomSkills.ts      Custom skill upload/GitHub import
    useProviders.ts         Provider list + configure
    useProviderModels.ts    Per-provider model list
    useRunners.ts           Runner CRUD + polling
    useAgentSkills.ts       Per-agent skill assignment
    useAgentChannels.ts     Channel integration
    useChannelConnections.ts  Connection status
    useHeartbeatTasks.ts    Health check tasks
    useSystemEvents.ts      System event SSE stream
  types/                    TypeScript interfaces (agent, skill, provider, auth, runner, channel, heartbeat)
  lib/
    utils.ts                cn() helper (clsx + tailwind-merge)
    agentProxy.ts           Pod proxy helpers (REST + WebSocket URLs)
    chatSession.ts          Chat session persistence (localStorage + pod hydration)
    backend.ts              Backend URL config
    posthog.ts              PostHog initialization
    format.ts               Date/ID formatting
```

## Architecture patterns

### Data fetching

- **All fetching through hooks.** Each entity has a `use*` hook in `hooks/`. Components never call `fetch` directly — they use `apiFetch` or `agentFetch`.
- **Agent data from backend, not pod.** Skills, providers, agent records come from `/api/*` (backend). Only Chat, Logs, Sessions, Memory, and Doctor fetch from the pod via `agentFetch`.
- **Pub-sub cache.** `useAgents`, `useProviders`, `useRunners` share data via module-level cache + listener sets. First caller fetches, others subscribe.
- **Polling.** 10s interval with `document.visibilityState` check — don't poll when tab is hidden.

### State management

- **No Redux/Zustand.** React hooks only — `useState` for local UI, pub-sub cache for shared data, refs for WebSocket/EventSource cleanup.
- **localStorage** for chat session persistence, hydrated from pod on mount.

### Streaming

- **WebSocket** for agent chat (`agentWsUrl()` handles localhost vs prod).
- **SSE (EventSource)** for agent logs and system events.
- **Reconnect:** Exponential backoff 1s → 30s cap, max 10 retries. Three states: connected / reconnecting / disconnected.

### Styling

- **Tailwind CSS 4 + CSS variables.** Design tokens in `globals.css` using oklch.
- **cn()** (clsx + tailwind-merge) for all class composition.
- **CVA** (class-variance-authority) for component variants (e.g. button).
- **CSS variables:** `--background`, `--foreground`, `--primary`, `--secondary`, `--muted`, `--border`, `--eaos-*` (backward compat).
- **StatusBadge** for all status displays — consistent color coding across the app.

### Component patterns

- **`"use client"` on interactive components.** Pages and components using hooks, state, or event handlers.
- **Domain folders** under `components/` — agents, skills, providers, runners.
- **Shared components** for cross-cutting UI (TopBar, Modal, EmptyState, StatusBadge).
- **shadcn/ui** for primitives — never build custom buttons, inputs, dialogs from scratch.
- **Tab routers** for detail pages (AgentDetailTabs pattern with URL search params).
- **Overlays** for creation/configuration flows (NewAgentOverlay, UploadSkillOverlay, etc.).

### Types

- **Strict TypeScript.** All components typed, generic hooks return typed data.
- **Type files** in `src/types/` — one per domain entity.
- **Hooks re-export types** for convenience: `export type { Agent } from "@/types/agent"`.

### Analytics

- **PostHog** initialized in `instrumentation-client.ts`.
- Events: `tab_switched`, `agent_deleted`, etc.
- User identification via `posthog.identify()`.

## Key rules

- Data fetching only through `use*` hooks — never raw `fetch` in components.
- Agent data from backend, pod data from pod proxy. Don't mix these up.
- All status displays go through `StatusBadge.tsx`.
- Use `agentWsUrl()` for WebSocket URLs — never hardcode `ws://` or `wss://`.
- Use CSS variables for colors — never hardcode hex/rgb values.
- Interactive components must have `"use client"` directive.
- Use `useRouter().push()` for programmatic navigation, not `Link`.
- Content and copy live inline in the component — no centralized config objects.

## Anti-patterns

- Do not fetch from agent pods for data the backend owns (skills, providers, agent records).
- Do not add `error.tsx` without `"use client"` — Next.js error boundaries must be client components.
- Do not create centralized config/content objects that separate copy from components — keep content inline.
- Do not introduce external state libraries (Redux, Zustand) — use the pub-sub cache pattern in hooks.
- Do not hardcode poll intervals — use the established 10s pattern with visibility check.
