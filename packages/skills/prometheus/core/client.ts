export function promBase(url: string): string {
  return `${url.replace(/\/$/, "")}/api/v1`;
}

export async function promGet(
  fetch: typeof globalThis.fetch,
  url: string,
  path: string,
  params?: Record<string, string | string[]>,
): Promise<any> {
  let qs = "";
  if (params && Object.keys(params).length > 0) {
    const parts: string[] = [];
    for (const [key, val] of Object.entries(params)) {
      if (Array.isArray(val)) {
        for (const v of val) parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(v)}`);
      } else {
        parts.push(`${encodeURIComponent(key)}=${encodeURIComponent(val)}`);
      }
    }
    qs = "?" + parts.join("&");
  }

  const res = await fetch(`${promBase(url)}${path}${qs}`);
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Prometheus ${res.status}: ${body}`);
  }
  const json = await res.json();
  if (json.status !== "success") {
    throw new Error(`Prometheus error: ${json.error ?? JSON.stringify(json)}`);
  }
  return json.data;
}
