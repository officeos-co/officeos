import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sPost, k8sDelete, enc, ns } from "../core/client.ts";

export const apply: Record<string, ActionDefinition> = {
  apply: {
    description: "Apply a YAML or JSON manifest.",
    params: z.object({
      manifest: z.string().describe("YAML or JSON manifest content"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      kind: z.string(),
      name: z.string(),
      namespace: z.string(),
      action: z.string().describe("created, configured, or unchanged"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      let parsed: any;
      try {
        parsed = JSON.parse(params.manifest);
      } catch {
        throw new Error("Manifest must be valid JSON. YAML support requires a parser library.");
      }
      const kind = parsed.kind ?? "Unknown";
      const name = parsed.metadata?.name ?? "unknown";
      const apiVersion = parsed.apiVersion ?? "v1";

      let basePath: string;
      if (apiVersion.includes("/")) {
        const [group, ver] = apiVersion.split("/");
        basePath = `/apis/${group}/${ver}`;
      } else {
        basePath = `/api/${apiVersion}`;
      }

      const nsPath = parsed.metadata?.namespace
        ? `/namespaces/${enc(parsed.metadata.namespace)}`
        : `/namespaces/${enc(n)}`;
      const kindPlural = kind.toLowerCase() + "s";
      const url = `${basePath}${nsPath}/${kindPlural}`;

      let action = "created";
      try {
        await k8sPost(ctx, url, parsed);
      } catch (e: any) {
        if (e.message.includes("409")) {
          await k8sPost(ctx, `${url}/${enc(name)}`, parsed, "PUT");
          action = "configured";
        } else {
          throw e;
        }
      }
      return { kind, name, namespace: parsed.metadata?.namespace ?? n, action };
    },
  },

  delete_resource: {
    description: "Delete a Kubernetes resource by kind and name.",
    params: z.object({
      kind: z.string().describe("Resource kind (e.g. Deployment, Service, Pod)"),
      name: z.string().describe("Resource name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      grace_period: z.number().optional().describe("Seconds for graceful termination"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ kind: z.string(), name: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const kindLower = params.kind.toLowerCase();
      const kindPlural = kindLower + "s";
      const appsKinds = ["deployment", "statefulset", "daemonset", "replicaset"];
      const batchKinds = ["job", "cronjob"];
      let basePath: string;
      if (appsKinds.includes(kindLower)) {
        basePath = `/apis/apps/v1/namespaces/${enc(n)}/${kindPlural}`;
      } else if (batchKinds.includes(kindLower)) {
        basePath = `/apis/batch/v1/namespaces/${enc(n)}/${kindPlural}`;
      } else {
        basePath = `/api/v1/namespaces/${enc(n)}/${kindPlural}`;
      }
      const qs = params.grace_period !== undefined ? `?gracePeriodSeconds=${params.grace_period}` : "";
      await k8sDelete(ctx, `${basePath}/${enc(params.name)}${qs}`);
      return { kind: params.kind, name: params.name };
    },
  },
};
