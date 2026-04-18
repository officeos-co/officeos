import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { projects } from "./cli/projects.ts";
import { deployments } from "./cli/deployments.ts";
import { domains } from "./cli/domains.ts";
import { env } from "./cli/env.ts";
import { logs } from "./cli/logs.ts";
import { teams } from "./cli/teams.ts";
import { aliases } from "./cli/aliases.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...projects, ...deployments, ...domains, ...env, ...logs, ...teams, ...aliases },
});
