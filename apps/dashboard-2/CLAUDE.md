# dashboard-2 — Next.js 16 + Bun + shadcn sidebar-07

Alpha dashboard. Built from scratch on shadcn's sidebar-07 template. All data is mock — no backend wiring yet.

## Commands

```bash
bun install
bun run dev          # Dev server on :3000
bun run build        # Production build
npx tsc --noEmit     # Type check only
```

## Project structure

```
src/
  app/
    layout.tsx                    Root layout — fonts, TooltipProvider. No sidebar here.
    page.tsx                      Redirects to /agents
    globals.css                   Design tokens (oklch), matches website color system
    pricing/page.tsx              Fullscreen pricing page — no sidebar (outside route group)
    (dashboard)/
      layout.tsx                  Sidebar shell — SidebarProvider + AppSidebar + SidebarInset
      agents/
        page.tsx                  Agent list — table with search, status filter, pagination
        [id]/page.tsx             Agent detail — sticky header, URL-driven tabs (agent, logs, memory, cron)
      quickstart/page.tsx         Agent creation — templates sidebar, config form, channel permissions. "Launch agent" calls useCreateAgent and routes to /agents/{id}. There is no /agents/new route.
      integrations/
        page.tsx                  Integration marketplace — cards, add/explore filter, credential dialog
        [slug]/page.tsx           Integration detail — SKILL.md rendered, tools card
      channels/
        page.tsx                  Channel marketplace — cards, connect/available filter, onboarding dialog
        [slug]/page.tsx           Channel detail — capabilities, documentation
      logs/page.tsx               Global log aggregation — table with agent/type filters, pagination
      usage/page.tsx              Token usage analytics — Recharts bar charts, stat cards
      cost/page.tsx               Cost analytics — stacked bar chart, cost breakdown cards
      profile/page.tsx            User profile — name, preferences, notification toggles
      team/page.tsx               Organization — name, member table, invite dialog
      billing/page.tsx            Subscription — plan, payment, extra usage, invoices
  components/
    app-sidebar.tsx               Sidebar with 3 nav groups + docs link + user menu
    nav-main.tsx                  Collapsible nav groups with active route detection
    nav-user.tsx                  User dropdown — support, legal links, logout
    team-switcher.tsx             Org header (AgentOS branding)
    page-header.tsx               Shared page header — SidebarTrigger + breadcrumbs + action slot
    log-table.tsx                 Reusable log table — icon, type, source, content, duration, time
    permission-cards.tsx          Reusable tool/channel permission cards with ask/allow/deny toggles
    credential-dialog.tsx         Dialog overlay for integration credential setup
    channel-onboarding-dialog.tsx Dialog overlay for channel connection wizard
    ui/                           shadcn/ui primitives — all components installed
  data/
    integrations.ts               Integration definitions — tools, credentials, SKILL.md, added state
    channels.ts                   Channel definitions — onboarding steps, permissions, capabilities
    agent-mock.ts                 Mock agent detail, log entries, file tree for memory
    analytics-mock.ts             Mock data for logs, usage, cost pages
```

## Sidebar structure

```
Managed Agents (collapsible, auto-opens on active route)
  ├── Quickstart
  ├── Agents
  ├── Integrations
  └── Channels

Analytics
  ├── Logs
  ├── Usage
  └── Cost

Manage
  ├── Profile
  ├── Team
  └── Billing

───────────────
Documentation (external link)
User card (name + plan → dropdown)
```

## Data model separation

**Integrations** = API-based tools agents call (GitHub, Notion, Linear, etc.)

- Have `tools: Tool[]` with name + description
- Have `credentials: CredentialField[]` for setup
- Have `added: boolean` state
- Have `skillMd` for documentation

**Channels** = WebSocket/webhook session connectors (Slack, Discord, Telegram, etc.)

- Have `capabilities: string[]` describing what they can do
- Have `onboarding: OnboardingStep[]` for guided setup (url, qr, copy, input steps)
- Have `defaultPermissions: ChannelPermissions` (receive/send/initiate → allow/ask/deny)
- Have `added: boolean` state

**These are fundamentally different.** Integrations are request/response APIs. Channels are persistent bidirectional connections that integrate into the agent's session.

## Key patterns

**Tab routing via URL params.** Agent detail uses `?tab=agent|logs|memory|cron`. Use `<Link>` with query params, not JS state.

**Overlays for configuration.** Credential setup (integrations) and onboarding wizards (channels) use shadcn `Dialog`. Never inline forms.

**Permission model.** Tool permissions cycle through ask → allow → deny per tool. Channel permissions have 3 dimensions: receive, send, initiate. Both use the shared `permission-cards.tsx` components.

**Status badges.** Use the showcase-style pattern: `rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-widest` with colored backgrounds (emerald=running, amber=pending, red=failed, zinc=stopped).

**Centered max-width.** All content pages use `max-w-{size} mx-auto w-full`. Agent detail uses `max-w-6xl`. Settings pages use `max-w-3xl`. Analytics use `max-w-4xl`.

**Charts.** Use Recharts (already installed). Bar charts for usage/cost, stacked bars for cost breakdown.

**Markdown rendering.** Use `react-markdown` + `remark-gfm` for SKILL.md and memory files. Styled via Tailwind `prose` classes.

## Design system

Colors match the website (`apps/website/`):

- `--background`: warm off-white `oklch(98.46% 0.002 247.84)`
- `--primary`: near-black `oklch(0.205 0 0)` — buttons, text
- `--secondary`: teal `oklch(55% 0.15 220)` — accent, org icon
- Font: Geist Sans via `--font-geist-sans`
- Radius: `0.625rem`
- All colors use oklch

## Key rules

