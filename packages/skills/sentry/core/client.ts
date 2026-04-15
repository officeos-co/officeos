export const SENTRY_API = "https://sentry.io/api/0";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export async function sentryFetch(ctx: Ctx, path: string, init?: RequestInit) {
  const res = await ctx.fetch(`${SENTRY_API}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${ctx.credentials.auth_token}`,
      "Content-Type": "application/json",
      ...init?.headers,
    },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Sentry API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

export async function sentryPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  return sentryFetch(ctx, path, { method, body: JSON.stringify(body) });
}

export async function sentryDelete(ctx: Ctx, path: string) {
  return sentryFetch(ctx, path, { method: "DELETE" });
}

export function enc(s: string) {
  return encodeURIComponent(s);
}
