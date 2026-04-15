import { defineSkill } from "@harro/skill-sdk";
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
  name: "linear",
  title: "Linear",
  emoji: "📊",
  description:
    "Full Linear project management: manage issues, projects, cycles, teams, labels, roadmaps, and users via the Linear GraphQL API.",
  doc,
  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      placeholder: "lin_api_…",
      help: "Linear API key. Create one at https://linear.app/settings/api.",
    },
  },
  actions: { ...issues, ...comments, ...projects, ...teams, ...cycles, ...labels, ...users, ...roadmap, ...search },
});
