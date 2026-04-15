import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { dPost, dockerUrl, hdrs } from "../core/client.ts";

export const compose: Record<string, ActionDefinition> = {
  compose_up: {
    description: "Start a Compose stack.",
    params: z.object({
      project_dir: z.string().describe("Path to docker-compose.yml"),
      detach: z.boolean().default(true).describe("Run in background"),
      build: z.boolean().default(false).describe("Build images before starting"),
      services: z.string().optional().describe("Comma-separated list of services"),
    }),
    returns: z.array(z.object({ name: z.string().describe("Service name"), status: z.string().describe("Service status") })),
    execute: async (params, ctx) => {
      const body: any = { project_dir: params.project_dir, action: "up", detach: params.detach, build: params.build };
      if (params.services) body.services = params.services.split(",").map((s) => s.trim());
      const data = await dPost(ctx, `/compose`, body);
      return Array.isArray(data) ? data : [{ name: "stack", status: "started" }];
    },
  },

  compose_down: {
    description: "Stop and remove a Compose stack.",
    params: z.object({
      project_dir: z.string().describe("Path to docker-compose.yml"),
      volumes: z.boolean().default(false).describe("Remove named volumes"),
      remove_orphans: z.boolean().default(false).describe("Remove orphan containers"),
    }),
    returns: z.array(z.object({ name: z.string().describe("Service name"), status: z.string().describe("Service status") })),
    execute: async (params, ctx) => {
      const body: any = { project_dir: params.project_dir, action: "down", volumes: params.volumes, remove_orphans: params.remove_orphans };
      const data = await dPost(ctx, `/compose`, body);
      return Array.isArray(data) ? data : [{ name: "stack", status: "stopped" }];
    },
  },

  compose_ps: {
    description: "List services in a Compose stack.",
    params: z.object({ project_dir: z.string().describe("Path to docker-compose.yml") }),
    returns: z.array(z.object({
      name: z.string().describe("Service name"),
      status: z.string().describe("Service status"),
      ports: z.any().describe("Exposed ports"),
    })),
    execute: async (params, ctx) => {
      const data = await dPost(ctx, `/compose`, { project_dir: params.project_dir, action: "ps" });
      return Array.isArray(data) ? data : [];
    },
  },

  compose_logs: {
    description: "Get logs from a Compose stack or specific service.",
    params: z.object({
      project_dir: z.string().describe("Path to docker-compose.yml"),
      service: z.string().optional().describe("Specific service name"),
      tail: z.number().default(200).describe("Number of lines from the end"),
      follow: z.boolean().default(false).describe("Stream logs in real time"),
    }),
    returns: z.string().describe("Log output as text"),
    execute: async (params, ctx) => {
      const body: any = { project_dir: params.project_dir, action: "logs", tail: params.tail };
      if (params.service) body.service = params.service;
      const res = await ctx.fetch(dockerUrl(ctx.credentials.host, `/compose`), {
        method: "POST",
        headers: hdrs(),
        body: JSON.stringify(body),
      });
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      return res.text();
    },
  },

  compose_build: {
    description: "Build images for a Compose stack.",
    params: z.object({
      project_dir: z.string().describe("Path to docker-compose.yml"),
      no_cache: z.boolean().default(false).describe("Build without cache"),
      service: z.string().optional().describe("Specific service to build"),
    }),
    returns: z.string().describe("Build output per service"),
    execute: async (params, ctx) => {
      const body: any = { project_dir: params.project_dir, action: "build", no_cache: params.no_cache };
      if (params.service) body.service = params.service;
      const res = await ctx.fetch(dockerUrl(ctx.credentials.host, `/compose`), {
        method: "POST",
        headers: hdrs(),
        body: JSON.stringify(body),
      });
      if (!res.ok) throw new Error(`Docker API ${res.status}: ${await res.text()}`);
      return res.text();
    },
  },
};
