/**
 * Client-side helper for the per-agent pod proxy exposed by v2-backend at
 * `/api/agents/{id}/proxy/{*path}`. Every REST call the embedded zeroclaw
 * dashboard used to make against its own pod (`/api/sessions`,
 * `/api/events/history`, `/api/cron`, `/api/memory`, ...) now goes through
 * this helper so that requests are routed to the correct pod by the backend.
 */

function proxyBase(agentId: string): string {
  return `/api/agents/${agentId}/proxy`;
}

export async function agentFetch<T>(
  agentId: string,
  path: string,
  init?: RequestInit,
): Promise<T> {
  const res = await fetch(`${proxyBase(agentId)}${path}`, {
    ...init,
    headers: {
      "content-type": "application/json",
      ...(init?.headers ?? {}),
    },
  });
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`${res.status} ${res.statusText}${text ? `: ${text}` : ""}`);
  }
  if (res.status === 204) return undefined as T;
  const ct = res.headers.get("content-type") ?? "";
  if (ct.includes("application/json")) {
    return (await res.json()) as T;
  }
  return (await res.text()) as unknown as T;
}

export function agentProxyUrl(agentId: string, path: string): string {
  return `${proxyBase(agentId)}${path}`;
}

/**
 * Build the dashboard-side WebSocket URL that proxies to the pod's
 * `/ws/chat`. The v2 backend's WebSocket proxy forwards the query string
 * unchanged, so callers can pass `session_id` and other params that the
 * zeroclaw gateway expects.
 */
export function agentWsUrl(agentId: string, params: Record<string, string> = {}): string {
  if (typeof window === "undefined") return "";
  const isLocalhost =
    window.location.hostname === "localhost" || window.location.hostname === "127.0.0.1";
  const base = isLocalhost
    ? `ws://localhost:5080/api/agents/${agentId}/ws`
    : `wss://api.harrokrog.com/api/agents/${agentId}/ws`;
  const qs = new URLSearchParams(params).toString();
  return qs.length > 0 ? `${base}?${qs}` : base;
}
