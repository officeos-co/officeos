import { defineSkill, z } from "@harro/skill-sdk";

const DRIVE_API = "https://www.googleapis.com/drive/v3";
const CALENDAR_API = "https://www.googleapis.com/calendar/v3";
const TOKEN_URL = "https://oauth2.googleapis.com/token";

/**
 * Create a JWT and exchange it for a Google OAuth2 access token.
 * Mirrors the service-account flow from GoogleSkill.cs.
 */
async function getAccessToken(
  serviceAccountJson: string,
  scopes: string[],
  fetchFn: typeof globalThis.fetch,
): Promise<string> {
  const sa = JSON.parse(serviceAccountJson);
  const clientEmail = sa.client_email;
  const privateKeyPem = sa.private_key;
  if (!clientEmail || !privateKeyPem) {
    throw new Error(
      "Google skill: service_account_json missing client_email or private_key.",
    );
  }

  const now = Math.floor(Date.now() / 1000);
  const header = { alg: "RS256", typ: "JWT" };
  const claims = {
    iss: clientEmail,
    scope: scopes.join(" "),
    aud: TOKEN_URL,
    iat: now,
    exp: now + 3600,
  };

  const encode = (obj: unknown) =>
    btoa(JSON.stringify(obj))
      .replace(/\+/g, "-")
      .replace(/\//g, "_")
      .replace(/=+$/, "");

  const signingInput = `${encode(header)}.${encode(claims)}`;

  // Import the PEM private key
  const pemBody = privateKeyPem
    .replace(/-----BEGIN PRIVATE KEY-----/, "")
    .replace(/-----END PRIVATE KEY-----/, "")
    .replace(/\s/g, "");
  const binaryKey = Uint8Array.from(atob(pemBody), (c) => c.charCodeAt(0));

  const key = await crypto.subtle.importKey(
    "pkcs8",
    binaryKey,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["sign"],
  );

  const signatureBuffer = await crypto.subtle.sign(
    "RSASSA-PKCS1-v1_5",
    key,
    new TextEncoder().encode(signingInput),
  );
  const signatureB64 = btoa(
    String.fromCharCode(...new Uint8Array(signatureBuffer)),
  )
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");

  const assertion = `${signingInput}.${signatureB64}`;

  const res = await fetchFn(TOKEN_URL, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: `grant_type=${encodeURIComponent("urn:ietf:params:oauth:grant-type:jwt-bearer")}&assertion=${encodeURIComponent(assertion)}`,
  });
  if (!res.ok) {
    throw new Error(`Google token exchange ${res.status}: ${await res.text()}`);
  }
  const data = await res.json();
  if (!data.access_token) {
    throw new Error("Google token exchange: no access_token in response");
  }
  return data.access_token;
}

import doc from "./SKILL.md";

export default defineSkill({
  name: "google",
  title: "Google Workspace",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M12.48 10.92v3.28h7.84c-.24 1.84-.853 3.187-1.787 4.133-1.147 1.147-2.933 2.4-6.053 2.4-4.827 0-8.6-3.893-8.6-8.72s3.773-8.72 8.6-8.72c2.6 0 4.507 1.027 5.907 2.347l2.307-2.307C18.747 1.44 16.133 0 12.48 0 5.867 0 .307 5.387.307 12s5.56 12 12.173 12c3.573 0 6.267-1.173 8.373-3.36 2.16-2.16 2.84-5.213 2.84-7.667 0-.76-.053-1.467-.173-2.053H12.48z\"/></svg>",
  emoji: "\ud83d\udd0e",
  description:
    "Search Google Drive and list upcoming Calendar events via a service-account key.",
  doc,

  credentials: {
    service_account_json: {
      label: "Service Account JSON",
      kind: "textarea",
      placeholder: '{ "type": "service_account", "project_id": "...", "private_key": "-----BEGIN PRIVATE KEY-----..." }',
      help: "Paste the entire contents of a service-account key file. Enable the Drive and Calendar APIs in your GCP project and share the relevant Drive folders / Calendar with the service-account email.",
    },
    calendar_id: {
      label: "Calendar ID (optional)",
      kind: "text",
      required: false,
      placeholder: "primary",
      help: "Defaults to 'primary'. Use the calendar's email address for shared calendars.",
    },
  },

  actions: {
    drive_search: {
      description:
        "Search Google Drive for files whose name contains the query.",
      params: z.object({
        query: z.string().describe("Search query"),
        page_size: z.number().min(1).max(100).default(10),
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
        const token = await getAccessToken(
          ctx.credentials.service_account_json,
          ["https://www.googleapis.com/auth/drive.readonly"],
          ctx.fetch,
        );

        const q = `name contains '${params.query.replace(/'/g, "\\'")}' and trashed = false`;
        const fields = "files(id,name,mimeType,webViewLink,modifiedTime)";
        const url = `${DRIVE_API}/files?q=${encodeURIComponent(q)}&pageSize=${params.page_size}&fields=${encodeURIComponent(fields)}`;

        const res = await ctx.fetch(url, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok)
          throw new Error(
            `Google Drive API ${res.status}: ${await res.text()}`,
          );
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
      description: "List the next N events on the configured calendar.",
      params: z.object({
        max_results: z.number().min(1).max(50).default(10),
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
        const token = await getAccessToken(
          ctx.credentials.service_account_json,
          ["https://www.googleapis.com/auth/calendar.readonly"],
          ctx.fetch,
        );

        const calendarId = ctx.credentials.calendar_id || "primary";
        const now = new Date().toISOString();
        const url =
          `${CALENDAR_API}/calendars/${encodeURIComponent(calendarId)}/events` +
          `?maxResults=${params.max_results}&orderBy=startTime&singleEvents=true&timeMin=${encodeURIComponent(now)}`;

        const res = await ctx.fetch(url, {
          headers: { Authorization: `Bearer ${token}` },
        });
        if (!res.ok)
          throw new Error(
            `Google Calendar API ${res.status}: ${await res.text()}`,
          );
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
