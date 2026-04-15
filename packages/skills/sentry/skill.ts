import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { projects } from "./cli/projects.ts";
import { issues } from "./cli/issues.ts";
import { events } from "./cli/events.ts";
import { releases } from "./cli/releases.ts";
import { alerts } from "./cli/alerts.ts";
import { performance } from "./cli/performance.ts";

export default defineSkill({
  name: "sentry",
  title: "Sentry",
  emoji: "🛡️",
  description:
    "Monitor errors, manage releases, track performance, and configure alerts via the Sentry API.",
  doc,
  credentials: {
    auth_token: {
      label: "Auth Token",
      kind: "password",
      placeholder: "sntrys_…",
      help: "Sentry auth token with org-level access. Create one at https://sentry.io/settings/account/api/auth-tokens/.",
    },
    organization: {
      label: "Organization Slug",
      kind: "text",
      placeholder: "my-org",
      help: "Your Sentry organization slug (visible in the URL).",
    },
  },
  actions: { ...projects, ...issues, ...events, ...releases, ...alerts, ...performance },
});
