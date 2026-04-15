import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { awsSignedFetch, s3Fetch, s3GetObject, s3PresignUrl, xmlBlocks, xmlVal, resolveRegion } from "../core/client.ts";

export const s3: Record<string, ActionDefinition> = {
  list_buckets: {
    description: "List all S3 buckets.",
    params: z.object({
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("Bucket name"),
      creation_date: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const xml = await awsSignedFetch(ctx, { service: "s3", region, method: "GET", path: "/", host: `s3.${region}.amazonaws.com` });
      return xmlBlocks(xml, "Bucket").map((b) => ({
        name: xmlVal(b, "Name"),
        creation_date: xmlVal(b, "CreationDate"),
      }));
    },
  },

  list_objects: {
    description: "List objects in an S3 bucket.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      prefix: z.string().optional().describe("Key prefix filter"),
      max_keys: z.number().min(1).max(1000).default(1000).describe("Maximum objects to return"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      key: z.string().describe("Object key"),
      size: z.number().describe("Object size in bytes"),
      last_modified: z.string().describe("Last modified date"),
      storage_class: z.string().describe("Storage class"),
      etag: z.string().describe("ETag"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const query: Record<string, string> = { "list-type": "2", "max-keys": String(params.max_keys) };
      if (params.prefix) query["prefix"] = params.prefix;
      const xml = await s3Fetch(ctx, region, "GET", params.bucket, "", query);
      return xmlBlocks(xml, "Contents").map((b) => ({
        key: xmlVal(b, "Key"),
        size: parseInt(xmlVal(b, "Size") || "0", 10),
        last_modified: xmlVal(b, "LastModified"),
        storage_class: xmlVal(b, "StorageClass") || "STANDARD",
        etag: xmlVal(b, "ETag"),
      }));
    },
  },

  get_object: {
    description: "Get an object from S3.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      key: z.string().describe("Object key"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      content_type: z.string().describe("Content type"),
      content_length: z.number().describe("Content length in bytes"),
      last_modified: z.string().describe("Last modified date"),
      body: z.string().describe("Object content (text or base64 for binary)"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      return s3GetObject(ctx, region, params.bucket, params.key);
    },
  },

  put_object: {
    description: "Put an object into S3.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      key: z.string().describe("Object key"),
      body: z.string().describe("Object content"),
      content_type: z.string().default("application/octet-stream").describe("MIME type"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      etag: z.string().describe("ETag of stored object"),
      version_id: z.string().nullable().describe("Version ID if versioning enabled"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const host = `${params.bucket}.s3.${region}.amazonaws.com`;
      const path = "/" + params.key.split("/").map(encodeURIComponent).join("/");
      const headers = { "content-type": params.content_type };
      const res = await awsSignedFetch(ctx, { service: "s3", region, method: "PUT", path, body: params.body, host, headers });
      return { etag: typeof res === "string" ? "" : (res.ETag ?? ""), version_id: null };
    },
  },

  delete_object: {
    description: "Delete an object from S3.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      key: z.string().describe("Object key"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
      version_id: z.string().nullable().describe("Version ID if versioning enabled"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const host = `${params.bucket}.s3.${region}.amazonaws.com`;
      const path = "/" + params.key.split("/").map(encodeURIComponent).join("/");
      await awsSignedFetch(ctx, { service: "s3", region, method: "DELETE", path, host });
      return { deleted: true, version_id: null };
    },
  },

  create_bucket: {
    description: "Create a new S3 bucket.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      bucket: z.string().describe("Bucket name"),
      location: z.string().describe("Bucket location"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const host = `${params.bucket}.s3.${region}.amazonaws.com`;
      let body = "";
      if (region !== "us-east-1") {
        body = `<CreateBucketConfiguration xmlns="http://s3.amazonaws.com/doc/2006-03-01/"><LocationConstraint>${region}</LocationConstraint></CreateBucketConfiguration>`;
      }
      await awsSignedFetch(ctx, { service: "s3", region, method: "PUT", path: "/", body, host, headers: body ? { "content-type": "application/xml" } : {} });
      return { bucket: params.bucket, location: `/${params.bucket}` };
    },
  },

  delete_bucket: {
    description: "Delete an empty S3 bucket.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const host = `${params.bucket}.s3.${region}.amazonaws.com`;
      await awsSignedFetch(ctx, { service: "s3", region, method: "DELETE", path: "/", host });
      return { deleted: true };
    },
  },

  presign_url: {
    description: "Generate a pre-signed URL for an S3 object.",
    params: z.object({
      bucket: z.string().describe("Bucket name"),
      key: z.string().describe("Object key"),
      expires_in: z.number().min(1).max(604800).default(3600).describe("URL validity in seconds"),
      method: z.enum(["GET", "PUT"]).default("GET").describe("HTTP method"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      url: z.string().describe("Pre-signed URL"),
      expires_at: z.string().describe("Expiration timestamp"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      return s3PresignUrl(ctx, region, params.bucket, params.key, params.method, params.expires_in);
    },
  },
};
