import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

const DRIVE_API = "https://www.googleapis.com/drive/v3";
const CALENDAR_API = "https://www.googleapis.com/calendar/v3";

function authHeaders(token: string) {
  return { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    drive_search: {
      description:
        "Search Google Drive for files whose name contains the query.",
      params: z.object({
        query: z.string().describe("Search query"),
        page_size: z.number().min(1).max(100).default(10).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          mime_type: z.string(),
          web_view_link: z.string().nullable(),
          modified_time: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const token = ctx.credentials.access_token;
        const q = `name contains '${params.query.replace(/'/g, "\\'")}' and trashed = false`;
        const fields = "files(id,name,mimeType,webViewLink,modifiedTime)";
        const url = `${DRIVE_API}/files?q=${encodeURIComponent(q)}&pageSize=${params.page_size}&fields=${encodeURIComponent(fields)}`;

        const res = await ctx.fetch(url, { headers: authHeaders(token) });
        if (!res.ok)
          throw new Error(`Google Drive API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return (data.files ?? []).map((f: any) => ({
          id: f.id,
          name: f.name,
          mime_type: f.mimeType,
          web_view_link: f.webViewLink,
          modified_time: f.modifiedTime,
        }));
      },
    },

    calendar_upcoming: {
      description: "List the next N events on the primary calendar.",
      params: z.object({
        max_results: z.number().min(1).max(50).default(10).describe("Max events to return"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          summary: z.string().nullable(),
          start: z.string().nullable(),
          end: z.string().nullable(),
          html_link: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const token = ctx.credentials.access_token;
        const now = new Date().toISOString();
        const url =
          `${CALENDAR_API}/calendars/primary/events` +
          `?maxResults=${params.max_results}&orderBy=startTime&singleEvents=true&timeMin=${encodeURIComponent(now)}`;

        const res = await ctx.fetch(url, { headers: authHeaders(token) });
        if (!res.ok)
          throw new Error(`Google Calendar API ${res.status}: ${await res.text()}`);
        const data = await res.json();
        return (data.items ?? []).map((e: any) => ({
          id: e.id,
          summary: e.summary,
          start: e.start?.dateTime ?? e.start?.date ?? null,
          end: e.end?.dateTime ?? e.end?.date ?? null,
          html_link: e.htmlLink,
        }));
      },
    },
  },
});
