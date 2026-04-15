import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { issues } from "./cli/issues.ts";
import { comments } from "./cli/comments.ts";
import { projects } from "./cli/projects.ts";

export default defineSkill({
  name: "jira",
  title: "Jira",
  emoji: "🎯",
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
