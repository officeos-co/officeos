import { z } from "@harro/skill-sdk";
import type { ActionDefinition } from "@harro/skill-sdk";
import { ghFetch, ghJsonHeaders, enc, repoUrl } from "../core/client.ts";

const ownerRepo = {
  owner: z.string().describe("Repository owner"),
  repo: z.string().describe("Repository name"),
};

export const workflows: Record<string, ActionDefinition> = {
  list_workflows: {
    description: "List GitHub Actions workflows in a repository.",
    params: z.object({ ...ownerRepo }),
    returns: z.array(z.object({
      id: z.number().describe("Workflow ID"),
      name: z.string().describe("Workflow name"),
      path: z.string().describe("Workflow file path"),
      state: z.string().describe("Workflow state"),
    })),
    execute: async (params, ctx) => {
      const data = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/actions/workflows`);
      return (data.workflows ?? []).map((w: any) => ({
        id: w.id,
        name: w.name,
        path: w.path,
        state: w.state,
      }));
    },
  },

  list_workflow_runs: {
    description: "List recent runs for a workflow.",
    params: z.object({
      ...ownerRepo,
      workflow_id: z.union([z.number(), z.string()]).describe("Workflow ID or filename (e.g. ci.yml)"),
      status: z.enum(["completed", "action_required", "cancelled", "failure", "neutral", "skipped", "stale", "success", "timed_out", "in_progress", "queued", "requested", "waiting", "pending"]).optional().describe("Filter by status"),
      per_page: z.number().min(1).max(100).default(10).describe("Results per page"),
    }),
    returns: z.array(z.object({
      id: z.number().describe("Run ID"),
      name: z.string().describe("Run name"),
      status: z.string().nullable().describe("Run status"),
      conclusion: z.string().nullable().describe("Run conclusion"),
      head_branch: z.string().nullable().describe("Head branch"),
      html_url: z.string().describe("Run URL"),
      created_at: z.string().describe("Creation date"),
    })),
    execute: async (params, ctx) => {
      let url = `${repoUrl(params.owner, params.repo)}/actions/workflows/${enc(String(params.workflow_id))}/runs?per_page=${params.per_page}`;
      if (params.status) url += `&status=${params.status}`;
      const data = await ghFetch(ctx, url);
      return (data.workflow_runs ?? []).map((r: any) => ({
        id: r.id,
        name: r.name,
        status: r.status,
        conclusion: r.conclusion,
        head_branch: r.head_branch,
        html_url: r.html_url,
        created_at: r.created_at,
      }));
    },
  },

  get_workflow_run: {
    description: "Get details of a specific workflow run.",
    params: z.object({
      ...ownerRepo,
      run_id: z.number().describe("Workflow run ID"),
    }),
    returns: z.object({
      id: z.number().describe("Run ID"),
      name: z.string().describe("Run name"),
      status: z.string().nullable().describe("Run status"),
      conclusion: z.string().nullable().describe("Run conclusion"),
      head_branch: z.string().nullable().describe("Head branch"),
      head_sha: z.string().describe("Head commit SHA"),
      html_url: z.string().describe("Run URL"),
      created_at: z.string().describe("Creation date"),
      updated_at: z.string().describe("Last update date"),
      run_attempt: z.number().describe("Run attempt number"),
    }),
    execute: async (params, ctx) => {
      const r = await ghFetch(ctx, `${repoUrl(params.owner, params.repo)}/actions/runs/${params.run_id}`);
      return {
        id: r.id,
        name: r.name,
        status: r.status,
        conclusion: r.conclusion,
        head_branch: r.head_branch,
        head_sha: r.head_sha,
        html_url: r.html_url,
        created_at: r.created_at,
        updated_at: r.updated_at,
        run_attempt: r.run_attempt,
      };
    },
  },

  trigger_workflow: {
    description: "Trigger a workflow_dispatch event to run a workflow manually.",
    params: z.object({
      ...ownerRepo,
      workflow_id: z.union([z.number(), z.string()]).describe("Workflow ID or filename (e.g. deploy.yml)"),
      ref: z.string().describe("Branch or tag to run the workflow on"),
      inputs: z.record(z.string()).optional().describe("Workflow input parameters"),
    }),
    returns: z.object({ triggered: z.boolean().describe("Whether the trigger succeeded") }),
    execute: async (params, ctx) => {
      const res = await ctx.fetch(
        `${repoUrl(params.owner, params.repo)}/actions/workflows/${enc(String(params.workflow_id))}/dispatches`,
        {
          method: "POST",
          headers: ghJsonHeaders(ctx.credentials.token),
          body: JSON.stringify({ ref: params.ref, inputs: params.inputs }),
        },
      );
      if (!res.ok) {
        const text = await res.text();
        throw new Error(`GitHub API ${res.status}: ${text}`);
      }
      return { triggered: true };
    },
  },
};
