import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, enc, ns } from "../core/client.ts";

export const secrets: Record<string, ActionDefinition> = {
  list_secrets: {
    description: "List secrets in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Secret name"),
        type: z.string().describe("Secret type"),
        data_keys: z.array(z.string()).describe("Data keys"),
        age: z.string().describe("Age since creation"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const data = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/secrets`);
      return (data.items ?? []).map((s: any) => ({
        name: s.metadata.name,
        type: s.type ?? "Opaque",
        data_keys: Object.keys(s.data ?? {}),
        age: s.metadata.creationTimestamp ?? "",
      }));
    },
  },

  get_secret: {
    description: "Get a secret (values are base64-encoded).",
    params: z.object({
      name: z.string().describe("Secret name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      namespace: z.string(),
      type: z.string(),
      data_keys: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const s = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/secrets/${enc(params.name)}`);
      return {
        name: s.metadata.name,
        namespace: s.metadata.namespace,
        type: s.type ?? "Opaque",
        data_keys: Object.keys(s.data ?? {}),
      };
    },
  },

  create_secret: {
    description: "Create a secret.",
    params: z.object({
      name: z.string().describe("Secret name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      type: z.string().default("Opaque").describe("Secret type"),
      data: z.string().describe("JSON object of key-value pairs (values will be base64-encoded)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      namespace: z.string(),
      type: z.string(),
      data_keys: z.array(z.string()),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const parsed = JSON.parse(params.data);
      const encoded: Record<string, string> = {};
      for (const [k, v] of Object.entries(parsed)) {
        encoded[k] = btoa(String(v));
      }
      const manifest = {
        apiVersion: "v1",
        kind: "Secret",
        metadata: { name: params.name, namespace: n },
        type: params.type,
        data: encoded,
      };
      const s = await k8sPost(ctx, `/api/v1/namespaces/${enc(n)}/secrets`, manifest);
      return {
        name: s.metadata.name,
        namespace: s.metadata.namespace,
        type: s.type,
        data_keys: Object.keys(s.data ?? {}),
      };
    },
  },
};
