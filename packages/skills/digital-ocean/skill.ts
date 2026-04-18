import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { droplets } from "./cli/droplets.ts";
import { databases } from "./cli/databases.ts";
import { domains } from "./cli/domains.ts";
import { kubernetes } from "./cli/kubernetes.ts";
import { spaces } from "./cli/spaces.ts";
import { networking } from "./cli/networking.ts";
import { apps } from "./cli/apps.ts";
import { account } from "./cli/account.ts";

export default defineSkill({
  ...manifest,
  doc,

  actions: { ...droplets, ...databases, ...domains, ...kubernetes, ...spaces, ...networking, ...apps, ...account },
});
