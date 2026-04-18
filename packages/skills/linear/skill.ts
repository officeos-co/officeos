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
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M2.886 4.18A11.982 11.982 0 0 1 11.99 0C18.624 0 24 5.376 24 12.009c0 3.64-1.62 6.903-4.18 9.105L2.887 4.18ZM1.817 5.626l16.556 16.556c-.524.33-1.075.62-1.65.866L.951 7.277c.247-.575.537-1.126.866-1.65ZM.322 9.163l14.515 14.515c-.71.172-1.443.282-2.195.322L0 11.358a12 12 0 0 1 .322-2.195Zm-.17 4.862 9.823 9.824a12.02 12.02 0 0 1-9.824-9.824Z\"/></svg>",
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
