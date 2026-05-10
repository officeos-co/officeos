# Dashboard Agent Instructions

This dashboard is a Next.js App Router client for operating EnterpriseAgentOs. Keep route code, feature code, shared UI, and backend contract code in their current lanes. When adding a file, first decide whether it is route composition, feature behavior, shared UI infrastructure, or a cross-feature type.

## Project Layout

- `src/app`: Next.js routes, route layouts, loading states, global providers, metadata, and global CSS.
- `src/app/(dashboard)`: authenticated operator routes. This route group does not appear in URLs; pages here are wrapped by the dashboard layout, sidebar, analytics pageview, and auth guard.
- `src/app/login` and `src/app/pricing`: public routes outside the authenticated dashboard shell.
- `src/ui`: flat design-system primitives and small generic controls. These wrap Base UI/shadcn-style primitives, Tailwind variants, and low-level UI behavior.
- `src/shell`: flat dashboard shell/chrome components such as the sidebar, navigation, page header/container, auth guard, analytics pageview, and workspace switchers.
- `src/features/agents`: agent management, agent detail tabs, sessions, models, MCP server/integration catalog, channels, credentials, browser, cron, logs, and memory.
- `src/features/analytics`: logs, usage, analytics queries, and analytics-facing view models.
- `src/features/manage`: organization, profile, billing, pricing, providers, GDPR, and login form behavior.
- `src/hooks`: shared hooks used across route/features, such as auth, mobile detection, and URL filter params.
- `src/contexts`: React contexts for shared app state. Keep these thin and backed by hooks.
- `src/lib`: cross-cutting utilities, environment config, SVG sanitization, Apollo client setup, and GraphQL operation/codegen support.
- `src/types`: cross-feature domain-shaped UI types, currently shared log entry types.
- `src/__tests__`: dashboard tests. The current test suite validates all GraphQL operations against the backend schema.

## Feature Boundaries

- Route pages in `src/app/**/page.tsx` should compose feature hooks/components, route params, URL state, page headers, and page-level loading/empty states. Do not turn route files into API clients or broad feature libraries.
- Feature-specific components belong under `src/features/<feature>/components`. Put agent tab panels, credential dialogs, MCP/channel cards, and feature dialogs there.
- Feature-specific data access belongs under `src/features/<feature>/api` as `useX.ts` hooks. Pages and components should call these hooks instead of importing Apollo directly.
- Feature-specific static catalogs, stable feature types, and feature-only data shapes belong under `src/features/<feature>/data`.
- Shared layout and shell components belong under `src/shell`. Keep this folder flat and limited to dashboard frame/chrome behavior.
- Shared UI primitives belong under `src/ui`. Keep this folder flat and generic: buttons, tables, dialogs, selects, tooltips, badges, inputs, skeletons, pagination, sidebar primitives. Do not put agent, billing, provider, channel, MCP, or analytics-specific logic here.
- Do not add files under `src/components`; that lane is deprecated. New files must go to `src/ui`, `src/shell`, or `src/features/<feature>/components`.
- Each feature exports its public surface from `src/features/<feature>/index.ts`. Route files should prefer the feature barrel when the hook/component is already exported. Inside a feature, use relative imports for nearby private files.
- Do not make one feature depend on another feature's private folders. If two features need the same concept, move the smallest shared type/helper to `src/types`, `src/hooks`, `src/lib`, `src/ui`, or `src/shell`.

## Flat Folder Rules

- Feature roots may contain only `api`, `components`, `hooks`, `data`, `types.ts`, and `index.ts`.
- Feature `api`, `components`, `hooks`, and `data` folders must stay flat. Do not add bucket folders such as `dialogs`, `tabs`, `tables`, `forms`, `cards`, `queries`, `mutations`, `types`, `utils`, `helpers`, or `shared`.
- Use strong file names instead of nested folders, such as `agent-create-dialog.tsx`, `integration-tools-tab.tsx`, and `useAgentBindings.ts`.
- If a feature folder becomes too broad, split the product/domain into a new top-level feature instead of adding nested subdomain folders.
- `src/ui`, `src/shell`, `src/hooks`, `src/contexts`, and `src/types` must stay flat.

## Where Types Go

- API hook input/output types that are only used with one hook stay in the same `src/features/<feature>/api/useX.ts` file.
- Raw GraphQL response types stay private in the hook file and should be mapped before leaving the hook.
- Feature data/catalog types live in `src/features/<feature>/data`, such as MCP server, channel, credential-field, and onboarding-step shapes.
- Cross-feature UI/domain types live in `src/types`, such as `AgentLog`.
- Component prop types stay next to the component unless reused by multiple components.
- Generated GraphQL types belong under `src/lib/graphql/generated` and are produced by `bun run codegen`. Do not hand-edit generated files.
- GraphQL operation files may live under `src/lib/graphql/operations` when they are used for codegen/contract tracking, but current runtime hooks mostly define `gql` documents next to the hook. Follow the local file's pattern when extending existing code.

## API And GraphQL

