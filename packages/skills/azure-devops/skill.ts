import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

const ADO_API_VERSION = "7.1";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function adoBase(org: string) {
  return `https://dev.azure.com/${encodeURIComponent(org)}`;
}

function adoHeaders(token: string) {
  return {
    Authorization: "Basic " + btoa(`:${token}`),
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

function adoJsonHeaders(token: string) {
  return {
    ...adoHeaders(token),
    "Content-Type": "application/json",
  };
}

function adoPatchHeaders(token: string) {
  return {
    ...adoHeaders(token),
    "Content-Type": "application/json-patch+json",
  };
}

function apiVer(extra?: Record<string, string>) {
  return new URLSearchParams({
    "api-version": ADO_API_VERSION,
    ...extra,
  }).toString();
}

async function adoGet(ctx: Ctx, path: string, params?: Record<string, string>) {
  const base = adoBase(ctx.credentials.organization);
  const qs = "?" + apiVer(params);
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    headers: adoHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Azure DevOps API ${res.status}: ${body}`);
  }
  return res.json();
}

async function adoPost(ctx: Ctx, path: string, body: unknown, params?: Record<string, string>) {
  const base = adoBase(ctx.credentials.organization);
  const qs = "?" + apiVer(params);
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    method: "POST",
    headers: adoJsonHeaders(ctx.credentials.token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Azure DevOps API ${res.status}: ${text}`);
  }
  return res.json();
}

