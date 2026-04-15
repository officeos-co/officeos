import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { spreadsheets } from "./cli/spreadsheets.ts";
import { values } from "./cli/values.ts";
import { sheets } from "./cli/sheets.ts";
import { format } from "./cli/format.ts";

export default defineSkill({
  name: "google-sheets",
  title: "Google Sheets",
  emoji: "📊",
  description:
    "Create, read, write, format, and manage spreadsheets, sheets, and cell data via the Google Sheets API v4.",
  doc,
  credentials: {
    access_token: {
      label: "OAuth2 Access Token",
      kind: "password",
      placeholder: "ya29.…",
      help: "Google OAuth2 access token with Sheets scopes.",
    },
  },
  actions: { ...spreadsheets, ...values, ...sheets, ...format },
});
