import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { incidents } from "./cli/incidents.ts";
import { servicesUsers } from "./cli/services_users.ts";
import { oncallsSchedules } from "./cli/oncalls_schedules.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...incidents,
    ...servicesUsers,
    ...oncallsSchedules,
  },
});
