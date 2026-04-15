import { defineSkill } from "@harro/skill-sdk";
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
  name: "digital-ocean",
  title: "DigitalOcean",
  emoji: "\uD83C\uDF0A",
  description:
    "Manage DigitalOcean cloud infrastructure: Droplets, Databases, Domains, Kubernetes, Spaces, Networking, Apps, and account resources via the DigitalOcean API.",
  doc,

  credentials: {
    token: {
      label: "API Token",
      kind: "password",
      placeholder: "dop_v1_...",
      help: "DigitalOcean personal access token with read+write scopes. Create one at https://cloud.digitalocean.com/account/api/tokens.",
    },
  },

  actions: { ...droplets, ...databases, ...domains, ...kubernetes, ...spaces, ...networking, ...apps, ...account },
});
