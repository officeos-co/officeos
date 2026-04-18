import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { metrics } from "./cli/metrics.ts";
import { monitors } from "./cli/monitors.ts";
import { dashboards } from "./cli/dashboards.ts";
import { logs } from "./cli/logs.ts";
import { hosts } from "./cli/hosts.ts";
import { events } from "./cli/events.ts";
import { downtimes } from "./cli/downtimes.ts";

export default defineSkill({
  ...manifest,
  doc,
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
