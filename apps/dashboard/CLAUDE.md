# dashboard — Next.js 16 + React 19

The product. Operators use this to create agents, install skills, configure providers, monitor runners, and inspect system events.

## Commands

```bash
npm install
npm run dev          # Dev server on :3000
npx tsc --noEmit     # Type check only, no output
npm run build        # Production build
npm run lint         # ESLint
```

## Project structure

```
src/
  app/
    layout.tsx                    Root layout — AppShell (sidebar + main). Always server component.
    page.tsx                      Dashboard home
    agents/
      page.tsx                    Agent list — grid, 10s polling, delete
      [id]/page.tsx               Agent detail — tabs via URL search params (chat, logs, memory, skills, config)
    skills/
      page.tsx                    Skill marketplace — install, configure, browse registry
      [name]/page.tsx             Skill detail — docs, credentials form, tool list
    providers/page.tsx            Provider configuration — API keys per provider
    runners/page.tsx              Runner management — create, monitor
    settings/                     Sub-routes: org, profile, api-keys, billing, channels, limits
    system-events/page.tsx        SSE-streamed system event log
    (fullscreen)/                 Auth pages (login, pricing) — no sidebar
    api/auth/                     OAuth callback routes (Google, me, logout)
    error.tsx                     Global error boundary — must be "use client"
  components/
    shared/                       Cross-cutting UI: AppShell, AuthGuard, Sidebar, TopBar, Modal, EmptyState, StatusBadge
    agents/                       Agent domain: chat, logs, memory, config, crons, skills tab
    skills/                       Skill domain: cards, credential forms, overlays
    providers/                    Provider domain: config cards, model pickers
    runners/                      Runner domain: create form, status table
    ui/                           shadcn/ui primitives only — button, input, dialog, table, badge
  hooks/                          Every hook described below — see hook conventions section
  types/                          TypeScript interfaces — one file per domain entity
  lib/
    utils.ts                      cn() helper (clsx + tailwind-merge)
    agentProxy.ts                 Pod proxy URL helpers — REST + WebSocket
    chatSession.ts                Chat session persistence (localStorage + pod hydration)
    backend.ts                    Backend base URL config
    posthog.ts                    PostHog init
```

## Hook conventions — follow exactly

All data fetching goes through `hooks/use{Entity}.ts`. Components never call `fetch` directly. There are two hook patterns:

### Pattern 1: Pub-sub cache (for globally shared entities)

Used by: `useAgents`, `useProviders`, `useRunners`

```typescript
"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { Agent } from "@/types/agent";

export type { Agent } from "@/types/agent";                   // re-export the type

let cache: Agent[] | null = null;                              // module-level cache
const listeners = new Set<(agents: Agent[]) => void>();        // subscriber set

function publish(agents: Agent[]) {                            // update cache + notify all
  cache = agents;
  listeners.forEach((fn) => fn(agents));
}

export function useAgents() {
  const [agents, setAgents] = useState<Agent[]>(cache ?? []);
  const [loading, setLoading] = useState(cache === null);
  const [error, setError] = useState<string | null>(null);

  const refetch = useCallback(async () => { /* fetch → publish() */ }, []);

  useEffect(() => {
    const listener = (next: Agent[]) => setAgents(next);       // subscribe
    listeners.add(listener);
    if (cache === null) refetch();                              // first caller fetches
    return () => { listeners.delete(listener); };              // unsubscribe on unmount
  }, [refetch]);

  // Optional: 10s polling with visibility guard
  useEffect(() => {
    const tick = () => { if (document.visibilityState === "visible") refetch(); };
    const id = setInterval(tick, 10_000);
    return () => clearInterval(id);
  }, [refetch]);

  // CRUD methods that call apiFetch then refetch()
  return { agents, loading, error, refetch, create, remove };
}
```

**Key properties:**
- Module-level `cache` + `listeners` — first caller fetches, all others subscribe instantly
- `publish()` updates cache and notifies every mounted instance
- Mutation methods (create, remove, configure) call `refetch()` after the API call
- Polling uses `document.visibilityState` guard — never polls a hidden tab

### Pattern 2: Simple fetch (for page-scoped entities)

Used by: `useSkills`, `useCustomSkills`, `useAgentSkills`, `useAgentChannels`, `useChannelConnections`, `useHeartbeatTasks`, `useSkillRegistry`, `useProviderModels`, `useSystemEvents`

```typescript
"use client";

import { useCallback, useEffect, useState } from "react";
import { apiFetch } from "./useApi";
import type { Skill } from "@/types/skill";

export type { Skill } from "@/types/skill";                    // re-export the type

export function useSkills() {
  const [skills, setSkills] = useState<Skill[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => { /* fetch → setSkills */ }, []);
  useEffect(() => { refresh(); }, [refresh]);

  // CRUD methods that call apiFetch then update local state with setSkills
  return { skills, loading, error, refresh, install, uninstall, putCredentials };
}
```

