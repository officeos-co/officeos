export const PD_BASE = "https://api.pagerduty.com";

export function pdHeaders(apiKey: string, extra?: Record<string, string>): Record<string, string> {
  return {
    Authorization: `Token token=${apiKey}`,
    Accept: "application/vnd.pagerduty+json;version=2",
    "Content-Type": "application/json",
    From: "api@officeos.co",
    ...extra,
  };
}

export async function pdGet(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  path: string,
  params?: Record<string, string | string[]>,
): Promise<any> {
  let qs = "";
  if (params && Object.keys(params).length > 0) {
    const parts: string[] = [];
    for (const [key, val] of Object.entries(params)) {
      if (Array.isArray(val)) {
        for (const v of val) parts.push(`${encodeURIComponent(key)}[]=${encodeURIComponent(v)}`);
      } else {
        parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(val)}`);
      }
    }
    qs = "?" + parts.join("&");
  }
  const res = await fetch(`${PD_BASE}${path}${qs}`, { headers: pdHeaders(apiKey) });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`PagerDuty ${res.status}: ${body}`);
  }
  return res.json();
}

export async function pdPost(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  path: string,
  body: unknown,
  method = "POST",
): Promise<any> {
  const res = await fetch(`${PD_BASE}${path}`, {
    method,
    headers: pdHeaders(apiKey),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`PagerDuty ${res.status}: ${text}`);
  }
  return res.json();
}
