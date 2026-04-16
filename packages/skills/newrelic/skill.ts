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
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M8.0015 14.3091v7.384L12.0008 24V12.0008L1.6078 5.9996v4.6167ZM12.0008 0 2.8232 5.2976 6.8209 7.606l5.1799-2.9893 6.3936 3.6913v7.384l-5.1783 2.9908v4.6167l9.176-5.2991V5.9996Z\"/></svg>",
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
