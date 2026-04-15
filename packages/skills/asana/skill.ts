import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { workspaces } from "./cli/workspaces.ts";
import { projects } from "./cli/projects.ts";
import { tasks } from "./cli/tasks.ts";

export default defineSkill({
  name: "asana",
  title: "Asana",
  emoji: "🌟",
  description:
    "Full Asana project management: workspaces, projects, tasks, sections, comments, tags, and search.",
  doc,

  credentials: {
    access_token: {
      label: "Personal Access Token",
      kind: "password",
      placeholder: "1/1234567890:...",
      help: "Asana Personal Access Token from https://app.asana.com/0/developer-console",
    },
  },

  actions: { ...workspaces, ...projects, ...tasks },
});
