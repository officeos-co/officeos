export type JiraCtx = {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
};

export function jiraBase(ctx: JiraCtx) {
  return `https://${ctx.credentials.domain}.atlassian.net/rest/api/3`;
}

export function jiraAgileBase(ctx: JiraCtx) {
  return `https://${ctx.credentials.domain}.atlassian.net/rest/agile/1.0`;
}

function authHeader(ctx: JiraCtx) {
  const token = Buffer.from(`${ctx.credentials.email}:${ctx.credentials.api_token}`).toString("base64");
  return `Basic ${token}`;
}

function baseHeaders(ctx: JiraCtx) {
  return {
    Authorization: authHeader(ctx),
    Accept: "application/json",
    "Content-Type": "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function jiraGet(ctx: JiraCtx, url: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${url}${qs}`, {
    headers: baseHeaders(ctx),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Jira API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function jiraPost(ctx: JiraCtx, url: string, body: unknown) {
  const res = await ctx.fetch(url, {
    method: "POST",
    headers: baseHeaders(ctx),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Jira API ${res.status}: ${text}`);
  }
  if (res.status === 204) return { success: true };
  return res.json();
}

export async function jiraPut(ctx: JiraCtx, url: string, body: unknown) {
  const res = await ctx.fetch(url, {
    method: "PUT",
    headers: baseHeaders(ctx),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Jira API ${res.status}: ${text}`);
  }
  if (res.status === 204) return { success: true };
  return res.json();
}

export async function jiraDelete(ctx: JiraCtx, url: string) {
  const res = await ctx.fetch(url, {
    method: "DELETE",
    headers: baseHeaders(ctx),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Jira API ${res.status}: ${text}`);
  }
  return { success: true };
}

export function mapIssue(issue: Record<string, unknown>) {
  const f = (issue.fields ?? {}) as Record<string, unknown>;
  return {
    id: issue.id as string,
    key: issue.key as string,
    summary: (f.summary as string) ?? "",
    status: ((f.status as Record<string, unknown>)?.name as string) ?? "",
    assignee: ((f.assignee as Record<string, unknown>)?.displayName as string) ?? null,
    reporter: ((f.reporter as Record<string, unknown>)?.displayName as string) ?? null,
    priority: ((f.priority as Record<string, unknown>)?.name as string) ?? null,
    labels: (f.labels as string[]) ?? [],
    created: f.created as string,
    updated: f.updated as string,
  };
}
