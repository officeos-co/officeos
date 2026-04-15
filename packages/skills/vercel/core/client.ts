export const VERCEL_API = "https://api.vercel.com";

export type Ctx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

export function vercelHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export function appendTeam(url: string, ctx: Ctx): string {
  const teamId = ctx.credentials.team_id;
  if (!teamId) return url;
  const tq = `teamId=${encodeURIComponent(teamId)}`;
  return url.includes("?") ? `${url}&${tq}` : `${url}?${tq}`;
}

export async function vFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(appendTeam(url, ctx), {
    ...init,
    headers: { ...vercelHeaders(ctx.credentials.token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Vercel API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

export async function vPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  return vFetch(ctx, url, { method, body: JSON.stringify(body) });
}

export async function vDelete(ctx: Ctx, url: string) {
  return vFetch(ctx, url, { method: "DELETE" });
}

export async function vPatch(ctx: Ctx, url: string, body: unknown) {
  return vFetch(ctx, url, { method: "PATCH", body: JSON.stringify(body) });
}

export function enc(s: string) {
  return encodeURIComponent(s);
}
