import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { VERCEL_API, vFetch, vPost, vDelete, enc } from "../core/client.ts";

export const domains: Record<string, ActionDefinition> = {
  list_domains: {
    description: "List domains for a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
    }),
    returns: z.array(
      z.object({
        name: z.string(),
        apexName: z.string(),
        verified: z.boolean(),
        gitBranch: z.string().nullable(),
      }),
    ),
    execute: async (params, ctx) => {
      const data = await vFetch(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/domains`,
      );
      return (data.domains ?? []).map((d: any) => ({
        name: d.name,
        apexName: d.apexName,
        verified: d.verified ?? false,
        gitBranch: d.gitBranch ?? null,
      }));
    },
  },

  add_domain: {
    description: "Add a domain to a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      domain: z.string().describe("Domain name to add"),
    }),
    returns: z.object({
      name: z.string(),
      apexName: z.string(),
      verified: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const r = await vPost(
        ctx,
        `${VERCEL_API}/v10/projects/${enc(params.project_id)}/domains`,
        { name: params.domain },
      );
      return { name: r.name, apexName: r.apexName, verified: r.verified ?? false };
    },
  },

  remove_domain: {
    description: "Remove a domain from a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      domain: z.string().describe("Domain name to remove"),
    }),
    returns: z.object({ deleted: z.boolean() }),
    execute: async (params, ctx) => {
      await vDelete(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/domains/${enc(params.domain)}`,
      );
      return { deleted: true };
    },
  },

  get_domain_config: {
    description: "Get DNS configuration status for a domain.",
    params: z.object({
      domain: z.string().describe("Domain name"),
    }),
    returns: z.object({
      configuredBy: z.string().nullable(),
      acceptedChallenges: z.array(z.string()),
      misconfigured: z.boolean(),
    }),
    execute: async (params, ctx) => {
      const r = await vFetch(ctx, `${VERCEL_API}/v6/domains/${enc(params.domain)}/config`);
      return {
        configuredBy: r.configuredBy ?? null,
        acceptedChallenges: r.acceptedChallenges ?? [],
        misconfigured: r.misconfigured ?? false,
      };
    },
  },

  verify_domain: {
    description: "Verify a domain for a project.",
    params: z.object({
      project_id: z.string().describe("Project ID or name"),
      domain: z.string().describe("Domain to verify"),
    }),
    returns: z.object({ name: z.string(), verified: z.boolean() }),
    execute: async (params, ctx) => {
      const r = await vPost(
        ctx,
        `${VERCEL_API}/v9/projects/${enc(params.project_id)}/domains/${enc(params.domain)}/verify`,
        {},
      );
      return { name: r.name ?? params.domain, verified: r.verified ?? false };
    },
  },
};
