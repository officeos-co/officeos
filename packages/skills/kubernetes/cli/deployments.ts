import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, k8sDelete, enc, ns } from "../core/client.ts";

export const deployments: Record<string, ActionDefinition> = {
  list_deployments: {
    description: "List deployments in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      label_selector: z.string().optional().describe("Label filter (e.g. app=backend)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Deployment name"),
        ready: z.string().describe("Ready replicas (e.g. 3/3)"),
        up_to_date: z.number().describe("Up-to-date replicas"),
        available: z.number().describe("Available replicas"),
        age: z.string().describe("Age since creation"),
        images: z.array(z.string()).describe("Container images"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const qs = new URLSearchParams();
      if (params.label_selector) qs.set("labelSelector", params.label_selector);
      const data = await k8sFetch(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments?${qs}`);
      return (data.items ?? []).map((d: any) => ({
        name: d.metadata.name,
        ready: `${d.status?.readyReplicas ?? 0}/${d.spec?.replicas ?? 0}`,
        up_to_date: d.status?.updatedReplicas ?? 0,
        available: d.status?.availableReplicas ?? 0,
        age: d.metadata.creationTimestamp ?? "",
        images: (d.spec?.template?.spec?.containers ?? []).map((c: any) => c.image),
      }));
    },
  },

  get_deployment: {
    description: "Get detailed information about a deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      replicas: z.number().describe("Desired replicas"),
      strategy: z.any().describe("Deployment strategy"),
      template: z.any().describe("Pod template"),
      conditions: z.any().describe("Deployment conditions"),
      revision_history: z.number().describe("Revision history limit"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const d = await k8sFetch(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}`);
      return {
        replicas: d.spec?.replicas ?? 0,
        strategy: d.spec?.strategy ?? {},
        template: d.spec?.template ?? {},
        conditions: d.status?.conditions ?? [],
        revision_history: d.spec?.revisionHistoryLimit ?? 10,
      };
    },
  },

  create_deployment: {
    description: "Create a deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      image: z.string().describe("Container image"),
      replicas: z.number().default(1).describe("Number of replicas"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      port: z.number().optional().describe("Container port"),
      env: z.string().optional().describe("JSON object of env vars"),
      labels: z.string().optional().describe("JSON object of labels"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      namespace: z.string(),
      replicas: z.number(),
      image: z.string(),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const lbls = params.labels ? JSON.parse(params.labels) : { app: params.name };
      const envVars = params.env
        ? Object.entries(JSON.parse(params.env)).map(([k, v]) => ({ name: k, value: String(v) }))
        : undefined;
      const container: any = { name: params.name, image: params.image };
      if (params.port) container.ports = [{ containerPort: params.port }];
      if (envVars) container.env = envVars;
      const manifest = {
        apiVersion: "apps/v1",
        kind: "Deployment",
        metadata: { name: params.name, namespace: n, labels: lbls },
        spec: {
          replicas: params.replicas,
          selector: { matchLabels: lbls },
          template: { metadata: { labels: lbls }, spec: { containers: [container] } },
        },
      };
      await k8sPost(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments`, manifest);
      return { name: params.name, namespace: n, replicas: params.replicas, image: params.image };
    },
  },

  scale: {
    description: "Scale a deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      replicas: z.number().describe("Desired replica count"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      name: z.string(),
      previous_replicas: z.number(),
      current_replicas: z.number(),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const d = await k8sFetch(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}`);
      const prev = d.spec?.replicas ?? 0;
      await k8sPost(
        ctx,
        `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}/scale`,
        { apiVersion: "autoscaling/v1", kind: "Scale", metadata: { name: params.name, namespace: n }, spec: { replicas: params.replicas } },
        "PUT",
      );
      return { name: params.name, previous_replicas: prev, current_replicas: params.replicas };
    },
  },

  rollout_status: {
    description: "Check the rollout status of a deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      status: z.string().describe("Rollout status"),
      message: z.string().describe("Status message"),
      ready_replicas: z.number().describe("Ready replicas"),
      updated_replicas: z.number().describe("Updated replicas"),
      available_replicas: z.number().describe("Available replicas"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const d = await k8sFetch(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}`);
      const conds = d.status?.conditions ?? [];
      const progressing = conds.find((c: any) => c.type === "Progressing");
      return {
        status: progressing?.status === "True" ? "progressing" : "complete",
        message: progressing?.message ?? "",
        ready_replicas: d.status?.readyReplicas ?? 0,
        updated_replicas: d.status?.updatedReplicas ?? 0,
        available_replicas: d.status?.availableReplicas ?? 0,
      };
    },
  },

  rollout_restart: {
    description: "Restart a deployment (rolling restart).",
    params: z.object({
      name: z.string().describe("Deployment name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), restart_timestamp: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const now = new Date().toISOString();
      await k8sPost(
        ctx,
        `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}`,
        { spec: { template: { metadata: { annotations: { "kubectl.kubernetes.io/restartedAt": now } } } } },
        "PATCH",
      );
      return { name: params.name, restart_timestamp: now };
    },
  },

  rollout_undo: {
    description: "Roll back a deployment to a previous revision.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      revision: z.number().optional().describe("Revision to roll back to (omit for previous)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), rolled_back_to: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const body: any = {
        kind: "DeploymentRollback",
        apiVersion: "apps/v1",
        name: params.name,
        rollbackTo: { revision: params.revision ?? 0 },
      };
      await k8sPost(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}/rollback`, body);
      return { name: params.name, rolled_back_to: params.revision ? `revision ${params.revision}` : "previous revision" };
    },
  },

  delete_deployment: {
    description: "Delete a deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      await k8sDelete(ctx, `/apis/apps/v1/namespaces/${enc(n)}/deployments/${enc(params.name)}`);
      return { name: params.name };
    },
  },
};
