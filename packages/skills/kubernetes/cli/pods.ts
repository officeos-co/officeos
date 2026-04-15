import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, k8sDelete, k8sText, k8sUrl, k8sHeaders, enc, ns } from "../core/client.ts";

export const pods: Record<string, ActionDefinition> = {
  list_pods: {
    description: "List pods in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace (default: default)"),
      label_selector: z.string().optional().describe("Label filter (e.g. app=backend)"),
      field_selector: z.string().optional().describe("Field filter (e.g. status.phase=Running)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Pod name"),
        namespace: z.string().describe("Namespace"),
        status: z.string().describe("Pod phase"),
        ready: z.string().describe("Ready containers (e.g. 1/1)"),
        restarts: z.number().describe("Total restarts"),
        age: z.string().describe("Age since creation"),
        node: z.string().describe("Node name"),
        ip: z.string().describe("Pod IP"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const qs = new URLSearchParams();
      if (params.label_selector) qs.set("labelSelector", params.label_selector);
      if (params.field_selector) qs.set("fieldSelector", params.field_selector);
      const data = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/pods?${qs}`);
      return (data.items ?? []).map((p: any) => {
        const containers = p.status?.containerStatuses ?? [];
        const ready = containers.filter((c: any) => c.ready).length;
        const total = containers.length || (p.spec?.containers?.length ?? 0);
        const restarts = containers.reduce((sum: number, c: any) => sum + (c.restartCount ?? 0), 0);
        return {
          name: p.metadata.name,
          namespace: p.metadata.namespace,
          status: p.status?.phase ?? "Unknown",
          ready: `${ready}/${total}`,
          restarts,
          age: p.metadata.creationTimestamp ?? "",
          node: p.spec?.nodeName ?? "",
          ip: p.status?.podIP ?? "",
        };
      });
    },
  },

  get_pod: {
    description: "Get detailed information about a pod.",
    params: z.object({
      name: z.string().describe("Pod name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      containers: z.any().describe("Container specs"),
      volumes: z.any().describe("Pod volumes"),
      status: z.any().describe("Pod status"),
      conditions: z.any().describe("Pod conditions"),
      events: z.any().describe("Pod events"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const p = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/pods/${enc(params.name)}`);
      return {
        containers: p.spec?.containers ?? [],
        volumes: p.spec?.volumes ?? [],
        status: p.status ?? {},
        conditions: p.status?.conditions ?? [],
        events: [],
      };
    },
  },

  delete_pod: {
    description: "Delete a pod.",
    params: z.object({
      name: z.string().describe("Pod name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      grace_period: z.number().default(30).describe("Seconds to wait for graceful stop"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      await k8sDelete(ctx, `/api/v1/namespaces/${enc(n)}/pods/${enc(params.name)}?gracePeriodSeconds=${params.grace_period}`);
      return { name: params.name, status: "deleted" };
    },
  },

  logs: {
    description: "Get pod logs.",
    params: z.object({
      name: z.string().describe("Pod name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      container: z.string().optional().describe("Container name (required if multi-container)"),
      tail: z.number().default(200).describe("Number of lines from the end"),
      previous: z.boolean().default(false).describe("Logs from previous container instance"),
      since: z.string().optional().describe("Duration (e.g. 1h, 30m)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.string().describe("Log output as text"),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const qs = new URLSearchParams({
        tailLines: String(params.tail),
        previous: String(params.previous),
      });
      if (params.container) qs.set("container", params.container);
      if (params.since) {
        const match = params.since.match(/^(\d+)([hms])$/);
        if (match) {
          const units: Record<string, number> = { h: 3600, m: 60, s: 1 };
          qs.set("sinceSeconds", String(parseInt(match[1]) * (units[match[2]] ?? 1)));
        }
      }
      return k8sText(ctx, `/api/v1/namespaces/${enc(n)}/pods/${enc(params.name)}/log?${qs}`);
    },
  },

  exec: {
    description: "Execute a command in a pod.",
    params: z.object({
      name: z.string().describe("Pod name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      command: z.string().describe("Command to execute"),
      container: z.string().optional().describe("Container name (required if multi-container)"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      exit_code: z.number().describe("Exit code"),
      stdout: z.string().describe("Standard output"),
      stderr: z.string().describe("Standard error"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const qs = new URLSearchParams({ stdout: "true", stderr: "true" });
      for (const part of params.command.split(" ")) {
        qs.append("command", part);
      }
      if (params.container) qs.set("container", params.container);
      const res = await ctx.fetch(
        k8sUrl(ctx.credentials.api_url, `/api/v1/namespaces/${enc(n)}/pods/${enc(params.name)}/exec?${qs}`),
        { headers: k8sHeaders(ctx.credentials.token) },
      );
      const text = await res.text();
      return { exit_code: res.ok ? 0 : 1, stdout: text, stderr: "" };
    },
  },

  describe_pod: {
    description: "Describe a pod with events and conditions.",
    params: z.object({
      name: z.string().describe("Pod name"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({
      events: z.any().describe("Pod events"),
      conditions: z.any().describe("Pod conditions"),
      resource_requests: z.any().describe("Resource requests"),
      resource_limits: z.any().describe("Resource limits"),
      volumes: z.any().describe("Pod volumes"),
    }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const p = await k8sFetch(ctx, `/api/v1/namespaces/${enc(n)}/pods/${enc(params.name)}`);
      const events = await k8sFetch(
        ctx,
        `/api/v1/namespaces/${enc(n)}/events?fieldSelector=involvedObject.name=${enc(params.name)}`,
      );
      const containers = p.spec?.containers ?? [];
      return {
        events: (events.items ?? []).map((e: any) => ({
          type: e.type,
          reason: e.reason,
          message: e.message,
          last_timestamp: e.lastTimestamp,
        })),
        conditions: p.status?.conditions ?? [],
        resource_requests: containers.map((c: any) => ({ name: c.name, ...c.resources?.requests })),
        resource_limits: containers.map((c: any) => ({ name: c.name, ...c.resources?.limits })),
        volumes: p.spec?.volumes ?? [],
      };
    },
  },
};
