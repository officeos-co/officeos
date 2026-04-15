import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { eventsPersons } from "./cli/events_persons.ts";
import { featureFlags } from "./cli/feature_flags.ts";
import { insightsDashboards } from "./cli/insights_dashboards.ts";
import { cohortsAnnotations } from "./cli/cohorts_annotations.ts";

export default defineSkill({
  name: "posthog",
  title: "PostHog",
  emoji: "🦔",
  description:
    "Product analytics: query events, manage persons, feature flags, insights, dashboards, cohorts, and annotations via the PostHog API.",
  doc,
  credentials: {
    api_key: {
      label: "Personal API Key",
      kind: "password",
      placeholder: "phx_…",
      help: "Personal API key from PostHog → Settings → Personal API Keys. Note: this is different from the project API key used for event ingestion.",
    },
    project_id: {
      label: "Project ID",
      kind: "text",
      placeholder: "12345",
      help: "Project ID from PostHog → Project Settings.",
    },
    host: {
      label: "Host URL",
      kind: "text",
      placeholder: "https://app.posthog.com",
      help: "PostHog host URL. Use https://app.posthog.com for cloud, or your self-hosted URL.",
    },
  },
  actions: {
    ...eventsPersons,
    ...featureFlags,
    ...insightsDashboards,
    ...cohortsAnnotations,
  },
});
