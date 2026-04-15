import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, k8sDelete, enc, ns } from "../core/client.ts";

export const services: Record<string, ActionDefinition> = {
  list_services: {
    description: "List services in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      label_selector: z.string().optional().describe("Label filter"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Service name"),
        type: z.string().describe("Service type"),
        cluster_ip: z.string().describe("Cluster IP"),
        external_ip: z.string().describe("External IP"),
        ports: z.any().describe("Service ports"),
        age: z.string().describe("Age since creation"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const qs = new URLSearchParams();
      if (params.label_selector) qs.set("labelSelector", params.label_selector);
      const data = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/services?${qs}`);
      return (data.items ?? []).map((s: any) => ({
        name: s.metadata.name,
        type: s.spec?.type ?? "ClusterIP",
        cluster_ip: s.spec?.clusterIP ?? "",
        external_ip: (s.status?.loadBalancer?.ingress ?? []).map((i: any) => i.ip || i.hostname).join(",") || "<none>",
        ports: (s.spec?.ports ?? []).map((p: any) => `${p.port}/${p.protocol}`),
        age: s.metadata.creationTimestamp ?? "",
      }));
    },
  },

  get_service: {
    description: "Get detailed information about a service.",
    params: z.object({
      name: z.string().describe("Service name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      type: z.string().describe("Service type"),
      selector: z.any().describe("Pod selector"),
      ports: z.any().describe("Port mappings"),
      endpoints: z.any().describe("Service endpoints"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const s = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/services/${enc(params.name)}`);
      let endpoints: any = [];
      try {
        const ep = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/endpoints/${enc(params.name)}`);
        endpoints = ep.subsets ?? [];
      } catch { /* no endpoints */ }
      return {
        type: s.spec?.type ?? "ClusterIP",
        selector: s.spec?.selector ?? {},
        ports: s.spec?.ports ?? [],
        endpoints,
      };
    },
  },

  create_service: {
    description: "Create a service.",
    params: z.object({
      name: z.string().describe("Service name"),
      type: z.string().default("ClusterIP").describe("ClusterIP, NodePort, or LoadBalancer"),
      port: z.number().describe("Service port"),
      target_port: z.number().optional().describe("Target port on pods"),
      selector: z.string().describe("JSON object of pod selectors"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      type: z.string(),
      cluster_ip: z.string(),
      ports: z.any(),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const manifest = {
        apiVersion: "v1",
        kind: "Service",
        metadata: { name: params.name, namespace: n },
        spec: {
          type: params.type,
          selector: JSON.parse(params.selector),
          ports: [{ port: params.port, targetPort: params.target_port ?? params.port }],
        },
      };
      const s = await k8sPost(ctx, `/api/v1/namespaces/${enc(n)}/services`, manifest);
      return {
        name: s.metadata.name,
        type: s.spec.type,
        cluster_ip: s.spec.clusterIP ?? "",
        ports: s.spec.ports,
      };
    },
  },

  delete_service: {
    description: "Delete a service.",
    params: z.object({
      name: z.string().describe("Service name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      await k8sDelete(ctx, `/api/v1/namespaces/${enc(n)}/services/${enc(params.name)}`);
      return { name: params.name };
    },
  },
};
