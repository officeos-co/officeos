import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { boards } from "./cli/boards.ts";
import { cards } from "./cli/cards.ts";

export default defineSkill({
  name: "trello",
  title: "Trello",
  emoji: "📋",
  description:
    "Full Trello board management: boards, lists, cards, comments, members, labels, and checklists.",
  doc,

  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      placeholder: "your-trello-api-key",
      help: "Trello API key from https://trello.com/app-key",
    },
    token: {
      label: "Token",
      kind: "password",
      placeholder: "your-trello-token",
      help: "Generate token at https://trello.com/1/authorize?expiration=never&scope=read,write&response_type=token&key={API_KEY}",
    },
  },

  actions: { ...boards, ...cards },
});
