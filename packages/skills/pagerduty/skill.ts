import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { incidents } from "./cli/incidents.ts";
import { servicesUsers } from "./cli/services_users.ts";
import { oncallsSchedules } from "./cli/oncalls_schedules.ts";

export default defineSkill({
  name: "pagerduty",
  title: "PagerDuty",
  emoji: "🚨",
  description:
    "Manage incidents, services, users, on-call schedules, and escalation policies via the PagerDuty REST API.",
  doc,
  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      placeholder: "u+…",
      help: "User or service API token from PagerDuty → My Profile → User Settings → Create API User Token, or from Configuration → API Access → Create New API Key.",
    },
  },
  actions: {
    ...incidents,
    ...servicesUsers,
    ...oncallsSchedules,
  },
});
