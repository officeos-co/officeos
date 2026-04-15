import { defineSkill } from "@harro/skill-sdk";
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
  name: "salesforce",
  title: "Salesforce",
  emoji: "☁️",
  description:
    "Query, create, update, and manage Salesforce records, leads, contacts, opportunities, accounts, cases, reports, and bulk operations via the Salesforce REST API and SOQL.",
  doc,

  credentials: {
    instance_url: {
      label: "Instance URL",
      kind: "text",
      placeholder: "https://yourorg.my.salesforce.com",
      help: "Your Salesforce org instance URL (e.g. https://yourorg.my.salesforce.com).",
    },
    access_token: {
      label: "Access Token",
      kind: "password",
      placeholder: "00D...",
      help: "OAuth2 access token. Obtain via connected app or session ID.",
    },
  },

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
