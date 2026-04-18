import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

const EXA_API = "https://api.exa.ai";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function exaHeaders(apiKey: string) {
  return {
    "x-api-key": apiKey,
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

async function exaPost(ctx: Ctx, path: string, body: unknown) {
  const res = await ctx.fetch(`${EXA_API}${path}`, {
    method: "POST",
    headers: exaHeaders(ctx.credentials.api_key),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Exa API ${res.status}: ${text}`);
  }
  return res.json();
}

const ResultSchema = z.object({
  id: z.string(),
  url: z.string(),
  title: z.string().nullable(),
  score: z.number().optional(),
  published_date: z.string().nullable().optional(),
  author: z.string().nullable().optional(),
  text: z.string().optional(),
  highlights: z.array(z.string()).optional(),
});

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    search: {
      description: "Search the web using Exa's neural or keyword search.",
      params: z.object({
        query: z.string().describe("Search query"),
        num_results: z.number().min(1).max(100).optional().describe("Number of results (1-100)"),
        type: z.enum(["neural", "keyword"]).optional().describe("Search type: neural or keyword"),
        include_domains: z.array(z.string()).optional().describe("Restrict results to these domains"),
        exclude_domains: z.array(z.string()).optional().describe("Exclude results from these domains"),
        start_crawl_date: z.string().optional().describe("ISO 8601 date — only pages crawled after"),
        end_crawl_date: z.string().optional().describe("ISO 8601 date — only pages crawled before"),
        start_published_date: z.string().optional().describe("ISO 8601 date — only pages published after"),
        end_published_date: z.string().optional().describe("ISO 8601 date — only pages published before"),
        include_text: z.boolean().optional().describe("Include page text in results"),
        highlights: z.boolean().optional().describe("Return relevant highlights"),
        category: z.string().optional().describe("Filter by category: news, research paper, tweet, github repo, movie, song, personal site, pdf"),
      }),
      returns: z.object({
        results: z.array(ResultSchema),
        total: z.number().optional(),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          query: params.query,
          numResults: params.num_results ?? 10,
          type: params.type ?? "neural",
        };
        if (params.include_domains) body["includeDomains"] = params.include_domains;
        if (params.exclude_domains) body["excludeDomains"] = params.exclude_domains;
        if (params.start_crawl_date) body["startCrawlDate"] = params.start_crawl_date;
        if (params.end_crawl_date) body["endCrawlDate"] = params.end_crawl_date;
        if (params.start_published_date) body["startPublishedDate"] = params.start_published_date;
        if (params.end_published_date) body["endPublishedDate"] = params.end_published_date;
        if (params.category) body["category"] = params.category;
        if (params.include_text || params.highlights) {
          body["contents"] = {
            text: params.include_text ? { maxCharacters: 3000 } : undefined,
            highlights: params.highlights ? { numSentences: 3 } : undefined,
          };
        }
        const data = await exaPost(ctx, "/search", body);
        return { results: data.results ?? [], total: data.results?.length };
      },
    },

    find_similar: {
      description: "Find pages similar to a given URL.",
      params: z.object({
        url: z.string().describe("URL to find similar pages for"),
        num_results: z.number().min(1).max(100).optional().describe("Number of results"),
        exclude_source_domain: z.boolean().optional().describe("Exclude results from the same domain as url"),
        include_domains: z.array(z.string()).optional().describe("Restrict results to these domains"),
        exclude_domains: z.array(z.string()).optional().describe("Exclude results from these domains"),
        include_text: z.boolean().optional().describe("Include page text in results"),
      }),
      returns: z.object({
        results: z.array(ResultSchema),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          url: params.url,
          numResults: params.num_results ?? 10,
          excludeSourceDomain: params.exclude_source_domain !== false,
        };
        if (params.include_domains) body["includeDomains"] = params.include_domains;
        if (params.exclude_domains) body["excludeDomains"] = params.exclude_domains;
        if (params.include_text) body["contents"] = { text: { maxCharacters: 3000 } };
        const data = await exaPost(ctx, "/findSimilar", body);
        return { results: data.results ?? [] };
      },
    },

    get_contents: {
      description: "Retrieve the full text content of one or more pages by URL or Exa ID.",
      params: z.object({
        ids: z.array(z.string()).describe("List of Exa result IDs or URLs"),
        text: z.boolean().optional().describe("Return full page text"),
        highlights: z.boolean().optional().describe("Return relevant highlights"),
        max_chars: z.number().optional().describe("Maximum characters of text per document"),
      }),
      returns: z.object({
        results: z.array(ResultSchema),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = { ids: params.ids };
        const contents: Record<string, unknown> = {};
        if (params.text !== false) contents["text"] = { maxCharacters: params.max_chars ?? 5000 };
        if (params.highlights) contents["highlights"] = { numSentences: 3 };
        if (Object.keys(contents).length > 0) body["contents"] = contents;
        const data = await exaPost(ctx, "/contents", body);
        return { results: data.results ?? [] };
      },
    },

    search_and_contents: {
      description: "Search the web and immediately return full text content of results. Combines search + get_contents.",
      params: z.object({
        query: z.string().describe("Search query"),
        num_results: z.number().min(1).max(20).optional().describe("Number of results (1-20 recommended)"),
        type: z.enum(["neural", "keyword"]).optional().describe("Search type: neural or keyword"),
        max_chars: z.number().optional().describe("Maximum characters of text per result"),
        include_domains: z.array(z.string()).optional().describe("Restrict results to these domains"),
        exclude_domains: z.array(z.string()).optional().describe("Exclude results from these domains"),
        start_published_date: z.string().optional().describe("ISO 8601 — only pages published after"),
        end_published_date: z.string().optional().describe("ISO 8601 — only pages published before"),
        category: z.string().optional().describe("Category filter (news, research paper, tweet, github repo, etc.)"),
      }),
      returns: z.object({
        results: z.array(ResultSchema),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          query: params.query,
          numResults: params.num_results ?? 5,
          type: params.type ?? "neural",
          contents: {
            text: { maxCharacters: params.max_chars ?? 3000 },
          },
        };
        if (params.include_domains) body["includeDomains"] = params.include_domains;
        if (params.exclude_domains) body["excludeDomains"] = params.exclude_domains;
        if (params.start_published_date) body["startPublishedDate"] = params.start_published_date;
        if (params.end_published_date) body["endPublishedDate"] = params.end_published_date;
        if (params.category) body["category"] = params.category;
        const data = await exaPost(ctx, "/search", body);
        return { results: data.results ?? [] };
      },
    },
  },
});
