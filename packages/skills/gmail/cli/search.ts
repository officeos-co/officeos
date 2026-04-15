import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, extractHeader, GMAIL_API } from "../core/client.ts";

export const search: Record<string, ActionDefinition> = {
  search: {
    description: "Search messages using full Gmail search syntax.",
    params: z.object({
      query: z.string().describe("Gmail search query (supports full Gmail syntax)"),
      max_results: z.number().min(1).max(500).default(10).describe("Results to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
      snippet: z.string().describe("Short snippet"),
      from: z.string().describe("Sender"),
      subject: z.string().describe("Subject line"),
      date: z.string().describe("Date header"),
    })),
    execute: async (params, ctx) => {
      const q = new URLSearchParams({ q: params.query, maxResults: String(params.max_results) });
      const list = await gFetch(ctx, `${GMAIL_API}/messages?${q}`);
      const msgs = list.messages ?? [];
      const results = [];
      for (const m of msgs) {
        const full = await gFetch(ctx, `${GMAIL_API}/messages/${m.id}?format=metadata&metadataHeaders=From&metadataHeaders=Subject&metadataHeaders=Date`);
        results.push({
          id: full.id,
          thread_id: full.threadId,
          snippet: full.snippet ?? "",
          from: extractHeader(full, "From"),
          subject: extractHeader(full, "Subject"),
          date: extractHeader(full, "Date"),
        });
      }
      return results;
    },
  },
};
