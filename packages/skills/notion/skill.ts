import { defineSkill, z } from "@harro/skill-sdk";

export default defineSkill({
  name: "notion",
  description: "Search and read Notion pages and databases via the Notion REST API.",

  doc: `# Notion

This skill gives you access to search and read Notion pages and databases.

## Commands

- \`notion search --query "meeting notes" --page_size 5\` — Search for pages matching a query.
- \`notion read_page --page_id "a1b2c3d4-..."\` — Fetch a page's content as plain text.

## Workflow

1. Always start with \`notion search\` to find pages. Never guess page IDs.
2. Use the \`id\` from search results with \`notion read_page\` to get content.
3. If a search returns no results, try alternative keywords — the content may exist under a different title.
4. When answering questions, cite which page the information came from.

## Limitations

- Only pages **shared with the integration** are visible. If search returns nothing, the pages may exist but not be shared.
- Page IDs are opaque UUIDs. Never fabricate them — always discover via search.
- \`read_page\` returns top-level blocks only. Deeply nested content (toggles, synced blocks) may be truncated.
- Notion API is rate-limited. Avoid rapid repeated queries in a loop.
- This skill is **read-only**. You cannot create, update, or delete pages.`,

  credentials: {
    api_key: z.string().describe("Internal Integration Token (secret_...)"),
  },

  actions: {
    search: {
      description: "Search the connected Notion workspace for pages matching a query.",
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
        })
      ),
      execute: async (params, ctx) => {
        const res = await ctx.fetch("https://api.notion.com/v1/search", {
          method: "POST",
          headers: {
            Authorization: `Bearer ${ctx.credentials.api_key}`,
            "Notion-Version": "2022-06-28",
            "Content-Type": "application/json",
          },
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
      description: "Fetch a Notion page's top-level block children as plain text.",
      params: z.object({
        page_id: z.string().describe("Page UUID from search results"),
      }),
      returns: z.object({
        page_id: z.string(),
        text: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `https://api.notion.com/v1/blocks/${params.page_id}/children?page_size=100`,
          {
            headers: {
              Authorization: `Bearer ${ctx.credentials.api_key}`,
              "Notion-Version": "2022-06-28",
            },
          }
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
  },
});
