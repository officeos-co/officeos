import { defineSkill } from "@harro/skill-sdk";
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
  name: "github",
  title: "GitHub",
  emoji: "🐙",
  description:
    "Full GitHub CLI parity: repositories, issues, PRs, releases, workflows, gists, search, and org management via the GitHub REST API.",
  doc,
  credentials: {
    token: {
      label: "Personal Access Token",
      kind: "password",
      placeholder: "github_pat_… or ghp_…",
      help: "Fine-grained PAT recommended. Needs appropriate scopes for the actions you want to use. Create one at https://github.com/settings/tokens.",
    },
  },
  actions: { ...repos, ...issues, ...pulls, ...reviews, ...releases, ...workflows, ...gists, ...search, ...orgs },
});
