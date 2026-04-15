import { defineSkill } from "@harro/skill-sdk";
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
  name: "cloudflare",
  title: "Cloudflare",
  emoji: "☁️",
  description:
    "Manage DNS, Workers, Pages, cache, SSL, firewall, analytics, and zone settings via the Cloudflare REST API.",
  doc,
  credentials: {
    api_token: {
      label: "API Token",
      kind: "password",
      placeholder: "your-cloudflare-api-token",
      help: "Cloudflare API token with appropriate permissions. Create one at https://dash.cloudflare.com/profile/api-tokens.",
    },
  },
  actions: { ...dns, ...workers, ...pages, ...cache, ...ssl, ...firewall, ...analytics, ...settings },
});
