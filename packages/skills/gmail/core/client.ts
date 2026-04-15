export const GMAIL_API = "https://gmail.googleapis.com/gmail/v1/users/me";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function authHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  };
}

export async function gFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...authHeaders(ctx.credentials.access_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Gmail API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

export async function gPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  return gFetch(ctx, url, { method, body: JSON.stringify(body) });
}

export async function gDelete(ctx: Ctx, url: string) {
  return gFetch(ctx, url, { method: "DELETE" });
}

export function buildRfc2822({
  to,
  subject,
  body,
  cc,
  bcc,
  from,
  references,
  inReplyTo,
}: {
  to: string;
  subject: string;
  body: string;
  cc?: string;
  bcc?: string;
  from?: string;
  references?: string;
  inReplyTo?: string;
}): string {
  const lines: string[] = [];
  if (from) lines.push(`From: ${from}`);
  lines.push(`To: ${to}`);
  if (cc) lines.push(`Cc: ${cc}`);
  if (bcc) lines.push(`Bcc: ${bcc}`);
  lines.push(`Subject: ${subject}`);
  if (inReplyTo) lines.push(`In-Reply-To: ${inReplyTo}`);
  if (references) lines.push(`References: ${references}`);
  lines.push("Content-Type: text/html; charset=utf-8");
  lines.push("MIME-Version: 1.0");
  lines.push("");
  lines.push(body);
  return lines.join("\r\n");
}

export function encodeMessage(raw: string): string {
  return btoa(unescape(encodeURIComponent(raw)))
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");
}

export function extractHeader(msg: any, name: string): string {
  const hdr = msg.payload?.headers?.find(
    (h: any) => h.name.toLowerCase() === name.toLowerCase(),
  );
  return hdr?.value ?? "";
}

export function extractBody(msg: any): { plain: string; html: string } {
  let plain = "";
  let html = "";
  function walk(part: any) {
    if (!part) return;
    if (part.mimeType === "text/plain" && part.body?.data) {
      plain = Buffer.from(part.body.data, "base64url").toString("utf-8");
    }
    if (part.mimeType === "text/html" && part.body?.data) {
      html = Buffer.from(part.body.data, "base64url").toString("utf-8");
    }
    if (part.parts) part.parts.forEach(walk);
  }
  walk(msg.payload);
  return { plain, html };
}

export function extractAttachments(msg: any): any[] {
  const atts: any[] = [];
  function walk(part: any) {
    if (!part) return;
    if (part.filename && part.body?.attachmentId) {
      atts.push({
        filename: part.filename,
        mime_type: part.mimeType,
        size: part.body.size,
        attachment_id: part.body.attachmentId,
      });
    }
    if (part.parts) part.parts.forEach(walk);
  }
  walk(msg.payload);
  return atts;
}
