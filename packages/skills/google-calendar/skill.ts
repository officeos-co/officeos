import { defineSkill, z } from "@harro/skill-sdk";

const CAL_API = "https://www.googleapis.com/calendar/v3";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

async function calFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...authHeaders(ctx.credentials.access_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Google Calendar API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function calPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  return calFetch(ctx, url, { method, body: JSON.stringify(body) });
}

async function calDelete(ctx: Ctx, url: string) {
  return calFetch(ctx, url, { method: "DELETE" });
}

function enc(s: string) {
  return encodeURIComponent(s);
}

import doc from "./SKILL.md";

export default defineSkill({
  name: "google-calendar",
  title: "Google Calendar",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M18.316 5.684H24v12.632h-5.684V5.684zM5.684 24h12.632v-5.684H5.684V24zM18.316 5.684V0H1.895A1.894 1.894 0 0 0 0 1.895v16.421h5.684V5.684h12.632zm-7.207 6.25v-.065c.272-.144.5-.349.687-.617s.279-.595.279-.982c0-.379-.099-.72-.3-1.025a2.05 2.05 0 0 0-.832-.714 2.703 2.703 0 0 0-1.197-.257c-.6 0-1.094.156-1.481.467-.386.311-.65.671-.793 1.078l1.085.452c.086-.249.224-.461.413-.633.189-.172.445-.257.767-.257.33 0 .602.088.816.264a.86.86 0 0 1 .322.703c0 .33-.12.589-.36.778-.24.19-.535.284-.886.284h-.567v1.085h.633c.407 0 .748.109 1.02.327.272.218.407.499.407.843 0 .336-.129.614-.387.832s-.565.327-.924.327c-.351 0-.651-.103-.897-.311-.248-.208-.422-.502-.521-.881l-1.096.452c.178.616.505 1.082.977 1.401.472.319.984.478 1.538.477a2.84 2.84 0 0 0 1.293-.291c.382-.193.684-.458.902-.794.218-.336.327-.72.327-1.149 0-.429-.115-.797-.344-1.105a2.067 2.067 0 0 0-.881-.689zm2.093-1.931l.602.913L15 10.045v5.744h1.187V8.446h-.827l-2.158 1.557zM22.105 0h-3.289v5.184H24V1.895A1.894 1.894 0 0 0 22.105 0zm-3.289 23.5l4.684-4.684h-4.684V23.5zM0 22.105C0 23.152.848 24 1.895 24h3.289v-5.184H0v3.289z\"/></svg>",
  description:
    "Create, read, update, and manage calendars, events, attendees, recurring events, and availability via the Google Calendar API v3.",
  doc,

  credentials: {
    access_token: {
      label: "OAuth2 Access Token",
      kind: "password",
      placeholder: "ya29.\u2026",
      help: "Google OAuth2 access token with Calendar scopes.",
    },
  },

  actions: {
    // ── Calendars ─────────────────────────────────────────────────────

    list_calendars: {
      description: "List calendars accessible to the authenticated user.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string().describe("Calendar ID"),
          summary: z.string().describe("Calendar name"),
          description: z.string().optional().describe("Calendar description"),
          time_zone: z.string().describe("Time zone"),
          primary: z.boolean().optional().describe("Whether this is the primary calendar"),
          access_role: z.string().describe("Access role"),
        }),
      ),
      execute: async (_params, ctx) => {
        const res = await calFetch(ctx, `${CAL_API}/users/me/calendarList`);
        return (res.items ?? []).map((c: any) => ({
          id: c.id,
          summary: c.summary ?? "",
          description: c.description,
          time_zone: c.timeZone ?? "",
          primary: c.primary,
          access_role: c.accessRole ?? "",
        }));
      },
    },

    get_calendar: {
      description: "Get details about a specific calendar.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
      }),
      returns: z.object({
        id: z.string().describe("Calendar ID"),
        summary: z.string().describe("Calendar name"),
        description: z.string().optional().describe("Calendar description"),
        time_zone: z.string().describe("Time zone"),
        location: z.string().optional().describe("Calendar location"),
      }),
      execute: async (params, ctx) => {
        const c = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}`);
        return {
          id: c.id,
          summary: c.summary ?? "",
          description: c.description,
          time_zone: c.timeZone ?? "",
          location: c.location,
        };
      },
    },

    create_calendar: {
      description: "Create a new calendar.",
      params: z.object({
        summary: z.string().describe("Calendar name"),
        time_zone: z.string().optional().describe("IANA time zone identifier"),
      }),
      returns: z.object({
        id: z.string().describe("Calendar ID"),
        summary: z.string().describe("Calendar name"),
      }),
      execute: async (params, ctx) => {
        const body: any = { summary: params.summary };
        if (params.time_zone) body.timeZone = params.time_zone;
        const res = await calPost(ctx, `${CAL_API}/calendars`, body);
        return { id: res.id, summary: res.summary };
      },
    },

    delete_calendar: {
      description: "Delete a calendar and all its events.",
      params: z.object({
        calendar_id: z.string().describe("Calendar ID to delete"),
      }),
      returns: z.object({
        status: z.string().describe("Confirmation status"),
      }),
      execute: async (params, ctx) => {
        await calDelete(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}`);
        return { status: "deleted" };
      },
    },

    // ── Events ────────────────────────────────────────────────────────

    list_events: {
      description: "List events in a calendar, optionally filtered by time range or query.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        time_min: z.string().optional().describe("Start of time range (RFC 3339)"),
        time_max: z.string().optional().describe("End of time range (RFC 3339)"),
        query: z.string().optional().describe("Free-text search across event fields"),
        max_results: z.number().min(1).max(2500).default(10).describe("Events to return"),
      }),
      returns: z.array(
        z.object({
          id: z.string().describe("Event ID"),
          summary: z.string().describe("Event title"),
          start: z.any().describe("Start time"),
          end: z.any().describe("End time"),
          location: z.string().optional().describe("Event location"),
          description: z.string().optional().describe("Event description"),
          attendees: z.array(z.any()).optional().describe("Attendees"),
          status: z.string().optional().describe("Event status"),
          html_link: z.string().describe("Web link"),
          recurring_event_id: z.string().optional().describe("Recurring event ID"),
        }),
      ),
      execute: async (params, ctx) => {
        const q = new URLSearchParams({
          maxResults: String(params.max_results),
          singleEvents: "true",
          orderBy: "startTime",
        });
        if (params.time_min) q.set("timeMin", params.time_min);
        if (params.time_max) q.set("timeMax", params.time_max);
        if (params.query) q.set("q", params.query);
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events?${q}`);
        return (res.items ?? []).map((e: any) => ({
          id: e.id,
          summary: e.summary ?? "",
          start: e.start,
          end: e.end,
          location: e.location,
          description: e.description,
          attendees: e.attendees,
          status: e.status,
          html_link: e.htmlLink ?? "",
          recurring_event_id: e.recurringEventId,
        }));
      },
    },

    get_event: {
      description: "Get full details of a single event.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        summary: z.string().describe("Event title"),
        description: z.string().optional().describe("Event description"),
        start: z.any().describe("Start time"),
        end: z.any().describe("End time"),
        location: z.string().optional().describe("Event location"),
        attendees: z.array(z.any()).optional().describe("Attendees"),
        recurrence: z.array(z.string()).optional().describe("Recurrence rules"),
        reminders: z.any().optional().describe("Reminders"),
        organizer: z.any().optional().describe("Organizer"),
        creator: z.any().optional().describe("Creator"),
        html_link: z.string().describe("Web link"),
      }),
      execute: async (params, ctx) => {
        const e = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        return {
          id: e.id,
          summary: e.summary ?? "",
          description: e.description,
          start: e.start,
          end: e.end,
          location: e.location,
          attendees: e.attendees,
          recurrence: e.recurrence,
          reminders: e.reminders,
          organizer: e.organizer,
          creator: e.creator,
          html_link: e.htmlLink ?? "",
        };
      },
    },

    create_event: {
      description: "Create a new event on a calendar.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        summary: z.string().describe("Event title"),
        start: z.string().describe("Start time (RFC 3339 or date for all-day)"),
        end: z.string().describe("End time (RFC 3339 or date for all-day)"),
        location: z.string().optional().describe("Event location"),
        description: z.string().optional().describe("Event description"),
        attendees: z.array(z.string()).optional().describe("List of attendee email addresses"),
        recurrence: z.array(z.string()).optional().describe("RRULE strings"),
        time_zone: z.string().optional().describe("IANA time zone for start/end"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        html_link: z.string().describe("Web link"),
        summary: z.string().describe("Event title"),
        start: z.any().describe("Start time"),
        end: z.any().describe("End time"),
      }),
      execute: async (params, ctx) => {
        const isAllDay = !params.start.includes("T");
        const body: any = {
          summary: params.summary,
          start: isAllDay
            ? { date: params.start }
            : { dateTime: params.start, timeZone: params.time_zone },
          end: isAllDay
            ? { date: params.end }
            : { dateTime: params.end, timeZone: params.time_zone },
        };
        if (params.location) body.location = params.location;
        if (params.description) body.description = params.description;
        if (params.attendees) body.attendees = params.attendees.map((e) => ({ email: e }));
        if (params.recurrence) body.recurrence = params.recurrence;
        const res = await calPost(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events`, body);
        return {
          id: res.id,
          html_link: res.htmlLink ?? "",
          summary: res.summary ?? "",
          start: res.start,
          end: res.end,
        };
      },
    },

    update_event: {
      description: "Update an existing event.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID to update"),
        summary: z.string().optional().describe("Updated title"),
        start: z.string().optional().describe("Updated start time"),
        end: z.string().optional().describe("Updated end time"),
        location: z.string().optional().describe("Updated location"),
        description: z.string().optional().describe("Updated description"),
        attendees: z.array(z.string()).optional().describe("Updated attendee list (replaces all)"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        html_link: z.string().describe("Web link"),
        summary: z.string().describe("Event title"),
        start: z.any().describe("Start time"),
        end: z.any().describe("End time"),
        updated: z.string().describe("Last updated timestamp"),
      }),
      execute: async (params, ctx) => {
        const existing = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        const body: any = { ...existing };
        if (params.summary !== undefined) body.summary = params.summary;
        if (params.location !== undefined) body.location = params.location;
        if (params.description !== undefined) body.description = params.description;
        if (params.start) {
          const isAllDay = !params.start.includes("T");
          body.start = isAllDay ? { date: params.start } : { dateTime: params.start };
        }
        if (params.end) {
          const isAllDay = !params.end.includes("T");
          body.end = isAllDay ? { date: params.end } : { dateTime: params.end };
        }
        if (params.attendees) body.attendees = params.attendees.map((e) => ({ email: e }));
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`, {
          method: "PUT",
          body: JSON.stringify(body),
        });
        return {
          id: res.id,
          html_link: res.htmlLink ?? "",
          summary: res.summary ?? "",
          start: res.start,
          end: res.end,
          updated: res.updated ?? "",
        };
      },
    },

    delete_event: {
      description: "Delete an event from a calendar.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID to delete"),
      }),
      returns: z.object({
        status: z.string().describe("Confirmation status"),
      }),
      execute: async (params, ctx) => {
        await calDelete(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        return { status: "deleted" };
      },
    },

    move_event: {
      description: "Move an event to a different calendar.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Source calendar ID"),
        event_id: z.string().describe("Event ID to move"),
        destination_calendar_id: z.string().describe("Target calendar ID"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        html_link: z.string().describe("Web link"),
        calendar_id: z.string().describe("New calendar ID"),
      }),
      execute: async (params, ctx) => {
        const res = await calPost(
          ctx,
          `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}/move?destination=${enc(params.destination_calendar_id)}`,
          {},
        );
        return {
          id: res.id,
          html_link: res.htmlLink ?? "",
          calendar_id: params.destination_calendar_id,
        };
      },
    },

    // ── Quick Add ─────────────────────────────────────────────────────

    quick_add: {
      description: "Create an event from a natural language description.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        text: z.string().describe("Natural language event description"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        html_link: z.string().describe("Web link"),
        summary: z.string().describe("Event title"),
        start: z.any().describe("Start time"),
        end: z.any().describe("End time"),
      }),
      execute: async (params, ctx) => {
        const res = await calPost(
          ctx,
          `${CAL_API}/calendars/${enc(params.calendar_id)}/events/quickAdd?text=${enc(params.text)}`,
          {},
        );
        return {
          id: res.id,
          html_link: res.htmlLink ?? "",
          summary: res.summary ?? "",
          start: res.start,
          end: res.end,
        };
      },
    },

    // ── Attendees ─────────────────────────────────────────────────────

    add_attendee: {
      description: "Add an attendee to an event.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID"),
        email: z.string().describe("Attendee email to add"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        attendees: z.array(z.any()).describe("Updated attendee list"),
      }),
      execute: async (params, ctx) => {
        const existing = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        const attendees = [...(existing.attendees ?? []), { email: params.email }];
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`, {
          method: "PATCH",
          body: JSON.stringify({ attendees }),
        });
        return { id: res.id, attendees: res.attendees ?? [] };
      },
    },

    remove_attendee: {
      description: "Remove an attendee from an event.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID"),
        email: z.string().describe("Attendee email to remove"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        attendees: z.array(z.any()).describe("Updated attendee list"),
      }),
      execute: async (params, ctx) => {
        const existing = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        const attendees = (existing.attendees ?? []).filter(
          (a: any) => a.email !== params.email,
        );
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`, {
          method: "PATCH",
          body: JSON.stringify({ attendees }),
        });
        return { id: res.id, attendees: res.attendees ?? [] };
      },
    },

    respond: {
      description: "Respond to an event invitation (accept, decline, or tentative).",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Event ID"),
        status: z.enum(["accepted", "declined", "tentative"]).describe("Response status"),
      }),
      returns: z.object({
        id: z.string().describe("Event ID"),
        status: z.string().describe("Response status"),
        attendees: z.array(z.any()).describe("Attendee list"),
      }),
      execute: async (params, ctx) => {
        const existing = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`);
        const attendees = (existing.attendees ?? []).map((a: any) => {
          if (a.self) return { ...a, responseStatus: params.status };
          return a;
        });
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}`, {
          method: "PATCH",
          body: JSON.stringify({ attendees }),
        });
        return { id: res.id, status: params.status, attendees: res.attendees ?? [] };
      },
    },

    // ── Recurring Events ──────────────────────────────────────────────

    list_instances: {
      description: "List individual instances of a recurring event.",
      params: z.object({
        calendar_id: z.string().default("primary").describe("Calendar ID"),
        event_id: z.string().describe("Recurring event ID"),
        time_min: z.string().optional().describe("Start of range (RFC 3339)"),
        time_max: z.string().optional().describe("End of range (RFC 3339)"),
        max_results: z.number().min(1).default(25).describe("Instances to return"),
      }),
      returns: z.array(
        z.object({
          id: z.string().describe("Instance event ID"),
          summary: z.string().describe("Event title"),
          start: z.any().describe("Start time"),
          end: z.any().describe("End time"),
          recurring_event_id: z.string().describe("Parent recurring event ID"),
        }),
      ),
      execute: async (params, ctx) => {
        const q = new URLSearchParams({ maxResults: String(params.max_results) });
        if (params.time_min) q.set("timeMin", params.time_min);
        if (params.time_max) q.set("timeMax", params.time_max);
        const res = await calFetch(ctx, `${CAL_API}/calendars/${enc(params.calendar_id)}/events/${enc(params.event_id)}/instances?${q}`);
        return (res.items ?? []).map((e: any) => ({
          id: e.id,
          summary: e.summary ?? "",
          start: e.start,
          end: e.end,
          recurring_event_id: e.recurringEventId ?? params.event_id,
        }));
      },
    },

    // ── Free/Busy ─────────────────────────────────────────────────────

    check_availability: {
      description: "Check free/busy availability for one or more calendars.",
      params: z.object({
        time_min: z.string().describe("Start of range (RFC 3339)"),
        time_max: z.string().describe("End of range (RFC 3339)"),
        calendars: z.array(z.string()).describe("List of calendar IDs to check"),
      }),
      returns: z.any().describe("Per-calendar list of busy intervals with start and end"),
      execute: async (params, ctx) => {
        const res = await calPost(ctx, `${CAL_API}/freeBusy`, {
          timeMin: params.time_min,
          timeMax: params.time_max,
          items: params.calendars.map((id) => ({ id })),
        });
        return res.calendars ?? {};
      },
    },

    // ── Settings ──────────────────────────────────────────────────────

    get_settings: {
      description: "Get calendar settings for the authenticated user.",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.string().describe("Setting ID"),
          value: z.string().describe("Setting value"),
        }),
      ),
      execute: async (_params, ctx) => {
        const res = await calFetch(ctx, `${CAL_API}/users/me/settings`);
        return (res.items ?? []).map((s: any) => ({
          id: s.id,
          value: s.value ?? "",
        }));
      },
    },
  },
});
