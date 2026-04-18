import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { boards } from "./cli/boards.ts";
import { cards } from "./cli/cards.ts";

export default defineSkill({
  name: "trello",
  title: "Trello",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M21.147 0H2.853A2.86 2.86 0 000 2.853v18.294A2.86 2.86 0 002.853 24h18.294A2.86 2.86 0 0024 21.147V2.853A2.86 2.86 0 0021.147 0zM10.34 17.287a.953.953 0 01-.953.953h-4a.954.954 0 01-.954-.953V5.38a.953.953 0 01.954-.953h4a.954.954 0 01.953.953zm9.233-5.467a.944.944 0 01-.953.947h-4a.947.947 0 01-.953-.947V5.38a.953.953 0 01.953-.953h4a.954.954 0 01.953.953z\"/></svg>",
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
