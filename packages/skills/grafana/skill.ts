import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { dashboards } from "./cli/dashboards.ts";
import { datasources } from "./cli/datasources.ts";
import { alerts } from "./cli/alerts.ts";
import { annotations } from "./cli/annotations.ts";
import { folders } from "./cli/folders.ts";
import { users } from "./cli/users.ts";
import { query } from "./cli/query.ts";

export default defineSkill({
  name: "grafana",
  title: "Grafana",
  emoji: "📊",
  description:
    "Full Grafana HTTP API coverage: dashboards, panels, datasources, queries, alerts, annotations, folders, users, orgs, and search.",
  doc,
  credentials: {
    url: {
      label: "Grafana Instance URL",
      kind: "text",
      placeholder: "https://grafana.example.com",
      help: "Base URL of your Grafana instance (e.g. https://grafana.example.com).",
    },
    api_key: {
      label: "API Key / Service Account Token",
      kind: "password",
      placeholder: "glsa_… or eyJ…",
      help: "Service Account token (recommended) or legacy API key. Create one at your Grafana instance under Administration > Service Accounts.",
    },
  },
  actions: { ...dashboards, ...datasources, ...alerts, ...annotations, ...folders, ...users, ...query },
});
