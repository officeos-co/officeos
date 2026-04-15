import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, cfDelete, enc } from "../core/client.ts";

export const dns: Record<string, ActionDefinition> = {
  list_zones: {
    description: "List zones (domains) in the account.",
    params: z.object({
      name: z.string().optional().describe("Filter by domain name"),
      status: z.string().optional().describe("active, pending, or moved"),
      per_page: z.number().default(20).describe("Results per page (1-50)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        status: z.string(),
        name_servers: z.array(z.string()),
        plan: z.string(),
        created_on: z.string(),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ per_page: String(params.per_page) });
      if (params.name) qs.set("name", params.name);
      if (params.status) qs.set("status", params.status);
      const data = await cfFetch(ctx, `/zones?${qs}`);
      return (Array.isArray(data) ? data : []).map((z: any) => ({
        id: z.id,
        name: z.name,
        status: z.status,
        name_servers: z.name_servers ?? [],
        plan: z.plan?.name ?? "",
        created_on: z.created_on ?? "",
      }));
    },
  },

  get_zone: {
    description: "Get details about a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      status: z.string(),
      name_servers: z.array(z.string()),
      plan: z.string(),
      ssl_status: z.string(),
      created_on: z.string(),
      modified_on: z.string(),
    }),
    execute: async (params, ctx) => {
      const zone = await cfFetch(ctx, `/zones/${enc(params.zone_id)}`);
      return {
        id: zone.id,
        name: zone.name,
        status: zone.status,
        name_servers: zone.name_servers ?? [],
        plan: zone.plan?.name ?? "",
        ssl_status: zone.ssl?.status ?? "",
        created_on: zone.created_on ?? "",
        modified_on: zone.modified_on ?? "",
      };
    },
  },

  list_dns_records: {
    description: "List DNS records for a zone.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      type: z.string().optional().describe("A, AAAA, CNAME, MX, TXT, NS, SRV"),
      name: z.string().optional().describe("Filter by record name"),
      per_page: z.number().default(50).describe("Results per page (1-100)"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        type: z.string(),
        name: z.string(),
        content: z.string(),
        proxied: z.boolean(),
        ttl: z.number(),
        priority: z.number().optional(),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams({ per_page: String(params.per_page) });
      if (params.type) qs.set("type", params.type);
      if (params.name) qs.set("name", params.name);
      const data = await cfFetch(ctx, `/zones/${enc(params.zone_id)}/dns_records?${qs}`);
      return (Array.isArray(data) ? data : []).map((r: any) => ({
        id: r.id,
        type: r.type,
        name: r.name,
        content: r.content,
        proxied: r.proxied ?? false,
        ttl: r.ttl,
        priority: r.priority,
      }));
    },
  },

  create_dns_record: {
    description: "Create a DNS record.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      type: z.string().describe("Record type (A, AAAA, CNAME, MX, TXT, etc.)"),
      name: z.string().describe("Record name (e.g. api.example.com)"),
      content: z.string().describe("Record value (IP, hostname, text)"),
      proxied: z.boolean().default(false).describe("Route through Cloudflare proxy"),
      ttl: z.number().default(1).describe("TTL in seconds (1 = auto)"),
      priority: z.number().optional().describe("Priority (required for MX, SRV)"),
    }),
    returns: z.object({
      id: z.string(),
      type: z.string(),
      name: z.string(),
      content: z.string(),
      proxied: z.boolean(),
      ttl: z.number(),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        type: params.type,
        name: params.name,
        content: params.content,
        proxied: params.proxied,
        ttl: params.ttl,
      };
      if (params.priority !== undefined) body.priority = params.priority;
      const r = await cfPost(ctx, `/zones/${enc(params.zone_id)}/dns_records`, body);
      return { id: r.id, type: r.type, name: r.name, content: r.content, proxied: r.proxied, ttl: r.ttl };
    },
  },

  update_dns_record: {
    description: "Update a DNS record.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      record_id: z.string().describe("DNS record ID"),
      type: z.string().optional().describe("Record type"),
      name: z.string().optional().describe("Record name"),
      content: z.string().optional().describe("Record value"),
      proxied: z.boolean().optional().describe("Route through Cloudflare proxy"),
      ttl: z.number().optional().describe("TTL in seconds"),
    }),
    returns: z.object({
      id: z.string(),
      type: z.string(),
      name: z.string(),
      content: z.string(),
      proxied: z.boolean(),
      ttl: z.number(),
    }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.type !== undefined) body.type = params.type;
      if (params.name !== undefined) body.name = params.name;
      if (params.content !== undefined) body.content = params.content;
      if (params.proxied !== undefined) body.proxied = params.proxied;
      if (params.ttl !== undefined) body.ttl = params.ttl;
      const r = await cfPost(
        ctx,
        `/zones/${enc(params.zone_id)}/dns_records/${enc(params.record_id)}`,
        body,
        "PATCH",
      );
      return { id: r.id, type: r.type, name: r.name, content: r.content, proxied: r.proxied, ttl: r.ttl };
    },
  },

  delete_dns_record: {
    description: "Delete a DNS record.",
    params: z.object({
      zone_id: z.string().describe("Zone ID"),
      record_id: z.string().describe("DNS record ID"),
    }),
    returns: z.object({ id: z.string() }),
    execute: async (params, ctx) => {
      const r = await cfDelete(ctx, `/zones/${enc(params.zone_id)}/dns_records/${enc(params.record_id)}`);
      return { id: r?.id ?? params.record_id };
    },
  },
};
