// AWS SigV4 signing and service-specific request helpers — no skill-sdk imports

export type AwsCtx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

// ── Crypto helpers ────────────────────────────────────────────────────────────

async function hmacSHA256(key: ArrayBuffer | Uint8Array, data: string): Promise<ArrayBuffer> {
  const cryptoKey = await crypto.subtle.importKey("raw", key, { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  return crypto.subtle.sign("HMAC", cryptoKey, new TextEncoder().encode(data));
}

export async function sha256(data: string): Promise<string> {
  const hash = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(data));
  return [...new Uint8Array(hash)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

async function getSignatureKey(secret: string, dateStamp: string, region: string, service: string): Promise<ArrayBuffer> {
  const kDate = await hmacSHA256(new TextEncoder().encode("AWS4" + secret), dateStamp);
  const kRegion = await hmacSHA256(kDate, region);
  const kService = await hmacSHA256(kRegion, service);
  return hmacSHA256(kService, "aws4_request");
}

export function toHex(buf: ArrayBuffer): string {
  return [...new Uint8Array(buf)].map((b) => b.toString(16).padStart(2, "0")).join("");
}

// ── Core signed fetch ─────────────────────────────────────────────────────────

export interface AwsRequestOptions {
  service: string;
  region: string;
  method: string;
  path: string;
  query?: Record<string, string>;
  headers?: Record<string, string>;
  body?: string;
  host?: string;
}

export async function awsSignedFetch(ctx: AwsCtx, opts: AwsRequestOptions): Promise<any> {
  const { service, region, method, path, query = {}, body = "" } = opts;
  const host = opts.host ?? `${service}.${region}.amazonaws.com`;
  const now = new Date();
  const amzDate = now.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
  const dateStamp = amzDate.slice(0, 8);

  const queryString = Object.keys(query)
    .sort()
    .map((k) => `${encodeURIComponent(k)}=${encodeURIComponent(query[k])}`)
    .join("&");

  const allHeaders: Record<string, string> = {
    host,
    "x-amz-date": amzDate,
    ...(opts.headers ?? {}),
  };
  if (body && !allHeaders["content-type"]) {
    allHeaders["content-type"] = "application/x-www-form-urlencoded; charset=utf-8";
  }

  const signedHeaderKeys = Object.keys(allHeaders).sort();
  const signedHeaders = signedHeaderKeys.join(";");
  const canonicalHeaders = signedHeaderKeys.map((k) => `${k}:${allHeaders[k]}\n`).join("");
  const payloadHash = await sha256(body);

  const canonicalRequest = [method, path, queryString, canonicalHeaders, signedHeaders, payloadHash].join("\n");
  const credentialScope = `${dateStamp}/${region}/${service}/aws4_request`;
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256(canonicalRequest)].join("\n");

  const signingKey = await getSignatureKey(ctx.credentials.secret_access_key, dateStamp, region, service);
  const signature = toHex(await hmacSHA256(signingKey, stringToSign));
  const authHeader = `AWS4-HMAC-SHA256 Credential=${ctx.credentials.access_key_id}/${credentialScope}, SignedHeaders=${signedHeaders}, Signature=${signature}`;

  const url = `https://${host}${path}${queryString ? "?" + queryString : ""}`;
  const fetchHeaders: Record<string, string> = { ...allHeaders, authorization: authHeader };

  const res = await ctx.fetch(url, { method, headers: fetchHeaders, body: body || undefined });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`AWS ${service} API ${res.status}: ${text}`);
  }
  const contentType = res.headers.get("content-type") ?? "";
  if (contentType.includes("json")) return res.json();
  return res.text();
}

// ── Service-specific helpers ──────────────────────────────────────────────────

export async function ec2Query(ctx: AwsCtx, region: string, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2016-11-15", ...params }).toString();
  return awsSignedFetch(ctx, { service: "ec2", region, method: "POST", path: "/", body });
}

export async function s3Fetch(ctx: AwsCtx, region: string, method: string, bucket: string, key = "", query: Record<string, string> = {}, body?: string, extraHeaders?: Record<string, string>): Promise<any> {
  const host = `${bucket}.s3.${region}.amazonaws.com`;
  const path = "/" + (key ? key.split("/").map(encodeURIComponent).join("/") : "");
  return awsSignedFetch(ctx, { service: "s3", region, method, path, query, body, host, headers: extraHeaders });
}

export async function iamQuery(ctx: AwsCtx, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2010-05-08", ...params }).toString();
  return awsSignedFetch(ctx, { service: "iam", region: "us-east-1", method: "POST", path: "/", body, host: "iam.amazonaws.com" });
}

