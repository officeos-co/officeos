---
name: posthog
description: PostHog product analytics integration — authoritative for all analytics/tracking code
when_to_use: when adding analytics, event tracking, or user identification to apps/dashboard/**
---

You are integrating PostHog product analytics into a Next.js App Router dashboard.

<setup>
## Client-side (posthog-js)

Initialize PostHog in `instrumentation-client.ts` at the project root:

```ts
import posthog from "posthog-js";

posthog.init(process.env.NEXT_PUBLIC_POSTHOG_KEY!, {
  api_host: process.env.NEXT_PUBLIC_POSTHOG_HOST ?? "https://us.i.posthog.com",
  capture_pageview: true,
  capture_pageleave: true,
});
```

## Server-side (posthog-node)

Create `src/lib/posthog.ts`:

```ts
import { PostHog } from "posthog-node";

export function getPostHogClient() {
  return new PostHog(process.env.NEXT_PUBLIC_POSTHOG_KEY!, {
    host: process.env.NEXT_PUBLIC_POSTHOG_HOST ?? "https://us.i.posthog.com",
    flushAt: 1,
    flushInterval: 0,
  });
}
```

## Environment variables

```
NEXT_PUBLIC_POSTHOG_KEY=phc_...
NEXT_PUBLIC_POSTHOG_HOST=https://us.i.posthog.com
```

</setup>

<principles>
- Always use environment variables for PostHog keys. Never hardcode them.
- Add PostHog alongside existing code — do not restructure.
- Capture events in event handlers where user interactions happen, not in useEffect.
- Use `posthog.capture('event_name', { properties })` for tracking.
- Use `posthog.identify(userId, { email, name })` on login/signup.
- Use `posthog.reset()` on logout.
- Keep event names in snake_case and descriptive: `agent_created`, `skill_installed`, `chat_message_sent`.
- Group related events with a common prefix: `agent_`, `skill_`, `settings_`.
- Include relevant context in properties: `{ agent_id, skill_name, tab_name }`.
- Anonymous events are cheaper — only identify when you have a real user.
- For reverse proxy setup, add rewrites in next.config.ts to route `/ingest` to PostHog.
</principles>

<event-planning>
Before adding events, create a `.posthog-events.json` file listing planned events:

```json
{
  "events": [
    {
      "name": "event_name",
      "description": "When this happens",
      "properties": ["prop1", "prop2"],
      "location": "src/components/..."
    }
  ]
}
```

This serves as the analytics plan and ensures consistent naming.
</event-planning>

<rules>
- Import `posthog` from `posthog-js` in client components only.
- Never import posthog-js in server components — use posthog-node instead.
- Do not wrap the app in a PostHogProvider — instrumentation-client.ts handles init.
- Do not use useEffect for capturing events — use event handlers.
- Track meaningful user actions, not every render or state change.
- Include the page/component context in event properties.
- Consider that the software evolves — use a structured event naming convention that scales.
</rules>
