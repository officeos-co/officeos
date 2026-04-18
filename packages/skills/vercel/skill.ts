import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { projects } from "./cli/projects.ts";
import { deployments } from "./cli/deployments.ts";
import { domains } from "./cli/domains.ts";
import { env } from "./cli/env.ts";
import { logs } from "./cli/logs.ts";
import { teams } from "./cli/teams.ts";
import { aliases } from "./cli/aliases.ts";

export default defineSkill({
  name: "vercel",
  title: "Vercel",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"m12 1.608 12 20.784H0Z\"/></svg>",
  description:
    "Manage Vercel projects, deployments, domains, environment variables, logs, teams, checks, aliases, and secrets via the Vercel REST API.",
  doc,
  credentials: {
    token: {
      label: "API Token",
      kind: "password",
      placeholder: "vercel_...",
      help: "Vercel API token. Create one at https://vercel.com/account/tokens.",
    },
    team_id: {
      label: "Team ID (optional)",
      kind: "text",
      placeholder: "team_...",
      help: "Optional. When set, all requests are scoped to this team.",
    },
  },
  actions: { ...projects, ...deployments, ...domains, ...env, ...logs, ...teams, ...aliases },
});
