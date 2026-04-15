import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, gPost, gDelete, buildRfc2822, encodeMessage, extractHeader, extractBody, extractAttachments, GMAIL_API } from "../core/client.ts";

export const messages: Record<string, ActionDefinition> = {
  list_messages: {
    description: "List messages in the mailbox, optionally filtered by query and label.",
    params: z.object({
      query: z.string().optional().describe("Gmail search query (same syntax as web)"),
      label: z.string().default("INBOX").describe("Label ID to filter by"),
      max_results: z.number().min(1).max(500).default(10).describe("Messages to return"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
      snippet: z.string().describe("Short snippet"),
      from: z.string().describe("Sender"),
      to: z.string().describe("Recipient"),
      subject: z.string().describe("Subject line"),
      date: z.string().describe("Date header"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    })),
    execute: async (params, ctx) => {
      const q = new URLSearchParams({ maxResults: String(params.max_results), labelIds: params.label });
      if (params.query) q.set("q", params.query);
      const list = await gFetch(ctx, `${GMAIL_API}/messages?${q}`);
      const messages = list.messages ?? [];
      const results = [];
      for (const m of messages) {
        const full = await gFetch(ctx, `${GMAIL_API}/messages/${m.id}?format=metadata&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Subject&metadataHeaders=Date`);
        results.push({
          id: full.id,
          thread_id: full.threadId,
          snippet: full.snippet ?? "",
          from: extractHeader(full, "From"),
          to: extractHeader(full, "To"),
          subject: extractHeader(full, "Subject"),
          date: extractHeader(full, "Date"),
          label_ids: full.labelIds ?? [],
        });
      }
      return results;
    },
  },

  get_message: {
    description: "Get full content of a single message including body and attachments.",
    params: z.object({
      message_id: z.string().describe("Message ID"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
      from: z.string().describe("Sender"),
      to: z.string().describe("Recipient"),
      cc: z.string().describe("CC recipients"),
      bcc: z.string().describe("BCC recipients"),
      subject: z.string().describe("Subject line"),
      date: z.string().describe("Date header"),
      body: z.object({ plain: z.string().describe("Plain text body"), html: z.string().describe("HTML body") }).describe("Message body"),
      label_ids: z.array(z.string()).describe("Label IDs"),
      attachments: z.array(z.object({
        filename: z.string().describe("File name"),
        mime_type: z.string().describe("MIME type"),
        size: z.number().describe("Size in bytes"),
        attachment_id: z.string().describe("Attachment ID"),
      })).describe("Attachments"),
    }),
    execute: async (params, ctx) => {
      const msg = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}?format=full`);
      const body = extractBody(msg);
      return {
        id: msg.id,
        thread_id: msg.threadId,
        from: extractHeader(msg, "From"),
        to: extractHeader(msg, "To"),
        cc: extractHeader(msg, "Cc"),
        bcc: extractHeader(msg, "Bcc"),
        subject: extractHeader(msg, "Subject"),
        date: extractHeader(msg, "Date"),
        body,
        label_ids: msg.labelIds ?? [],
        attachments: extractAttachments(msg),
      };
    },
  },

  send_message: {
    description: "Send a new email message.",
    params: z.object({
      to: z.string().describe("Recipient email address"),
      subject: z.string().describe("Email subject line"),
      body: z.string().describe("Email body (plain text or HTML)"),
      cc: z.string().optional().describe("CC recipients (comma-separated)"),
      bcc: z.string().optional().describe("BCC recipients (comma-separated)"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const raw = buildRfc2822({ to: params.to, subject: params.subject, body: params.body, cc: params.cc, bcc: params.bcc });
      const res = await gPost(ctx, `${GMAIL_API}/messages/send`, { raw: encodeMessage(raw) });
      return { id: res.id, thread_id: res.threadId, label_ids: res.labelIds ?? [] };
    },
  },

  reply_message: {
    description: "Reply to an existing message.",
    params: z.object({
      message_id: z.string().describe("Message ID to reply to"),
      body: z.string().describe("Reply body"),
      cc: z.string().optional().describe("Additional CC recipients"),
      bcc: z.string().optional().describe("BCC recipients"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
    }),
    execute: async (params, ctx) => {
      const orig = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}?format=metadata&metadataHeaders=From&metadataHeaders=To&metadataHeaders=Subject&metadataHeaders=Message-ID&metadataHeaders=References`);
      const from = extractHeader(orig, "From");
      const subject = extractHeader(orig, "Subject");
      const messageId = extractHeader(orig, "Message-ID");
      const refs = extractHeader(orig, "References");
      const raw = buildRfc2822({
        to: from,
        subject: subject.startsWith("Re: ") ? subject : `Re: ${subject}`,
        body: params.body,
        cc: params.cc,
        bcc: params.bcc,
        inReplyTo: messageId,
        references: refs ? `${refs} ${messageId}` : messageId,
      });
      const res = await gPost(ctx, `${GMAIL_API}/messages/send`, { raw: encodeMessage(raw), threadId: orig.threadId });
      return { id: res.id, thread_id: res.threadId };
    },
  },

  forward_message: {
    description: "Forward a message to another recipient.",
    params: z.object({
      message_id: z.string().describe("Message ID to forward"),
      to: z.string().describe("Recipient email address"),
      body: z.string().optional().describe("Optional message to prepend"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      thread_id: z.string().describe("Thread ID"),
    }),
    execute: async (params, ctx) => {
      const orig = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}?format=full`);
      const subject = extractHeader(orig, "Subject");
      const origBody = extractBody(orig);
      const fwdBody = `${params.body ?? ""}\n\n---------- Forwarded message ----------\n${origBody.plain || origBody.html}`;
      const raw = buildRfc2822({
        to: params.to,
        subject: subject.startsWith("Fwd: ") ? subject : `Fwd: ${subject}`,
        body: fwdBody,
      });
      const res = await gPost(ctx, `${GMAIL_API}/messages/send`, { raw: encodeMessage(raw) });
      return { id: res.id, thread_id: res.threadId };
    },
  },

  delete_message: {
    description: "Permanently delete a message. Use trash_message for safer removal.",
    params: z.object({
      message_id: z.string().describe("Message ID to permanently delete"),
    }),
    returns: z.object({ status: z.string().describe("Confirmation status") }),
    execute: async (params, ctx) => {
      await gDelete(ctx, `${GMAIL_API}/messages/${params.message_id}`);
      return { status: "deleted" };
    },
  },

  trash_message: {
    description: "Move a message to trash.",
    params: z.object({
      message_id: z.string().describe("Message ID to trash"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/messages/${params.message_id}/trash`, {});
      return { id: res.id, label_ids: res.labelIds ?? [] };
    },
  },

  untrash_message: {
    description: "Remove a message from trash.",
    params: z.object({
      message_id: z.string().describe("Message ID to untrash"),
    }),
    returns: z.object({
      id: z.string().describe("Message ID"),
      label_ids: z.array(z.string()).describe("Label IDs"),
    }),
    execute: async (params, ctx) => {
      const res = await gPost(ctx, `${GMAIL_API}/messages/${params.message_id}/untrash`, {});
      return { id: res.id, label_ids: res.labelIds ?? [] };
    },
  },

};
