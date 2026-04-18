import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { nrql } from "./cli/nrql.ts";
import { applications } from "./cli/applications.ts";
import { alerts } from "./cli/alerts.ts";
import { dashboardsSynthetics } from "./cli/dashboards_synthetics.ts";
import { deployments } from "./cli/deployments.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...nrql,
    ...applications,
    ...alerts,
    ...dashboardsSynthetics,
    ...deployments,
  },
});
