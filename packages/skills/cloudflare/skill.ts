import { defineSkill } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";
import { dns } from "./cli/dns.ts";
import { workers } from "./cli/workers.ts";
import { pages } from "./cli/pages.ts";
import { cache } from "./cli/cache.ts";
import { ssl } from "./cli/ssl.ts";
import { firewall } from "./cli/firewall.ts";
import { analytics } from "./cli/analytics.ts";
import { settings } from "./cli/settings.ts";

export default defineSkill({
  ...manifest,
  doc,
  actions: { ...dns, ...workers, ...pages, ...cache, ...ssl, ...firewall, ...analytics, ...settings },
});