async function adoPatch(ctx: Ctx, path: string, body: unknown, params?: Record<string, string>) {
  const base = adoBase(ctx.credentials.organization);
  const qs = "?" + apiVer(params);
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    method: "PATCH",
    headers: adoPatchHeaders(ctx.credentials.token),
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Azure DevOps API ${res.status}: ${text}`);
  }
  return res.json();
}

function enc(s: string | number) {
  return encodeURIComponent(String(s));
}

function normalizeBranch(branch: string) {
  return branch.startsWith("refs/") ? branch : `refs/heads/${branch}`;
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Projects ──────────────────────────────────────────────────────────

    list_projects: {
      description: "List all projects in the organization.",
      params: z.object({
        top: z.number().min(1).max(500).default(100).describe("Max number of results"),
        skip: z.number().min(0).default(0).describe("Number of results to skip"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          description: z.string().nullable(),
          state: z.string(),
          visibility: z.string(),
          last_update_time: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await adoGet(ctx, "/_apis/projects", {
          $top: String(params.top),
          $skip: String(params.skip),
        });
        return (data.value ?? []).map((p: any) => ({
          id: p.id,
          name: p.name,
          description: p.description ?? null,
          state: p.state,
          visibility: p.visibility,
          last_update_time: p.lastUpdateTime ?? null,
        }));
      },
    },

    get_project: {
      description: "Get details for a specific project.",
      params: z.object({
        project: z.string().describe("Project name or ID"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        description: z.string().nullable(),
        state: z.string(),
        visibility: z.string(),
        default_team_name: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const p = await adoGet(ctx, `/_apis/projects/${enc(params.project)}`);
        return {
          id: p.id,
          name: p.name,
          description: p.description ?? null,
          state: p.state,
          visibility: p.visibility,
          default_team_name: p.defaultTeam?.name ?? null,
        };
      },
    },

    // ── Repositories ──────────────────────────────────────────────────────

    list_repos: {
      description: "List Git repositories in a project.",
      params: z.object({
        project: z.string().describe("Project name"),
      }),
      returns: z.array(
        z.object({
          id: z.string(),
          name: z.string(),
          default_branch: z.string().nullable(),
          remote_url: z.string(),
          size: z.number(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await adoGet(ctx, `/${enc(params.project)}/_apis/git/repositories`);
        return (data.value ?? []).map((r: any) => ({
          id: r.id,
          name: r.name,
          default_branch: r.defaultBranch ?? null,
          remote_url: r.remoteUrl,
          size: r.size ?? 0,
        }));
      },
    },

    get_repo: {
      description: "Get details for a specific Git repository.",
      params: z.object({
        project: z.string().describe("Project name"),
        repo: z.string().describe("Repository name or ID"),
      }),
      returns: z.object({
        id: z.string(),
        name: z.string(),
        default_branch: z.string().nullable(),
        remote_url: z.string(),
        size: z.number(),
      }),
      execute: async (params, ctx) => {
        const r = await adoGet(
          ctx,
          `/${enc(params.project)}/_apis/git/repositories/${enc(params.repo)}`,
        );
        return {
          id: r.id,
          name: r.name,
          default_branch: r.defaultBranch ?? null,
          remote_url: r.remoteUrl,
          size: r.size ?? 0,
        };
      },
    },

    // ── Pull Requests ─────────────────────────────────────────────────────

    list_prs: {
      description: "List pull requests for a repository.",
      params: z.object({
        project: z.string().describe("Project name"),
        repo: z.string().describe("Repository name"),
        status: z
          .enum(["active", "abandoned", "completed", "all"])
          .default("active")
          .describe("PR status filter"),
        top: z.number().min(1).max(200).default(25).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          pull_request_id: z.number(),
          title: z.string(),
          status: z.string(),
          source_branch: z.string(),
          target_branch: z.string(),
          created_by: z.string(),
          creation_date: z.string(),
          url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await adoGet(
          ctx,
          `/${enc(params.project)}/_apis/git/repositories/${enc(params.repo)}/pullrequests`,
          { "searchCriteria.status": params.status, $top: String(params.top) },
        );
        return (data.value ?? []).map((pr: any) => ({
          pull_request_id: pr.pullRequestId,
          title: pr.title,
          status: pr.status,
          source_branch: pr.sourceRefName,
          target_branch: pr.targetRefName,
          created_by: pr.createdBy?.displayName ?? "",
          creation_date: pr.creationDate,
          url: pr.url,
        }));
      },
    },

    create_pr: {
      description: "Create a new pull request.",
      params: z.object({
        project: z.string().describe("Project name"),
        repo: z.string().describe("Repository name"),
        source_branch: z.string().describe("Source branch (refs/heads/… or short name)"),
        target_branch: z.string().describe("Target branch (refs/heads/… or short name)"),
        title: z.string().describe("PR title"),
        description: z.string().optional().describe("PR description"),
        auto_complete: z.boolean().default(false).describe("Auto-complete when policies pass"),
      }),
      returns: z.object({
        pull_request_id: z.number(),
        title: z.string(),
        status: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          sourceRefName: normalizeBranch(params.source_branch),
          targetRefName: normalizeBranch(params.target_branch),
          title: params.title,
        };
        if (params.description) body.description = params.description;
        const pr = await adoPost(
          ctx,
          `/${enc(params.project)}/_apis/git/repositories/${enc(params.repo)}/pullrequests`,
          body,
        );
        return {
          pull_request_id: pr.pullRequestId,
          title: pr.title,
          status: pr.status,
          url: pr.url,
        };
      },
    },

    get_pr: {
      description: "Get details for a specific pull request.",
      params: z.object({
        project: z.string().describe("Project name"),
        repo: z.string().describe("Repository name"),
        pr_id: z.number().describe("Pull request ID"),
      }),
      returns: z.object({
        pull_request_id: z.number(),
        title: z.string(),
        description: z.string().nullable(),
        status: z.string(),
        source_branch: z.string(),
        target_branch: z.string(),
        created_by: z.string(),
        creation_date: z.string(),
        merge_status: z.string().nullable(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const pr = await adoGet(
          ctx,
          `/${enc(params.project)}/_apis/git/repositories/${enc(params.repo)}/pullrequests/${params.pr_id}`,
        );
        return {
          pull_request_id: pr.pullRequestId,
          title: pr.title,
          description: pr.description ?? null,
          status: pr.status,
          source_branch: pr.sourceRefName,
          target_branch: pr.targetRefName,
          created_by: pr.createdBy?.displayName ?? "",
          creation_date: pr.creationDate,
          merge_status: pr.mergeStatus ?? null,
          url: pr.url,
        };
      },
    },

    // ── Work Items ────────────────────────────────────────────────────────

    list_work_items: {
      description: "Query work items using WIQL (Work Item Query Language).",
      params: z.object({
        project: z.string().describe("Project name"),
        wiql: z
          .string()
          .describe(
            "WIQL query, e.g. \"SELECT [System.Id],[System.Title] FROM WorkItems WHERE [System.WorkItemType]='Bug'\"",
          ),
        top: z.number().min(1).max(200).default(100).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          title: z.string().nullable(),
          state: z.string().nullable(),
          work_item_type: z.string().nullable(),
          assigned_to: z.string().nullable(),
          url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const queryResult = await adoPost(
          ctx,
          `/${enc(params.project)}/_apis/wit/wiql`,
          { query: params.wiql },
          { $top: String(params.top) },
        );
        const ids: number[] = (queryResult.workItems ?? []).map((wi: any) => wi.id);
        if (ids.length === 0) return [];
        const batchIds = ids.slice(0, params.top).join(",");
        const fields = "System.Id,System.Title,System.State,System.WorkItemType,System.AssignedTo";
        const data = await adoGet(ctx, "/_apis/wit/workitems", {
          ids: batchIds,
          fields,
        });
        return (data.value ?? []).map((wi: any) => ({
          id: wi.id,
          title: wi.fields?.["System.Title"] ?? null,
          state: wi.fields?.["System.State"] ?? null,
          work_item_type: wi.fields?.["System.WorkItemType"] ?? null,
          assigned_to: wi.fields?.["System.AssignedTo"]?.displayName ?? null,
          url: wi.url,
        }));
      },
    },

    create_work_item: {
      description: "Create a new work item (Bug, Task, User Story, etc.).",
      params: z.object({
        project: z.string().describe("Project name"),
        type: z.string().describe("Work item type, e.g. Bug, Task, User Story, Feature"),
        title: z.string().describe("Work item title"),
        description: z.string().optional().describe("Work item description or repro steps"),
        assigned_to: z.string().optional().describe("Assignee display name or email"),
        area_path: z.string().optional().describe("Area path override"),
      }),
      returns: z.object({ id: z.number(), title: z.string(), url: z.string() }),
      execute: async (params, ctx) => {
        const ops: Array<{ op: string; path: string; value: unknown }> = [
          { op: "add", path: "/fields/System.Title", value: params.title },
        ];
        if (params.description)
          ops.push({ op: "add", path: "/fields/System.Description", value: params.description });
        if (params.assigned_to)
          ops.push({ op: "add", path: "/fields/System.AssignedTo", value: params.assigned_to });
        if (params.area_path)
          ops.push({ op: "add", path: "/fields/System.AreaPath", value: params.area_path });
        const wi = await adoPatch(
          ctx,
          `/${enc(params.project)}/_apis/wit/workitems/${enc("$" + params.type)}`,
          ops,
        );
        return {
          id: wi.id,
          title: wi.fields?.["System.Title"] ?? params.title,
          url: wi.url,
        };
      },
    },

    get_work_item: {
      description: "Get a single work item by ID.",
      params: z.object({
        id: z.number().describe("Work item ID"),
      }),
      returns: z.object({
        id: z.number(),
        title: z.string().nullable(),
        state: z.string().nullable(),
        work_item_type: z.string().nullable(),
        assigned_to: z.string().nullable(),
        description: z.string().nullable(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const wi = await adoGet(ctx, `/_apis/wit/workitems/${params.id}`);
        return {
          id: wi.id,
          title: wi.fields?.["System.Title"] ?? null,
          state: wi.fields?.["System.State"] ?? null,
          work_item_type: wi.fields?.["System.WorkItemType"] ?? null,
          assigned_to: wi.fields?.["System.AssignedTo"]?.displayName ?? null,
          description: wi.fields?.["System.Description"] ?? null,
          url: wi.url,
        };
      },
    },

    // ── Builds ────────────────────────────────────────────────────────────

    list_builds: {
      description: "List builds for a project.",
      params: z.object({
        project: z.string().describe("Project name"),
        definition_id: z.number().optional().describe("Filter by build definition ID"),
        branch: z.string().optional().describe("Filter by branch name"),
        result: z
          .enum(["succeeded", "failed", "canceled", "partiallySucceeded", "none"])
          .optional()
          .describe("Filter by build result"),
        top: z.number().min(1).max(500).default(25).describe("Max results"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          build_number: z.string(),
          status: z.string(),
          result: z.string().nullable(),
          definition_name: z.string(),
          source_branch: z.string(),
          requested_by: z.string(),
          start_time: z.string().nullable(),
          finish_time: z.string().nullable(),
          url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = { $top: String(params.top) };
        if (params.definition_id) q.definitions = String(params.definition_id);
        if (params.branch) q.branchName = normalizeBranch(params.branch);
        if (params.result) q.resultFilter = params.result;
        const data = await adoGet(ctx, `/${enc(params.project)}/_apis/build/builds`, q);
        return (data.value ?? []).map((b: any) => ({
          id: b.id,
          build_number: b.buildNumber,
          status: b.status,
          result: b.result ?? null,
          definition_name: b.definition?.name ?? "",
          source_branch: b.sourceBranch,
          requested_by: b.requestedBy?.displayName ?? "",
          start_time: b.startTime ?? null,
          finish_time: b.finishTime ?? null,
          url: b._links?.web?.href ?? b.url,
        }));
      },
    },

    queue_build: {
      description: "Queue (trigger) a build by definition ID.",
      params: z.object({
        project: z.string().describe("Project name"),
        definition_id: z.number().describe("Build definition ID"),
        branch: z.string().optional().describe("Branch to build (refs/heads/… or short name)"),
        parameters: z.string().optional().describe("JSON string of parameter overrides"),
      }),
      returns: z.object({ id: z.number(), build_number: z.string(), status: z.string(), url: z.string() }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {
          definition: { id: params.definition_id },
        };
        if (params.branch) body.sourceBranch = normalizeBranch(params.branch);
        if (params.parameters) body.parameters = params.parameters;
        const b = await adoPost(ctx, `/${enc(params.project)}/_apis/build/builds`, body);
        return {
          id: b.id,
          build_number: b.buildNumber,
          status: b.status,
          url: b._links?.web?.href ?? b.url,
        };
      },
    },

    // ── Pipelines ─────────────────────────────────────────────────────────

    list_pipelines: {
      description: "List YAML pipelines for a project.",
      params: z.object({
        project: z.string().describe("Project name"),
        top: z.number().min(1).max(500).default(100).describe("Max results"),
      }),
      returns: z.array(
        z.object({ id: z.number(), name: z.string(), folder: z.string(), revision: z.number() }),
      ),
      execute: async (params, ctx) => {
        const data = await adoGet(ctx, `/${enc(params.project)}/_apis/pipelines`, {
          $top: String(params.top),
        });
        return (data.value ?? []).map((p: any) => ({
          id: p.id,
          name: p.name,
          folder: p.folder ?? "\\",
          revision: p.revision ?? 1,
        }));
      },
    },

    run_pipeline: {
      description: "Run a YAML pipeline.",
      params: z.object({
        project: z.string().describe("Project name"),
        pipeline_id: z.number().describe("Pipeline ID"),
        branch: z.string().optional().describe("Branch to run on (refs/heads/… or short name)"),
        variables: z
          .string()
          .optional()
          .describe("JSON object of variable overrides, e.g. {\"MY_VAR\":{\"value\":\"foo\"}}"),
      }),
      returns: z.object({
        id: z.number(),
        state: z.string(),
        result: z.string().nullable(),
        pipeline_name: z.string(),
        url: z.string(),
      }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = {};
        if (params.branch) body.resources = { repositories: { self: { refName: normalizeBranch(params.branch) } } };
        if (params.variables) body.variables = JSON.parse(params.variables);
        const run = await adoPost(
          ctx,
          `/${enc(params.project)}/_apis/pipelines/${params.pipeline_id}/runs`,
          body,
        );
        return {
          id: run.id,
          state: run.state,
          result: run.result ?? null,
          pipeline_name: run.pipeline?.name ?? "",
          url: run._links?.web?.href ?? run.url ?? "",
        };
      },
    },
  },
});
