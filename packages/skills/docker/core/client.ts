export type DockerCtx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function dockerUrl(host: string, path: string): string {
  return `${host.replace(/\/$/, "")}${path}`;
}

export function hdrs(): Record<string, string> {
  return { "Content-Type": "application/json" };
}

export function enc(s: string): string {
  return encodeURIComponent(s);
}

export async function dFetch(ctx: DockerCtx, path: string, init?: RequestInit): Promise<any> {
  const res = await ctx.fetch(dockerUrl(ctx.credentials.host, path), {
    ...init,
    headers: { ...hdrs(), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Docker API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function dPost(ctx: DockerCtx, path: string, body?: unknown, method = "POST"): Promise<any> {
  const res = await ctx.fetch(dockerUrl(ctx.credentials.host, path), {
    method,
    headers: hdrs(),
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Docker API ${res.status}: ${text}`);
  }
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) return res.json();
  return { status: "ok" };
}

export async function dDelete(ctx: DockerCtx, path: string): Promise<any> {
  const res = await ctx.fetch(dockerUrl(ctx.credentials.host, path), {
    method: "DELETE",
    headers: hdrs(),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Docker API ${res.status}: ${text}`);
  }
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) return res.json();
  return { status: "ok" };
}
