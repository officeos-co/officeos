import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function buildHeaders(ctx: Ctx) {
  return {
    "Content-Type": "application/json",
    "X-IMAP-Host": ctx.credentials.imap_host ?? "",
    "X-SMTP-Host": ctx.credentials.smtp_host ?? "",
    "X-Email": ctx.credentials.email ?? "",
    "X-Email-Password": ctx.credentials.password ?? "",
  };
}

async function emailGet(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    headers: buildHeaders(ctx),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Email proxy ${res.status}: ${body}`);
  }
  return res.json();
}

async function emailPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    method,
    headers: buildHeaders(ctx),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Email proxy ${res.status}: ${text}`);
  }
  return res.json();
}

const messageHeader = z.object({
  uid: z.number(),
  subject: z.string(),
  from: z.string(),
  to: z.union([z.string(), z.array(z.string())]),
  date: z.string(),
  flags: z.array(z.string()),
  size_bytes: z.number(),
  has_attachments: z.boolean(),
});

const attachmentSpec = z.object({
  filename: z.string().describe("Attachment filename"),
  content_base64: z.string().describe("Base64-encoded file content"),
  content_type: z.string().describe("MIME content type, e.g. application/pdf"),
});

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Folders ────────────────────────────────────────────────────────────

    list_folders: {
      description: "List all mailbox folders on the IMAP server.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string(),
          delimiter: z.string(),
          flags: z.array(z.string()),
          message_count: z.number(),
          unseen_count: z.number(),
        }),
      ),
      execute: async (_params, ctx) => emailGet(ctx, "/folders"),
    },

    get_folder: {
      description: "Get status info for a specific folder.",
      params: z.object({
        folder: z.string().describe("Folder/mailbox name, e.g. INBOX"),
      }),
      returns: z.object({
        name: z.string(),
        flags: z.array(z.string()),
        message_count: z.number(),
        recent_count: z.number(),
        unseen_count: z.number(),
        uid_validity: z.number(),
        uid_next: z.number(),
      }),
      execute: async (params, ctx) =>
        emailGet(ctx, `/folders/${encodeURIComponent(params.folder)}`),
    },

    // ── Messages ───────────────────────────────────────────────────────────

    list_messages: {
      description: "List messages in a folder (newest first).",
      params: z.object({
        folder: z.string().default("INBOX").describe("Folder to list messages from"),
        limit: z.number().int().default(20).describe("Maximum messages to return"),
        offset: z.number().int().default(0).describe("Message offset for pagination"),
        unseen_only: z.boolean().default(false).describe("Return only unread messages"),
      }),
      returns: z.array(messageHeader),
      execute: async (params, ctx) =>
        emailGet(
          ctx,
          `/folders/${encodeURIComponent(params.folder)}/messages?limit=${params.limit}&offset=${params.offset}&unseen_only=${params.unseen_only}`,
        ),
    },

    get_message: {
      description: "Get the full content of a message by UID.",
      params: z.object({
        folder: z.string().default("INBOX").describe("Folder name"),
        uid: z.number().int().describe("Message UID"),
        include_body: z.boolean().default(true).describe("Include message body"),
        format: z.enum(["text", "html"]).default("text").describe("Body format: text or html"),
      }),
      returns: z.object({
        uid: z.number(),
        message_id: z.string(),
        subject: z.string(),
        from: z.string(),
        to: z.union([z.string(), z.array(z.string())]),
        cc: z.union([z.string(), z.array(z.string())]).nullable(),
        bcc: z.union([z.string(), z.array(z.string())]).nullable(),
        date: z.string(),
        flags: z.array(z.string()),
        text_body: z.string().nullable(),
        html_body: z.string().nullable(),
        attachments: z.array(
          z.object({ filename: z.string(), content_type: z.string(), size_bytes: z.number() }),
        ),
      }),
      execute: async (params, ctx) =>
        emailGet(
          ctx,
          `/folders/${encodeURIComponent(params.folder)}/messages/${params.uid}?include_body=${params.include_body}&format=${params.format}`,
        ),
    },

    search: {
      description: "Search for messages matching criteria across a folder.",
      params: z.object({
        folder: z.string().default("INBOX").describe("Folder to search in"),
        query: z
          .string()
          .optional()
          .describe("Free-text search (from:, to:, subject:, body: prefixes supported)"),
        since: z.string().optional().describe("Return messages after this date (ISO 8601)"),
        before: z.string().optional().describe("Return messages before this date (ISO 8601)"),
        unseen: z.boolean().optional().describe("Filter by unseen flag"),
        flagged: z.boolean().optional().describe("Filter by flagged status"),
        limit: z.number().int().default(50).describe("Maximum results"),
      }),
      returns: z.array(messageHeader),
      execute: async (params, ctx) =>
        emailPost(ctx, `/folders/${encodeURIComponent(params.folder)}/search`, params),
    },

    // ── Sending ────────────────────────────────────────────────────────────

    send: {
      description: "Send a new email via SMTP.",
      params: z.object({
        to: z.array(z.string()).describe("Recipient email addresses"),
        subject: z.string().describe("Message subject"),
        body: z.string().describe("Plain-text body"),
        html_body: z.string().optional().describe("HTML body (sent as alternative part)"),
        cc: z.array(z.string()).optional().describe("CC recipients"),
        bcc: z.array(z.string()).optional().describe("BCC recipients"),
        reply_to: z.string().optional().describe("Reply-To address"),
        attachments: z.array(attachmentSpec).optional().describe("File attachments"),
      }),
      returns: z.object({
        message_id: z.string(),
        accepted: z.array(z.string()),
        rejected: z.array(z.string()),
        sent_at: z.string(),
      }),
      execute: async (params, ctx) => emailPost(ctx, "/messages/send", params),
    },

    reply: {
      description: "Reply to an existing message.",
      params: z.object({
        folder: z.string().default("INBOX").describe("Folder containing the original message"),
        uid: z.number().int().describe("UID of the message to reply to"),
        body: z.string().describe("Reply body text"),
        html_body: z.string().optional().describe("HTML reply body"),
        reply_all: z
          .boolean()
          .default(false)
          .describe("Include all original recipients in reply"),
        attachments: z.array(attachmentSpec).optional().describe("File attachments"),
      }),
      returns: z.object({
        message_id: z.string(),
        accepted: z.array(z.string()),
        sent_at: z.string(),
      }),
      execute: async (params, ctx) => emailPost(ctx, "/messages/reply", params),
    },

    forward: {
      description: "Forward an existing message to new recipients.",
      params: z.object({
        folder: z.string().default("INBOX").describe("Folder containing the original message"),
        uid: z.number().int().describe("UID of the message to forward"),
        to: z.array(z.string()).describe("Forward-to addresses"),
        note: z.string().optional().describe("Prepend note above forwarded content"),
      }),
      returns: z.object({
        message_id: z.string(),
        accepted: z.array(z.string()),
        sent_at: z.string(),
      }),
      execute: async (params, ctx) => emailPost(ctx, "/messages/forward", params),
    },

    // ── Message Management ─────────────────────────────────────────────────

    move: {
      description: "Move a message to another folder.",
      params: z.object({
        folder: z.string().describe("Source folder"),
        uid: z.number().int().describe("Message UID"),
        destination: z.string().describe("Destination folder name"),
      }),
      returns: z.object({ uid: z.number(), new_folder: z.string(), success: z.boolean() }),
      execute: async (params, ctx) =>
        emailPost(ctx, `/folders/${encodeURIComponent(params.folder)}/messages/${params.uid}/move`, {
          destination: params.destination,
        }),
    },

    copy: {
      description: "Copy a message to another folder (original is kept).",
      params: z.object({
        folder: z.string().describe("Source folder"),
        uid: z.number().int().describe("Message UID"),
        destination: z.string().describe("Destination folder name"),
      }),
      returns: z.object({ uid: z.number(), destination: z.string(), success: z.boolean() }),
      execute: async (params, ctx) =>
        emailPost(ctx, `/folders/${encodeURIComponent(params.folder)}/messages/${params.uid}/copy`, {
          destination: params.destination,
        }),
    },

    delete_message: {
      description: "Delete a message. Set expunge: true for permanent deletion.",
      params: z.object({
        folder: z.string().describe("Folder name"),
        uid: z.number().int().describe("Message UID"),
        expunge: z
          .boolean()
          .default(true)
          .describe("Permanently expunge (true) or just mark as deleted (false)"),
      }),
      returns: z.object({ uid: z.number(), deleted: z.boolean(), expunged: z.boolean() }),
      execute: async (params, ctx) =>
        emailPost(
          ctx,
          `/folders/${encodeURIComponent(params.folder)}/messages/${params.uid}`,
          { expunge: params.expunge },
          "DELETE",
        ),
    },

    set_flags: {
      description: "Add or remove IMAP flags on a message (e.g. mark as read/unread, flag).",
      params: z.object({
        folder: z.string().describe("Folder name"),
        uid: z.number().int().describe("Message UID"),
        flags: z
          .array(z.string())
          .describe("IMAP flags: \\\\Seen, \\\\Flagged, \\\\Answered, \\\\Draft"),
        action: z.enum(["add", "remove"]).describe("add or remove the specified flags"),
      }),
      returns: z.object({ uid: z.number(), flags: z.array(z.string()) }),
      execute: async (params, ctx) =>
        emailPost(
          ctx,
          `/folders/${encodeURIComponent(params.folder)}/messages/${params.uid}/flags`,
          { flags: params.flags, action: params.action },
        ),
    },
  },
});
