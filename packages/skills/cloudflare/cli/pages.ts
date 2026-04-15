import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { cfFetch, cfPost, getAccountId, enc } from "../core/client.ts";

export const pages: Record<string, ActionDefinition> = {
  list_pages_projects: {
    description: "List Pages projects.",
    params: z.object({}),
    returns: z.array(
      z.object({
        name: z.string(),
        subdomain: z.string(),
        production_branch: z.string(),
        latest_deployment: z.any(),
        created_on: z.string(),
      }),
    ),
    execute: async (_params, ctx) => {
      const accountId = await getAccountId(ctx);
      const data = await cfFetch(ctx, `/accounts/${enc(accountId)}/pages/projects`);
      return (Array.isArray(data) ? data : []).map((p: any) => ({
        name: p.name,
        subdomain: p.subdomain ?? "",
        production_branch: p.production_branch ?? "main",
        latest_deployment: p.latest_deployment ?? null,
        created_on: p.created_on ?? "",
      }));
    },
  },

  create_pages_project: {
    description: "Create a Pages project.",
    params: z.object({
      name: z.string().describe("Project name"),
      production_branch: z.string().default("main").describe("Branch for production deploys"),
      build_command: z.string().optional().describe("Build command"),
      build_output_dir: z.string().optional().describe("Output directory"),
    }),
    returns: z.object({
      name: z.string(),
      subdomain: z.string(),
      production_branch: z.string(),
    }),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      const body: any = { name: params.name, production_branch: params.production_branch };
      if (params.build_command || params.build_output_dir) {
        body.build_config = {};
        if (params.build_command) body.build_config.build_command = params.build_command;
        if (params.build_output_dir) body.build_config.destination_dir = params.build_output_dir;
      }
      const p = await cfPost(ctx, `/accounts/${enc(accountId)}/pages/projects`, body);
      return {
        name: p.name,
        subdomain: p.subdomain ?? "",
        production_branch: p.production_branch ?? params.production_branch,
      };
    },
  },

  deploy_pages: {
    description: "Trigger a Pages deployment.",
    params: z.object({
      name: z.string().describe("Project name"),
      branch: z.string().default("main").describe("Branch to deploy"),
    }),
    returns: z.object({
      id: z.string(),
      url: z.string(),
      environment: z.string(),
      status: z.string(),
      created_on: z.string(),
    }),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      const d = await cfPost(
        ctx,
        `/accounts/${enc(accountId)}/pages/projects/${enc(params.name)}/deployments`,
        { branch: params.branch },
      );
      return {
        id: d.id ?? "",
        url: d.url ?? "",
        environment: d.environment ?? "production",
        status: d.latest_stage?.status ?? "active",
        created_on: d.created_on ?? "",
      };
    },
  },
};
