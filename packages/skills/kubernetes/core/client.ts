export type K8sCtx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function k8sUrl(apiUrl: string, path: string): string {
  return `${apiUrl.replace(/\/$/, "")}${path}`;
}

export function k8sHeaders(token: string): Record<string, string> {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
    "Content-Type": "application/json",
  };
}

export function enc(s: string): string {
  return encodeURIComponent(s);
}

export function ns(namespace?: string): string {
  return namespace ?? "default";
}

export async function k8sFetch(ctx: K8sCtx, path: string, init?: RequestInit): Promise<any> {
  const res = await ctx.fetch(k8sUrl(ctx.credentials.api_url, path), {
    ...init,
    headers: { ...k8sHeaders(ctx.credentials.token), ...init?.headers },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`K8s API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function k8sPost(ctx: K8sCtx, path: string, body: unknown, method = "POST"): Promise<any> {
  const res = await ctx.fetch(k8sUrl(ctx.credentials.api_url, path), {
    method,
    headers: k8sHeaders(ctx.credentials.token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`K8s API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function k8sDelete(ctx: K8sCtx, path: string): Promise<any> {
  const res = await ctx.fetch(k8sUrl(ctx.credentials.api_url, path), {
    method: "DELETE",
    headers: k8sHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`K8s API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function k8sText(ctx: K8sCtx, path: string): Promise<string> {
  const res = await ctx.fetch(k8sUrl(ctx.credentials.api_url, path), {
    headers: k8sHeaders(ctx.credentials.token),
  });
  if (!res.ok) throw new Error(`K8s API ${res.status}: ${await res.text()}`);
  return res.text();
}
