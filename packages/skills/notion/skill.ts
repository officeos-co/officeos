import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

const NOTION_API = "https://api.notion.com/v1";
const NOTION_VERSION = "2022-06-28";

function headers(apiKey: string) {
  return {
    Authorization: `Bearer ${apiKey}`,
    "Notion-Version": NOTION_VERSION,
    "Content-Type": "application/json",
  };
}

const BLOCK_TYPES = [
  "paragraph",
  "heading_1",
  "heading_2",
  "heading_3",
  "bulleted_list_item",
  "numbered_list_item",
  "to_do",
  "toggle",
  "quote",
  "callout",
  "divider",
  "code",
] as const;

export default defineSkill({
  name: "notion",
  title: "Notion",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M4.459 4.208c.746.606 1.026.56 2.428.466l13.215-.793c.28 0 .047-.28-.046-.326L17.86 1.968c-.42-.326-.981-.7-2.055-.607L3.01 2.295c-.466.046-.56.28-.374.466zm.793 3.08v13.904c0 .747.373 1.027 1.214.98l14.523-.84c.841-.046.935-.56.935-1.167V6.354c0-.606-.233-.933-.748-.887l-15.177.887c-.56.047-.747.327-.747.933zm14.337.745c.093.42 0 .84-.42.888l-.7.14v10.264c-.608.327-1.168.514-1.635.514-.748 0-.935-.234-1.495-.933l-4.577-7.186v6.952L12.21 19s0 .84-1.168.84l-3.222.186c-.093-.186 0-.653.327-.746l.84-.233V9.854L7.822 9.76c-.094-.42.14-1.026.793-1.073l3.456-.233 4.764 7.279v-6.44l-1.215-.139c-.093-.514.28-.887.747-.933zM1.936 1.035l13.31-.98c1.634-.14 2.055-.047 3.082.7l4.249 2.986c.7.513.934.653.934 1.213v16.378c0 1.026-.373 1.634-1.68 1.726l-15.458.934c-.98.047-1.448-.093-1.962-.747l-3.129-4.06c-.56-.747-.793-1.306-.793-1.96V2.667c0-.839.374-1.54 1.447-1.632z\"/></svg>",
  emoji: "\ud83d\udcdd",
  description:
    "Search, read, create, and manage Notion pages, blocks, and to-do items.",
  doc,

  credentials: {
    api_key: {
      label: "Internal Integration Token",
      kind: "password",
      placeholder: "secret_\u2026",
      help: "Create an Internal Integration at https://www.notion.so/my-integrations and share the pages you want the agent to access with it.",
    },
  },

  actions: {
    search: {
      description:
        "Search the connected Notion workspace for pages matching a query.",
      params: z.object({
        query: z.string().describe("Free-text search query"),
        page_size: z.number().min(1).max(100).default(10),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          title: z.string(),
          url: z.string(),
          object_type: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${NOTION_API}/search`, {
          method: "POST",
          headers: headers(ctx.credentials.api_key),
          body: JSON.stringify({
            query: params.query,
            page_size: params.page_size,
          }),
        });
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return (data.results ?? []).map((p: any) => {
          let title = "(untitled)";
          if (p.properties) {
            for (const prop of Object.values(p.properties) as any[]) {
              if (prop.type === "title" && Array.isArray(prop.title)) {
                const text = prop.title
                  .map((f: any) => f.plain_text)
                  .join("");
                if (text) {
                  title = text;
                  break;
                }
              }
            }
          }
          return {
            id: p.id,
            title,
            url: p.url,
            object_type: p.object,
          };
        });
      },
    },

    read_page: {
      description:
        "Fetch a Notion page's top-level block children as plain text.",
      params: z.object({
        page_id: z.string().describe("Page UUID from search results"),
      }),
      returns: z.object({
        page_id: z.string(),
        text: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.page_id}/children?page_size=100`,
          { headers: headers(ctx.credentials.api_key) },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        const lines = (data.results ?? [])
          .map((b: any) => {
            const rich = b[b.type]?.rich_text ?? [];
            return rich.map((r: any) => r.plain_text).join("");
          })
          .filter(Boolean);
        return { page_id: params.page_id, text: lines.join("\n") };
      },
    },

    create_page: {
      description: "Create a new child page under a parent page.",
      params: z.object({
        parent_id: z.string().describe("Parent page UUID"),
        title: z.string().describe("Page title"),
      }),
      returns: z.object({
        id: z.string(),
        url: z.string(),
        title: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(`${NOTION_API}/pages`, {
          method: "POST",
          headers: headers(ctx.credentials.api_key),
          body: JSON.stringify({
            parent: { page_id: params.parent_id },
            properties: {
              title: {
                title: [{ text: { content: params.title } }],
              },
            },
          }),
        });
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const page = await res.json();
        return { id: page.id, url: page.url, title: params.title };
      },
    },

    list_blocks: {
      description:
        "List all blocks on a page with their types, content, and IDs.",
      params: z.object({
        page_id: z.string().describe("Page UUID"),
        type: z
          .string()
          .optional()
          .describe(
            "Filter by block type (paragraph, heading_1, heading_2, heading_3, bulleted_list_item, numbered_list_item, to_do, toggle, quote, callout, divider, code)",
          ),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          type: z.string(),
          content: z.string(),
          checked: z.boolean().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.page_id}/children?page_size=100`,
          { headers: headers(ctx.credentials.api_key) },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        let blocks = (data.results ?? []).map((b: any) => {
          const rich = b[b.type]?.rich_text ?? [];
          const content = rich.map((r: any) => r.plain_text).join("");
          const checked =
            b.type === "to_do" ? (b.to_do?.checked ?? false) : null;
          return { id: b.id, type: b.type, content, checked };
        });
        if (params.type) {
          blocks = blocks.filter((b: any) => b.type === params.type);
        }
        return blocks;
      },
    },

    add_block: {
      description: "Append a block to a page.",
      params: z.object({
        page_id: z.string().describe("Page UUID to append to"),
        content: z.string().default("").describe("Text content of the block"),
        type: z
          .string()
          .default("paragraph")
          .describe(
            "Block type: paragraph, heading_1, heading_2, heading_3, bulleted_list_item, numbered_list_item, to_do, toggle, quote, callout, divider, code",
          ),
      }),
      returns: z.object({
        id: z.string(),
        type: z.string(),
      }),
      execute: async (params, ctx) => {
        const blockType = params.type;
        let block: any;

        if (blockType === "divider") {
          block = { object: "block", type: "divider", divider: {} };
        } else {
          block = {
            object: "block",
            type: blockType,
            [blockType]: {
              rich_text: [{ type: "text", text: { content: params.content } }],
              ...(blockType === "to_do" ? { checked: false } : {}),
            },
          };
        }

        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.page_id}/children`,
          {
            method: "PATCH",
            headers: headers(ctx.credentials.api_key),
            body: JSON.stringify({ children: [block] }),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        const created = data.results?.[0];
        return { id: created?.id ?? "", type: created?.type ?? blockType };
      },
    },

    update_block: {
      description: "Update a block's text content.",
      params: z.object({
        block_id: z.string().describe("Block UUID from list_blocks"),
        content: z.string().describe("New text content"),
      }),
      returns: z.object({
        id: z.string(),
        type: z.string(),
      }),
      execute: async (params, ctx) => {
        // First get the block to know its type
        const getRes = await ctx.fetch(
          `${NOTION_API}/blocks/${params.block_id}`,
          { headers: headers(ctx.credentials.api_key) },
        );
        if (!getRes.ok)
          throw new Error(`Notion API ${getRes.status}: ${await getRes.text()}`);
        const existing = await getRes.json();
        const blockType = existing.type;

        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.block_id}`,
          {
            method: "PATCH",
            headers: headers(ctx.credentials.api_key),
            body: JSON.stringify({
              [blockType]: {
                rich_text: [
                  { type: "text", text: { content: params.content } },
                ],
              },
            }),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const updated = await res.json();
        return { id: updated.id, type: updated.type };
      },
    },

    delete_block: {
      description: "Delete a block from a page.",
      params: z.object({
        block_id: z.string().describe("Block UUID from list_blocks"),
      }),
      returns: z.object({
        id: z.string(),
        deleted: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.block_id}`,
          {
            method: "DELETE",
            headers: headers(ctx.credentials.api_key),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        return { id: params.block_id, deleted: true };
      },
    },

    add_todo: {
      description: "Add a to-do item to a page.",
      params: z.object({
        page_id: z.string().describe("Page UUID to add the to-do to"),
        content: z.string().describe("To-do item text"),
        checked: z.boolean().default(false).describe("Initial checked state"),
      }),
      returns: z.object({
        id: z.string(),
        content: z.string(),
        checked: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.page_id}/children`,
          {
            method: "PATCH",
            headers: headers(ctx.credentials.api_key),
            body: JSON.stringify({
              children: [
                {
                  object: "block",
                  type: "to_do",
                  to_do: {
                    rich_text: [
                      { type: "text", text: { content: params.content } },
                    ],
                    checked: params.checked,
                  },
                },
              ],
            }),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        const created = data.results?.[0];
        return {
          id: created?.id ?? "",
          content: params.content,
          checked: params.checked,
        };
      },
    },

    update_todo: {
      description: "Mark a to-do item as done or not done.",
      params: z.object({
        block_id: z.string().describe("To-do block UUID from list_blocks"),
        checked: z.boolean().describe("true to mark done, false to uncheck"),
      }),
      returns: z.object({
        id: z.string(),
        checked: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${NOTION_API}/blocks/${params.block_id}`,
          {
            method: "PATCH",
            headers: headers(ctx.credentials.api_key),
            body: JSON.stringify({
              to_do: { checked: params.checked },
            }),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        return { id: params.block_id, checked: params.checked };
      },
    },

    query_database: {
      description: "Query a Notion database with optional filter and sort.",
      params: z.object({
        database_id: z.string().describe("Database UUID"),
        filter_json: z
          .string()
          .optional()
          .describe("JSON filter object (Notion API filter syntax)"),
        sort_json: z
          .string()
          .optional()
          .describe("JSON sort array (Notion API sort syntax)"),
        page_size: z.number().min(1).max(100).default(10),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          url: z.string(),
          properties: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const body: any = { page_size: params.page_size };
        if (params.filter_json) body.filter = JSON.parse(params.filter_json);
        if (params.sort_json) body.sorts = JSON.parse(params.sort_json);

        const res = await ctx.fetch(
          `${NOTION_API}/databases/${params.database_id}/query`,
          {
            method: "POST",
            headers: headers(ctx.credentials.api_key),
            body: JSON.stringify(body),
          },
        );
        if (!res.ok)
          throw new Error(`Notion API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return (data.results ?? []).map((row: any) => ({
          id: row.id,
          url: row.url,
          properties: JSON.stringify(
            Object.fromEntries(
              Object.entries(row.properties ?? {}).map(
                ([k, v]: [string, any]) => {
                  if (v.type === "title")
                    return [
                      k,
                      v.title?.map((t: any) => t.plain_text).join("") ?? "",
                    ];
                  if (v.type === "rich_text")
                    return [
                      k,
                      v.rich_text?.map((t: any) => t.plain_text).join("") ?? "",
                    ];
                  if (v.type === "number") return [k, v.number];
                  if (v.type === "checkbox") return [k, v.checkbox];
                  if (v.type === "select")
                    return [k, v.select?.name ?? null];
                  if (v.type === "multi_select")
                    return [
                      k,
                      v.multi_select?.map((s: any) => s.name) ?? [],
                    ];
                  if (v.type === "date")
                    return [k, v.date?.start ?? null];
                  if (v.type === "url") return [k, v.url];
                  return [k, `[${v.type}]`];
                },
              ),
            ),
          ),
        }));
      },
    },
  },
});
