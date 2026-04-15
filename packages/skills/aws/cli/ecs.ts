import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ecsFetch, resolveRegion } from "../core/client.ts";

export const ecs: Record<string, ActionDefinition> = {
  list_clusters: {
    description: "List ECS clusters.",
    params: z.object({
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      cluster_arn: z.string().describe("Cluster ARN"),
      cluster_name: z.string().describe("Cluster name"),
      status: z.string().describe("Cluster status"),
      running_tasks_count: z.number().describe("Running tasks"),
      pending_tasks_count: z.number().describe("Pending tasks"),
      active_services_count: z.number().describe("Active services"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const listData = await ecsFetch(ctx, region, "ListClusters", {});
      const arns = listData.clusterArns ?? [];
      if (arns.length === 0) return [];
      const descData = await ecsFetch(ctx, region, "DescribeClusters", { clusters: arns });
      return (descData.clusters ?? []).map((c: any) => ({
        cluster_arn: c.clusterArn,
        cluster_name: c.clusterName,
        status: c.status,
        running_tasks_count: c.runningTasksCount,
        pending_tasks_count: c.pendingTasksCount,
        active_services_count: c.activeServicesCount,
      }));
    },
  },

  list_services: {
    description: "List ECS services in a cluster.",
    params: z.object({
      cluster: z.string().describe("Cluster name or ARN"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      service_name: z.string().describe("Service name"),
      service_arn: z.string().describe("Service ARN"),
      status: z.string().describe("Service status"),
      desired_count: z.number().describe("Desired task count"),
      running_count: z.number().describe("Running task count"),
      pending_count: z.number().describe("Pending task count"),
      task_definition: z.string().describe("Task definition"),
      launch_type: z.string().describe("Launch type"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const listData = await ecsFetch(ctx, region, "ListServices", { cluster: params.cluster });
      const arns = listData.serviceArns ?? [];
      if (arns.length === 0) return [];
      const descData = await ecsFetch(ctx, region, "DescribeServices", { cluster: params.cluster, services: arns });
      return (descData.services ?? []).map((s: any) => ({
        service_name: s.serviceName,
        service_arn: s.serviceArn,
        status: s.status,
        desired_count: s.desiredCount,
        running_count: s.runningCount,
        pending_count: s.pendingCount,
        task_definition: s.taskDefinition,
        launch_type: s.launchType ?? "EC2",
      }));
    },
  },

  list_tasks: {
    description: "List ECS tasks in a cluster.",
    params: z.object({
      cluster: z.string().describe("Cluster name or ARN"),
      service: z.string().optional().describe("Filter by service name"),
      status: z.enum(["RUNNING", "STOPPED", "PENDING"]).default("RUNNING").describe("Task status filter"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.array(z.object({
      task_arn: z.string().describe("Task ARN"),
      task_definition_arn: z.string().describe("Task definition ARN"),
      last_status: z.string().describe("Last known status"),
      desired_status: z.string().describe("Desired status"),
      started_at: z.string().nullable().describe("Start time"),
      container_instance_arn: z.string().nullable().describe("Container instance ARN"),
    })),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const listPayload: Record<string, any> = { cluster: params.cluster, desiredStatus: params.status };
      if (params.service) listPayload.serviceName = params.service;
      const listData = await ecsFetch(ctx, region, "ListTasks", listPayload);
      const arns = listData.taskArns ?? [];
      if (arns.length === 0) return [];
      const descData = await ecsFetch(ctx, region, "DescribeTasks", { cluster: params.cluster, tasks: arns });
      return (descData.tasks ?? []).map((t: any) => ({
        task_arn: t.taskArn,
        task_definition_arn: t.taskDefinitionArn,
        last_status: t.lastStatus,
        desired_status: t.desiredStatus,
        started_at: t.startedAt ?? null,
        container_instance_arn: t.containerInstanceArn ?? null,
      }));
    },
  },

  describe_service: {
    description: "Get detailed info about an ECS service.",
    params: z.object({
      cluster: z.string().describe("Cluster name or ARN"),
      service: z.string().describe("Service name or ARN"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      service_name: z.string().describe("Service name"),
      service_arn: z.string().describe("Service ARN"),
      status: z.string().describe("Service status"),
      task_definition: z.string().describe("Task definition"),
      desired_count: z.number().describe("Desired count"),
      running_count: z.number().describe("Running count"),
      pending_count: z.number().describe("Pending count"),
      launch_type: z.string().describe("Launch type"),
      load_balancers: z.array(z.any()).describe("Load balancers"),
      deployments: z.array(z.any()).describe("Deployments"),
      events: z.array(z.object({ created_at: z.string(), message: z.string() })).describe("Recent events"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const data = await ecsFetch(ctx, region, "DescribeServices", { cluster: params.cluster, services: [params.service] });
      const s = data.services?.[0];
      if (!s) throw new Error(`Service ${params.service} not found`);
      return {
        service_name: s.serviceName,
        service_arn: s.serviceArn,
        status: s.status,
        task_definition: s.taskDefinition,
        desired_count: s.desiredCount,
        running_count: s.runningCount,
        pending_count: s.pendingCount,
        launch_type: s.launchType ?? "EC2",
        load_balancers: s.loadBalancers ?? [],
        deployments: (s.deployments ?? []).map((d: any) => ({
          id: d.id,
          status: d.status,
          desired_count: d.desiredCount,
          running_count: d.runningCount,
          task_definition: d.taskDefinition,
        })),
        events: (s.events ?? []).slice(0, 10).map((e: any) => ({
          created_at: e.createdAt,
          message: e.message,
        })),
      };
    },
  },

  update_service: {
    description: "Update an ECS service (desired count, task definition, etc.).",
    params: z.object({
      cluster: z.string().describe("Cluster name or ARN"),
      service: z.string().describe("Service name or ARN"),
      desired_count: z.number().optional().describe("Desired number of tasks"),
      task_definition: z.string().optional().describe("New task definition family:revision"),
      force_deployment: z.boolean().default(false).describe("Force new deployment"),
      region: z.string().optional().describe("AWS region"),
    }),
    returns: z.object({
      service_name: z.string().describe("Service name"),
      service_arn: z.string().describe("Service ARN"),
      status: z.string().describe("Service status"),
      desired_count: z.number().describe("Desired count"),
      running_count: z.number().describe("Running count"),
      task_definition: z.string().describe("Task definition"),
    }),
    execute: async (params, ctx) => {
      const region = resolveRegion(ctx, params.region);
      const payload: Record<string, any> = {
        cluster: params.cluster,
        service: params.service,
        forceNewDeployment: params.force_deployment,
      };
      if (params.desired_count !== undefined) payload.desiredCount = params.desired_count;
      if (params.task_definition) payload.taskDefinition = params.task_definition;
      const data = await ecsFetch(ctx, region, "UpdateService", payload);
      const s = data.service;
      return {
        service_name: s.serviceName,
        service_arn: s.serviceArn,
        status: s.status,
        desired_count: s.desiredCount,
        running_count: s.runningCount,
        task_definition: s.taskDefinition,
      };
    },
  },
};
