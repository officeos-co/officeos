import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, enc, ns } from "../core/client.ts";

export const configmaps: Record<string, ActionDefinition> = {
  list_configmaps: {
    description: "List ConfigMaps in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("ConfigMap name"),
        data_keys: z.array(z.string()).describe("Data keys"),
        age: z.string().describe("Age since creation"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const data = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/configmaps`);
      return (data.items ?? []).map((cm: any) => ({
        name: cm.metadata.name,
        data_keys: Object.keys(cm.data ?? {}),
        age: cm.metadata.creationTimestamp ?? "",
      }));
    },
  },

  get_configmap: {
    description: "Get a ConfigMap.",
    params: z.object({
      name: z.string().describe("ConfigMap name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      namespace: z.string(),
      data: z.any().describe("Key-value pairs"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const cm = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/configmaps/${enc(params.name)}`);
      return { name: cm.metadata.name, namespace: cm.metadata.namespace, data: cm.data ?? {} };
    },
  },

  create_configmap: {
    description: "Create a ConfigMap.",
    params: z.object({
      name: z.string().describe("ConfigMap name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      data: z.string().describe("JSON object of key-value pairs"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      namespace: z.string(),
      data_keys: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const parsed = JSON.parse(params.data);
      const manifest = {
        apiVersion: "v1",
        kind: "ConfigMap",
        metadata: { name: params.name, namespace: n },
        data: parsed,
      };
      const cm = await k8sPost(ctx, `/api/v1/namespaces/${enc(n)}/configmaps`, manifest);
      return { name: cm.metadata.name, namespace: cm.metadata.namespace, data_keys: Object.keys(cm.data ?? {}) };
    },
  },
};
