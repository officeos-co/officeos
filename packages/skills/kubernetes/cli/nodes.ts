import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, enc } from "../core/client.ts";

export const nodes: Record<string, ActionDefinition> = {
  list_nodes: {
    description: "List cluster nodes.",
    params: z.object({
      label_selector: z.string().optional().describe("Label filter"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Node name"),
        status: z.string().describe("Node status"),
        roles: z.string().describe("Node roles"),
        version: z.string().describe("Kubelet version"),
        os: z.string().describe("Operating system"),
        arch: z.string().describe("Architecture"),
        cpu: z.string().describe("CPU capacity"),
        memory: z.string().describe("Memory capacity"),
        pods: z.string().describe("Pod capacity"),
      }),
    ),
    execute: async (params, ctx) => {
      const qs = new URLSearchParams();
      if (params.label_selector) qs.set("labelSelector", params.label_selector);
      const data = await k8sFetch(ctx, `/api/v1/nodes?${qs}`);
      return (data.items ?? []).map((n: any) => {
        const conds = n.status?.conditions ?? [];
        const ready = conds.find((c: any) => c.type === "Ready");
        const roles = Object.keys(n.metadata?.labels ?? {})
          .filter((l: string) => l.startsWith("node-role.kubernetes.io/"))
          .map((l: string) => l.replace("node-role.kubernetes.io/", ""))
          .join(",") || "<none>";
        return {
          name: n.metadata.name,
          status: ready?.status === "True" ? "Ready" : "NotReady",
          roles,
          version: n.status?.nodeInfo?.kubeletVersion ?? "",
          os: n.status?.nodeInfo?.operatingSystem ?? "",
          arch: n.status?.nodeInfo?.architecture ?? "",
          cpu: n.status?.capacity?.cpu ?? "",
          memory: n.status?.capacity?.memory ?? "",
          pods: n.status?.capacity?.pods ?? "",
        };
      });
    },
  },

  describe_node: {
    description: "Describe a node with full details.",
    params: z.object({
      name: z.string().describe("Node name"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      conditions: z.any().describe("Node conditions"),
      capacity: z.any().describe("Node capacity"),
      allocatable: z.any().describe("Allocatable resources"),
      system_info: z.any().describe("System info"),
      pods: z.any().describe("Pods running on node"),
      events: z.any().describe("Node events"),
    }),
    execute: async (params, ctx) => {
      const n = await k8sFetch(ctx, `/api/v1/nodes/${enc(params.name)}`);
      const pods = await k8sFetch(ctx, `/api/v1/pods?fieldSelector=spec.nodeName=${enc(params.name)}`);
      const events = await k8sFetch(ctx, `/api/v1/events?fieldSelector=involvedObject.name=${enc(params.name)}`);
      return {
        conditions: n.status?.conditions ?? [],
        capacity: n.status?.capacity ?? {},
        allocatable: n.status?.allocatable ?? {},
        system_info: n.status?.nodeInfo ?? {},
        pods: (pods.items ?? []).map((p: any) => ({ name: p.metadata.name, namespace: p.metadata.namespace, status: p.status?.phase })),
        events: (events.items ?? []).map((e: any) => ({ type: e.type, reason: e.reason, message: e.message })),
      };
    },
  },

  cordon: {
    description: "Mark a node as unschedulable.",
    params: z.object({
      name: z.string().describe("Node name"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await k8sPost(ctx, `/api/v1/nodes/${enc(params.name)}`, { spec: { unschedulable: true } }, "PATCH");
      return { status: "cordoned" };
    },
  },

  uncordon: {
    description: "Mark a node as schedulable.",
    params: z.object({
      name: z.string().describe("Node name"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ status: z.string() }),
    execute: async (params, ctx) => {
      await k8sPost(ctx, `/api/v1/nodes/${enc(params.name)}`, { spec: { unschedulable: false } }, "PATCH");
      return { status: "uncordoned" };
    },
  },

  drain: {
    description: "Drain a node (evict all pods).",
    params: z.object({
      name: z.string().describe("Node name"),
      ignore_daemonsets: z.boolean().default(true).describe("Ignore DaemonSet pods"),
      delete_emptydir_data: z.boolean().default(false).describe("Delete emptyDir data"),
      grace_period: z.number().default(30).describe("Seconds for graceful termination"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      evicted_pods: z.array(z.string()).describe("Evicted pod names"),
      status: z.string(),
    }),
    execute: async (params, ctx) => {
      await k8sPost(ctx, `/api/v1/nodes/${enc(params.name)}`, { spec: { unschedulable: true } }, "PATCH");
      const pods = await k8sFetch(ctx, `/api/v1/pods?fieldSelector=spec.nodeName=${enc(params.name)}`);
      const evicted: string[] = [];
      for (const p of pods.items ?? []) {
        const isDaemonSet = p.metadata?.ownerReferences?.some((r: any) => r.kind === "DaemonSet");
        if (isDaemonSet && params.ignore_daemonsets) continue;
        try {
          await k8sPost(ctx, `/api/v1/namespaces/${enc(p.metadata.namespace)}/pods/${enc(p.metadata.name)}/eviction`, {
            apiVersion: "policy/v1",
            kind: "Eviction",
            metadata: { name: p.metadata.name, namespace: p.metadata.namespace },
            deleteOptions: { gracePeriodSeconds: params.grace_period },
          });
          evicted.push(p.metadata.name);
        } catch { /* pod may already be gone */ }
      }
      return { evicted_pods: evicted, status: "drained" };
    },
  },
};
