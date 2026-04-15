import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { CF_API, cfFetch, cfPost, cfDelete, getAccountId, enc } from "../core/client.ts";

export const workers: Record<string, ActionDefinition> = {
  list_workers: {
    description: "List Workers scripts.",
    params: z.object({}),
    returns: z.array(
      z.object({
        id: z.string(),
        name: z.string(),
        created_on: z.string(),
        modified_on: z.string(),
        routes: z.array(z.string()),
      }),
    ),
    execute: async (_params, ctx) => {
      const accountId = await getAccountId(ctx);
      const data = await cfFetch(ctx, `/accounts/${enc(accountId)}/workers/scripts`);
      return (Array.isArray(data) ? data : []).map((w: any) => ({
        id: w.id ?? w.name ?? "",
        name: w.id ?? w.name ?? "",
        created_on: w.created_on ?? "",
        modified_on: w.modified_on ?? "",
        routes: [],
      }));
    },
  },

  get_worker: {
    description: "Get a Worker script and its metadata.",
    params: z.object({
      name: z.string().describe("Worker name"),
    }),
    returns: z.object({
      name: z.string(),
      script: z.string(),
      routes: z.array(z.string()),
      bindings: z.any(),
      created_on: z.string(),
      modified_on: z.string(),
    }),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      const res = await ctx.fetch(
        `${CF_API}/accounts/${enc(accountId)}/workers/scripts/${enc(params.name)}`,
        { headers: { Authorization: `Bearer ${ctx.credentials.api_token}` } },
      );
      if (!res.ok) throw new Error(`Cloudflare API ${res.status}: ${await res.text()}`);
      const script = await res.text();
      let bindings: any = {};
      let created_on = "";
      let modified_on = "";
      try {
        const settings = await cfFetch(
          ctx,
          `/accounts/${enc(accountId)}/workers/scripts/${enc(params.name)}/settings`,
        );
        bindings = settings?.bindings ?? {};
        created_on = settings?.created_on ?? "";
        modified_on = settings?.modified_on ?? "";
      } catch { /* settings may not be available */ }
      return { name: params.name, script, routes: [], bindings, created_on, modified_on };
    },
  },

  deploy_worker: {
    description: "Deploy a Worker script.",
    params: z.object({
      name: z.string().describe("Worker name"),
      script: z.string().describe("Worker script content"),
      routes: z.array(z.string()).optional().describe("Route patterns (e.g. example.com/*)"),
      bindings: z.string().optional().describe("JSON object of bindings (KV, R2, etc.)"),
    }),
    returns: z.object({
      name: z.string(),
      tag: z.string(),
      routes: z.array(z.string()),
      size: z.number().describe("Script size in bytes"),
    }),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      const res = await ctx.fetch(
        `${CF_API}/accounts/${enc(accountId)}/workers/scripts/${enc(params.name)}`,
        {
          method: "PUT",
          headers: {
            Authorization: `Bearer ${ctx.credentials.api_token}`,
            "Content-Type": "application/javascript",
          },
          body: params.script,
        },
      );
      if (!res.ok) throw new Error(`Cloudflare API ${res.status}: ${await res.text()}`);
      const json = await res.json();
      return {
        name: params.name,
        tag: json.result?.tag ?? "",
        routes: params.routes ?? [],
        size: params.script.length,
      };
    },
  },

  delete_worker: {
    description: "Delete a Worker script.",
    params: z.object({
      name: z.string().describe("Worker name"),
    }),
    returns: z.object({ name: z.string() }),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      await cfDelete(ctx, `/accounts/${enc(accountId)}/workers/scripts/${enc(params.name)}`);
      return { name: params.name };
    },
  },

  tail_worker: {
    description: "Get tail logs from a Worker.",
    params: z.object({
      name: z.string().describe("Worker name"),
      status: z.string().optional().describe("Filter: ok or error"),
      sampling_rate: z.number().default(1.0).describe("Sample rate (0.0-1.0)"),
    }),
    returns: z.any().describe("Stream of log entries with timestamp, outcome, event, logs, exceptions"),
    execute: async (params, ctx) => {
      const accountId = await getAccountId(ctx);
      const body: any = {};
      if (params.status) body.filters = [{ outcome: [params.status] }];
      if (params.sampling_rate < 1.0) body.sampling_rate = params.sampling_rate;
      return cfPost(
        ctx,
        `/accounts/${enc(accountId)}/workers/scripts/${enc(params.name)}/tails`,
        body,
      );
    },
  },
};
