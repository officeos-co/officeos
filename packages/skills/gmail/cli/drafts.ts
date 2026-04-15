import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, gPost, gDelete, buildRfc2822, encodeMessage, extractHeader, extractBody, GMAIL_API } from "../core/client.ts";

export const drafts: Record<string, ActionDefinition> = {
  list_drafts: {
    description: "List drafts in the mailbox.",
    params: z.object({
      max_results: z.number().min(1).max(500).default(10).describe("Drafts to return"),
    }),
    returns: z.array(z.object({
      draft_id: z.string().describe("Draft ID"),
      message_id: z.string().describe("Message ID"),
      snippet: z.string().describe("Short snippet"),
      subject: z.string().describe("Subject line"),
    })),
    execute: async (params, ctx) => {
      const list = await gFetch(ctx, `${GMAIL_API}/drafts?maxResults=${params.max_results}`);
      const draftList = list.drafts ?? [];
      const results = [];
      for (const d of draftList) {
        const full = await gFetch(ctx, `${GMAIL_API}/drafts/${d.id}`);
        results.push({
          draft_id: full.id,
          message_id: full.message?.id ?? "",
          snippet: full.message?.snippet ?? "",
          subject: extractHeader(full.message ?? {}, "Subject"),
        });
      }
      return results;
    },
  },

  get_draft: {
    description: "Get full content of a draft.",
    params: z.object({
      draft_id: z.string().describe("Draft ID"),
    }),
    returns: z.object({
      draft_id: z.string().describe("Draft ID"),
      message: z.object({
        to: z.string().describe("Recipient"),
        from: z.string().describe("Sender"),
        subject: z.string().describe("Subject line"),
        body: z.object({ plain: z.string().describe("Plain text body"), html: z.string().describe("HTML body") }).describe("Draft body"),
      }).describe("Full message object"),
    }),
    execute: async (params, ctx) => {
      const d = await gFetch(ctx, `${GMAIL_API}/drafts/${params.draft_id}?format=full`);
      const msg = d.message ?? {};
      const body = extractBody(msg);
      return {
        draft_id: d.id,
        message: {
          to: extractHeader(msg, "To"),
          from: extractHeader(msg, "From"),
          subject: extractHeader(msg, "Subject"),
          body,
        },
      };
    },
  },

  create_draft: {
    description: "Create a new draft.",
    params: z.object({
      to: z.string().describe("Recipient email address"),
      subject: z.string().describe("Email subject line"),
      body: z.string().describe("Draft body"),
      cc: z.string().optional().describe("CC recipients (comma-separated)"),
      bcc: z.string().optional().describe("BCC recipients"),
    }),
    returns: z.object({
      draft_id: z.string().describe("Draft ID"),
      message_id: z.string().describe("Message ID"),
    }),
    execute: async (params, ctx) => {
      const raw = buildRfc2822({ to: params.to, subject: params.subject, body: params.body, cc: params.cc, bcc: params.bcc });
      const res = await gPost(ctx, `${GMAIL_API}/drafts`, { message: { raw: encodeMessage(raw) } });
      return { draft_id: res.id, message_id: res.message?.id ?? "" };
    },
  },

  update_draft: {
    description: "Update an existing draft.",
    params: z.object({
      draft_id: z.string().describe("Draft ID to update"),
      to: z.string().optional().describe("Updated recipient"),
      subject: z.string().optional().describe("Updated subject"),
      body: z.string().optional().describe("Updated body"),
      cc: z.string().optional().describe("Updated CC recipients"),
      bcc: z.string().optional().describe("Updated BCC recipients"),
    }),
    returns: z.object({
      draft_id: z.string().describe("Draft ID"),
      message_id: z.string().describe("Message ID"),
    }),
    execute: async (params, ctx) => {
      const existing = await gFetch(ctx, `${GMAIL_API}/drafts/${params.draft_id}?format=full`);
      const msg = existing.message ?? {};
      const raw = buildRfc2822({
        to: params.to ?? extractHeader(msg, "To"),
        subject: params.subject ?? extractHeader(msg, "Subject"),
        body: params.body ?? extractBody(msg).plain,
        cc: params.cc ?? (extractHeader(msg, "Cc") || undefined),
        bcc: params.bcc ?? (extractHeader(msg, "Bcc") || undefined),
      });
      const res = await gFetch(ctx, `${GMAIL_API}/drafts/${params.draft_id}`, {
        method: "PUT",
        body: JSON.stringify({ message: { raw: encodeMessage(raw) } }),
      });
      return { draft_id: res.id, message_id: res.message?.id ?? "" };
    },
  },

  send_draft: {
    description: "Send an existing draft.",
    params: z.object({
      draft_id: z.string().describe("Draft ID to send"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/drafts/send`, { id: params.draft_id });
      return { id: res.id, thread_id: res.threadId, label_ids: res.labelIds ?? [] };
    },
  },

  delete_draft: {
    description: "Delete a draft.",
    params: z.object({
      draft_id: z.string().describe("Draft ID to delete"),
    }),
    returns: z.object({ status: z.string().describe("Confirmation status") }),
    execute: async (params, ctx) => {
      await gDelete(ctx, `${GMAIL_API}/drafts/${params.draft_id}`);
      return { status: "deleted" };
    },
  },
};
