export type TrelloCtx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

export const TRELLO_BASE = "https://api.trello.com/1";

function authParams(ctx: TrelloCtx) {
  return {
    key: ctx.credentials.api_key,
    token: ctx.credentials.token,
  };
}

function buildUrl(ctx: TrelloCtx, path: string, params?: Record<string, string>) {
  const all = { ...authParams(ctx), ...params };
  return `${TRELLO_BASE}${path}?${new URLSearchParams(all).toString()}`;
}

function baseHeaders() {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function trelloGet(ctx: TrelloCtx, path: string, params?: Record<string, string>) {
  const res = await ctx.fetch(buildUrl(ctx, path, params), {
    headers: baseHeaders(),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Trello API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function trelloPost(ctx: TrelloCtx, path: string, body?: Record<string, unknown>) {
  const res = await ctx.fetch(buildUrl(ctx, path), {
    method: "POST",
    headers: baseHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Trello API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function trelloPut(ctx: TrelloCtx, path: string, body?: Record<string, unknown>) {
  const res = await ctx.fetch(buildUrl(ctx, path), {
    method: "PUT",
    headers: baseHeaders(),
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Trello API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function trelloDelete(ctx: TrelloCtx, path: string) {
  const res = await ctx.fetch(buildUrl(ctx, path), {
    method: "DELETE",
    headers: baseHeaders(),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Trello API ${res.status}: ${text}`);
  }
  return { success: true };
}
