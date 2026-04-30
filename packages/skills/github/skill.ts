import { defineSkill, type SkillDefinition } from "@harro/skill-sdk";
import _manifest from "./skill.json" with { type: "json" };

const manifest = _manifest as unknown as Omit<
  SkillDefinition,
  "doc" | "actions"
>;
import doc from "./SKILL.md";
import { repos } from "./cli/repos.ts";
import { issues } from "./cli/issues.ts";
import { pulls } from "./cli/pulls.ts";
import { reviews } from "./cli/reviews.ts";
import { releases } from "./cli/releases.ts";
import { workflows } from "./cli/workflows.ts";
import { gists } from "./cli/gists.ts";
import { search } from "./cli/search.ts";
import { orgs } from "./cli/orgs.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...repos,
    ...issues,
    ...pulls,
    ...reviews,
    ...releases,
    ...workflows,
    ...gists,
    ...search,
    ...orgs,
  },
});
