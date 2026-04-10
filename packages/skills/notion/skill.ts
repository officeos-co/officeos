import { defineSkill, z } from "@eaos/skill-sdk";

export default defineSkill({
  name: "notion",
  description: "Search and read Notion pages and databases via the Notion REST API.",

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
