import { defineSkill } from "@harro/skill-sdk";
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
  name: "meta-ads",
  title: "Meta Ads",
  emoji: "📱",
  description:
    "Manage Facebook and Instagram ad campaigns, ad sets, ads, creatives, targeting, insights, and audiences via the Meta Marketing API.",
  doc,
  credentials: {
    access_token: {
      label: "Access Token",
      kind: "password",
      placeholder: "EAAxxxxxxx…",
      help: "Meta Marketing API access token. Generate one from https://developers.facebook.com/tools/explorer/ with ads_management and ads_read permissions.",
    },
  },
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
