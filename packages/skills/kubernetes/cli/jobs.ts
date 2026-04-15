import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { k8sFetch, k8sPost, enc, ns } from "../core/client.ts";

export const jobs: Record<string, ActionDefinition> = {
  list_jobs: {
    description: "List jobs in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("Job name"),
        completions: z.string().describe("Completions"),
        succeeded: z.number().describe("Succeeded pods"),
        failed: z.number().describe("Failed pods"),
        age: z.string().describe("Age since creation"),
        duration: z.string().describe("Duration"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const data = await k8sFetch(ctx, `/apis/batch/v1/namespaces/${enc(n)}/jobs`);
      return (data.items ?? []).map((j: any) => ({
        name: j.metadata.name,
        completions: `${j.status?.succeeded ?? 0}/${j.spec?.completions ?? 1}`,
        succeeded: j.status?.succeeded ?? 0,
        failed: j.status?.failed ?? 0,
        age: j.metadata.creationTimestamp ?? "",
        duration: j.status?.completionTime && j.status?.startTime
          ? `${Math.round((new Date(j.status.completionTime).getTime() - new Date(j.status.startTime).getTime()) / 1000)}s`
          : "",
      }));
    },
  },

  create_job: {
    description: "Create a job.",
    params: z.object({
      name: z.string().describe("Job name"),
      image: z.string().describe("Container image"),
      command: z.string().describe("Command to execute"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      backoff_limit: z.number().default(6).describe("Number of retries"),
      restart_policy: z.string().default("Never").describe("Never or OnFailure"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), namespace: z.string(), status: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const manifest = {
        apiVersion: "batch/v1",
        kind: "Job",
        metadata: { name: params.name, namespace: n },
        spec: {
          backoffLimit: params.backoff_limit,
          template: {
            spec: {
              containers: [{ name: params.name, image: params.image, command: ["/bin/sh", "-c", params.command] }],
              restartPolicy: params.restart_policy,
            },
          },
        },
      };
      await k8sPost(ctx, `/apis/batch/v1/namespaces/${enc(n)}/jobs`, manifest);
      return { name: params.name, namespace: n, status: "created" };
    },
  },

  list_cronjobs: {
    description: "List CronJobs in a namespace.",
    params: z.object({
      namespace: z.string().optional().describe("Kubernetes namespace"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.array(
      z.object({
        name: z.string().describe("CronJob name"),
        schedule: z.string().describe("Cron schedule"),
        suspend: z.boolean().describe("Whether suspended"),
        active: z.number().describe("Active jobs"),
        last_schedule: z.string().describe("Last schedule time"),
        age: z.string().describe("Age since creation"),
      }),
    ),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const data = await k8sFetch(ctx, `/apis/batch/v1/namespaces/${enc(n)}/cronjobs`);
      return (data.items ?? []).map((cj: any) => ({
        name: cj.metadata.name,
        schedule: cj.spec?.schedule ?? "",
        suspend: cj.spec?.suspend ?? false,
        active: (cj.status?.active ?? []).length,
        last_schedule: cj.status?.lastScheduleTime ?? "",
        age: cj.metadata.creationTimestamp ?? "",
      }));
    },
  },

  create_cronjob: {
    description: "Create a CronJob.",
    params: z.object({
      name: z.string().describe("CronJob name"),
      image: z.string().describe("Container image"),
      command: z.string().describe("Command to execute"),
      schedule: z.string().describe("Cron expression"),
      namespace: z.string().optional().describe("Kubernetes namespace"),
      successful_jobs_history_limit: z.number().default(3).describe("Completed jobs to keep"),
      failed_jobs_history_limit: z.number().default(1).describe("Failed jobs to keep"),
      context: z.string().optional().describe("Kubeconfig context"),
    }),
    returns: z.object({ name: z.string(), namespace: z.string(), schedule: z.string() }),
    execute: async (params, ctx) => {
      const n = ns(params.namespace ?? ctx.credentials.namespace);
      const manifest = {
        apiVersion: "batch/v1",
        kind: "CronJob",
        metadata: { name: params.name, namespace: n },
        spec: {
          schedule: params.schedule,
          successfulJobsHistoryLimit: params.successful_jobs_history_limit,
          failedJobsHistoryLimit: params.failed_jobs_history_limit,
          jobTemplate: {
            spec: {
              template: {
                spec: {
                  containers: [{ name: params.name, image: params.image, command: ["/bin/sh", "-c", params.command] }],
                  restartPolicy: "OnFailure",
                },
              },
            },
          },
        },
      };
      await k8sPost(ctx, `/apis/batch/v1/namespaces/${enc(n)}/cronjobs`, manifest);
      return { name: params.name, namespace: n, schedule: params.schedule };
    },
  },
};
