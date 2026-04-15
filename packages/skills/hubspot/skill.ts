import { defineSkill } from "@harro/skill-sdk";
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
  name: "hubspot",
  title: "HubSpot",
  emoji: "🧲",
  description:
    "Manage CRM contacts, companies, deals, tickets, pipelines, engagements, lists, and marketing emails via the HubSpot API.",
  doc,

  credentials: {
    access_token: {
      label: "Access Token",
      kind: "password",
      placeholder: "pat-na1-…",
      help: "HubSpot private app access token. Create one at Settings > Integrations > Private Apps. Needs CRM scopes for contacts, companies, deals, and tickets.",
    },
  },

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
