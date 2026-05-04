import { getEnvConfig } from "./env";

export function getSiteConfig() {
  const env = getEnvConfig();
  return {
    name: "OfficeOS",
    url: env.websiteUrl,
    dashboardUrl: env.dashboardUrl,
    docsUrl: env.docsUrl,
    description:
      "OfficeOS is an AI agent platform that deploys autonomous agents across your company — with enterprise knowledge, custom skills, and full infrastructure control.",
    links: {
      github: "https://github.com/officeos-co/officeos",
    },
  };
}

export type SiteConfig = ReturnType<typeof getSiteConfig>;
