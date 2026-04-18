import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { boards } from "./cli/boards.ts";
import { cards } from "./cli/cards.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: { ...boards, ...cards },
});
