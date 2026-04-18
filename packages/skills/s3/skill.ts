import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

// ── AWS Signature Version 4 helpers ──────────────────────────────────────────

async function hmacSha256(key: ArrayBuffer | CryptoKey, data: string): Promise<ArrayBuffer> {
  const cryptoKey =
    key instanceof CryptoKey
      ? key
      : await crypto.subtle.importKey("raw", key, { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  return crypto.subtle.sign("HMAC", cryptoKey, new TextEncoder().encode(data));
}

async function sha256Hex(data: string): Promise<string> {
  const hash = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(data));
  return Array.from(new Uint8Array(hash))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

function toHex(buf: ArrayBuffer): string {
  return Array.from(new Uint8Array(buf))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

async function getSigningKey(secret: string, date: string, region: string, service: string): Promise<CryptoKey> {
  const kDate = await hmacSha256(new TextEncoder().encode(`AWS4${secret}`), date);
  const kRegion = await hmacSha256(kDate, region);
  const kService = await hmacSha256(kRegion, service);
  const kSigning = await hmacSha256(kService, "aws4_request");
  return crypto.subtle.importKey("raw", kSigning, { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
}

interface SignedRequest {
  url: string;
  headers: Record<string, string>;
}

async function signRequest(
  method: string,
  url: string,
  body: string,
  accessKey: string,
  secretKey: string,
  region: string,
  service = "s3",
  extraHeaders: Record<string, string> = {},
): Promise<SignedRequest> {
  const parsed = new URL(url);
  const now = new Date();
  const amzDate = now.toISOString().replace(/[:-]|\.\d{3}/g, "").slice(0, 15) + "Z";
  const dateStamp = amzDate.slice(0, 8);

  const bodyHash = await sha256Hex(body);
  const headers: Record<string, string> = {
    host: parsed.host,
    "x-amz-date": amzDate,
    "x-amz-content-sha256": bodyHash,
    ...extraHeaders,
  };

  const signedHeaderNames = Object.keys(headers).sort().join(";");
  const canonicalHeaders = Object.keys(headers)
    .sort()
    .map((k) => `${k}:${headers[k]}\n`)
    .join("");

  const canonicalUri = parsed.pathname || "/";
  const canonicalQuery = parsed.searchParams.toString()
    .split("&")
    .sort()
    .join("&");

  const canonicalRequest = [
    method.toUpperCase(),
    canonicalUri,
    canonicalQuery,
    canonicalHeaders,
    signedHeaderNames,
    bodyHash,
  ].join("\n");

  const credentialScope = `${dateStamp}/${region}/${service}/aws4_request`;
  const stringToSign = ["AWS4-HMAC-SHA256", amzDate, credentialScope, await sha256Hex(canonicalRequest)].join("\n");

  const signingKey = await getSigningKey(secretKey, dateStamp, region, service);
  const signature = toHex(await hmacSha256(signingKey, stringToSign));

  headers[
    "authorization"
  ] = `AWS4-HMAC-SHA256 Credential=${accessKey}/${credentialScope}, SignedHeaders=${signedHeaderNames}, Signature=${signature}`;

  return { url, headers };
}

// ── S3 fetch helpers ──────────────────────────────────────────────────────────

interface S3Ctx {
  fetch: typeof globalThis.fetch;
  credentials: Record<string, string>;
}

function s3Url(region: string, bucket?: string, key?: string, query?: Record<string, string>): string {
  const base =
    region === "us-east-1"
      ? `https://s3.amazonaws.com`
      : `https://s3.${region}.amazonaws.com`;
  let path = bucket ? `/${bucket}` : "/";
  if (key) path += `/${key.startsWith("/") ? key.slice(1) : key}`;
  const url = new URL(base + path);
  if (query) Object.entries(query).forEach(([k, v]) => url.searchParams.set(k, v));
  return url.toString();
}

async function parseS3Error(res: Response): Promise<Error> {
  const text = await res.text();
  const codeMatch = text.match(/<Code>([^<]+)<\/Code>/);
  const msgMatch = text.match(/<Message>([^<]+)<\/Message>/);
  return new Error(`S3 ${res.status} ${codeMatch?.[1] ?? ""}: ${msgMatch?.[1] ?? text}`);
}

async function s3Fetch(
  ctx: S3Ctx,
  method: string,
  bucket?: string,
  key?: string,
  body = "",
  query?: Record<string, string>,
  extraHeaders: Record<string, string> = {},
): Promise<Response> {
  const { access_key_id, secret_access_key, region } = ctx.credentials;
  const url = s3Url(region, bucket, key, query);
  const { headers } = await signRequest(method, url, body, access_key_id, secret_access_key, region, "s3", extraHeaders);
  const res = await ctx.fetch(url, {
    method,
    headers,
    body: body || undefined,
  });
  return res;
}

function parseXml(xml: string, tag: string): string[] {
  const re = new RegExp(`<${tag}>([^<]*)</${tag}>`, "g");
  const results: string[] = [];
  let m: RegExpExecArray | null;
  while ((m = re.exec(xml)) !== null) results.push(m[1]);
  return results;
}

// ── Skill definition ──────────────────────────────────────────────────────────

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Buckets ───────────────────────────────────────────────────────

    buckets_list: {
      description: "List all S3 buckets owned by the authenticated user.",
      params: z.object({}),
      returns: z.array(z.object({ name: z.string(), creationDate: z.string() })),
      execute: async (_params, ctx) => {
        const res = await s3Fetch(ctx, "GET");
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        const names = parseXml(xml, "Name");
        const dates = parseXml(xml, "CreationDate");
        return names.map((name, i) => ({ name, creationDate: dates[i] ?? "" }));
      },
    },

    buckets_create: {
      description: "Create a new S3 bucket.",
      params: z.object({
        bucket: z.string().describe("Bucket name (globally unique, 3-63 chars, lowercase)"),
        region: z.string().optional().describe("Region override (default: credential region)"),
      }),
      returns: z.object({ bucket: z.string(), location: z.string() }),
      execute: async (params, ctx) => {
        const region = params.region ?? ctx.credentials.region;
        const body =
          region === "us-east-1"
            ? ""
            : `<CreateBucketConfiguration><LocationConstraint>${region}</LocationConstraint></CreateBucketConfiguration>`;
        const { access_key_id, secret_access_key } = ctx.credentials;
        const url = s3Url(region, params.bucket);
        const extraHeaders = body ? { "content-type": "application/xml" } : {};
        const { headers } = await signRequest("PUT", url, body, access_key_id, secret_access_key, region, "s3", extraHeaders);
        const res = await ctx.fetch(url, { method: "PUT", headers, body: body || undefined });
        if (!res.ok) throw await parseS3Error(res);
        const location = res.headers.get("location") ?? `/${params.bucket}`;
        return { bucket: params.bucket, location };
      },
    },

    buckets_delete: {
      description: "Delete an empty S3 bucket.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "DELETE", params.bucket);
        if (!res.ok) throw await parseS3Error(res);
        return { success: true };
      },
    },

    buckets_head: {
      description: "Check if a bucket exists and you have permission to access it.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
      }),
      returns: z.object({ exists: z.boolean(), region: z.string() }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "HEAD", params.bucket);
        if (res.status === 404) return { exists: false, region: "" };
        if (!res.ok) throw await parseS3Error(res);
        return {
          exists: true,
          region: res.headers.get("x-amz-bucket-region") ?? ctx.credentials.region,
        };
      },
    },

    // ── Objects ───────────────────────────────────────────────────────

    objects_list: {
      description: "List objects in a bucket with optional prefix and pagination.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        prefix: z.string().optional().describe("Key prefix filter"),
        max_keys: z.number().int().min(1).max(1000).default(1000).describe("Maximum objects per response"),
        continuation_token: z.string().optional().describe("Token from a previous truncated response"),
      }),
      returns: z.object({
        objects: z.array(
          z.object({ key: z.string(), size: z.number(), lastModified: z.string(), etag: z.string() }),
        ),
        next_continuation_token: z.string().nullable(),
        is_truncated: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const query: Record<string, string> = {
          "list-type": "2",
          "max-keys": String(params.max_keys),
        };
        if (params.prefix) query.prefix = params.prefix;
        if (params.continuation_token) query["continuation-token"] = params.continuation_token;
        const res = await s3Fetch(ctx, "GET", params.bucket, undefined, "", query);
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        const keys = parseXml(xml, "Key");
        const sizes = parseXml(xml, "Size");
        const dates = parseXml(xml, "LastModified");
        const etags = parseXml(xml, "ETag");
        const truncated = parseXml(xml, "IsTruncated")[0] === "true";
        const nextToken = parseXml(xml, "NextContinuationToken")[0] ?? null;
        return {
          objects: keys.map((key, i) => ({
            key,
            size: parseInt(sizes[i] ?? "0", 10),
            lastModified: dates[i] ?? "",
            etag: (etags[i] ?? "").replace(/&quot;/g, '"'),
          })),
          next_continuation_token: nextToken,
          is_truncated: truncated,
        };
      },
    },

    objects_get: {
      description: "Download an S3 object and return its content.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        encoding: z.enum(["text", "base64"]).default("text").describe("Return content as UTF-8 text or base64"),
      }),
      returns: z.object({
        content: z.string(),
        content_type: z.string(),
        content_length: z.number(),
        etag: z.string(),
      }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "GET", params.bucket, params.key);
        if (!res.ok) throw await parseS3Error(res);
        const buf = await res.arrayBuffer();
        const content =
          params.encoding === "base64"
            ? btoa(String.fromCharCode(...new Uint8Array(buf)))
            : new TextDecoder().decode(buf);
        return {
          content,
          content_type: res.headers.get("content-type") ?? "application/octet-stream",
          content_length: buf.byteLength,
          etag: (res.headers.get("etag") ?? "").replace(/"/g, ""),
        };
      },
    },

    objects_put: {
      description: "Upload content as an S3 object.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key (path within bucket)"),
        body: z.string().describe("Object content (text or base64 string)"),
        content_type: z.string().default("application/octet-stream").describe("MIME type of the object"),
        encoding: z.enum(["text", "base64"]).default("text").describe("Encoding of the body param"),
      }),
      returns: z.object({ etag: z.string() }),
      execute: async (params, ctx) => {
        const bodyStr =
          params.encoding === "base64"
            ? atob(params.body)
            : params.body;
        const res = await s3Fetch(ctx, "PUT", params.bucket, params.key, bodyStr, undefined, {
          "content-type": params.content_type,
        });
        if (!res.ok) throw await parseS3Error(res);
        return { etag: (res.headers.get("etag") ?? "").replace(/"/g, "") };
      },
    },

    objects_delete: {
      description: "Delete a single S3 object.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "DELETE", params.bucket, params.key);
        if (!res.ok) throw await parseS3Error(res);
        return { success: true };
      },
    },

    objects_delete_batch: {
      description: "Delete up to 1000 S3 objects in a single request.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        keys: z.array(z.string()).min(1).max(1000).describe("Array of object keys to delete"),
      }),
      returns: z.object({
        deleted: z.array(z.string()),
        errors: z.array(z.object({ key: z.string(), code: z.string(), message: z.string() })),
      }),
      execute: async (params, ctx) => {
        const body =
          `<?xml version="1.0" encoding="UTF-8"?><Delete><Quiet>false</Quiet>${params.keys
            .map((k) => `<Object><Key>${k}</Key></Object>`)
            .join("")}</Delete>`;
        const res = await s3Fetch(ctx, "POST", params.bucket, undefined, body, { delete: "" }, {
          "content-type": "application/xml",
          "content-md5": btoa(String.fromCharCode(...new Uint8Array(await crypto.subtle.digest("MD5", new TextEncoder().encode(body))))),
        });
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        const deleted = parseXml(xml, "Key");
        const errKeys = parseXml(xml, "Key");
        const errCodes = parseXml(xml, "Code");
        const errMsgs = parseXml(xml, "Message");
        return {
          deleted,
          errors: errKeys.map((key, i) => ({
            key,
            code: errCodes[i] ?? "",
            message: errMsgs[i] ?? "",
          })),
        };
      },
    },

    objects_head: {
      description: "Get metadata for an S3 object without downloading the body.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
      }),
      returns: z.object({
        content_type: z.string(),
        content_length: z.number(),
        etag: z.string(),
        last_modified: z.string(),
        metadata: z.record(z.string()),
      }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "HEAD", params.bucket, params.key);
        if (!res.ok) throw await parseS3Error(res);
        const metadata: Record<string, string> = {};
        res.headers.forEach((value, key) => {
          if (key.startsWith("x-amz-meta-")) {
            metadata[key.slice("x-amz-meta-".length)] = value;
          }
        });
        return {
          content_type: res.headers.get("content-type") ?? "",
          content_length: parseInt(res.headers.get("content-length") ?? "0", 10),
          etag: (res.headers.get("etag") ?? "").replace(/"/g, ""),
          last_modified: res.headers.get("last-modified") ?? "",
          metadata,
        };
      },
    },

    objects_copy: {
      description: "Server-side copy an S3 object within or between buckets.",
      params: z.object({
        source_bucket: z.string().describe("Source bucket name"),
        source_key: z.string().describe("Source object key"),
        dest_bucket: z.string().describe("Destination bucket name"),
        dest_key: z.string().describe("Destination object key"),
      }),
      returns: z.object({ etag: z.string(), last_modified: z.string() }),
      execute: async (params, ctx) => {
        const copySource = `/${params.source_bucket}/${params.source_key}`;
        const res = await s3Fetch(ctx, "PUT", params.dest_bucket, params.dest_key, "", undefined, {
          "x-amz-copy-source": copySource,
        });
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        return {
          etag: (parseXml(xml, "ETag")[0] ?? "").replace(/&quot;/g, "").replace(/"/g, ""),
          last_modified: parseXml(xml, "LastModified")[0] ?? "",
        };
      },
    },

    // ── Presigned URLs ────────────────────────────────────────────────

    presign_get: {
      description: "Generate a presigned GET URL for temporary object access.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        expires_in: z.number().int().min(1).max(604800).default(3600).describe("URL validity in seconds (max 7 days)"),
      }),
      returns: z.object({ url: z.string(), expires_at: z.string() }),
      execute: async (params, ctx) => {
        const { access_key_id, secret_access_key, region } = ctx.credentials;
        const now = new Date();
        const amzDate = now.toISOString().replace(/[:-]|\.\d{3}/g, "").slice(0, 15) + "Z";
        const dateStamp = amzDate.slice(0, 8);
        const credentialScope = `${dateStamp}/${region}/s3/aws4_request`;
        const baseUrl = s3Url(region, params.bucket, params.key);
        const parsed = new URL(baseUrl);
        parsed.searchParams.set("X-Amz-Algorithm", "AWS4-HMAC-SHA256");
        parsed.searchParams.set("X-Amz-Credential", `${access_key_id}/${credentialScope}`);
        parsed.searchParams.set("X-Amz-Date", amzDate);
        parsed.searchParams.set("X-Amz-Expires", String(params.expires_in));
        parsed.searchParams.set("X-Amz-SignedHeaders", "host");
        const canonicalRequest = [
          "GET",
          parsed.pathname,
          parsed.searchParams.toString().split("&").sort().join("&"),
          `host:${parsed.host}\n`,
          "host",
          "UNSIGNED-PAYLOAD",
        ].join("\n");
        const stringToSign = [
          "AWS4-HMAC-SHA256",
          amzDate,
          credentialScope,
          await sha256Hex(canonicalRequest),
        ].join("\n");
        const signingKey = await getSigningKey(secret_access_key, dateStamp, region, "s3");
        const signature = toHex(await hmacSha256(signingKey, stringToSign));
        parsed.searchParams.set("X-Amz-Signature", signature);
        const expiresAt = new Date(now.getTime() + params.expires_in * 1000).toISOString();
        return { url: parsed.toString(), expires_at: expiresAt };
      },
    },

    presign_put: {
      description: "Generate a presigned PUT URL for direct client uploads.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        content_type: z.string().describe("Expected content type of the upload"),
        expires_in: z.number().int().min(1).max(604800).default(3600).describe("URL validity in seconds"),
      }),
      returns: z.object({ url: z.string(), expires_at: z.string() }),
      execute: async (params, ctx) => {
        const { access_key_id, secret_access_key, region } = ctx.credentials;
        const now = new Date();
        const amzDate = now.toISOString().replace(/[:-]|\.\d{3}/g, "").slice(0, 15) + "Z";
        const dateStamp = amzDate.slice(0, 8);
        const credentialScope = `${dateStamp}/${region}/s3/aws4_request`;
        const baseUrl = s3Url(region, params.bucket, params.key);
        const parsed = new URL(baseUrl);
        parsed.searchParams.set("X-Amz-Algorithm", "AWS4-HMAC-SHA256");
        parsed.searchParams.set("X-Amz-Credential", `${access_key_id}/${credentialScope}`);
        parsed.searchParams.set("X-Amz-Date", amzDate);
        parsed.searchParams.set("X-Amz-Expires", String(params.expires_in));
        parsed.searchParams.set("X-Amz-SignedHeaders", "content-type;host");
        const canonicalRequest = [
          "PUT",
          parsed.pathname,
          parsed.searchParams.toString().split("&").sort().join("&"),
          `content-type:${params.content_type}\nhost:${parsed.host}\n`,
          "content-type;host",
          "UNSIGNED-PAYLOAD",
        ].join("\n");
        const stringToSign = [
          "AWS4-HMAC-SHA256",
          amzDate,
          credentialScope,
          await sha256Hex(canonicalRequest),
        ].join("\n");
        const signingKey = await getSigningKey(secret_access_key, dateStamp, region, "s3");
        const signature = toHex(await hmacSha256(signingKey, stringToSign));
        parsed.searchParams.set("X-Amz-Signature", signature);
        const expiresAt = new Date(now.getTime() + params.expires_in * 1000).toISOString();
        return { url: parsed.toString(), expires_at: expiresAt };
      },
    },

    // ── Multipart Upload ──────────────────────────────────────────────

    multipart_create: {
      description: "Initiate a multipart upload and return an upload ID.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        content_type: z.string().optional().describe("MIME type of the final object"),
      }),
      returns: z.object({ upload_id: z.string() }),
      execute: async (params, ctx) => {
        const extraHeaders = params.content_type ? { "content-type": params.content_type } : {};
        const res = await s3Fetch(ctx, "POST", params.bucket, params.key, "", { uploads: "" }, extraHeaders);
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        const uploadId = parseXml(xml, "UploadId")[0];
        if (!uploadId) throw new Error("S3 multipart_create: no UploadId in response");
        return { upload_id: uploadId };
      },
    },

    multipart_complete: {
      description: "Complete a multipart upload by providing all part ETags.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        upload_id: z.string().describe("Upload ID returned by multipart_create"),
        parts: z
          .array(z.object({ part_number: z.number().int(), etag: z.string() }))
          .describe("Parts in ascending order by part_number"),
      }),
      returns: z.object({ location: z.string(), etag: z.string() }),
      execute: async (params, ctx) => {
        const body = `<?xml version="1.0" encoding="UTF-8"?><CompleteMultipartUpload>${params.parts
          .map((p) => `<Part><PartNumber>${p.part_number}</PartNumber><ETag>"${p.etag}"</ETag></Part>`)
          .join("")}</CompleteMultipartUpload>`;
        const res = await s3Fetch(
          ctx,
          "POST",
          params.bucket,
          params.key,
          body,
          { uploadId: params.upload_id },
          { "content-type": "application/xml" },
        );
        if (!res.ok) throw await parseS3Error(res);
        const xml = await res.text();
        return {
          location: parseXml(xml, "Location")[0] ?? "",
          etag: (parseXml(xml, "ETag")[0] ?? "").replace(/&quot;/g, "").replace(/"/g, ""),
        };
      },
    },

    multipart_abort: {
      description: "Abort a multipart upload and remove uploaded parts.",
      params: z.object({
        bucket: z.string().describe("Bucket name"),
        key: z.string().describe("Object key"),
        upload_id: z.string().describe("Upload ID to abort"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const res = await s3Fetch(ctx, "DELETE", params.bucket, params.key, "", { uploadId: params.upload_id });
        if (!res.ok) throw await parseS3Error(res);
        return { success: true };
      },
    },
  },
});
