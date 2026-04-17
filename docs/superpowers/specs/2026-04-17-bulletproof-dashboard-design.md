# Bulletproof React Refactor — dashboard

**Date:** 2026-04-17
**Scope:** `apps/dashboard/src/` only

## Goal

Reorganize dashboard from flat `hooks/`, `data/`, `components/` folders into Bulletproof React feature-based architecture. Routes stay in `app/` as thin wrappers; business logic moves into `src/features/`.

## Feature modules

Three features matching the sidebar navigation groups:

### `features/agents/` — Managed Agents
- **api/**: `useAgents`, `useIntegrations`, `useChannels`, `useAgentTemplates`, `useSendAgentMessage`, `useProviders`
- **components/**: `credential-dialog.tsx`, `channel-onboarding-dialog.tsx`
- **data/**: `integrations.ts`, `channels.ts`, `agent-mock.ts`, `agents-list-mock.ts`, `agent-templates.ts`
- **index.ts**: barrel export

### `features/analytics/` — Analytics
- **api/**: `useAgentLogs`, `useGlobalLogs`, `useAnalytics`
- **data/**: `analytics-mock.ts`
- **index.ts**: barrel export

### `features/manage/` — Manage
- **api/**: `useProfile`, `useOrganization`, `useBilling`, `usePricing`
- **components/**: `login-form.tsx`
- **data/**: `billing-mock.ts`
- **index.ts**: barrel export

## Stays at top level (shared)

- **`components/`**: app-sidebar, nav-main, nav-user, nav-projects, team-switcher, page-header, log-table, permission-cards, analytics-pageview, auth-guard
- **`components/ui/`**: shadcn primitives (untouched)
- **`contexts/`**: AuthContext
- **`hooks/`**: useAuth, use-mobile
- **`lib/`**: graphql client, utils, mock-mode

## Route pages

Each `page.tsx` under `app/(dashboard)/` becomes a thin wrapper that imports from its feature module. Loading skeletons stay in route folders.

## Rules

- No cross-feature imports
- Features export via barrel `index.ts`
- All `@/hooks/` and `@/data/` imports updated to `@/features/<name>/api/` or `@/features/<name>/data/`
- No behavioral changes — pure file reorganization
