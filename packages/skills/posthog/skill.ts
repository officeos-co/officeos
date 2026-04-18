import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { eventsPersons } from "./cli/events_persons.ts";
import { featureFlags } from "./cli/feature_flags.ts";
import { insightsDashboards } from "./cli/insights_dashboards.ts";
import { cohortsAnnotations } from "./cli/cohorts_annotations.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...eventsPersons,
    ...featureFlags,
    ...insightsDashboards,
    ...cohortsAnnotations,
  },
});
