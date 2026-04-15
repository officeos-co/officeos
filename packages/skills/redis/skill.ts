import { defineSkill } from "@harro/skill-sdk";
import doc from "./SKILL.md";
import { strings } from "./cli/strings.ts";
import { hashes } from "./cli/hashes.ts";
import { lists } from "./cli/lists.ts";
import { sets } from "./cli/sets.ts";
import { sorted_sets } from "./cli/sorted_sets.ts";
import { keys } from "./cli/keys.ts";
import { pubsub } from "./cli/pubsub.ts";
import { server } from "./cli/server.ts";
import { json } from "./cli/json.ts";

export default defineSkill({
  name: "redis",
  title: "Redis",
  emoji: "🟥",
  description:
    "Full Redis data store management: strings, hashes, lists, sets, sorted sets, keys, pub/sub, server diagnostics, and RedisJSON via a backend proxy.",
  doc,
  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "text",
      placeholder: "https://api.officeos.co/redis",
      help: "Backend proxy endpoint for Redis commands.",
    },
  },
  actions: { ...strings, ...hashes, ...lists, ...sets, ...sorted_sets, ...keys, ...pubsub, ...server, ...json },
});
