import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dFetch, dPost, dDelete, enc } from "../core/client.ts";

export const networks: Record<string, ActionDefinition> = {
  list_networks: {
    description: "List networks.",
    params: z.object({
      filter: z.string().optional().describe("Filter expression (e.g. driver=bridge)"),
    }),
    returns: z.array(
      z.object({
        id: z.string().describe("Network ID"),
        name: z.string().describe("Network name"),
        driver: z.string().describe("Network driver"),
        scope: z.string().describe("Network scope"),
        containers: z.any().describe("Connected containers"),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      if (params.filter) {
        const [k, v] = params.filter.split("=");
        qs.set("filters", JSON.stringify({ [k]: [v] }));
      }
      const data = await dFetch(ctx, `/networks?${qs}`);
      return data.map((n: any) => ({ id: n.Id, name: n.Name, driver: n.Driver, scope: n.Scope, containers: n.Containers ?? {} }));
    },
  },

  create_network: {
    description: "Create a network.",
    params: z.object({
      name: z.string().describe("Network name"),
      driver: z.string().default("bridge").describe("Network driver"),
      subnet: z.string().optional().describe("Subnet CIDR"),
      gateway: z.string().optional().describe("Gateway address"),
      internal: z.boolean().default(false).describe("Restrict external access"),
    }),
    returns: z.object({
      id: z.string().describe("Network ID"),
      name: z.string().describe("Network name"),
      driver: z.string().describe("Network driver"),
    }),
    execute: async (params, ctx) => {
      const body: any = { Name: params.name, Driver: params.driver, Internal: params.internal };
      if (params.subnet || params.gateway) {
        const ipam: any = { Config: [{}] };
        if (params.subnet) ipam.Config[0].Subnet = params.subnet;
        if (params.gateway) ipam.Config[0].Gateway = params.gateway;
        body.IPAM = ipam;
      }
      const n = await dPost(ctx, `/networks/create`, body);
      return { id: n.Id, name: params.name, driver: params.driver };
    },
  },

  rm_network: {
    description: "Remove a network.",
    params: z.object({ name: z.string().describe("Network name") }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      await dDelete(ctx, `/networks/${enc(params.name)}`);
      return { name: params.name };
    },
  },

  connect: {
    description: "Connect a container to a network.",
    params: z.object({
      network: z.string().describe("Network name or ID"),
      container_id: z.string().describe("Container ID or name"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await dPost(ctx, `/networks/${enc(params.network)}/connect`, { Container: params.container_id });
      return { status: "connected" };
    },
  },

  disconnect: {
    description: "Disconnect a container from a network.",
    params: z.object({
      network: z.string().describe("Network name or ID"),
      container_id: z.string().describe("Container ID or name"),
      force: z.boolean().default(false).describe("Force disconnect"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await dPost(ctx, `/networks/${enc(params.network)}/disconnect`, { Container: params.container_id, Force: params.force });
      return { status: "disconnected" };
    },
  },
};
