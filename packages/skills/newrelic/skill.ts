import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { nrql } from "./cli/nrql.ts";
import { applications } from "./cli/applications.ts";
import { alerts } from "./cli/alerts.ts";
import { dashboardsSynthetics } from "./cli/dashboards_synthetics.ts";
import { deployments } from "./cli/deployments.ts";

export default defineSkill({
  name: "newrelic",
  title: "New Relic",
  emoji: "📡",
  description:
    "Observability and alerting: run NRQL queries, inspect APM applications, manage alert policies and conditions, dashboards, synthetic monitors, and deployment markers via the New Relic NerdGraph and REST APIs.",
  doc,
  credentials: {
    api_key: {
      label: "User API Key",
      kind: "password",
      placeholder: "NRAK-…",
      help: "User API key from New Relic → Profile → API Keys. Must be a User key (NRAK- prefix), not a license/ingest key.",
    },
    account_id: {
      label: "Account ID",
      kind: "text",
      placeholder: "1234567",
      help: "Your New Relic account ID, visible in the URL or under Account Settings.",
    },
  },
  actions: {
    ...nrql,
    ...applications,
    ...alerts,
    ...dashboardsSynthetics,
    ...deployments,
  },
});