- Use Apollo through hooks in `src/features/*/api`. Do not create new `ApolloClient` instances, raw `fetch` GraphQL calls, or route-local GraphQL clients.
- Keep the singleton Apollo setup in `src/lib/graphql/client.ts`. Queries and mutations use `/api/dashboard/graphql`, which is proxied by `next.config.ts`; subscriptions use the backend WebSocket URL built from `src/lib/env.ts`.
- Define GraphQL documents with named operations. The contract test scans both `gql` template literals and `.graphql` files.
- Hooks own GraphQL variables, `skip`, `fetchPolicy`, `pollInterval`, cache updates, refetch behavior, and mutation result mapping.
- Hooks should map backend nullability, enum/storage casing, JSON string fields, date strings, and deprecated naming into dashboard-friendly types before returning data.
- Keep backend contract assumptions explicit in the hook. For example, parse JSON fields in the MCP hook, normalize log type names in analytics hooks, and lower-case agent status in agent hooks.
- Put cache writes or invalidation next to the mutation hook that changes data. Route pages should call the hook result, not know Apollo cache internals.
- Authentication state comes from backend cookies through GraphQL and `/api/auth/*` rewrites. Keep dashboard auth checks in `AuthGuard`, `AuthContext`, and auth hooks, not scattered across feature components.

## Routes, Tabs, And State

- Use App Router files for route ownership. Add new authenticated pages under `src/app/(dashboard)/<route>/page.tsx`; add public pages outside `(dashboard)`.
- Mark a component with `"use client"` when it uses React state/effects, Apollo hooks, Next navigation hooks, browser APIs, or event handlers. Keep server components server-only when they are just metadata/layout wrappers.
- Dynamic route params follow the current Next 16 pattern in this app: page props receive `params: Promise<...>` and client pages unwrap with `use(params)`.
- Tabs that represent navigable page state must use URL parameters, not local-only React state. The agent detail page uses `?tab=integrations|logs|browser|memory|cron`; add new tab links with `Link href=...` and derive the active tab from `useSearchParams`.
- Filters, search, pagination, and view modes that should survive navigation should use URL search params. Use `useFilterParams(defaults, basePath)` for list pages where applicable, and omit params that equal defaults.
- Ephemeral UI state can stay local with `useState`: open dialogs, selected table rows, optimistic text input, temporary loading overrides, and local search when persistence is not required by the workflow.
- Use `router.push` for real navigation and `router.replace` for URL-state changes that should not add history entries, such as filter updates or boot-status cleanup.
- Keep OAuth and external auth redirects explicit and encoded with `returnTo`; current integration flows assign `window.location` to `/api/auth/<provider>?returnTo=...`.

## Components And Styling

- Use `PageHeader` and `PageContainer` from `src/shell` for standard page chrome and width constraints. Choose the existing widths: `full`, `wide`, `thin`, or `narrow`.
- Use primitives from `src/ui` before adding custom controls. Buttons, selects, dropdown menus, dialogs, tables, inputs, skeletons, badges, tooltips, pagination, and empty states already exist.
- Use `lucide-react` icons in controls and state indicators when an icon exists.
- Compose class names with `cn` from `src/lib/utils` when classes are conditional or merged with props.
- Use Tailwind tokens from `globals.css`: `background`, `foreground`, `card`, `muted`, `border`, `primary`, `destructive`, `sidebar`, and chart tokens. Do not hard-code one-off theme systems in components.
- Keep the UI dense and operator-focused. This is a management dashboard, not a marketing page: prefer tables, compact controls, clear empty/loading states, and restrained cards for repeated items or framed detail groups.
- Use `Skeleton` for loading states and `EmptyState` for empty list/table states where the existing UI has that pattern.
- Use `HelpTooltip` or `WithTooltip` for compact explanations attached to controls or technical fields; do not add long in-page instructional copy.
- Sanitize backend-provided SVG before rendering. MCP/integration logos go through `sanitizeSvg` before `dangerouslySetInnerHTML`.

## Environment And Client Boundaries

- Keep environment selection centralized in `src/lib/env.ts`. `next.config.ts` uses it for `/api/:path*` rewrites, and the Apollo client uses it for WebSocket URLs.
- Do not read arbitrary environment variables throughout components or feature hooks. Add derived config to `getEnvConfig()` if dashboard code needs it.
- Browser-only APIs such as `window.location` belong in client components and event handlers.
- `src/app/providers.tsx` is the client provider tree for Apollo and auth. Keep it thin; global provider additions belong there, while `src/app/layout.tsx` owns document shell, fonts, tooltip provider, and toaster.
- Authenticated dashboard layout belongs in `src/app/(dashboard)/layout.tsx`. Do not duplicate sidebar/auth shell code in individual pages.

## What Stays Out Of Dashboard Code

- Do not implement backend business rules, billing enforcement, provider dispatch logic, MCP execution behavior, agent turn behavior, or permission semantics in the dashboard. Surface backend state and send explicit mutations.
- Do not infer provider configuration, model availability, billing state, or permissions from display names or string prefixes when the backend exposes a field for it.
- Do not store secrets in dashboard state longer than needed for a credential form submission. Credentials are sent to backend mutations or auth redirects.
- Do not bypass the backend GraphQL contract with direct database, Kubernetes, pod-executor, MCP server, Stripe, or provider API calls from dashboard code.
- Do not hand-edit generated GraphQL artifacts or vendored UI primitive internals unless the task is specifically to change that shared primitive.

## Tests

- Use `bun test` from `apps/dashboard` for the dashboard test suite.
- The current contract test in `src/__tests__/graphql-contracts.test.ts` introspects `GRAPHQL_SCHEMA_URL` or defaults to the production dashboard GraphQL endpoint, then validates every named operation found in `gql` tags and `src/lib/graphql/operations/**/*.graphql`.
- When adding or changing GraphQL operations, run the contract test against a backend/schema that includes the matching API changes.
- Add focused tests under `src/__tests__` when changing shared parsing/mapping behavior, GraphQL contract assumptions, or cross-feature utilities.
- For UI-only changes without tests, at least run `bun run lint` when practical and manually verify the affected route in the dashboard.