**Key properties:**
- No module-level cache — each mount fetches independently
- Mutations update local state via `setSkills` (optimistic or refetch)
- No polling unless explicitly needed

### Every hook

| Hook | Pattern | Entity | API path | Notes |
|------|---------|--------|----------|-------|
| `useApi` | — | — | — | Exports `apiFetch<T>()` — all hooks import from here |
| `useAuth` | Special | User | `/api/auth/me` | Auth check + logout, not a data hook |
| `useAgents` | Pub-sub | Agent | `/api/agents` | 10s polling, create/remove |
| `useProviders` | Pub-sub | Provider | `/api/providers` | configure/clear |
| `useRunners` | Pub-sub | Runner | `/api/runners` | 10s polling |
| `useSkills` | Simple | Skill | `/api/skills` | install/uninstall/putCredentials/setRunTarget |
| `useSkillRegistry` | Simple | SkillRegistry | `/api/skill-registry` | publish/unpublish |
| `useCustomSkills` | Simple | CustomSkill | `/api/custom-skills` | upload/importFromGithub |
| `useAgentSkills` | Simple | AgentSkill | `/api/agents/{id}/skills` | assign/unassign |
| `useAgentChannels` | Simple | Channel | `/api/agents/{id}/channels` | assign/unassign |
| `useChannelConnections` | Simple | Connection | `/api/channels` | connect/disconnect |
| `useHeartbeatTasks` | Simple | Heartbeat | `/api/agents/{id}/heartbeat` | |
| `useProviderModels` | Simple | Model | `/api/providers/{name}/models` | |
| `useSystemEvents` | SSE | SystemEvent | `/api/system-events/stream` | EventSource, not fetch |

### Adding a new hook

1. Create `hooks/use{Entity}.ts`.
2. Define the type in `types/{entity}.ts`.
3. Import `apiFetch` from `./useApi`.
4. Re-export the type: `export type { Entity } from "@/types/entity"`.
5. If the data is globally shared and used on multiple pages → use Pattern 1 (pub-sub cache).
6. If the data is page-scoped → use Pattern 2 (simple fetch).
7. Return `{ entities, loading, error, refetch/refresh, ...mutations }`.

## Architecture patterns

**Two API surfaces — never mix them:**
- `apiFetch` → backend (`/api/*`) — owns agents, skills, providers, runners, auth
- `agentFetch` / `agentWsUrl` → agent pod proxy — owns chat, logs, memory, sessions, health

**Tab routing via URL search params.** Agent detail and skill detail pages use `?tab=` for deep linking. Use `useRouter().push()`, not `Link`, for programmatic tab navigation.

**WebSocket for chat.** Use `agentWsUrl()` from `lib/agentProxy.ts`. Reconnect uses exponential backoff: 1s → 30s cap, max 10 retries.

**SSE for logs and system events.** `EventSource` with same backoff pattern. Three states: `connected` / `reconnecting` / `disconnected`.

## Key rules

- **All data fetching through hooks.** Components never call `fetch` directly. Follow Pattern 1 or Pattern 2 above.
- **Re-export types from hooks.** Components import types from the hook file, not from `types/` directly.
- **Status via `StatusBadge` only.** Every status indicator goes through `components/shared/StatusBadge.tsx`.
- **`"use client"` on interactive components.** Any component using hooks, state, refs, or event handlers needs it.
- **shadcn/ui for primitives.** Do not build custom buttons, inputs, dialogs — use `components/ui/`.
- **CSS variables for colors.** The token system is in `globals.css` using oklch. Never hardcode hex or rgb.
- **Content inline in components.** All copy and data arrays live in the component that renders them.

## Anti-patterns

- Do not fetch skills, providers, or agent records from the pod. The backend owns them. The pod only owns chat, logs, memory, sessions, and health.
- Do not create a new data fetching pattern. Use Pattern 1 (pub-sub cache) or Pattern 2 (simple fetch) exactly as described. No SWR, no React Query, no custom wrappers.
- Do not call `fetch` directly in components. Always go through `apiFetch` via a `use*` hook.
- Do not skip the type re-export in hook files. Components should import types from `hooks/use{Entity}`, not from `types/`.
- Do not use `Link` for programmatic navigation. Use `useRouter().push()`.
- Do not add `error.tsx` without `"use client"`. Next.js error boundaries must be client components.
- Do not introduce state management libraries (Redux, Zustand, Jotai). Use the pub-sub cache pattern.
- Do not hardcode `ws://` or `wss://` URLs. Use `agentWsUrl()` from `lib/agentProxy.ts`.
- Do not create centralized content config objects. Copy lives inline in the component.
- Do not poll on a hidden tab. The visibility guard in `useAgents` is intentional — replicate it in any new polling hook.
