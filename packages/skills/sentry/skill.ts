import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { projects } from "./cli/projects.ts";
import { issues } from "./cli/issues.ts";
import { events } from "./cli/events.ts";
import { releases } from "./cli/releases.ts";
import { alerts } from "./cli/alerts.ts";
import { performance } from "./cli/performance.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...projects, ...issues, ...events, ...releases, ...alerts, ...performance },
});
