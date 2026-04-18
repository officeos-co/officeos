import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { issues } from "./cli/issues.ts";
import { comments } from "./cli/comments.ts";
import { projects } from "./cli/projects.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: { ...issues, ...comments, ...projects },
});
