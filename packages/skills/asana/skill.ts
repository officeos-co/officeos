import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { workspaces } from "./cli/workspaces.ts";
import { projects } from "./cli/projects.ts";
import { tasks } from "./cli/tasks.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: { ...workspaces, ...projects, ...tasks },
});
