import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { messages } from "./cli/messages.ts";
import { drafts } from "./cli/drafts.ts";
import { labels } from "./cli/labels.ts";
import { threads } from "./cli/threads.ts";
import { attachments } from "./cli/attachments.ts";
import { search } from "./cli/search.ts";

export default defineSkill({
  name: "gmail",
  title: "Gmail",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M24 5.457v13.909c0 .904-.732 1.636-1.636 1.636h-3.819V11.73L12 16.64l-6.545-4.91v9.273H1.636A1.636 1.636 0 0 1 0 19.366V5.457c0-2.023 2.309-3.178 3.927-1.964L5.455 4.64 12 9.548l6.545-4.91 1.528-1.145C21.69 2.28 24 3.434 24 5.457z\"/></svg>",
  emoji: "✉️",
  description:
    "Send, search, read, and manage emails, drafts, labels, threads, and attachments via the Gmail API.",
  doc,
  credentials: {
    access_token: {
      label: "OAuth2 Access Token",
      kind: "password",
      placeholder: "ya29.…",
      help: "Google OAuth2 access token with Gmail scopes.",
    },
  },
  actions: { ...messages, ...drafts, ...labels, ...threads, ...attachments, ...search },
});
