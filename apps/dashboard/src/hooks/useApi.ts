import { isMockEnabled, mockApiFetch } from "@/lib/mock-data";

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  if (isMockEnabled()) {
    // Simulate network delay
    await new Promise((r) => setTimeout(r, 150 + Math.random() * 200));
    const result = mockApiFetch<T>(path, init);
    if (result !== null) return result;
    // Fall through to real fetch for unmatched routes
  }

  const res = await fetch(path, {
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
  if (res.status === 204) {
    return undefined as T;
  }
  return (await res.json()) as T;
}
