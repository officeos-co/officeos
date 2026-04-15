import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, qs } from "../core/client.ts";

export const account: Record<string, ActionDefinition> = {
  get_account: {
    description: "Get account info and resource limits.",
    params: z.object({}),
    returns: z.object({
      email: z.string().describe("Account email"),
      uuid: z.string().describe("Account UUID"),
      droplet_limit: z.number().describe("Droplet limit"),
      floating_ip_limit: z.number().describe("Floating IP limit"),
      volume_limit: z.number().describe("Volume limit"),
      status: z.string().describe("Account status"),
      team: z.any().nullable().describe("Team info"),
    }),
    execute: async (_params, ctx) => {
      const data = await doGet(ctx, "/account");
      const a = data.account;
      return {
        email: a.email,
        uuid: a.uuid,
        droplet_limit: a.droplet_limit,
        floating_ip_limit: a.floating_ip_limit,
        volume_limit: a.volume_limit,
        status: a.status,
        team: a.team ?? null,
      };
    },
  },

  list_ssh_keys: {
    description: "List SSH keys.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("SSH key ID"),
      name: z.string().describe("Key name"),
      fingerprint: z.string().describe("Key fingerprint"),
      public_key: z.string().describe("Public key content"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/account/keys${qs({ per_page: params.per_page })}`);
      return (data.ssh_keys ?? []).map((k: any) => ({
        id: k.id,
        name: k.name,
        fingerprint: k.fingerprint,
        public_key: k.public_key,
      }));
    },
  },

  add_ssh_key: {
    description: "Add an SSH key to the account.",
    params: z.object({
      name: z.string().describe("Key name"),
      public_key: z.string().describe("Public key content"),
    }),
    returns: z.object({
      id: z.number().describe("SSH key ID"),
      name: z.string().describe("Key name"),
      fingerprint: z.string().describe("Key fingerprint"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, "/account/keys", { name: params.name, public_key: params.public_key });
      const k = data.ssh_key;
      return { id: k.id, name: k.name, fingerprint: k.fingerprint };
    },
  },

  list_regions: {
    description: "List available regions.",
    params: z.object({}),
    returns: z.array(z.object({
      slug: z.string().describe("Region slug"),
      name: z.string().describe("Region name"),
      available: z.boolean().describe("Whether region is available"),
      sizes: z.array(z.string()).describe("Available sizes"),
      features: z.array(z.string()).describe("Available features"),
    })),
    execute: async (_params, ctx) => {
      const data = await doGet(ctx, "/regions");
      return (data.regions ?? []).map((r: any) => ({
        slug: r.slug,
        name: r.name,
        available: r.available,
        sizes: r.sizes ?? [],
        features: r.features ?? [],
      }));
    },
  },

  list_sizes: {
    description: "List available droplet sizes.",
    params: z.object({}),
    returns: z.array(z.object({
      slug: z.string().describe("Size slug"),
      memory: z.number().describe("Memory in MB"),
      vcpus: z.number().describe("Number of vCPUs"),
      disk: z.number().describe("Disk in GB"),
      transfer: z.number().describe("Transfer in TB"),
      price_monthly: z.number().describe("Monthly price in USD"),
      price_hourly: z.number().describe("Hourly price in USD"),
      available: z.boolean().describe("Whether size is available"),
      regions: z.array(z.string()).describe("Available in regions"),
    })),
    execute: async (_params, ctx) => {
      const data = await doGet(ctx, "/sizes");
      return (data.sizes ?? []).map((s: any) => ({
        slug: s.slug,
        memory: s.memory,
        vcpus: s.vcpus,
        disk: s.disk,
        transfer: s.transfer,
        price_monthly: s.price_monthly,
        price_hourly: s.price_hourly,
        available: s.available,
        regions: s.regions ?? [],
      }));
    },
  },

  list_images: {
    description: "List images (distributions, applications, or user snapshots).",
    params: z.object({
      type: z.enum(["distribution", "application", "user"]).optional().describe("Image type filter"),
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Image ID"),
      name: z.string().describe("Image name"),
      slug: z.string().nullable().describe("Image slug"),
      distribution: z.string().describe("Distribution"),
      type: z.string().describe("Image type"),
      regions: z.array(z.string()).describe("Available regions"),
      min_disk_size: z.number().describe("Minimum disk size in GB"),
      size_gigabytes: z.number().describe("Image size in GB"),
      created_at: z.string().describe("Creation timestamp"),
      status: z.string().describe("Image status"),
    })),
    execute: async (params, ctx) => {
      const q = qs({ type: params.type, per_page: params.per_page });
      const data = await doGet(ctx, `/images${q}`);
      return (data.images ?? []).map((i: any) => ({
        id: i.id,
        name: i.name,
        slug: i.slug ?? null,
        distribution: i.distribution,
        type: i.type,
        regions: i.regions ?? [],
        min_disk_size: i.min_disk_size,
        size_gigabytes: i.size_gigabytes ?? 0,
        created_at: i.created_at,
        status: i.status,
      }));
    },
  },
};
