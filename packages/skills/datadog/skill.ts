import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { metrics } from "./cli/metrics.ts";
import { monitors } from "./cli/monitors.ts";
import { dashboards } from "./cli/dashboards.ts";
import { logs } from "./cli/logs.ts";
import { hosts } from "./cli/hosts.ts";
import { events } from "./cli/events.ts";
import { downtimes } from "./cli/downtimes.ts";

export default defineSkill({
  name: "datadog",
  title: "Datadog",
  emoji: "🐶",
  description:
    "Monitor infrastructure and applications: query metrics, manage monitors, dashboards, logs, hosts, events, and downtimes via the Datadog API.",
  doc,
  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      placeholder: "dd_api_…",
      help: "Found in Organization Settings → API Keys.",
    },
    app_key: {
      label: "Application Key",
      kind: "password",
      placeholder: "dd_app_…",
      help: "Found in Organization Settings → Application Keys.",
    },
    site: {
      label: "Datadog Site",
      kind: "text",
      placeholder: "datadoghq.com",
      help: "Your Datadog site (e.g. datadoghq.com, datadoghq.eu, us3.datadoghq.com).",
    },
  },
  actions: {
    ...metrics,
    ...monitors,
    ...dashboards,
    ...logs,
    ...hosts,
    ...events,
    ...downtimes,
  },
});
