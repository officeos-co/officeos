import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, vPost, vDelete, enc } from "../core/client.ts";

export const aliases: Record<string, ActionDefinition> = {
  list_aliases: {
    description: "List aliases, optionally filtered by project.",
    params: z.object({
      project_id: z.string().optional().describe("Filter by project"),
      limit: z.number().min(1).max(100).default(20).describe("Results per page"),
    }),
    returns: z.array(
      z.object({
        uid: z.string(),
        alias: z.string(),
        deploymentId: z.string().nullable(),
        createdAt: z.number(),
      }),
    ),
    execute: async (params, ctx) => {
      const qp = new URLSearchParams();
      qp.set("limit", String(params.limit));
      if (params.project_id) qp.set("projectId", params.project_id);
      const data = await vFetch(ctx, `${VERCEL_API}/v4/aliases?${qp.toString()}`);
      return (data.aliases ?? []).map((a: any) => ({
        uid: a.uid,
        alias: a.alias,
        deploymentId: a.deploymentId ?? null,
        createdAt: a.createdAt ?? a.created,
      }));
    },
  },

  set_alias: {
    description: "Assign an alias to a deployment.",
    params: z.object({
      deployment_id: z.string().describe("Deployment ID"),
      alias: z.string().describe("Alias domain to assign"),
    }),
    returns: z.object({ uid: z.string(), alias: z.string() }),
    execute: async (params, ctx) => {
      const r = await vPost(
        ctx,
        `${VERCEL_API}/v2/deployments/${enc(params.deployment_id)}/aliases`,
        { alias: params.alias },
      );
      return { uid: r.uid, alias: r.alias };
    },
  },

  remove_alias: {
    description: "Remove an alias.",
    params: z.object({
      alias_id: z.string().describe("Alias ID"),
    }),
    returns: z.object({ deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await vDelete(ctx, `${VERCEL_API}/v2/aliases/${enc(params.alias_id)}`);
      return { deleted: true };
    },
  },

  list_secrets: {
    description: "List secrets for the authenticated user or team.",
    params: z.object({
      limit: z.number().min(1).max(100).default(20).describe("Results per page"),
    }),
    returns: z.array(
      z.object({ uid: z.string(), name: z.string(), createdAt: z.number() }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(ctx, `${VERCEL_API}/v3/secrets?limit=${params.limit}`);
      return (data.secrets ?? []).map((s: any) => ({
        uid: s.uid,
        name: s.name,
        createdAt: s.createdAt ?? s.created,
      }));
    },
  },

  create_secret: {
    description: "Create a new secret.",
    params: z.object({
      name: z.string().describe("Secret name"),
      value: z.string().describe("Secret value"),
    }),
    returns: z.object({ uid: z.string(), name: z.string(), createdAt: z.number() }),
    execute: async (params, ctx) => {
      const r = await vPost(ctx, `${VERCEL_API}/v2/secrets`, {
        name: params.name,
        value: params.value,
      });
      return { uid: r.uid, name: r.name, createdAt: r.createdAt ?? r.created };
    },
  },

  delete_secret: {
    description: "Delete a secret by name or ID.",
    params: z.object({
      name: z.string().describe("Secret name or ID"),
    }),
    returns: z.object({ deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await vDelete(ctx, `${VERCEL_API}/v2/secrets/${enc(params.name)}`);
      return { deleted: true };
    },
  },
};
