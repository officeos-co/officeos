import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { query } from "./cli/query.ts";
import { seriesLabels } from "./cli/series_labels.ts";
import { targetsRules } from "./cli/targets_rules.ts";
import { configMetadata } from "./cli/config_metadata.ts";

export default defineSkill({
  name: "prometheus",
  title: "Prometheus",
  emoji: "🔥",
  description:
    "Query metrics, explore series and labels, inspect scrape targets, alerting rules, and configuration via the Prometheus HTTP API.",
  doc,
  credentials: {
    url: {
      label: "Prometheus URL",
      kind: "text",
      placeholder: "http://localhost:9090",
      help: "Base URL of your Prometheus server. If protected by a reverse proxy with basic auth, include credentials in the URL.",
    },
  },
  actions: {
    ...query,
    ...seriesLabels,
    ...targetsRules,
    ...configMetadata,
  },
});
