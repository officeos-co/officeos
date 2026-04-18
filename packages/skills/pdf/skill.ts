import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

async function proxyGet(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    headers: { "Content-Type": "application/json" },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`PDF proxy ${res.status}: ${body}`);
  }
  return res.json();
}

async function proxyPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}${path}`, {
    method,
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`PDF proxy ${res.status}: ${text}`);
  }
  return res.json();
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Documents ──────────────────────────────────────────────────────────

    create: {
      description: "Create a new blank PDF document.",
      params: z.object({
        filename: z.string().describe("Output filename, e.g. report.pdf"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => proxyPost(ctx, "/documents", params),
    },

    open: {
      description: "Open an existing PDF by file ID.",
      params: z.object({
        file_id: z.string().describe("ID of a previously stored file"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        page_count: z.number(),
        title: z.string().nullable(),
        author: z.string().nullable(),
        creator: z.string().nullable(),
        producer: z.string().nullable(),
        creation_date: z.string().nullable(),
        modification_date: z.string().nullable(),
        encrypted: z.boolean(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/documents/${params.file_id}`),
    },

    get_info: {
      description: "Get detailed metadata and page info for a PDF.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        file_id: z.string(),
        page_count: z.number(),
        title: z.string().nullable(),
        author: z.string().nullable(),
        subject: z.string().nullable(),
        keywords: z.string().nullable(),
        creator: z.string().nullable(),
        producer: z.string().nullable(),
        creation_date: z.string().nullable(),
        modification_date: z.string().nullable(),
        page_sizes: z.array(z.object({ page: z.number(), width: z.number(), height: z.number() })),
        encrypted: z.boolean(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/documents/${params.file_id}/info`),
    },

    save: {
      description: "Persist all pending changes to a PDF.",
      params: z.object({
        file_id: z.string().describe("File ID to save"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        saved_at: z.string(),
      }),
      execute: async (params, ctx) => proxyPost(ctx, `/documents/${params.file_id}/save`, {}),
    },

    // ── Content ────────────────────────────────────────────────────────────

    extract_text: {
      description: "Extract text content from a PDF (requires embedded text layer).",
      params: z.object({
        file_id: z.string().describe("File ID"),
        pages: z
          .string()
          .optional()
          .describe("Page range expression: 1-3,5,7 (1-indexed). Omit for all pages."),
      }),
      returns: z.object({
        text: z.string(),
        pages: z.array(z.object({ page: z.number(), text: z.string() })),
      }),
      execute: async (params, ctx) => {
        const qs = params.pages ? `?pages=${encodeURIComponent(params.pages)}` : "";
        return proxyGet(ctx, `/documents/${params.file_id}/text${qs}`);
      },
    },

    add_page: {
      description: "Add a blank page to the PDF.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        width: z.number().int().default(595).describe("Page width in points (595 = A4)"),
        height: z.number().int().default(842).describe("Page height in points (842 = A4)"),
        size: z
          .enum(["A4", "A3", "Letter", "Legal"])
          .optional()
          .describe("Named page size (overrides width/height)"),
        position: z
          .number()
          .int()
          .optional()
          .describe("0-based page index to insert at (omit to append)"),
      }),
      returns: z.object({
        page_index: z.number(),
        page_count: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/pages`, params),
    },

    add_text: {
      description: "Draw text onto a page at specified coordinates.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        page_index: z.number().int().describe("Zero-based page index"),
        text: z.string().describe("Text to draw"),
        x: z.number().int().default(50).describe("X position in points from left edge"),
        y: z.number().int().default(750).describe("Y position in points from bottom edge"),
        font_size: z.number().int().default(12).describe("Font size in points"),
        font: z
          .enum(["Helvetica", "Times-Roman", "Courier"])
          .default("Helvetica")
          .describe("Font family"),
        color: z.string().default("#000000").describe("Hex text color"),
        bold: z.boolean().default(false).describe("Bold text"),
        italic: z.boolean().default(false).describe("Italic text"),
        opacity: z.number().min(0).max(1).default(1.0).describe("Opacity (0.0–1.0)"),
      }),
      returns: z.object({
        text_element_id: z.string(),
        page_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/pages/${params.page_index}/text`, params),
    },

    add_image: {
      description: "Embed an image onto a PDF page.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        page_index: z.number().int().describe("Zero-based page index"),
        image_url: z.string().describe("URL or base64 data URI of the image (PNG or JPEG)"),
        x: z.number().int().optional().describe("X position in points from left edge"),
        y: z.number().int().optional().describe("Y position in points from bottom edge"),
        width: z.number().int().optional().describe("Image width in points"),
        height: z.number().int().optional().describe("Image height in points"),
      }),
      returns: z.object({
        image_element_id: z.string(),
        page_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/pages/${params.page_index}/images`, params),
    },

    delete_page: {
      description: "Delete a page from the PDF.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        page_index: z.number().int().describe("Zero-based page index to delete"),
      }),
      returns: z.object({
        deleted_index: z.number(),
        page_count: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/documents/${params.file_id}/pages/${params.page_index}`,
          {},
          "DELETE",
        ),
    },

    rotate: {
      description: "Rotate one or more pages by a multiple of 90 degrees.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        pages: z
          .string()
          .describe("Page range expression: 1-3,5 (1-indexed), or all"),
        degrees: z
          .union([z.literal(90), z.literal(180), z.literal(270)])
          .describe("Rotation angle in degrees: 90, 180, or 270"),
      }),
      returns: z.object({ rotated_pages: z.array(z.number()), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/rotate`, {
          pages: params.pages,
          degrees: params.degrees,
        }),
    },

    // ── Multi-file operations ──────────────────────────────────────────────

    merge: {
      description: "Merge multiple PDFs into a single document.",
      params: z.object({
        file_ids: z.array(z.string()).describe("Ordered list of file IDs to merge"),
        output_filename: z.string().optional().describe("Output filename for merged PDF"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        page_count: z.number(),
        size_bytes: z.number(),
      }),
      execute: async (params, ctx) => proxyPost(ctx, "/documents/merge", params),
    },

    split: {
      description: "Split a PDF into multiple documents by page ranges.",
      params: z.object({
        file_id: z.string().describe("Source file ID"),
        ranges: z
          .array(z.string())
          .describe("Array of page range expressions (1-indexed): [\"1-3\",\"4-6\",\"7\"]"),
      }),
      returns: z.array(
        z.object({
          file_id: z.string(),
          filename: z.string(),
          page_count: z.number(),
          range: z.string(),
        }),
      ),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/split`, { ranges: params.ranges }),
    },

    copy_pages: {
      description: "Copy pages from one PDF into another.",
      params: z.object({
        source_file_id: z.string().describe("Source file ID"),
        target_file_id: z.string().describe("Destination file ID"),
        pages: z.string().describe("Page range from source (1-indexed)"),
        insert_at: z
          .number()
          .int()
          .optional()
          .describe("0-based page index in target to insert before (omit to append)"),
      }),
      returns: z.object({ page_count: z.number(), target_file_id: z.string() }),
      execute: async (params, ctx) => proxyPost(ctx, "/documents/copy-pages", params),
    },

    // ── Metadata ───────────────────────────────────────────────────────────

    set_metadata: {
      description: "Update PDF document metadata.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        title: z.string().optional().describe("Document title"),
        author: z.string().optional().describe("Document author"),
        subject: z.string().optional().describe("Document subject"),
        keywords: z.string().optional().describe("Comma-separated keywords"),
        creator: z.string().optional().describe("Creating application name"),
      }),
      returns: z.object({ file_id: z.string(), metadata: z.record(z.unknown()) }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/metadata`, params, "PATCH"),
    },

    // ── Export ─────────────────────────────────────────────────────────────

    download: {
      description: "Get a download URL for a PDF file.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        download_url: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        expires_at: z.string(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/documents/${params.file_id}/download`),
    },
  },
});
