import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { issues } from "./cli/issues.ts";
import { comments } from "./cli/comments.ts";
import { projects } from "./cli/projects.ts";
import { teams } from "./cli/teams.ts";
import { cycles } from "./cli/cycles.ts";
import { labels } from "./cli/labels.ts";
import { users } from "./cli/users.ts";
import { roadmap } from "./cli/roadmap.ts";
import { search } from "./cli/search.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...issues, ...comments, ...projects, ...teams, ...cycles, ...labels, ...users, ...roadmap, ...search },
});
