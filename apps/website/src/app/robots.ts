import type { MetadataRoute } from "next";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: [
      {
        userAgent: "*",
        allow: "/",
      },
      {
        userAgent: [
          "GPTBot",
          "ChatGPT-User",
          "ClaudeBot",
          "anthropic-ai",
          "Google-Extended",
          "Applebot-Extended",
          "Bytespider",
          "CCBot",
          "Amazonbot",
          "CloudflareBrowserRenderingCrawler",
          "meta-externalagent",
          "FacebookBot",
          "PerplexityBot",
        ],
        disallow: "/",
      },
    ],
    sitemap: "https://www.officeos.co/sitemap.xml",
  };
}
