export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function sfUrl(ctx: Ctx, path: string) {
  return `${ctx.credentials.instance_url}/services/data/v59.0${path}`;
}

export function sfHeaders(ctx: Ctx): Record<string, string> {
  return {
    Authorization: `Bearer ${ctx.credentials.access_token}`,
    "Content-Type": "application/json",
    Accept: "application/json",
  };
}

export async function sfFetch(ctx: Ctx, path: string, init?: RequestInit) {
  const res = await ctx.fetch(sfUrl(ctx, path), {
    ...init,
    headers: { ...sfHeaders(ctx), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Salesforce API ${res.status}: ${body}`);
  }
  // DELETE returns 204 with no body
  if (res.status === 204) return {};
  return res.json();
}

export async function sfPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  return sfFetch(ctx, path, { method, body: JSON.stringify(body) });
}

export function enc(s: string) {
  return encodeURIComponent(s);
}
