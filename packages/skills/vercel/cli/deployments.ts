import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, vPost, vPatch, enc } from "../core/client.ts";

export const deployments: Record<string, ActionDefinition> = {
  list_deployments: {
    description: "List deployments, optionally filtered by project and state.",
    params: z.object({
      project_id: z.string().optional().describe("Filter by project ID"),
      state: z
        .enum(["BUILDING", "READY", "ERROR", "QUEUED", "CANCELED"])
        .optional()
        .describe("Filter by deployment state"),
      limit: z.number().min(1).max(100).default(20).describe("Results per page"),
      team_id: z.string().optional().describe("Team ID to scope the request"),
    }),
    returns: z.array(
      z.object({
        uid: z.string(),
        name: z.string(),
        url: z.string().nullable(),
        state: z.string().nullable(),
        createdAt: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const qp = new URLSearchParams();
      qp.set("limit", String(params.limit));
      if (params.project_id) qp.set("projectId", params.project_id);
      if (params.state) qp.set("state", params.state);
      if (params.team_id) qp.set("teamId", params.team_id);
      const data = await vFetch(ctx, `${VERCEL_API}/v6/deployments?${qp.toString()}`);
      return (data.deployments ?? []).map((d: any) => ({
        uid: d.uid,
        name: d.name,
        url: d.url ?? null,
        state: d.state ?? null,
        createdAt: d.createdAt ?? d.created,
      }));
    },
  },

  get_deployment: {
    description: "Get detailed information about a single deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      url: z.string().nullable(),
      state: z.string().nullable(),
      readyState: z.string().nullable(),
      createdAt: z.number(),
      buildingAt: z.number().nullable(),
      ready: z.number().nullable(),
      meta: z.any().nullable(),
    }),
    execute: async (params, ctx) => {
      const r = await vFetch(ctx, `${VERCEL_API}/v13/deployments/${enc(params.deployment_id)}`);
      return {
        id: r.id,
        name: r.name,
        url: r.url ?? null,
        state: r.state ?? null,
        readyState: r.readyState ?? null,
        createdAt: r.createdAt ?? r.created,
        buildingAt: r.buildingAt ?? null,
        ready: r.ready ?? null,
        meta: r.meta ?? null,
      };
    },
  },

  create_deployment: {
    description: "Create a new deployment.",
    params: z.object({
      name: z.string().describe("Deployment name"),
      project_id: z.string().optional().describe("Project to deploy"),
      target: z.enum(["production", "preview"]).optional().describe("Deployment target"),
      git_source: z
        .object({
          type: z.string().describe("Git provider"),
          ref: z.string().describe("Branch or tag"),
          repoId: z.string().describe("Repository ID"),
        })
        .optional()
        .describe("Git source for the deployment"),
    }),
    returns: z.object({
      id: z.string(),
      url: z.string().nullable(),
      readyState: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: any = { name: params.name };
      if (params.project_id) body.project = params.project_id;
      if (params.target) body.target = params.target;
      if (params.git_source) body.gitSource = params.git_source;
      const r = await vPost(ctx, `${VERCEL_API}/v13/deployments`, body);
      return { id: r.id, url: r.url ?? null, readyState: r.readyState ?? null };
    },
  },

  cancel_deployment: {
    description: "Cancel an in-progress deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
    }),
    returns: z.object({ id: z.string(), state: z.string().nullable() }),
    execute: async (params, ctx) => {
      const r = await vPatch(
        ctx,
        `${VERCEL_API}/v12/deployments/${enc(params.deployment_id)}/cancel`,
        {},
      );
      return { id: r.id ?? params.deployment_id, state: r.state ?? r.readyState ?? null };
    },
  },

  redeploy: {
    description: "Redeploy an existing deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID to redeploy"),
      target: z.enum(["production", "preview"]).optional().describe("Deployment target"),
    }),
    returns: z.object({
      id: z.string(),
      url: z.string().nullable(),
      readyState: z.string().nullable(),
    }),
    execute: async (params, ctx) => {
      const body: any = {};
      if (params.target) body.target = params.target;
      const r = await vPost(
        ctx,
        `${VERCEL_API}/v13/deployments?forceNew=1&deploymentId=${enc(params.deployment_id)}`,
        body,
      );
      return { id: r.id, url: r.url ?? null, readyState: r.readyState ?? null };
    },
  },

  list_checks: {
    description: "List checks for a deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
    }),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        status: z.string(),
        conclusion: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(
        ctx,
        `${VERCEL_API}/v1/deployments/${enc(params.deployment_id)}/checks`,
      );
      return (data.checks ?? []).map((c: any) => ({
        id: c.id,
        name: c.name,
        status: c.status,
        conclusion: c.conclusion ?? null,
      }));
    },
  },

  get_check: {
    description: "Get a single check for a deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
      check_id: z.string().describe("Check ID"),
    }),
    returns: z.object({
      id: z.string(),
      name: z.string(),
      status: z.string(),
      conclusion: z.string().nullable(),
      detailsUrl: z.string().nullable(),
      completedAt: z.number().nullable(),
    }),
    execute: async (params, ctx) => {
      const c = await vFetch(
        ctx,
        `${VERCEL_API}/v1/deployments/${enc(params.deployment_id)}/checks/${enc(params.check_id)}`,
      );
      return {
        id: c.id,
        name: c.name,
        status: c.status,
        conclusion: c.conclusion ?? null,
        detailsUrl: c.detailsUrl ?? null,
        completedAt: c.completedAt ?? null,
      };
    },
  },
};
