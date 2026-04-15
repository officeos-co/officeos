export type AsanaCtx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

export const ASANA_BASE = "https://app.asana.com/api/1.0";

function baseHeaders(ctx: AsanaCtx) {
  return {
    Authorization: `Bearer ${ctx.credentials.access_token}`,
    Accept: "application/json",
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function asanaGet(ctx: AsanaCtx, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${ASANA_BASE}${path}${qs}`, {
    headers: baseHeaders(ctx),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Asana API ${res.status}: ${text}`);
  }
  const json = await res.json();
  return json.data ?? json;
}

export async function asanaPost(ctx: AsanaCtx, path: string, body: unknown) {
  const res = await ctx.fetch(`${ASANA_BASE}${path}`, {
    method: "POST",
    headers: baseHeaders(ctx),
    body: JSON.stringify({ data: body }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Asana API ${res.status}: ${text}`);
  }
  const json = await res.json();
  return json.data ?? json;
}

export async function asanaPut(ctx: AsanaCtx, path: string, body: unknown) {
  const res = await ctx.fetch(`${ASANA_BASE}${path}`, {
    method: "PUT",
    headers: baseHeaders(ctx),
    body: JSON.stringify({ data: body }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Asana API ${res.status}: ${text}`);
  }
  const json = await res.json();
  return json.data ?? json;
}

export async function asanaDelete(ctx: AsanaCtx, path: string) {
  const res = await ctx.fetch(`${ASANA_BASE}${path}`, {
    method: "DELETE",
    headers: baseHeaders(ctx),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Asana API ${res.status}: ${text}`);
  }
  return { success: true };
}

export function mapTask(t: Record<string, unknown>) {
  return {
    gid: t.gid as string,
    name: t.name as string,
    completed: (t.completed as boolean) ?? false,
    assignee: (t.assignee as Record<string, unknown>)?.name as string ?? null,
    due_on: (t.due_on as string) ?? null,
    created_at: t.created_at as string,
    modified_at: t.modified_at as string,
  };
}
