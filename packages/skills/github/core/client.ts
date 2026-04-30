export const GITHUB_API = "https://api.github.com";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function ghHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export function ghJsonHeaders(token: string) {
  return {
    ...ghHeaders(token),
    "Content-Type": "application/json",
  };
}

export async function ghFetch(ctx: Ctx, url: string, init?: RequestInit) {
  const res = await ctx.fetch(url, {
    ...init,
    headers: { ...ghHeaders(ctx.credentials.access_token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`GitHub API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function ghPost(ctx: Ctx, url: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(url, {
    method,
    headers: ghJsonHeaders(ctx.credentials.access_token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitHub API ${res.status}: ${text}`);
  }
  return res.json();
}

export function enc(s: string) {
  return encodeURIComponent(s);
}

export function repoUrl(owner: string, repo: string) {
  return `${GITHUB_API}/repos/${enc(owner)}/${enc(repo)}`;
}
