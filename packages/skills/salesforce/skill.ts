import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { query } from "./cli/query.ts";
import { records } from "./cli/records.ts";
import { leads } from "./cli/leads.ts";
import { contacts } from "./cli/contacts.ts";
import { opportunities } from "./cli/opportunities.ts";
import { accounts } from "./cli/accounts.ts";
import { cases } from "./cli/cases.ts";
import { reports } from "./cli/reports.ts";
import { bulk } from "./cli/bulk.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    ...query,
    ...records,
    ...leads,
    ...contacts,
    ...opportunities,
    ...accounts,
    ...cases,
    ...reports,
    ...bulk,
  },
});
