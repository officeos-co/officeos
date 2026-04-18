import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { accounts } from "./cli/accounts.ts";
import { campaigns } from "./cli/campaigns.ts";
import { adsets } from "./cli/adsets.ts";
import { ads } from "./cli/ads.ts";
import { creatives } from "./cli/creatives.ts";
import { targeting } from "./cli/targeting.ts";
import { insights } from "./cli/insights.ts";
import { audiences } from "./cli/audiences.ts";
import { adImages } from "./cli/images.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: {
    ...accounts,
    ...campaigns,
    ...adsets,
    ...ads,
    ...creatives,
    ...targeting,
    ...insights,
    ...audiences,
    ...adImages,
  },
});
