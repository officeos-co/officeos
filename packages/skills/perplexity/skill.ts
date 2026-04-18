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
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M22.3977 7.0896h-2.3106V.0676l-7.5094 6.3542V.1577h-1.1554v6.1966L4.4904 0v7.0896H1.6023v10.3976h2.8882V24l6.932-6.3591v6.2005h1.1554v-6.0469l6.9318 6.1807v-6.4879h2.8882V7.0896zm-3.4657-4.531v4.531h-5.355l5.355-4.531zm-13.2862.0676 4.8691 4.4634H5.6458V2.6262zM2.7576 16.332V8.245h7.8476l-6.1149 6.1147v1.9723H2.7576zm2.8882 5.0404v-3.8852h.0001v-2.6488l5.7763-5.7764v7.0111l-5.7764 5.2993zm12.7086.0248-5.7766-5.1509V9.0618l5.7766 5.7766v6.5588zm2.8882-5.0652h-1.733v-1.9723L13.3948 8.245h7.8478v8.087z\"/></svg>",
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
