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
