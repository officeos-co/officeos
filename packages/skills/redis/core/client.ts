export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export async function redisCmd(ctx: Ctx, command: string, args: unknown[] = []) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}/command`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command, args }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Redis proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export async function redisSubscribe(ctx: Ctx, channels: string[], timeout: number) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}/subscribe`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ channels, timeout }),
  });
  if (!res.ok) throw new Error(`Redis proxy ${res.status}: ${await res.text()}`);
  return res.json();
}

export async function redisConnect(
  ctx: Ctx,
  host: string,
  port: number,
  password: string | undefined,
  db: number,
  tls: boolean,
) {
  const res = await ctx.fetch(`${ctx.credentials.proxy_url}/command`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command: "INFO", args: ["server"], host, port, password, db, tls }),
  });
  if (!res.ok) throw new Error(`Redis proxy ${res.status}: ${await res.text()}`);
  return res.json();
}