export async function lambdaFetch(ctx: AwsCtx, region: string, method: string, path: string, body?: unknown): Promise<any> {
  const headers: Record<string, string> = {};
  let bodyStr: string | undefined;
  if (body !== undefined) {
    headers["content-type"] = "application/json";
    bodyStr = JSON.stringify(body);
  }
  return awsSignedFetch(ctx, { service: "lambda", region, method, path, body: bodyStr, headers });
}

export async function cwQuery(ctx: AwsCtx, region: string, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2010-08-01", ...params }).toString();
  return awsSignedFetch(ctx, { service: "monitoring", region, method: "POST", path: "/", body });
}

export async function cwlFetch(ctx: AwsCtx, region: string, action: string, payload: Record<string, any>): Promise<any> {
  const headers = {
    "content-type": "application/x-amz-json-1.1",
    "x-amz-target": `Logs_20140328.${action}`,
  };
  return awsSignedFetch(ctx, { service: "logs", region, method: "POST", path: "/", body: JSON.stringify(payload), headers });
}

export async function ecsFetch(ctx: AwsCtx, region: string, action: string, payload: Record<string, any>): Promise<any> {
  const headers = {
    "content-type": "application/x-amz-json-1.1",
    "x-amz-target": `AmazonEC2ContainerServiceV20141113.${action}`,
  };
  return awsSignedFetch(ctx, { service: "ecs", region, method: "POST", path: "/", body: JSON.stringify(payload), headers });
}

export async function r53Fetch(ctx: AwsCtx, method: string, path: string, body?: string): Promise<any> {
  const headers: Record<string, string> = {};
  if (body) headers["content-type"] = "application/xml";
  return awsSignedFetch(ctx, { service: "route53", region: "us-east-1", method, path, body, host: "route53.amazonaws.com", headers });
}

export async function sqsQuery(ctx: AwsCtx, region: string, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2012-11-05", ...params }).toString();
  return awsSignedFetch(ctx, { service: "sqs", region, method: "POST", path: "/", body });
}

export async function snsQuery(ctx: AwsCtx, region: string, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2010-03-31", ...params }).toString();
  return awsSignedFetch(ctx, { service: "sns", region, method: "POST", path: "/", body });
}

export async function rdsQuery(ctx: AwsCtx, region: string, params: Record<string, string>): Promise<any> {
  const body = new URLSearchParams({ Version: "2014-10-31", ...params }).toString();
  return awsSignedFetch(ctx, { service: "rds", region, method: "POST", path: "/", body });
}

// ── Pre-signed URL helpers ────────────────────────────────────────────────────

export async function s3PresignUrl(ctx: AwsCtx, region: string, bucket: string, key: string, method: "GET" | "PUT", expiresIn: number): Promise<{ url: string; expires_at: string }> {
  const host = `${bucket}.s3.${region}.amazonaws.com`;
  const path = "/" + key.split("/").map(encodeURIComponent).join("/");
  const now = new Date();
  const amzDate = now.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
  const dateStamp = amzDate.slice(0, 8);
  const credentialScope = `${dateStamp}/${region}/s3/aws4_request`;
  const credential = `${ctx.credentials.access_key_id}/${credentialScope}`;

  const queryParams: Record<string, string> = {
    "X-Amz-Algorithm": "AWS4-HMAC-SHA256",
    "X-Amz-Credential": credential,
    "X-Amz-Date": amzDate,
    "X-Amz-Expires": String(expiresIn),
    "X-Amz-SignedHeaders": "host",
  };
  const queryString = Object.keys(queryParams).sort().map((k) => `${encodeURIComponent(k)}=${encodeURIComponent(queryParams[k])}`).join("&");
  const canonicalRequest = [method, path, queryString, `host:${host}\n`, "host", "UNSIGNED-PAYLOAD"].join("\n");
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256(canonicalRequest)].join("\n");
  const signingKey = await getSignatureKey(ctx.credentials.secret_access_key, dateStamp, region, "s3");
  const signature = toHex(await hmacSHA256(signingKey, stringToSign));

  return {
    url: `https://${host}${path}?${queryString}&X-Amz-Signature=${signature}`,
    expires_at: new Date(now.getTime() + expiresIn * 1000).toISOString(),
  };
}