- **All data is mock.** `data/*.ts` files contain hardcoded mock data. When wiring to the backend, replace these with hooks following the patterns from dashboard v1.
- **shadcn/ui for all primitives.** Button, Input, Dialog, Select, Switch, Tabs, etc. — never build custom.
- **`"use client"` on interactive pages.** Any page with useState, useEffect, or event handlers.
- **Server components for static pages.** Placeholder pages and layouts stay as server components.
- **No overview/dashboard page.** `/` redirects to `/agents`. Every page serves a specific purpose.
- **Reuse shared components.** `LogTable`, `ToolPermissionCard`, `ChannelPermissionCard`, `CredentialDialog`, `ChannelOnboardingDialog`, `PageHeader` — don't duplicate.

## Anti-patterns

- Do not add an overview/home dashboard page. It's a useless aggregation layer.
- Do not mix integrations and channels in the same data model. They are fundamentally different (API vs WebSocket).
- Do not inline credential forms or onboarding wizards. Use Dialog overlays.
- Do not use `Date.now()` in mock data at module level. It causes hydration mismatches. Use static strings.
- Do not add state management libraries. React useState is sufficient for the alpha.
- Do not create pages without `max-w-*` + `mx-auto`. Content should never stretch to full viewport width.

## Backend wiring

The dashboard talks to the backend exclusively over GraphQL at
`POST {NEXT_PUBLIC_API_URL}/api/dashboard/graphql` (subscriptions over WS at the
same path). Apollo is configured in `src/lib/graphql/client.ts` and provided to
the tree via `src/app/providers.tsx` (wrapped inside `layout.tsx`). Session auth
rides on cookies via `credentials: "include"`.

### Mock toggle

`NEXT_PUBLIC_USE_MOCKS=1` → hooks return `data/*.ts` fixtures; no network calls.
Unset / any other value → hooks call GraphQL via `apolloClient`.

The toggle is checked at the **hook layer**, not the client layer — so Apollo is
always mounted (it can still run auth/session side-effects) while individual
hooks short-circuit to mocks. Every hook file follows the same shape:

```ts
import { USE_MOCKS } from "@/lib/graphql/mock-mode"

export function useFoo() {
  if (USE_MOCKS) {
    return { data: mockFoo, loading: false, error: null }
  }
  return useGeneratedFooQuery(...)
}
```

The return shape must be identical in both branches so page/component code does
not care which mode it's in.

### Hooks

Domain data access always goes through hooks under `src/hooks/`. Current
hooks:

| Hook | Backing query / mutation | Shape |
|---|---|---|
| `useAgents` | `agents` | List rows for /agents |
| `useAgent(id)` | `agent(id)` | Detail view |
| `useCreateAgent` | `createAgent(input)` — takes `{ name, model, systemPrompt, toolNames, channelSlugs }`, translates `model` → `provider` at the boundary | Returns `{ id, name }` |
| `useUpdateAgent` / `useDeleteAgent` | `updateAgent` / `deleteAgent` | — |
| `useIntegrations` | `skills` (integrations == skills in the backend) | Merges UI-only metadata from `data/integrations.ts` with live catalog |
| `useSkillComments` / `useLikeSkill` / `useCommentOnSkill` | `skillComments`, `likeSkill`, `commentOnSkill` | — |
| `useChannels` | `channelTypes` + `channelConnections` | Channel catalog merged with connection state |
| `useCreateChannelConnection` / `useDeleteChannelConnection` / `useBindChannelToAgent` | matching mutations | — |
| `useAgentTemplates` / `useCreateAgentFromTemplate` | templates endpoints | — |
| `useAgentLogs` / `useGlobalLogs` / `useSendAgentMessage` / `useProviders` / `useRunners` / `useBilling` / `useAudit` | respective GraphQL ops | — |
| `useAnalytics` | typed `track*` mutations — see PostHog section | — |

### Codegen

`bun run codegen` regenerates typed operations + hooks into
`src/lib/graphql/generated/` (requires the backend running on `:5000`, override
via `GRAPHQL_SCHEMA_URL`). `.graphql` operation documents live in
`src/lib/graphql/operations/`. Regenerate after any backend schema change.

### Rules

- **Do not import from `src/data/` outside hooks.** Pages and components read
  domain data only through hooks so the mock toggle flips cleanly and Stage 8
  can migrate one domain at a time without touching UI code.
- **Do not add AuthGuard yet** — that lands after hooks are wired.
- **Do not move the mock check into the Apollo link.** Apollo must behave
  normally for auth/session; mocking is a hook-layer concern.

### Analytics

PostHog events go through the **backend**, not a client snippet. Each use-case
has a dedicated typed GraphQL mutation — there is no generic
`captureEvent(name, properties)`. `useAnalytics()` in
`src/hooks/useAnalytics.ts` exposes one function per event:

- `trackPageView(path)`
- `trackNavClicked(destination)`
- `trackSkillInstalled(skillName)`
- `trackSkillConfigured(skillName)`
- `trackChannelConnected(channelSlug)`
- `trackAgentCreated({ agentName, provider, template, skillCount, allowSkills, denySkills })`

Each function calls its matching `track*` mutation on the backend, which
forwards to PostHog using the server-side API key (see
`apps/backend/Entities/PostHog/`). With `NEXT_PUBLIC_USE_MOCKS=1` every call
is a `console.debug` no-op. Page-level `$pageview` events are emitted globally
by `components/analytics-pageview.tsx` mounted in `(dashboard)/layout.tsx`.

**Never install the `posthog-js` snippet in dashboard-2.** All capture goes
through the backend so the PostHog key never leaves the server.

Adding a new event requires a matching backend mutation — see
`apps/backend/Entities/PostHog/EVENTS.md` for the contract and the full
catalog.

### Env

See `.env.local.example`:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_USE_MOCKS=1
```
