import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doDelete, qs } from "../core/client.ts";

export const droplets: Record<string, ActionDefinition> = {
  list_droplets: {
    description: "List droplets, optionally filtered by tag.",
    params: z.object({
      tag: z.string().optional().describe("Filter by tag name"),
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Droplet ID"),
      name: z.string().describe("Droplet name"),
      status: z.string().describe("Droplet status"),
      region: z.string().describe("Region slug"),
      size: z.string().describe("Size slug"),
      image: z.string().describe("Image slug or name"),
      ip_address: z.string().nullable().describe("Public IPv4 address"),
      private_ip: z.string().nullable().describe("Private IPv4 address"),
      vcpus: z.number().describe("Number of vCPUs"),
      memory: z.number().describe("Memory in MB"),
      disk: z.number().describe("Disk in GB"),
      tags: z.array(z.string()).describe("Tags"),
      created_at: z.string().describe("Creation timestamp"),
    })),
    execute: async (params, ctx) => {
      const q = qs({ tag_name: params.tag, per_page: params.per_page });
      const data = await doGet(ctx, `/droplets${q}`);
      return (data.droplets ?? []).map((d: any) => ({
        id: d.id,
        name: d.name,
        status: d.status,
        region: d.region?.slug ?? "",
        size: d.size?.slug ?? d.size_slug ?? "",
        image: d.image?.slug ?? d.image?.name ?? "",
        ip_address: d.networks?.v4?.find((n: any) => n.type === "public")?.ip_address ?? null,
        private_ip: d.networks?.v4?.find((n: any) => n.type === "private")?.ip_address ?? null,
        vcpus: d.vcpus,
        memory: d.memory,
        disk: d.disk,
        tags: d.tags ?? [],
        created_at: d.created_at,
      }));
    },
  },

  get_droplet: {
    description: "Get detailed info about a single droplet.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
    }),
    returns: z.object({
      id: z.number().describe("Droplet ID"),
      name: z.string().describe("Droplet name"),
      status: z.string().describe("Droplet status"),
      region: z.string().describe("Region slug"),
      size: z.string().describe("Size slug"),
      image: z.string().describe("Image slug or name"),
      ip_address: z.string().nullable().describe("Public IPv4 address"),
      private_ip: z.string().nullable().describe("Private IPv4 address"),
      vcpus: z.number().describe("Number of vCPUs"),
      memory: z.number().describe("Memory in MB"),
      disk: z.number().describe("Disk in GB"),
      tags: z.array(z.string()).describe("Tags"),
      volumes: z.array(z.string()).describe("Attached volume IDs"),
      vpc_uuid: z.string().nullable().describe("VPC UUID"),
      created_at: z.string().describe("Creation timestamp"),
      kernel: z.any().nullable().describe("Kernel info"),
      features: z.array(z.string()).describe("Enabled features"),
      networks: z.any().describe("Network details"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/droplets/${params.droplet_id}`);
      const d = data.droplet;
      return {
        id: d.id,
        name: d.name,
        status: d.status,
        region: d.region?.slug ?? "",
        size: d.size?.slug ?? d.size_slug ?? "",
        image: d.image?.slug ?? d.image?.name ?? "",
        ip_address: d.networks?.v4?.find((n: any) => n.type === "public")?.ip_address ?? null,
        private_ip: d.networks?.v4?.find((n: any) => n.type === "private")?.ip_address ?? null,
        vcpus: d.vcpus,
        memory: d.memory,
        disk: d.disk,
        tags: d.tags ?? [],
        volumes: d.volume_ids ?? [],
        vpc_uuid: d.vpc_uuid ?? null,
        created_at: d.created_at,
        kernel: d.kernel ?? null,
        features: d.features ?? [],
        networks: d.networks,
      };
    },
  },

  create_droplet: {
    description: "Create a new droplet.",
    params: z.object({
      name: z.string().describe("Droplet name"),
      region: z.string().describe("Region slug (e.g. nyc3, sfo3)"),
      size: z.string().describe("Size slug (e.g. s-1vcpu-1gb)"),
      image: z.string().describe("Image slug or ID"),
      ssh_keys: z.array(z.number()).optional().describe("Array of SSH key IDs"),
      backups: z.boolean().default(false).describe("Enable automated backups"),
      ipv6: z.boolean().default(false).describe("Enable IPv6"),
      monitoring: z.boolean().default(false).describe("Enable monitoring agent"),
      user_data: z.string().optional().describe("Cloud-init user data script"),
      tags: z.array(z.string()).optional().describe("Tags to apply"),
      vpc_uuid: z.string().optional().describe("VPC UUID"),
      volumes: z.array(z.string()).optional().describe("Volume IDs to attach"),
    }),
    returns: z.object({
      id: z.number().describe("Droplet ID"),
      name: z.string().describe("Droplet name"),
      status: z.string().describe("Droplet status"),
      region: z.string().describe("Region slug"),
      ip_address: z.string().nullable().describe("Public IP (may be null initially)"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const body: any = {
        name: params.name,
        region: params.region,
        size: params.size,
        image: params.image,
        backups: params.backups,
        ipv6: params.ipv6,
        monitoring: params.monitoring,
      };
      if (params.ssh_keys) body.ssh_keys = params.ssh_keys;
      if (params.user_data) body.user_data = params.user_data;
      if (params.tags) body.tags = params.tags;
      if (params.vpc_uuid) body.vpc_uuid = params.vpc_uuid;
      if (params.volumes) body.volumes = params.volumes;
      const data = await doPost(ctx, "/droplets", body);
      const d = data.droplet;
      return {
        id: d.id,
        name: d.name,
        status: d.status,
        region: d.region?.slug ?? params.region,
        ip_address: d.networks?.v4?.find((n: any) => n.type === "public")?.ip_address ?? null,
        created_at: d.created_at,
      };
    },
  },

  delete_droplet: {
    description: "Delete a droplet permanently.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/droplets/${params.droplet_id}`);
      return { deleted: true };
    },
  },

  power_on: {
    description: "Power on a droplet.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
    }),
    returns: z.object({
      action_id: z.number().describe("Action ID"),
      status: z.string().describe("Action status"),
      type: z.string().describe("Action type"),
      started_at: z.string().describe("Action start time"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/droplets/${params.droplet_id}/actions`, { type: "power_on" });
      const a = data.action;
      return { action_id: a.id, status: a.status, type: a.type, started_at: a.started_at };
    },
  },

  power_off: {
    description: "Power off a droplet (hard shutdown).",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
    }),
    returns: z.object({
      action_id: z.number().describe("Action ID"),
      status: z.string().describe("Action status"),
      type: z.string().describe("Action type"),
      started_at: z.string().describe("Action start time"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/droplets/${params.droplet_id}/actions`, { type: "power_off" });
      const a = data.action;
      return { action_id: a.id, status: a.status, type: a.type, started_at: a.started_at };
    },
  },

  reboot: {
    description: "Reboot a droplet.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
    }),
    returns: z.object({
      action_id: z.number().describe("Action ID"),
      status: z.string().describe("Action status"),
      type: z.string().describe("Action type"),
      started_at: z.string().describe("Action start time"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/droplets/${params.droplet_id}/actions`, { type: "reboot" });
      const a = data.action;
      return { action_id: a.id, status: a.status, type: a.type, started_at: a.started_at };
    },
  },

  resize: {
    description: "Resize a droplet to a new size.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
      size: z.string().describe("New size slug"),
      disk: z.boolean().default(false).describe("Also resize disk (irreversible)"),
    }),
    returns: z.object({
      action_id: z.number().describe("Action ID"),
      status: z.string().describe("Action status"),
      type: z.string().describe("Action type"),
      started_at: z.string().describe("Action start time"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/droplets/${params.droplet_id}/actions`, { type: "resize", size: params.size, disk: params.disk });
      const a = data.action;
      return { action_id: a.id, status: a.status, type: a.type, started_at: a.started_at };
    },
  },

  snapshot: {
    description: "Create a snapshot of a droplet.",
    params: z.object({
      droplet_id: z.number().describe("Droplet ID"),
      name: z.string().describe("Snapshot name"),
    }),
    returns: z.object({
      action_id: z.number().describe("Action ID"),
      status: z.string().describe("Action status"),
      type: z.string().describe("Action type"),
      started_at: z.string().describe("Action start time"),
    }),
    execute: async (params, ctx) => {
      const data = await doPost(ctx, `/droplets/${params.droplet_id}/actions`, { type: "snapshot", name: params.name });
      const a = data.action;
      return { action_id: a.id, status: a.status, type: a.type, started_at: a.started_at };
    },
  },
};
