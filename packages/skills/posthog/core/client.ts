export const DEFAULT_HOST = "https://app.posthog.com";

export function phHeaders(apiKey: string): Record<string, string> {
  return {
    Authorization: `Bearer ${apiKey}`,
    "Content-Type": "application/json",
  };
}

export function projectBase(host: string, projectId: string): string {
  return `${host}/api/projects/${projectId}`;
}

export async function phGet(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  host: string,
  path: string,
  params?: Record<string, string>,
): Promise<any> {
  const qs = params && Object.keys(params).length > 0
    ? "?" + new URLSearchParams(params).toString()
    : "";
  const res = await fetch(`${host}${path}${qs}`, { headers: phHeaders(apiKey) });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`PostHog ${res.status}: ${body}`);
  }
  return res.json();
}

export async function phPost(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  host: string,
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const res = await fetch(`${host}${path}`, {
    method,
    headers: phHeaders(apiKey),
    body: JSON.stringify(body),
  });
  if (res.status === 204) return {};
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`PostHog ${res.status}: ${text}`);
  }
  return res.json();
}

export async function phDelete(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  host: string,
  path: string,
  body?: unknown,
): Promise<any> {
  const res = await fetch(`${host}${path}`, {
    method: "DELETE",
    headers: phHeaders(apiKey),
    body: body ? JSON.stringify(body) : undefined,
  });
  if (res.status === 204) return {};
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`PostHog ${res.status}: ${text}`);
  }
  return res.json();
}
