import { envConfig } from "./env";

export const siteConfig = {
  name: "OfficeOS",
  url: envConfig.websiteUrl,
  dashboardUrl: envConfig.dashboardUrl,
  docsUrl: envConfig.docsUrl,
  description:
    "OfficeOS is an AI agent platform that deploys autonomous agents across your company — with enterprise knowledge, custom skills, and full infrastructure control.",
  links: {
    github: "https://github.com/officeos",
  },
};

export type SiteConfig = typeof siteConfig;
