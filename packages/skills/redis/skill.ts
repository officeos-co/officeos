import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
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
  ...manifest,
  doc,
  actions: { ...strings, ...hashes, ...lists, ...sets, ...sorted_sets, ...keys, ...pubsub, ...server, ...json },
});
