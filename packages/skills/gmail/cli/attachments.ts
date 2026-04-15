import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { gFetch, extractAttachments, GMAIL_API } from "../core/client.ts";

export const attachments: Record<string, ActionDefinition> = {
  get_attachment: {
    description: "Get attachment metadata and base64-encoded content.",
    params: z.object({
      message_id: z.string().describe("Parent message ID"),
      attachment_id: z.string().describe("Attachment ID"),
    }),
    returns: z.object({
      attachment_id: z.string().describe("Attachment ID"),
      size: z.number().describe("Size in bytes"),
      data: z.string().describe("Base64-encoded content"),
    }),
    execute: async (params, ctx) => {
      const att = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}/attachments/${params.attachment_id}`);
      return {
        attachment_id: params.attachment_id,
        size: att.size ?? 0,
        data: att.data ?? "",
      };
    },
  },

  download_attachment: {
    description: "Download an attachment to a local file path.",
    params: z.object({
      message_id: z.string().describe("Parent message ID"),
      attachment_id: z.string().describe("Attachment ID"),
      output_path: z.string().describe("Local file path to save to"),
    }),
    returns: z.object({
      file_path: z.string().describe("Saved file path"),
      size: z.number().describe("Size in bytes"),
      mime_type: z.string().describe("MIME type"),
    }),
    execute: async (params, ctx) => {
      const msg = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}?format=full`);
      const atts = extractAttachments(msg);
      const meta = atts.find((a: any) => a.attachment_id === params.attachment_id);
      const att = await gFetch(ctx, `${GMAIL_API}/messages/${params.message_id}/attachments/${params.attachment_id}`);
      const data = Buffer.from(att.data ?? "", "base64url");
      const { writeFile } = await import("fs/promises");
      await writeFile(params.output_path, data);
      return {
        file_path: params.output_path,
        size: data.length,
        mime_type: meta?.mime_type ?? "application/octet-stream",
      };
    },
  },
};
