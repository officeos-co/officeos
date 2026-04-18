import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { spreadsheets } from "./cli/spreadsheets.ts";
import { values } from "./cli/values.ts";
import { sheets } from "./cli/sheets.ts";
import { format } from "./cli/format.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...spreadsheets, ...values, ...sheets, ...format },
});
