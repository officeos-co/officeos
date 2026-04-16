# PostHog event catalog

Source of truth for every PostHog event the product fires. Each use-case has
a dedicated GraphQL mutation (no generic `captureEvent(name, properties)`
escape hatch) — the schema is the contract.

## Client-fired events (dashboard-2 → backend → PostHog)

| Event name | Mutation | Input fields | Trigger |
|------------|----------|--------------|---------|
| `$pageview` | `trackPageView` | `path` | `components/analytics-pageview.tsx` — route change |
| `nav_clicked` | `trackNavClicked` | `destination` | `components/nav-main.tsx` — sidebar nav click |
| `skill_installed` | `trackSkillInstalled` | `skillName` | `app/(dashboard)/integrations/page.tsx` — install or credential save |
| `skill_configured` | `trackSkillConfigured` | `skillName` | `app/(dashboard)/integrations/page.tsx` — credential dialog save |
| `channel_connected` | `trackChannelConnected` | `channelSlug` | `app/(dashboard)/channels/page.tsx` — onboarding complete |
| `agent_created` | `trackAgentCreated` | `agentName, provider, template, skillCount, allowSkills, denySkills` | `app/(dashboard)/quickstart/page.tsx` — Launch button |

The dashboard hook is `useAnalytics()` in `apps/dashboard-2/src/hooks/useAnalytics.ts`;
it exposes one typed function per mutation (`trackPageView`, `trackNavClicked`, …).
With `NEXT_PUBLIC_USE_MOCKS=1` every call is a `console.debug` no-op.

## Server-fired events (emitted directly by the backend)

These cannot be captured reliably from the client (template flow runs server-
side; message delivery and agent creation succeed only after backend work).

| Event name | Properties | Backend location |
|------------|------------|------------------|
| `agent_created` | `agent_id, provider, model` | `AgentService.CreateAsync` (distinctId = ownerId) |
| `agent_deleted` | `agent_id` | `AgentService.DeleteAsync` |
| `agent_message_sent` | `agent_id, content_length` | `AgentLogService.SendMessageAsync` |
| `agent_created_from_template` | `template_id, template_name, agent_id` | `AgentTemplateService.CreateAgentFromTemplateAsync` |

`agent_created` fires from **both** sides intentionally: the client event carries
UI context (template choice, skill counts) while the server event guarantees we
capture the action even if the client navigates away before the mutation
resolves.

## Identify

`mutation identifyUser` — no input, reads the session user and calls
`IPostHogService.IdentifyAsync` with `{ email, name }`. The legacy
`posthog.identify(userId, …)` client call in dashboard v1 is gone; dashboard-2
never holds the PostHog key.

## Adding a new event

1. Add a typed input record + mutation to `Entities/PostHog/PostHogMutations.cs`.
2. Add a corresponding `track*` helper in `apps/dashboard-2/src/hooks/useAnalytics.ts`.
3. Call the helper from the relevant component.
4. Document it in the table above.

Never add a generic `captureEvent(name, properties)` passthrough.
