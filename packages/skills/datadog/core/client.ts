export const DEFAULT_SITE = "datadoghq.com";

export function baseUrl(site: string): string {
  return `https://api.${site}`;
}

export function ddHeaders(apiKey: string, appKey: string): Record<string, string> {
  return {
    "DD-API-KEY": apiKey,
    "DD-APPLICATION-KEY": appKey,
    "Content-Type": "application/json",
  };
}

export async function ddGet(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  appKey: string,
  site: string,
  path: string,
  params?: Record<string, string>,
): Promise<any> {
  const qs = params && Object.keys(params).length > 0
    ? "?" + new URLSearchParams(params).toString()
    : "";
  const res = await fetch(`${baseUrl(site)}${path}${qs}`, {
    headers: ddHeaders(apiKey, appKey),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Datadog ${res.status}: ${body}`);
  }
  return res.json();
}

export async function ddPost(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  appKey: string,
  site: string,
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const res = await fetch(`${baseUrl(site)}${path}`, {
    method,
    headers: ddHeaders(apiKey, appKey),
    body: JSON.stringify(body),
  });
  if (res.status === 204) return {};
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Datadog ${res.status}: ${text}`);
  }
  return res.json();
}

export async function ddDelete(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  appKey: string,
  site: string,
  path: string,
): Promise<any> {
  const res = await fetch(`${baseUrl(site)}${path}`, {
    method: "DELETE",
    headers: ddHeaders(apiKey, appKey),
  });
  if (res.status === 204) return {};
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Datadog ${res.status}: ${text}`);
  }
  return res.json();
}
