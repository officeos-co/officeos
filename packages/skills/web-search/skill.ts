import { defineSkill, z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function baseUrl(ctx: Ctx) {
  return ctx.credentials.instance_url.replace(/\/$/, "");
}

async function searxFetch(ctx: Ctx, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${baseUrl(ctx)}${path}${qs}`);
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`SearXNG ${res.status}: ${text}`);
  }
  return res.json();
}

const searchResultSchema = z.object({
  title: z.string(),
  url: z.string(),
  content: z.string().optional(),
  engine: z.string(),
  score: z.number().optional(),
  category: z.string().optional(),
  publishedDate: z.string().optional(),
});

const imageResultSchema = z.object({
  title: z.string(),
  url: z.string(),
  img_src: z.string(),
  thumbnail_src: z.string().optional(),
  engine: z.string(),
});

export default defineSkill({
  name: "web-search",
  title: "Web Search",
  emoji: "\uD83D\uDD0D",
  description: "Self-hosted meta-search engine powered by SearXNG. Searches across multiple engines without tracking.",
  doc,

  credentials: {
    instance_url: {
      label: "SearXNG Instance URL",
      kind: "text",
      placeholder: "https://search.example.com",
      help: "Base URL of your SearXNG instance.",
    },
  },

  actions: {
    search: {
      description: "Search the web across multiple engines.",
      params: z.object({
        query: z.string().describe("Search query"),
        categories: z.string().optional().describe("Comma-separated categories: general, images, news, videos, music, files, it, science, social media"),
        engines: z.string().optional().describe("Comma-separated engine names (e.g. google,bing,duckduckgo)"),
        language: z.string().default("en").describe("Search language (BCP 47 code)"),
        page: z.number().default(1).describe("Page number (1-based)"),
        limit: z.number().default(10).describe("Max results to return"),
        time_range: z.string().optional().describe("Time filter: day, week, month, year"),
        safe_search: z.number().default(0).describe("Safe search: 0 (off), 1 (moderate), 2 (strict)"),
      }),
      returns: z.array(searchResultSchema),
      execute: async (params, ctx) => {
        const qp: Record<string, string> = {
          q: params.query,
          format: "json",
          pageno: String(params.page),
          safesearch: String(params.safe_search),
          language: params.language,
        };
        if (params.categories) qp.categories = params.categories;
        if (params.engines) qp.engines = params.engines;
        if (params.time_range) qp.time_range = params.time_range;
        const data = await searxFetch(ctx, "/search", qp);
        return (data.results ?? []).slice(0, params.limit);
      },
    } satisfies ActionDefinition,

    search_images: {
      description: "Search for images.",
      params: z.object({
        query: z.string().describe("Image search query"),
        limit: z.number().default(10).describe("Max results"),
      }),
      returns: z.array(imageResultSchema),
      execute: async (params, ctx) => {
        const data = await searxFetch(ctx, "/search", {
          q: params.query,
          format: "json",
          categories: "images",
        });
        return (data.results ?? []).slice(0, params.limit);
      },
    } satisfies ActionDefinition,

    search_news: {
      description: "Search for news articles.",
      params: z.object({
        query: z.string().describe("News search query"),
        time_range: z.string().optional().describe("Time filter: day, week, month, year"),
        limit: z.number().default(10).describe("Max results"),
      }),
      returns: z.array(searchResultSchema),
      execute: async (params, ctx) => {
        const qp: Record<string, string> = {
          q: params.query,
          format: "json",
          categories: "news",
        };
        if (params.time_range) qp.time_range = params.time_range;
        const data = await searxFetch(ctx, "/search", qp);
        return (data.results ?? []).slice(0, params.limit);
      },
    } satisfies ActionDefinition,

    get_engines: {
      description: "List all available search engines.",
      params: z.object({}),
      returns: z.array(z.object({
        name: z.string(),
        enabled: z.boolean(),
        shortcut: z.string(),
        categories: z.array(z.string()),
      })),
      execute: async (_params, ctx) => {
        const data = await searxFetch(ctx, "/config");
        return data.engines ?? [];
      },
    } satisfies ActionDefinition,

    get_categories: {
      description: "List all available search categories.",
      params: z.object({}),
      returns: z.array(z.string()),
      execute: async (_params, ctx) => {
        const data = await searxFetch(ctx, "/config");
        return data.categories ?? [];
      },
    } satisfies ActionDefinition,

    autocomplete: {
      description: "Get search suggestions for a partial query.",
      params: z.object({
        query: z.string().describe("Partial query for suggestions"),
      }),
      returns: z.array(z.string()),
      execute: async (params, ctx) => {
        const data = await searxFetch(ctx, "/autocompleter", { q: params.query });
        return data ?? [];
      },
    } satisfies ActionDefinition,
  },
});
