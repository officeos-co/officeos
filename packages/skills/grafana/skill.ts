import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { dashboards } from "./cli/dashboards.ts";
import { datasources } from "./cli/datasources.ts";
import { alerts } from "./cli/alerts.ts";
import { annotations } from "./cli/annotations.ts";
import { folders } from "./cli/folders.ts";
import { users } from "./cli/users.ts";
import { query } from "./cli/query.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...dashboards, ...datasources, ...alerts, ...annotations, ...folders, ...users, ...query },
});
