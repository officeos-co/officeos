import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, k8sDelete, enc } from "../core/client.ts";

export const namespaces: Record<string, ActionDefinition> = {
  list_namespaces: {
    description: "List all namespaces.",
    params: z.object({
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Namespace name"),
        status: z.string().describe("Namespace phase"),
        age: z.string().describe("Age since creation"),
        labels: z.any().describe("Namespace labels"),
      }),
    ),
    execute: async (_params, ctx) => {
      const data = await k8sFetch(ctx, `/api/v1/namespaces`);
      return (data.items ?? []).map((n: any) => ({
        name: n.metadata.name,
        status: n.status?.phase ?? "Active",
        age: n.metadata.creationTimestamp ?? "",
        labels: n.metadata.labels ?? {},
      }));
    },
  },

  create_namespace: {
    description: "Create a namespace.",
    params: z.object({
      name: z.string().describe("Namespace name"),
      labels: z.string().optional().describe("JSON object of labels"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const manifest: any = {
        apiVersion: "v1",
        kind: "Namespace",
        metadata: { name: params.name },
      };
      if (params.labels) manifest.metadata.labels = JSON.parse(params.labels);
      const n = await k8sPost(ctx, `/api/v1/namespaces`, manifest);
      return { name: n.metadata.name, status: n.status?.phase ?? "Active" };
    },
  },

  delete_namespace: {
    description: "Delete a namespace and all resources within it.",
    params: z.object({
      name: z.string().describe("Namespace name"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      await k8sDelete(ctx, `/api/v1/namespaces/${enc(params.name)}`);
      return { name: params.name };
    },
  },
};
