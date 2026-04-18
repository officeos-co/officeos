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
    throw new Error(`Word proxy ${res.status}: ${body}`);
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
    throw new Error(`Word proxy ${res.status}: ${text}`);
  }
  return res.json();
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Documents ──────────────────────────────────────────────────────────

    create: {
      description: "Create a new Word document.",
      params: z.object({
        filename: z.string().describe("Output filename, e.g. report.docx"),
        title: z.string().optional().describe("Document title metadata"),
        author: z.string().optional().describe("Document author metadata"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => proxyPost(ctx, "/documents", params),
    },

    open: {
      description: "Open an existing document by file ID.",
      params: z.object({
        file_id: z.string().describe("ID of a previously created file"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        paragraph_count: z.number(),
        section_count: z.number(),
        author: z.string().nullable(),
        title: z.string().nullable(),
        created: z.string(),
        modified: z.string(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/documents/${params.file_id}`),
    },

    save: {
      description: "Persist all pending changes to a document.",
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

    get_text: {
      description: "Extract all text from a document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        include_formatting: z
          .boolean()
          .default(false)
          .describe("Include style/font info per paragraph"),
      }),
      returns: z.object({
        text: z.string(),
        paragraphs: z.array(z.record(z.unknown())),
      }),
      execute: async (params, ctx) =>
        proxyGet(
          ctx,
          `/documents/${params.file_id}/text?include_formatting=${params.include_formatting}`,
        ),
    },

    replace_text: {
      description: "Find and replace text throughout the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        search: z.string().describe("Text to find"),
        replace: z.string().describe("Replacement text"),
        match_case: z.boolean().default(false).describe("Case-sensitive match"),
      }),
      returns: z.object({
        replacements_made: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/replace`, {
          search: params.search,
          replace: params.replace,
          match_case: params.match_case,
        }),
    },

    // ── Content ────────────────────────────────────────────────────────────

    add_paragraph: {
      description: "Add a paragraph of text to the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        text: z.string().describe("Paragraph text"),
        style: z
          .string()
          .default("Normal")
          .describe("Style: Normal, Heading1–Heading6, ListBullet, Quote"),
        position: z
          .number()
          .int()
          .optional()
          .describe("Paragraph index to insert at (omit to append)"),
        bold: z.boolean().default(false).describe("Bold text"),
        italic: z.boolean().default(false).describe("Italic text"),
        font_size: z.number().int().optional().describe("Font size in points"),
      }),
      returns: z.object({ paragraph_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/paragraphs`, params),
    },

    add_table: {
      description: "Insert a table into the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        rows: z.number().int().describe("Number of data rows (excluding header)"),
        cols: z.number().int().describe("Number of columns"),
        headers: z.array(z.string()).optional().describe("Header row values"),
        data: z.array(z.array(z.string())).optional().describe("2D array of cell values"),
        style: z.string().optional().describe("Table style name, e.g. Table Grid"),
      }),
      returns: z.object({ table_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/tables`, params),
    },

    add_image: {
      description: "Insert an image into the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        image_url: z.string().describe("URL or base64 data URI of the image"),
        width_cm: z.number().optional().describe("Image width in centimetres"),
        height_cm: z.number().optional().describe("Image height in centimetres"),
        alt_text: z.string().optional().describe("Accessibility alt text"),
        position: z.number().int().optional().describe("Paragraph index to insert before"),
      }),
      returns: z.object({ image_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/images`, params),
    },

    add_heading: {
      description: "Insert a heading paragraph.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        text: z.string().describe("Heading text"),
        level: z.number().int().min(1).max(6).default(1).describe("Heading level (1–6)"),
      }),
      returns: z.object({ paragraph_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/headings`, params),
    },

    add_page_break: {
      description: "Insert a page break at the end of the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({ paragraph_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/page-breaks`, {}),
    },

    delete_paragraph: {
      description: "Delete a paragraph by index.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        index: z.number().int().describe("Zero-based paragraph index to delete"),
      }),
      returns: z.object({
        deleted_index: z.number(),
        paragraph_count: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/documents/${params.file_id}/paragraphs/${params.index}`,
          {},
          "DELETE",
        ),
    },

    // ── Sections & Styles ──────────────────────────────────────────────────

    list_styles: {
      description: "List available styles in the document.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        type: z
          .enum(["paragraph", "character", "table"])
          .default("paragraph")
          .describe("Style type to list"),
      }),
      returns: z.array(
        z.object({ name: z.string(), type: z.string(), base_style: z.string().nullable() }),
      ),
      execute: async (params, ctx) =>
        proxyGet(ctx, `/documents/${params.file_id}/styles?type=${params.type}`),
    },

    set_properties: {
      description: "Update document metadata (title, author, subject, keywords).",
      params: z.object({
        file_id: z.string().describe("File ID"),
        title: z.string().optional().describe("Document title"),
        subject: z.string().optional().describe("Document subject"),
        author: z.string().optional().describe("Document author"),
        keywords: z.array(z.string()).optional().describe("Keyword list"),
      }),
      returns: z.object({ file_id: z.string(), properties: z.record(z.unknown()) }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/properties`, params, "PATCH"),
    },

    // ── Export ─────────────────────────────────────────────────────────────

    export_pdf: {
      description: "Export the document to PDF.",
      params: z.object({
        file_id: z.string().describe("Source Word file ID"),
        output_filename: z.string().optional().describe("PDF filename (defaults to docx name)"),
      }),
      returns: z.object({
        pdf_file_id: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        download_url: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/documents/${params.file_id}/export/pdf`, {
          output_filename: params.output_filename,
        }),
    },

    download: {
      description: "Get a download URL for a document file.",
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
