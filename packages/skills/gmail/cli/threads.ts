import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, gPost, extractHeader, GMAIL_API } from "../core/client.ts";

export const threads: Record<string, ActionDefinition> = {
  list_threads: {
    description: "List threads, optionally filtered by query.",
    params: z.object({
      query: z.string().optional().describe("Gmail search query"),
      max_results: z.number().min(1).max(500).default(10).describe("Threads to return"),
    }),
    returns: z.array(z.object({
      thread_id: z.string().describe("Thread ID"),
      snippet: z.string().describe("Short snippet"),
      history_id: z.string().describe("History ID"),
    })),
    execute: async (params, ctx) => {
      const q = new URLSearchParams({ maxResults: String(params.max_results) });
      if (params.query) q.set("q", params.query);
      const list = await gFetch(ctx, `${GMAIL_API}/threads?${q}`);
      return (list.threads ?? []).map((t: any) => ({
        thread_id: t.id,
        snippet: t.snippet ?? "",
        history_id: t.historyId ?? "",
      }));
    },
  },

  get_thread: {
    description: "Get all messages in a thread.",
    params: z.object({
      thread_id: z.string().describe("Thread ID"),
    }),
    returns: z.object({
      thread_id: z.string().describe("Thread ID"),
      messages: z.array(z.object({
        id: z.string().describe("Message ID"),
        from: z.string().describe("Sender"),
        to: z.string().describe("Recipient"),
        subject: z.string().describe("Subject"),
        date: z.string().describe("Date"),
        snippet: z.string().describe("Snippet"),
      })).describe("Messages in chronological order"),
    }),
    execute: async (params, ctx) => {
      const t = await gFetch(ctx, `${GMAIL_API}/threads/${params.thread_id}?format=metadata&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Subject&metadataHeaders=Date`);
      return {
        thread_id: t.id,
        messages: (t.messages ?? []).map((m: any) => ({
          id: m.id,
          from: extractHeader(m, "From"),
          to: extractHeader(m, "To"),
          subject: extractHeader(m, "Subject"),
          date: extractHeader(m, "Date"),
          snippet: m.snippet ?? "",
        })),
      };
    },
  },

  trash_thread: {
    description: "Move a thread to trash.",
    params: z.object({
      thread_id: z.string().describe("Thread ID to trash"),
    }),
    returns: z.object({ thread_id: z.string().describe("Thread ID") }),
    execute: async (params, ctx) => {
      await gPost(ctx, `${GMAIL_API}/threads/${params.thread_id}/trash`, {});
      return { thread_id: params.thread_id };
    },
  },

  untrash_thread: {
    description: "Remove a thread from trash.",
    params: z.object({
      thread_id: z.string().describe("Thread ID to untrash"),
    }),
    returns: z.object({ thread_id: z.string().describe("Thread ID") }),
    execute: async (params, ctx) => {
      await gPost(ctx, `${GMAIL_API}/threads/${params.thread_id}/untrash`, {});
      return { thread_id: params.thread_id };
    },
  },
};
