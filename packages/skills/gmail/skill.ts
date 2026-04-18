import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { messages } from "./cli/messages.ts";
import { drafts } from "./cli/drafts.ts";
import { labels } from "./cli/labels.ts";
import { threads } from "./cli/threads.ts";
import { attachments } from "./cli/attachments.ts";
import { search } from "./cli/search.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...messages, ...drafts, ...labels, ...threads, ...attachments, ...search },
});
