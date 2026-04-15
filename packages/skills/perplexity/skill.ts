import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

const PERPLEXITY_API = "https://api.perplexity.ai/chat/completions";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

const MessageSchema = z.object({
  role: z.enum(["system", "user", "assistant"]).describe("Message role"),
  content: z.string().describe("Message content"),
});

const CitationSchema = z.object({
  url: z.string(),
  title: z.string().optional(),
});

const UsageSchema = z.object({
  prompt_tokens: z.number(),
  completion_tokens: z.number(),
});

async function pplxChat(
  ctx: Ctx,
  model: string,
  messages: Array<{ role: string; content: string }>,
  extra?: Record<string, unknown>,
) {
  const res = await ctx.fetch(PERPLEXITY_API, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${ctx.credentials.api_key}`,
      "Content-Type": "application/json",
      "User-Agent": "eaos-skill-runtime/1.0",
    },
    body: JSON.stringify({ model, messages, ...extra }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Perplexity API ${res.status}: ${text}`);
  }
  return res.json();
}

function parseCitations(data: { citations?: string[] }): Array<{ url: string; title?: string }> {
  return (data.citations ?? []).map((url: string) => ({ url }));
}

export default defineSkill({
  name: "perplexity",
  title: "Perplexity",
  emoji: "🔍",
  description:
    "AI-powered search and chat using Perplexity's online LLMs with real-time web access and citations.",
  doc,

  credentials: {
    api_key: {
      label: "API Key",
      kind: "password",
      placeholder: "pplx-...",
      help: "Your Perplexity API key from https://www.perplexity.ai/settings/api",
    },
  },

  actions: {
    search: {
      description: "Perform a web search using a Perplexity online model. Returns a grounded answer with citations.",
      params: z.object({
        query: z.string().describe("Search query"),
        model: z.string().optional().describe("Perplexity model: sonar, sonar-pro, sonar-reasoning, sonar-reasoning-pro"),
        search_domain_filter: z.string().optional().describe("Comma-separated list of domains to restrict search to"),
        return_images: z.boolean().optional().describe("Include image results"),
        return_related: z.boolean().optional().describe("Include related questions"),
        recency: z.enum(["month", "week", "day", "hour"]).optional().describe("Filter by recency"),
      }),
      returns: z.object({
        answer: z.string(),
        citations: z.array(CitationSchema),
        model: z.string(),
        usage: UsageSchema,
      }),
      execute: async (params, ctx) => {
        const model = params.model ?? "sonar";
        const extra: Record<string, unknown> = {};
        if (params.search_domain_filter) {
          extra["search_domain_filter"] = params.search_domain_filter.split(",").map((d) => d.trim());
        }
        if (params.return_images) extra["return_images"] = true;
        if (params.return_related) extra["return_related_questions"] = true;
        if (params.recency) extra["search_recency_filter"] = params.recency;

        const data = await pplxChat(ctx, model, [{ role: "user", content: params.query }], extra);
        return {
          answer: data.choices[0].message.content,
          citations: parseCitations(data),
          model: data.model,
          usage: {
            prompt_tokens: data.usage?.prompt_tokens ?? 0,
            completion_tokens: data.usage?.completion_tokens ?? 0,
          },
        };
      },
    },

    chat: {
      description: "Multi-turn conversation with a Perplexity model. Supports system prompts and conversation history.",
      params: z.object({
        messages: z.array(MessageSchema).describe("Array of { role, content } messages"),
        model: z.string().optional().describe("Perplexity model to use"),
        temperature: z.number().min(0).max(2).optional().describe("Sampling temperature (0–2)"),
        max_tokens: z.number().optional().describe("Maximum tokens in response"),
      }),
      returns: z.object({
        content: z.string(),
        role: z.string(),
        model: z.string(),
        citations: z.array(z.string()),
        usage: UsageSchema,
      }),
      execute: async (params, ctx) => {
        const model = params.model ?? "sonar";
        const extra: Record<string, unknown> = {};
        if (params.temperature !== undefined) extra["temperature"] = params.temperature;
        if (params.max_tokens !== undefined) extra["max_tokens"] = params.max_tokens;

        const data = await pplxChat(ctx, model, params.messages, extra);
        return {
          content: data.choices[0].message.content,
          role: data.choices[0].message.role,
          model: data.model,
          citations: data.citations ?? [],
          usage: {
            prompt_tokens: data.usage?.prompt_tokens ?? 0,
            completion_tokens: data.usage?.completion_tokens ?? 0,
          },
        };
      },
    },

    search_news: {
      description: "Search recent news articles on a topic.",
      params: z.object({
        query: z.string().describe("News search query"),
        recency: z.enum(["month", "week", "day", "hour"]).optional().describe("Filter by recency"),
        model: z.string().optional().describe("Perplexity model to use"),
      }),
      returns: z.object({
        answer: z.string(),
        citations: z.array(CitationSchema),
        model: z.string(),
      }),
      execute: async (params, ctx) => {
        const model = params.model ?? "sonar";
        const extra: Record<string, unknown> = {
          search_recency_filter: params.recency ?? "week",
        };
        const data = await pplxChat(ctx, model, [{ role: "user", content: params.query }], extra);
        return {
          answer: data.choices[0].message.content,
          citations: parseCitations(data),
          model: data.model,
        };
      },
    },

    search_academic: {
      description: "Search academic papers and research on a topic.",
      params: z.object({
        query: z.string().describe("Academic search query"),
        search_domain_filter: z.string().optional().describe("Comma-separated domains to restrict to"),
        model: z.string().optional().describe("Perplexity model to use"),
      }),
      returns: z.object({
        answer: z.string(),
        citations: z.array(CitationSchema),
        model: z.string(),
      }),
      execute: async (params, ctx) => {
        const model = params.model ?? "sonar";
        const extra: Record<string, unknown> = {};
        if (params.search_domain_filter) {
          extra["search_domain_filter"] = params.search_domain_filter.split(",").map((d) => d.trim());
        }
        const data = await pplxChat(ctx, model, [{ role: "user", content: params.query }], extra);
        return {
          answer: data.choices[0].message.content,
          citations: parseCitations(data),
          model: data.model,
        };
      },
    },
  },
});
