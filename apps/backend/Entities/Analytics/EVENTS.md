# Analytics event catalog

Source of truth for every PostHog event the product fires. The new dashboard
(`apps/dashboard-2/`) must preserve this contract by calling
`mutation captureEvent(input: { name, properties })` — the backend forwards
those calls to PostHog via `IAnalyticsService`.

Legacy init (for reference only — dashboard-2 does **not** embed the PostHog
snippet): `apps/dashboard/src/lib/posthog.ts` uses `posthog-node` with
`NEXT_PUBLIC_POSTHOG_KEY` / `NEXT_PUBLIC_POSTHOG_HOST`
(default `https://us.i.posthog.com`). `distinct_id` comes from
`posthog.identify(userId, { email, name })` in `apps/dashboard/src/hooks/useAuth.ts`.
The backend replaces both: it owns the API key, and uses the session user id as
`distinct_id`.

## Client-fired events (originally from dashboard v1)

| Event name | Properties | Legacy location | Trigger |
|------------|------------|-----------------|---------|
| `agent_created` | `agent_name, provider, template, skill_count, allow_skills, deny_skills` | `components/agents/NewAgentOverlay.tsx` | New agent overlay "Launch" success |
| `agent_deleted` | `agent_id, agent_name` | `app/agents/[id]/page.tsx` | Delete button on agent detail |
| `chat_message_sent` | `agent_id` | `components/agents/AgentChatPanel.tsx` | User sends chat message |
| `tab_switched` | `agent_id, tab_name` | `components/agents/AgentDetailTabs.tsx` | Agent detail tab switch |
| `model_changed` | `agent_id, model, previous_model` | `components/agents/AgentOverviewPanel.tsx` | Model dropdown save |
| `skill_assigned` | `agent_id, skill_name` | `components/agents/AgentOverviewPanel.tsx` | Assign skill to agent (configured + unconfigured paths) |
| `skill_removed` | `agent_id, skill_name` | `components/agents/AgentOverviewPanel.tsx` | Unassign skill from agent |
| `skill_installed` | `skill_name` | `app/skills/[name]/page.tsx` | Install skill button |
| `skill_uninstalled` | `skill_name` | `app/skills/[name]/page.tsx` | Uninstall skill button |
| `skill_configured` | `skill_name` | `components/skills/SkillCredentialsForm.tsx` | Save credentials form |
| `nav_clicked` | `destination` | `components/shared/AppSidebar.tsx` | Sidebar nav item click |

## Server-fired events (new, emitted directly by the backend)

These cannot be captured reliably from the client (template flow runs server-
side; message delivery and agent creation succeed only after backend work).

| Event name | Properties | Backend location |
|------------|------------|------------------|
| `agent_created` | `agent_id, provider, model` | `AgentService.CreateAsync` (distinctId = ownerId) |
| `agent_deleted` | `agent_id` | `AgentService.DeleteAsync` |
| `agent_message_sent` | `agent_id, content_length` | `AgentLogService.SendMessageAsync` |
| `agent_created_from_template` | `template_id, template_name, agent_id` | `AgentTemplateService.CreateAgentFromTemplateAsync` |

`agent_created` and `agent_deleted` fire from **both** sides intentionally: the
client event carries UI context (template choice, skill counts) while the
server event guarantees we capture the action even if the client navigates away
before the mutation resolves.

## Identify

Legacy: `useAuth.ts` calls `posthog.identify(userId, { email, name })` on auth
check. Replaced by `mutation identifyUser` which calls
`IAnalyticsService.IdentifyAsync` for the session user.
