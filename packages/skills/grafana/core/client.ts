export type Ctx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

export function baseUrl(ctx: Ctx): string {
  return ctx.credentials.url.replace(/\/+$/, "");
}

export function gfHeaders(ctx: Ctx): Record<string, string> {
  return {
    Authorization: `Bearer ${ctx.credentials.api_key}`,
    Accept: "application/json",
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function gfFetch(ctx: Ctx, path: string, init?: RequestInit) {
  const res = await ctx.fetch(`${baseUrl(ctx)}/api${path}`, {
    ...init,
    headers: { ...gfHeaders(ctx), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Grafana API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

export async function gfPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  return gfFetch(ctx, path, { method, body: JSON.stringify(body) });
}

export async function gfDelete(ctx: Ctx, path: string) {
  return gfFetch(ctx, path, { method: "DELETE" });
}

export function qs(params: Record<string, unknown>): string {
  const parts: string[] = [];
  for (const [k, v] of Object.entries(params)) {
    if (v !== undefined && v !== null && v !== "") {
      if (Array.isArray(v)) {
        for (const item of v) parts.push(`${k}=${encodeURIComponent(item)}`);
      } else {
        parts.push(`${k}=${encodeURIComponent(String(v))}`);
      }
    }
  }
  return parts.length ? `?${parts.join("&")}` : "";
}

export function enc(s: string) {
  return encodeURIComponent(s);
}