export async function s3GetObject(ctx: AwsCtx, region: string, bucket: string, key: string): Promise<{ content_type: string; content_length: number; last_modified: string; body: string }> {
  const host = `${bucket}.s3.${region}.amazonaws.com`;
  const path = "/" + key.split("/").map(encodeURIComponent).join("/");
  const now = new Date();
  const amzDate = now.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
  const dateStamp = amzDate.slice(0, 8);
  const allHeaders: Record<string, string> = { host, "x-amz-date": amzDate };
  const signedHeaderKeys = Object.keys(allHeaders).sort();
  const signedHeaders = signedHeaderKeys.join(";");
  const canonicalHeaders = signedHeaderKeys.map((k) => `${k}:${allHeaders[k]}\n`).join("");
  const payloadHash = await sha256("");
  const canonicalRequest = ["GET", path, "", canonicalHeaders, signedHeaders, payloadHash].join("\n");
  const credentialScope = `${dateStamp}/${region}/s3/aws4_request`;
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256(canonicalRequest)].join("\n");
  const signingKey = await getSignatureKey(ctx.credentials.secret_access_key, dateStamp, region, "s3");
  const signature = toHex(await hmacSHA256(signingKey, stringToSign));
  const authHeader = `AWS4-HMAC-SHA256 Credential=${ctx.credentials.access_key_id}/${credentialScope}, SignedHeaders=${signedHeaders}, Signature=${signature}`;

  const res = await ctx.fetch(`https://${host}${path}`, {
    method: "GET",
    headers: { ...allHeaders, authorization: authHeader },
  });
  if (!res.ok) throw new Error(`AWS S3 API ${res.status}: ${await res.text()}`);

  const ct = res.headers.get("content-type") ?? "application/octet-stream";
  const cl = parseInt(res.headers.get("content-length") ?? "0", 10);
  const lm = res.headers.get("last-modified") ?? "";
  const isText = ct.startsWith("text/") || ct.includes("json") || ct.includes("xml") || ct.includes("yaml");
  let body: string;
  if (isText) {
    body = await res.text();
  } else {
    const buf = await res.arrayBuffer();
    body = btoa(String.fromCharCode(...new Uint8Array(buf)));
  }
  return { content_type: ct, content_length: cl, last_modified: lm, body };
}

export async function lambdaInvoke(ctx: AwsCtx, region: string, functionName: string, payload: string, invocationType: string): Promise<{ status_code: number; payload: string; function_error: string | null; executed_version: string | null }> {
  const headers: Record<string, string> = {
    "content-type": "application/json",
    "x-amz-invocation-type": invocationType,
  };
  const host = `lambda.${region}.amazonaws.com`;
  const path = `/2015-03-31/functions/${encodeURIComponent(functionName)}/invocations`;

  const now = new Date();
  const amzDate = now.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
  const dateStamp = amzDate.slice(0, 8);
  const allHeaders: Record<string, string> = { host, "x-amz-date": amzDate, ...headers };
  const signedHeaderKeys = Object.keys(allHeaders).sort();
  const signedHeaders = signedHeaderKeys.join(";");
  const canonicalHeaders = signedHeaderKeys.map((k) => `${k}:${allHeaders[k]}\n`).join("");
  const payloadHash = await sha256(payload);
  const canonicalRequest = ["POST", path, "", canonicalHeaders, signedHeaders, payloadHash].join("\n");
  const credentialScope = `${dateStamp}/${region}/lambda/aws4_request`;
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256(canonicalRequest)].join("\n");
  const signingKey = await getSignatureKey(ctx.credentials.secret_access_key, dateStamp, region, "lambda");
  const signature = toHex(await hmacSHA256(signingKey, stringToSign));
  const authHeader = `AWS4-HMAC-SHA256 Credential=${ctx.credentials.access_key_id}/${credentialScope}, SignedHeaders=${signedHeaders}, Signature=${signature}`;

  const res = await ctx.fetch(`https://${host}${path}`, {
    method: "POST",
    headers: { ...allHeaders, authorization: authHeader },
    body: payload,
  });

  const responseBody = await res.text();
  return {
    status_code: res.status,
    payload: responseBody,
    function_error: res.headers.get("x-amz-function-error") ?? null,
    executed_version: res.headers.get("x-amz-executed-version") ?? null,
  };
}

// ── XML parse helpers ─────────────────────────────────────────────────────────

export function xmlVal(xml: string, tag: string): string {
  const m = xml.match(new RegExp(`<${tag}>([\\s\\S]*?)</${tag}>`));
  return m ? m[1].trim() : "";
}

export function xmlAll(xml: string, tag: string): string[] {
  const re = new RegExp(`<${tag}>([\\s\\S]*?)</${tag}>`, "g");
  const results: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(xml)) !== null) results.push(m[1]);
  return results;
}

export function xmlBlocks(xml: string, tag: string): string[] {
  const re = new RegExp(`<${tag}>[\\s\\S]*?</${tag}>`, "g");
  const results: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(xml)) !== null) results.push(m[0]);
  return results;
}

export function resolveRegion(ctx: AwsCtx, region?: string): string {
  return region ?? ctx.credentials.region ?? "us-east-1";
}
