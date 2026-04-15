export const STRIPE_API = "https://api.stripe.com/v1";

export type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

export function stripeHeaders(key: string): Record<string, string> {
  return {
    Authorization: `Bearer ${key}`,
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

export async function stripeGet(ctx: Ctx, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${STRIPE_API}${path}${qs}`, {
    headers: stripeHeaders(ctx.credentials.secret_key),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Stripe API ${res.status}: ${body}`);
  }
  return res.json();
}

export async function stripePost(ctx: Ctx, path: string, body?: Record<string, string>, method = "POST") {
  const res = await ctx.fetch(`${STRIPE_API}${path}`, {
    method,
    headers: {
      ...stripeHeaders(ctx.credentials.secret_key),
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body: body ? new URLSearchParams(body).toString() : undefined,
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Stripe API ${res.status}: ${text}`);
  }
  return res.json();
}

export async function stripeDelete(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${STRIPE_API}${path}`, {
    method: "DELETE",
    headers: stripeHeaders(ctx.credentials.secret_key),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Stripe API ${res.status}: ${text}`);
  }
  return res.json();
}

/** Flatten nested objects into Stripe's bracket notation for form encoding */
export function flattenParams(obj: Record<string, unknown>, prefix = ""): Record<string, string> {
  const out: Record<string, string> = {};
  for (const [k, v] of Object.entries(obj)) {
    if (v === undefined || v === null) continue;
    const key = prefix ? `${prefix}[${k}]` : k;
    if (typeof v === "object" && !Array.isArray(v)) {
      Object.assign(out, flattenParams(v as Record<string, unknown>, key));
    } else if (Array.isArray(v)) {
      v.forEach((item, i) => {
        if (typeof item === "object") {
          Object.assign(out, flattenParams(item as Record<string, unknown>, `${key}[${i}]`));
        } else {
          out[`${key}[${i}]`] = String(item);
        }
      });
    } else {
      out[key] = String(v);
    }
  }
  return out;
}
