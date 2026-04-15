export const NERDGRAPH_URL = "https://api.newrelic.com/graphql";
export const REST_BASE = "https://api.newrelic.com/v2";

export function nrHeaders(apiKey: string): Record<string, string> {
  return {
    "Api-Key": apiKey,
    "Content-Type": "application/json",
  };
}

export async function nerdgraph(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  query: string,
  variables?: Record<string, any>,
): Promise<any> {
  const res = await fetch(NERDGRAPH_URL, {
    method: "POST",
    headers: nrHeaders(apiKey),
    body: JSON.stringify({ query, variables: variables ?? {} }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`New Relic NerdGraph ${res.status}: ${body}`);
  }
  const json = await res.json();
  if (json.errors?.length) {
    throw new Error(`NerdGraph errors: ${json.errors.map((e: any) => e.message).join("; ")}`);
  }
  return json.data;
}

export async function nrRestGet(
  fetch: typeof globalThis.fetch,
  apiKey: string,
  path: string,
  params?: Record<string, string>,
): Promise<any> {
  const qs = params && Object.keys(params).length > 0
    ? "?" + new URLSearchParams(params).toString()
    : "";
  const res = await fetch(`${REST_BASE}${path}${qs}`, { headers: nrHeaders(apiKey) });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`New Relic REST ${res.status}: ${body}`);
  }
  return res.json();
}
