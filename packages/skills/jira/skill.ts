import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { issues } from "./cli/issues.ts";
import { comments } from "./cli/comments.ts";
import { projects } from "./cli/projects.ts";

export default defineSkill({
  name: "jira",
  title: "Jira",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M11.571 11.513H0a5.218 5.218 0 0 0 5.232 5.215h2.13v2.057A5.215 5.215 0 0 0 12.575 24V12.518a1.005 1.005 0 0 0-1.005-1.005zm5.723-5.756H5.736a5.215 5.215 0 0 0 5.215 5.214h2.129v2.058a5.218 5.218 0 0 0 5.215 5.214V6.758a1.001 1.001 0 0 0-1.001-1.001zM23.013 0H11.455a5.215 5.215 0 0 0 5.215 5.215h2.129v2.057A5.215 5.215 0 0 0 24 12.483V1.005A1.001 1.001 0 0 0 23.013 0Z\"/></svg>",
  description:
    "Full Jira Cloud project management: issues, projects, boards, sprints, transitions, comments, and JQL search.",
  doc,

  credentials: {
    domain: {
      label: "Atlassian Domain",
      kind: "text",
      placeholder: "yourcompany",
      help: "Your Atlassian subdomain (e.g. yourcompany for yourcompany.atlassian.net)",
    },
    email: {
      label: "Email",
      kind: "text",
      placeholder: "you@example.com",
      help: "Your Atlassian account email address",
    },
    api_token: {
      label: "API Token",
      kind: "password",
      placeholder: "ATATT3xFfGF0...",
      help: "Jira API token from https://id.atlassian.com/manage-profile/security/api-tokens",
    },
  },

  actions: { ...issues, ...comments, ...projects },
});
