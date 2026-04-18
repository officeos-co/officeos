import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { contacts } from "./cli/contacts.ts";
import { companies } from "./cli/companies.ts";
import { deals } from "./cli/deals.ts";
import { pipelines } from "./cli/pipelines.ts";
import { tickets } from "./cli/tickets.ts";
import { associations } from "./cli/associations.ts";
import { engagement } from "./cli/engagement.ts";
import { lists } from "./cli/lists.ts";
import { properties } from "./cli/properties.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    ...contacts,
    ...companies,
    ...deals,
    ...pipelines,
    ...tickets,
    ...associations,
    ...engagement,
    ...lists,
    ...properties,
  },
});
