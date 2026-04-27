# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
bun dev              # Start dev server (Next.js)
bun run build        # Production build
bun run lint         # ESLint
bun test             # Run all tests
bun test src/__tests__/foo.test.ts  # Run a single test
bun run codegen      # Regenerate GraphQL types from backend schema
bun run codegen:watch
```

The backend must be running at localhost:5000 for dev and codegen (schema URL: `http://localhost:5000/api/dashboard/graphql`).

## Architecture

Next.js 16 app (App Router, React 19, Tailwind v4, bun). Uses Apollo Client for all data fetching via GraphQL (`/api/dashboard/graphql` — proxied to backend via Next.js rewrites).

### Feature-based structure (Bulletproof React, approach A)

- **`src/features/{agents,analytics,manage}/`** — domain logic. Each feature has `api/` (Apollo hooks), `components/`, and optionally `data/` for static config. Feature barrel exports via `index.ts`.
- **`src/app/`** — thin route wrappers only. `(dashboard)/` group holds authenticated routes (agents, billing, channels, integrations, logs, etc.). `login/` and `pricing/` are public.
- **`src/components/`** — shared layout components (sidebar, nav, page-header) and `ui/` (shadcn primitives).
- **`src/contexts/AuthContext.tsx`** — auth state; consumed via `useAuth` hook.
- **`src/hooks/`** — shared hooks (`useAuth`, `useFilterParams`, `useMobile`).
- **`src/lib/graphql/`** — Apollo client singleton (`client.ts`), codegen output (`generated/`), and raw `.graphql` operations (`operations/`).

### Key patterns

- **GraphQL codegen**: All typed hooks/fragments are generated into `src/lib/graphql/generated/`. Run `bun run codegen` after changing `.graphql` files or `gql` template literals.
- **No WebSocket subscriptions** — real-time uses polling. Next.js rewrites don't support WS upgrades.
- **Standalone Docker output** (`output: "standalone"` in next.config.ts). Production image uses `bun server.js`.
- **Auth**: Cookie-based (`credentials: "include"` on Apollo). `AuthProvider` wraps all client components via `providers.tsx`.
- **Production backend**: `https://api.officeos.co`; local dev: `http://localhost:5000`.
