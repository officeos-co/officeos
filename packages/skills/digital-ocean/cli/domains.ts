import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doPut, doDelete, qs } from "../core/client.ts";

export const domains: Record<string, ActionDefinition> = {
  list_domains: {
    description: "List domains.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      name: z.string().describe("Domain name"),
      ttl: z.number().describe("Default TTL"),
      zone_file: z.string().nullable().describe("Zone file content"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/domains${qs({ per_page: params.per_page })}`);
      return (data.domains ?? []).map((d: any) => ({
        name: d.name,
        ttl: d.ttl,
        zone_file: d.zone_file ?? null,
      }));
    },
  },

  get_domain: {
    description: "Get info about a single domain.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
    }),
    returns: z.object({
      name: z.string().describe("Domain name"),
      ttl: z.number().describe("Default TTL"),
      zone_file: z.string().nullable().describe("Zone file content"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/domains/${encodeURIComponent(params.domain_name)}`);
      const d = data.domain;
      return { name: d.name, ttl: d.ttl, zone_file: d.zone_file ?? null };
    },
  },

  create_domain: {
    description: "Create a domain.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
      ip_address: z.string().optional().describe("IP for automatic A record"),
    }),
    returns: z.object({
      name: z.string().describe("Domain name"),
      ttl: z.number().describe("Default TTL"),
    }),
    execute: async (params, ctx) => {
      const body: any = { name: params.domain_name };
      if (params.ip_address) body.ip_address = params.ip_address;
      const data = await doPost(ctx, "/domains", body);
      const d = data.domain;
      return { name: d.name, ttl: d.ttl };
    },
  },

  delete_domain: {
    description: "Delete a domain.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/domains/${encodeURIComponent(params.domain_name)}`);
      return { deleted: true };
    },
  },

  list_records: {
    description: "List DNS records for a domain.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
      type: z.string().optional().describe("Filter by type: A, AAAA, CNAME, MX, TXT, NS, SRV"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Record ID"),
      type: z.string().describe("Record type"),
      name: z.string().describe("Record name"),
      data: z.string().describe("Record data"),
      priority: z.number().nullable().describe("Priority"),
      port: z.number().nullable().describe("Port"),
      ttl: z.number().describe("TTL"),
      weight: z.number().nullable().describe("Weight"),
    })),
    execute: async (params, ctx) => {
      const q = qs({ type: params.type });
      const data = await doGet(ctx, `/domains/${encodeURIComponent(params.domain_name)}/records${q}`);
      return (data.domain_records ?? []).map((r: any) => ({
        id: r.id,
        type: r.type,
        name: r.name,
        data: r.data,
        priority: r.priority ?? null,
        port: r.port ?? null,
        ttl: r.ttl,
        weight: r.weight ?? null,
      }));
    },
  },

  create_record: {
    description: "Create a DNS record.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
      type: z.string().describe("Record type (A, AAAA, CNAME, MX, TXT, NS, SRV)"),
      name: z.string().describe("Record name (e.g. api, @)"),
      data: z.string().describe("Record value"),
      ttl: z.number().default(1800).describe("TTL in seconds"),
      priority: z.number().optional().describe("Priority (MX and SRV records)"),
      port: z.number().optional().describe("Port (SRV records)"),
      weight: z.number().optional().describe("Weight (SRV records)"),
    }),
    returns: z.object({
      id: z.number().describe("Record ID"),
      type: z.string().describe("Record type"),
      name: z.string().describe("Record name"),
      data: z.string().describe("Record data"),
      ttl: z.number().describe("TTL"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        type: params.type,
        name: params.name,
        data: params.data,
        ttl: params.ttl,
      };
      if (params.priority !== undefined) body.priority = params.priority;
      if (params.port !== undefined) body.port = params.port;
      if (params.weight !== undefined) body.weight = params.weight;
      const data = await doPost(ctx, `/domains/${encodeURIComponent(params.domain_name)}/records`, body);
      const r = data.domain_record;
      return { id: r.id, type: r.type, name: r.name, data: r.data, ttl: r.ttl };
    },
  },

  update_record: {
    description: "Update a DNS record.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
      record_id: z.number().describe("Record ID"),
      name: z.string().optional().describe("Updated record name"),
      data: z.string().optional().describe("Updated record value"),
      ttl: z.number().optional().describe("Updated TTL"),
      priority: z.number().optional().describe("Updated priority"),
    }),
    returns: z.object({
      id: z.number().describe("Record ID"),
      type: z.string().describe("Record type"),
      name: z.string().describe("Record name"),
      data: z.string().describe("Record data"),
      ttl: z.number().describe("TTL"),
    }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.name !== undefined) body.name = params.name;
      if (params.data !== undefined) body.data = params.data;
      if (params.ttl !== undefined) body.ttl = params.ttl;
      if (params.priority !== undefined) body.priority = params.priority;
      const data = await doPut(ctx, `/domains/${encodeURIComponent(params.domain_name)}/records/${params.record_id}`, body);
      const r = data.domain_record;
      return { id: r.id, type: r.type, name: r.name, data: r.data, ttl: r.ttl };
    },
  },

  delete_record: {
    description: "Delete a DNS record.",
    params: z.object({
      domain_name: z.string().describe("Domain name"),
      record_id: z.number().describe("Record ID"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/domains/${encodeURIComponent(params.domain_name)}/records/${params.record_id}`);
      return { deleted: true };
    },
  },
};
