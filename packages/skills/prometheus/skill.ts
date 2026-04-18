import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { query } from "./cli/query.ts";
import { seriesLabels } from "./cli/series_labels.ts";
import { targetsRules } from "./cli/targets_rules.ts";
import { configMetadata } from "./cli/config_metadata.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...query,
    ...seriesLabels,
    ...targetsRules,
    ...configMetadata,
  },
});
