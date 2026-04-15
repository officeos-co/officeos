import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { doGet, doPost, doDelete, qs, DO_API } from "../core/client.ts";

export const kubernetes: Record<string, ActionDefinition> = {
  list_clusters: {
    description: "List Kubernetes clusters.",
    params: z.object({
      per_page: z.number().min(1).max(200).default(20).describe("Results per page (1-200)"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Cluster ID"),
      name: z.string().describe("Cluster name"),
      region: z.string().describe("Region slug"),
      version: z.string().describe("Kubernetes version"),
      status: z.string().describe("Cluster status"),
      endpoint: z.string().nullable().describe("API server endpoint"),
      node_pools: z.array(z.any()).describe("Node pools"),
      created_at: z.string().describe("Creation timestamp"),
      auto_upgrade: z.boolean().describe("Auto upgrade enabled"),
      surge_upgrade: z.boolean().describe("Surge upgrade enabled"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/kubernetes/clusters${qs({ per_page: params.per_page })}`);
      return (data.kubernetes_clusters ?? []).map((c: any) => ({
        id: c.id,
        name: c.name,
        region: c.region,
        version: c.version,
        status: c.status?.state ?? c.status ?? "",
        endpoint: c.endpoint ?? null,
        node_pools: c.node_pools ?? [],
        created_at: c.created_at,
        auto_upgrade: c.auto_upgrade ?? false,
        surge_upgrade: c.surge_upgrade ?? false,
      }));
    },
  },

  get_cluster: {
    description: "Get detailed info about a Kubernetes cluster.",
    params: z.object({
      cluster_id: z.string().describe("Kubernetes cluster ID"),
    }),
    returns: z.object({
      id: z.string().describe("Cluster ID"),
      name: z.string().describe("Cluster name"),
      region: z.string().describe("Region slug"),
      version: z.string().describe("Kubernetes version"),
      status: z.string().describe("Cluster status"),
      endpoint: z.string().nullable().describe("API server endpoint"),
      ipv4: z.string().nullable().describe("IPv4 address"),
      node_pools: z.array(z.any()).describe("Node pools"),
      maintenance_policy: z.any().nullable().describe("Maintenance policy"),
      auto_upgrade: z.boolean().describe("Auto upgrade enabled"),
      surge_upgrade: z.boolean().describe("Surge upgrade enabled"),
      created_at: z.string().describe("Creation timestamp"),
      updated_at: z.string().describe("Last updated timestamp"),
    }),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/kubernetes/clusters/${params.cluster_id}`);
      const c = data.kubernetes_cluster;
      return {
        id: c.id,
        name: c.name,
        region: c.region,
        version: c.version,
        status: c.status?.state ?? c.status ?? "",
        endpoint: c.endpoint ?? null,
        ipv4: c.ipv4 ?? null,
        node_pools: c.node_pools ?? [],
        maintenance_policy: c.maintenance_policy ?? null,
        auto_upgrade: c.auto_upgrade ?? false,
        surge_upgrade: c.surge_upgrade ?? false,
        created_at: c.created_at,
        updated_at: c.updated_at,
      };
    },
  },

  create_cluster: {
    description: "Create a new Kubernetes cluster.",
    params: z.object({
      name: z.string().describe("Cluster name"),
      region: z.string().describe("Region slug"),
      version: z.string().describe("Kubernetes version slug"),
      node_pool_name: z.string().describe("Default node pool name"),
      node_pool_size: z.string().describe("Node size slug"),
      node_pool_count: z.number().describe("Number of nodes"),
      node_pool_tags: z.array(z.string()).optional().describe("Tags for nodes"),
      auto_upgrade: z.boolean().default(false).describe("Enable auto version upgrades"),
      surge_upgrade: z.boolean().default(false).describe("Enable surge upgrades"),
      vpc_uuid: z.string().optional().describe("VPC UUID"),
      tags: z.array(z.string()).optional().describe("Tags for the cluster"),
    }),
    returns: z.object({
      id: z.string().describe("Cluster ID"),
      name: z.string().describe("Cluster name"),
      region: z.string().describe("Region slug"),
      version: z.string().describe("Kubernetes version"),
      status: z.string().describe("Cluster status"),
      endpoint: z.string().nullable().describe("API server endpoint"),
      created_at: z.string().describe("Creation timestamp"),
    }),
    execute: async (params, ctx) => {
      const nodePool: any = {
        name: params.node_pool_name,
        size: params.node_pool_size,
        count: params.node_pool_count,
      };
      if (params.node_pool_tags) nodePool.tags = params.node_pool_tags;
      const body: any = {
        name: params.name,
        region: params.region,
        version: params.version,
        node_pools: [nodePool],
        auto_upgrade: params.auto_upgrade,
        surge_upgrade: params.surge_upgrade,
      };
      if (params.vpc_uuid) body.vpc_uuid = params.vpc_uuid;
      if (params.tags) body.tags = params.tags;
      const data = await doPost(ctx, "/kubernetes/clusters", body);
      const c = data.kubernetes_cluster;
      return {
        id: c.id,
        name: c.name,
        region: c.region,
        version: c.version,
        status: c.status?.state ?? c.status ?? "",
        endpoint: c.endpoint ?? null,
        created_at: c.created_at,
      };
    },
  },

  delete_cluster: {
    description: "Delete a Kubernetes cluster.",
    params: z.object({
      cluster_id: z.string().describe("Kubernetes cluster ID"),
    }),
    returns: z.object({
      deleted: z.boolean().describe("Whether deletion succeeded"),
    }),
    execute: async (params, ctx) => {
      await doDelete(ctx, `/kubernetes/clusters/${params.cluster_id}`);
      return { deleted: true };
    },
  },

  get_kubeconfig: {
    description: "Get kubeconfig for a Kubernetes cluster.",
    params: z.object({
      cluster_id: z.string().describe("Kubernetes cluster ID"),
    }),
    returns: z.object({
      kubeconfig: z.string().describe("Kubeconfig YAML"),
      expires_at: z.string().describe("Config expiration time"),
    }),
    execute: async (params, ctx) => {
      const res = await ctx.fetch(`${DO_API}/kubernetes/clusters/${params.cluster_id}/kubeconfig`, {
        headers: { Authorization: `Bearer ${ctx.credentials.token}` },
      });
      if (!res.ok) throw new Error(`DigitalOcean API ${res.status}: ${await res.text()}`);
      const yaml = await res.text();
      const expiresAt = res.headers.get("x-kubeconfig-expires") ?? new Date(Date.now() + 7 * 24 * 3600 * 1000).toISOString();
      return { kubeconfig: yaml, expires_at: expiresAt };
    },
  },

  list_node_pools: {
    description: "List node pools for a Kubernetes cluster.",
    params: z.object({
      cluster_id: z.string().describe("Kubernetes cluster ID"),
    }),
    returns: z.array(z.object({
      id: z.string().describe("Node pool ID"),
      name: z.string().describe("Node pool name"),
      size: z.string().describe("Node size slug"),
      count: z.number().describe("Node count"),
      tags: z.array(z.string()).describe("Tags"),
      auto_scale: z.boolean().describe("Auto-scaling enabled"),
      min_nodes: z.number().describe("Minimum nodes"),
      max_nodes: z.number().describe("Maximum nodes"),
      nodes: z.array(z.any()).describe("Node details"),
    })),
    execute: async (params, ctx) => {
      const data = await doGet(ctx, `/kubernetes/clusters/${params.cluster_id}/node_pools`);
      return (data.node_pools ?? []).map((p: any) => ({
        id: p.id,
        name: p.name,
        size: p.size,
        count: p.count,
        tags: p.tags ?? [],
        auto_scale: p.auto_scale ?? false,
        min_nodes: p.min_nodes ?? 0,
        max_nodes: p.max_nodes ?? 0,
        nodes: p.nodes ?? [],
      }));
    },
  },
};
