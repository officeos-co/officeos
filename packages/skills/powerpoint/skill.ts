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
    throw new Error(`PowerPoint proxy ${res.status}: ${body}`);
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
    throw new Error(`PowerPoint proxy ${res.status}: ${text}`);
  }
  return res.json();
}

const positionParams = {
  x: z.number().optional().describe("X position as % of slide width (0–100)"),
  y: z.number().optional().describe("Y position as % of slide height (0–100)"),
  w: z.number().optional().describe("Width as % of slide width (0–100)"),
  h: z.number().optional().describe("Height as % of slide height (0–100)"),
};

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Presentations ──────────────────────────────────────────────────────

    create: {
      description: "Create a new PowerPoint presentation.",
      params: z.object({
        filename: z.string().describe("Output filename, e.g. deck.pptx"),
        title: z.string().optional().describe("Presentation title metadata"),
        author: z.string().optional().describe("Author metadata"),
        width_px: z.number().int().default(1280).describe("Slide width in pixels"),
        height_px: z.number().int().default(720).describe("Slide height in pixels"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => proxyPost(ctx, "/presentations", params),
    },

    open: {
      description: "Open an existing presentation by file ID.",
      params: z.object({
        file_id: z.string().describe("ID of a previously saved file"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        slide_count: z.number(),
        title: z.string().nullable(),
        author: z.string().nullable(),
        created: z.string(),
        modified: z.string(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/presentations/${params.file_id}`),
    },

    save: {
      description: "Persist all pending changes to a presentation.",
      params: z.object({
        file_id: z.string().describe("File ID to save"),
      }),
      returns: z.object({
        file_id: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        saved_at: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/save`, {}),
    },

    get_info: {
      description: "Get metadata and dimensions of a presentation.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        file_id: z.string(),
        slide_count: z.number(),
        title: z.string().nullable(),
        author: z.string().nullable(),
        subject: z.string().nullable(),
        keywords: z.array(z.string()),
        width_px: z.number(),
        height_px: z.number(),
        created: z.string(),
        modified: z.string(),
      }),
      execute: async (params, ctx) => proxyGet(ctx, `/presentations/${params.file_id}/info`),
    },

    // ── Slides ─────────────────────────────────────────────────────────────

    add_slide: {
      description: "Add a new slide to the presentation.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        layout: z
          .enum(["BLANK", "TITLE_ONLY", "TITLE_AND_CONTENT", "TWO_CONTENT"])
          .default("BLANK")
          .describe("Slide layout"),
        position: z
          .number()
          .int()
          .optional()
          .describe("Slide index to insert at (0-based, omit to append)"),
      }),
      returns: z.object({
        slide_index: z.number(),
        slide_id: z.string(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/slides`, params),
    },

    delete_slide: {
      description: "Delete a slide by index.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
      }),
      returns: z.object({
        deleted_index: z.number(),
        slide_count: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}`,
          {},
          "DELETE",
        ),
    },

    reorder_slide: {
      description: "Move a slide from one index to another.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        from_index: z.number().int().describe("Current zero-based index"),
        to_index: z.number().int().describe("Target zero-based index"),
      }),
      returns: z.object({ slide_count: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/slides/reorder`, {
          from_index: params.from_index,
          to_index: params.to_index,
        }),
    },

    duplicate_slide: {
      description: "Duplicate a slide and append the copy after the original.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Index of slide to copy"),
      }),
      returns: z.object({
        new_slide_index: z.number(),
        slide_count: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/slides/${params.slide_index}/duplicate`, {}),
    },

    // ── Content ────────────────────────────────────────────────────────────

    add_text: {
      description: "Add a text box to a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        text: z.string().describe("Text content"),
        ...positionParams,
        font_size: z.number().int().default(24).describe("Font size in points"),
        bold: z.boolean().default(false).describe("Bold text"),
        italic: z.boolean().default(false).describe("Italic text"),
        color: z.string().optional().describe("Hex color, e.g. #FF0000"),
        align: z.enum(["left", "center", "right"]).default("left").describe("Text alignment"),
      }),
      returns: z.object({
        element_id: z.string(),
        slide_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/slides/${params.slide_index}/text`, params),
    },

    add_image: {
      description: "Add an image to a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        image_url: z.string().describe("URL or base64 data URI of the image"),
        ...positionParams,
        alt_text: z.string().optional().describe("Accessibility alt text"),
      }),
      returns: z.object({
        element_id: z.string(),
        slide_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}/images`,
          params,
        ),
    },

    add_chart: {
      description: "Add a chart to a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        chart_type: z
          .enum(["bar", "column", "line", "pie", "doughnut", "area", "scatter"])
          .describe("Chart type"),
        title: z.string().optional().describe("Chart title"),
        categories: z.array(z.string()).describe("X-axis category labels"),
        series: z
          .array(z.object({ name: z.string(), values: z.array(z.number()) }))
          .describe("Data series: array of {name, values}"),
        ...positionParams,
      }),
      returns: z.object({
        element_id: z.string(),
        slide_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}/charts`,
          params,
        ),
    },

    add_shape: {
      description: "Add a shape to a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        shape_type: z
          .enum(["rect", "ellipse", "arrow", "star", "triangle"])
          .describe("Shape type"),
        ...positionParams,
        fill_color: z.string().optional().describe("Hex fill color"),
        line_color: z.string().optional().describe("Hex border color"),
        text: z.string().optional().describe("Text label inside shape"),
      }),
      returns: z.object({
        element_id: z.string(),
        slide_index: z.number(),
        file_id: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}/shapes`,
          params,
        ),
    },

    set_background: {
      description: "Set the background color or image of a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        color: z.string().optional().describe("Hex background color"),
        image_url: z.string().optional().describe("Background image URL or base64"),
      }),
      returns: z.object({ slide_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}/background`,
          params,
        ),
    },

    set_notes: {
      description: "Set speaker notes on a slide.",
      params: z.object({
        file_id: z.string().describe("File ID"),
        slide_index: z.number().int().describe("Zero-based slide index"),
        notes: z.string().describe("Speaker notes text"),
      }),
      returns: z.object({ slide_index: z.number(), file_id: z.string() }),
      execute: async (params, ctx) =>
        proxyPost(
          ctx,
          `/presentations/${params.file_id}/slides/${params.slide_index}/notes`,
          { notes: params.notes },
        ),
    },

    // ── Export ─────────────────────────────────────────────────────────────

    export_pdf: {
      description: "Export the presentation to PDF.",
      params: z.object({
        file_id: z.string().describe("Source presentation file ID"),
        output_filename: z.string().optional().describe("PDF filename (defaults to pptx name)"),
      }),
      returns: z.object({
        pdf_file_id: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        download_url: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyPost(ctx, `/presentations/${params.file_id}/export/pdf`, {
          output_filename: params.output_filename,
        }),
    },

    download: {
      description: "Get a download URL for a presentation file.",
      params: z.object({
        file_id: z.string().describe("File ID"),
      }),
      returns: z.object({
        download_url: z.string(),
        filename: z.string(),
        size_bytes: z.number(),
        expires_at: z.string(),
      }),
      execute: async (params, ctx) =>
        proxyGet(ctx, `/presentations/${params.file_id}/download`),
    },
  },
});
