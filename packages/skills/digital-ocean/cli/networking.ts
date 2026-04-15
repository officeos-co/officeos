import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, qs } from "../core/client.ts";

export const networking: Record<string, ActionDefinition> = {
  list_firewalls: {
    description: "List firewalls.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Firewall ID"),
      name: z.string().describe("Firewall name"),
      status: z.string().describe("Firewall status"),
      inbound_rules: z.array(z.any()).describe("Inbound rules"),
      outbound_rules: z.array(z.any()).describe("Outbound rules"),
      droplet_ids: z.array(z.number()).describe("Associated droplet IDs"),
      tags: z.array(z.string()).describe("Tags"),
      created_at: z.string().describe("Creation timestamp"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/firewalls${qs({ per_page: params.per_page })}`);
      return (data.firewalls ?? []).map((f: any) => ({
        id: f.id,
        name: f.name,
        status: f.status,
        inbound_rules: f.inbound_rules ?? [],
        outbound_rules: f.outbound_rules ?? [],
        droplet_ids: f.droplet_ids ?? [],
        tags: f.tags ?? [],
        created_at: f.created_at,
      }));
    },
  },

  create_firewall: {
    description: "Create a firewall.",
    params: z.object({
      name: z.string().describe("Firewall name"),
      inbound_rules: z.string().describe("JSON array of inbound rules"),
      outbound_rules: z.string().describe("JSON array of outbound rules"),
      droplet_ids: z.array(z.number()).optional().describe("Droplet IDs to apply to"),
      tags: z.array(z.string()).optional().describe("Tags to apply to"),
    }),
    returns: z.object({
      id: z.string().describe("Firewall ID"),
      name: z.string().describe("Firewall name"),
      status: z.string().describe("Firewall status"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        name: params.name,
        inbound_rules: JSON.parse(params.inbound_rules),
        outbound_rules: JSON.parse(params.outbound_rules),
      };
      if (params.droplet_ids) body.droplet_ids = params.droplet_ids;
      if (params.tags) body.tags = params.tags;
      const data = await doPost(ctx, "/firewalls", body);
      const f = data.firewall;
      return { id: f.id, name: f.name, status: f.status, created_at: f.created_at };
    },
  },

  list_load_balancers: {
    description: "List load balancers.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Load balancer ID"),
      name: z.string().describe("Load balancer name"),
      ip: z.string().describe("Public IP address"),
      status: z.string().describe("Status"),
      region: z.string().describe("Region slug"),
      algorithm: z.string().describe("Balancing algorithm"),
      forwarding_rules: z.array(z.any()).describe("Forwarding rules"),
      health_check: z.any().nullable().describe("Health check config"),
      droplet_ids: z.array(z.number()).describe("Associated droplet IDs"),
      created_at: z.string().describe("Creation timestamp"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/load_balancers${qs({ per_page: params.per_page })}`);
      return (data.load_balancers ?? []).map((lb: any) => ({
        id: lb.id,
        name: lb.name,
        ip: lb.ip ?? "",
        status: lb.status,
        region: lb.region?.slug ?? "",
        algorithm: lb.algorithm ?? "round_robin",
        forwarding_rules: lb.forwarding_rules ?? [],
        health_check: lb.health_check ?? null,
        droplet_ids: lb.droplet_ids ?? [],
        created_at: lb.created_at,
      }));
    },
  },

  create_load_balancer: {
    description: "Create a load balancer.",
    params: z.object({
      name: z.string().describe("Load balancer name"),
      region: z.string().describe("Region slug"),
      forwarding_rules: z.string().describe("JSON array of forwarding rules"),
      droplet_ids: z.array(z.number()).optional().describe("Droplet IDs to balance across"),
      tag: z.string().optional().describe("Tag to auto-include droplets"),
      algorithm: z.enum(["round_robin", "least_connections"]).default("round_robin").describe("Balancing algorithm"),
      health_check: z.string().optional().describe("JSON health check config"),
      vpc_uuid: z.string().optional().describe("VPC UUID"),
    }),
    returns: z.object({
      id: z.string().describe("Load balancer ID"),
      name: z.string().describe("Load balancer name"),
      ip: z.string().describe("Public IP"),
      status: z.string().describe("Status"),
      region: z.string().describe("Region slug"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        name: params.name,
        region: params.region,
        forwarding_rules: JSON.parse(params.forwarding_rules),
        algorithm: params.algorithm,
      };
      if (params.droplet_ids) body.droplet_ids = params.droplet_ids;
      if (params.tag) body.tag = params.tag;
      if (params.health_check) body.health_check = JSON.parse(params.health_check);
      if (params.vpc_uuid) body.vpc_uuid = params.vpc_uuid;
      const data = await doPost(ctx, "/load_balancers", body);
      const lb = data.load_balancer;
      return {
        id: lb.id,
        name: lb.name,
        ip: lb.ip ?? "",
        status: lb.status,
        region: lb.region?.slug ?? params.region,
        created_at: lb.created_at,
      };
    },
  },

  list_floating_ips: {
    description: "List reserved/floating IPs.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      ip: z.string().describe("IP address"),
      region: z.string().describe("Region slug"),
      droplet: z.any().nullable().describe("Assigned droplet or null"),
      locked: z.boolean().describe("Whether IP is locked"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/floating_ips${qs({ per_page: params.per_page })}`);
      return (data.floating_ips ?? []).map((ip: any) => ({
        ip: ip.ip,
        region: ip.region?.slug ?? "",
        droplet: ip.droplet ?? null,
        locked: ip.locked ?? false,
      }));
    },
  },
};
