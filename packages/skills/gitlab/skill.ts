import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function glBase(credentials: Record<string, string>) {
  const url = (credentials.url ?? "https://gitlab.com").replace(/\/$/, "");
  return `${url}/api/v4`;
}

function glHeaders(token: string) {
  return {
    "PRIVATE-TOKEN": token,
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

async function glFetch(ctx: Ctx, path: string, params?: Record<string, string>) {
  const base = glBase(ctx.credentials);
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    headers: glHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`GitLab API ${res.status}: ${body}`);
  }
  return res.json();
}

async function glPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const base = glBase(ctx.credentials);
  const res = await ctx.fetch(`${base}${path}`, {
    method,
    headers: {
      ...glHeaders(ctx.credentials.token),
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitLab API ${res.status}: ${text}`);
  }
  return res.json();
}

async function glDelete(ctx: Ctx, path: string) {
  const base = glBase(ctx.credentials);
  const res = await ctx.fetch(`${base}${path}`, {
    method: "DELETE",
    headers: glHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitLab API ${res.status}: ${text}`);
  }
  return res.status === 204 ? { success: true } : res.json();
}

function enc(s: string | number) {
  return encodeURIComponent(String(s));
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Projects ──────────────────────────────────────────────────────────

    list_projects: {
      description: "List projects accessible to the authenticated user.",
      params: z.object({
        membership: z.boolean().default(true).describe("Only projects the user is a member of"),
        search: z.string().optional().describe("Filter by name"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page (1-100)"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          path_with_namespace: z.string(),
          description: z.string().nullable(),
          web_url: z.string(),
          default_branch: z.string().nullable(),
          visibility: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = {
          membership: String(params.membership),
          per_page: String(params.per_page),
        };
        if (params.search) q.search = params.search;
        const data = await glFetch(ctx, "/projects", q);
        return data.map((p: any) => ({
          id: p.id,
          name: p.name,
          path_with_namespace: p.path_with_namespace,
          description: p.description ?? null,
          web_url: p.web_url,
          default_branch: p.default_branch ?? null,
          visibility: p.visibility,
        }));
      },
    },

    get_project: {
      description: "Get detailed information about a project.",
      params: z.object({
        id: z.union([z.string(), z.number()]).describe("Project ID or URL-encoded path"),
      }),
      returns: z.object({
        id: z.number(),
        name: z.string(),
        path_with_namespace: z.string(),
        description: z.string().nullable(),
        web_url: z.string(),
        default_branch: z.string().nullable(),
        visibility: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const p = await glFetch(ctx, `/projects/${enc(params.id)}`);
        return {
          id: p.id,
          name: p.name,
          path_with_namespace: p.path_with_namespace,
          description: p.description ?? null,
          web_url: p.web_url,
          default_branch: p.default_branch ?? null,
          visibility: p.visibility,
          created_at: p.created_at,
        };
      },
    },

    // ── Merge Requests ───────────────────────────────────────────────────

    list_merge_requests: {
      description: "List merge requests for a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        state: z
          .enum(["opened", "closed", "merged", "all"])
          .default("opened")
          .describe("MR state filter"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          iid: z.number(),
          title: z.string(),
          state: z.string(),
          source_branch: z.string(),
          target_branch: z.string(),
          web_url: z.string(),
          created_at: z.string(),
          author: z.object({ username: z.string() }),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/merge_requests`,
          { state: params.state, per_page: String(params.per_page) },
        );
        return data.map((mr: any) => ({
          iid: mr.iid,
          title: mr.title,
          state: mr.state,
          source_branch: mr.source_branch,
          target_branch: mr.target_branch,
          web_url: mr.web_url,
          created_at: mr.created_at,
          author: { username: mr.author?.username ?? "" },
        }));
      },
    },

    get_mr: {
      description: "Get a single merge request by internal ID.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        mr_iid: z.number().describe("Merge request internal ID"),
      }),
      returns: z.object({
        iid: z.number(),
        title: z.string(),
        description: z.string().nullable(),
        state: z.string(),
        source_branch: z.string(),
        target_branch: z.string(),
        web_url: z.string(),
        created_at: z.string(),
        merged_at: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const mr = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/merge_requests/${params.mr_iid}`,
        );
        return {
          iid: mr.iid,
          title: mr.title,
          description: mr.description ?? null,
          state: mr.state,
          source_branch: mr.source_branch,
          target_branch: mr.target_branch,
          web_url: mr.web_url,
          created_at: mr.created_at,
          merged_at: mr.merged_at ?? null,
        };
      },
    },

    create_mr: {
      description: "Create a new merge request.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        source_branch: z.string().describe("Source branch name"),
        target_branch: z.string().describe("Target branch name"),
        title: z.string().describe("MR title"),
        description: z.string().optional().describe("MR description"),
      }),
      returns: z.object({ iid: z.number(), web_url: z.string(), state: z.string() }),
      execute: async (params, ctx) => {
        const body: Record<string, string> = {
          source_branch: params.source_branch,
          target_branch: params.target_branch,
          title: params.title,
        };
        if (params.description) body.description = params.description;
        const mr = await glPost(ctx, `/projects/${enc(params.project_id)}/merge_requests`, body);
        return { iid: mr.iid, web_url: mr.web_url, state: mr.state };
      },
    },

    merge_mr: {
      description: "Accept (merge) a merge request.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        mr_iid: z.number().describe("MR internal ID"),
        squash: z.boolean().default(false).describe("Squash commits on merge"),
        should_remove_source_branch: z
          .boolean()
          .default(false)
          .describe("Delete source branch after merge"),
      }),
      returns: z.object({ iid: z.number(), state: z.string(), merged_at: z.string().nullable() }),
      execute: async (params, ctx) => {
        const mr = await glPost(
          ctx,
          `/projects/${enc(params.project_id)}/merge_requests/${params.mr_iid}/merge`,
          { squash: params.squash, should_remove_source_branch: params.should_remove_source_branch },
          "PUT",
        );
        return { iid: mr.iid, state: mr.state, merged_at: mr.merged_at ?? null };
      },
    },

    // ── Issues ───────────────────────────────────────────────────────────

    list_issues: {
      description: "List issues for a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        state: z.enum(["opened", "closed", "all"]).default("opened").describe("Issue state"),
        labels: z.string().optional().describe("Comma-separated label names"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          iid: z.number(),
          title: z.string(),
          state: z.string(),
          labels: z.array(z.string()),
          web_url: z.string(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = {
          state: params.state,
          per_page: String(params.per_page),
        };
        if (params.labels) q.labels = params.labels;
        const data = await glFetch(ctx, `/projects/${enc(params.project_id)}/issues`, q);
        return data.map((i: any) => ({
          iid: i.iid,
          title: i.title,
          state: i.state,
          labels: i.labels ?? [],
          web_url: i.web_url,
          created_at: i.created_at,
        }));
      },
    },

    get_issue: {
      description: "Get a single issue by internal ID.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        issue_iid: z.number().describe("Issue internal ID"),
      }),
      returns: z.object({
        iid: z.number(),
        title: z.string(),
        description: z.string().nullable(),
        state: z.string(),
        labels: z.array(z.string()),
        web_url: z.string(),
        created_at: z.string(),
      }),
      execute: async (params, ctx) => {
        const i = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/issues/${params.issue_iid}`,
        );
        return {
          iid: i.iid,
          title: i.title,
          description: i.description ?? null,
          state: i.state,
          labels: i.labels ?? [],
          web_url: i.web_url,
          created_at: i.created_at,
        };
      },
    },

    create_issue: {
      description: "Create a new issue in a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        title: z.string().describe("Issue title"),
        description: z.string().optional().describe("Issue body"),
        labels: z.string().optional().describe("Comma-separated label names"),
        assignee_ids: z.string().optional().describe("Comma-separated user IDs"),
      }),
      returns: z.object({ iid: z.number(), web_url: z.string(), state: z.string() }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = { title: params.title };
        if (params.description) body.description = params.description;
        if (params.labels) body.labels = params.labels;
        if (params.assignee_ids)
          body.assignee_ids = params.assignee_ids.split(",").map((s) => Number(s.trim()));
        const i = await glPost(ctx, `/projects/${enc(params.project_id)}/issues`, body);
        return { iid: i.iid, web_url: i.web_url, state: i.state };
      },
    },

    // ── Pipelines ────────────────────────────────────────────────────────

    list_pipelines: {
      description: "List pipelines for a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        status: z
          .enum(["running", "pending", "success", "failed", "canceled", "skipped"])
          .optional()
          .describe("Filter by status"),
        ref: z.string().optional().describe("Branch or tag name"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          status: z.string(),
          ref: z.string(),
          sha: z.string(),
          web_url: z.string(),
          created_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = { per_page: String(params.per_page) };
        if (params.status) q.status = params.status;
        if (params.ref) q.ref = params.ref;
        const data = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/pipelines`,
          q,
        );
        return data.map((p: any) => ({
          id: p.id,
          status: p.status,
          ref: p.ref,
          sha: p.sha,
          web_url: p.web_url,
          created_at: p.created_at,
        }));
      },
    },

    get_pipeline: {
      description: "Get a single pipeline.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        pipeline_id: z.number().describe("Pipeline ID"),
      }),
      returns: z.object({
        id: z.number(),
        status: z.string(),
        ref: z.string(),
        sha: z.string(),
        web_url: z.string(),
        created_at: z.string(),
        finished_at: z.string().nullable(),
        duration: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const p = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/pipelines/${params.pipeline_id}`,
        );
        return {
          id: p.id,
          status: p.status,
          ref: p.ref,
          sha: p.sha,
          web_url: p.web_url,
          created_at: p.created_at,
          finished_at: p.finished_at ?? null,
          duration: p.duration ?? null,
        };
      },
    },

    retry_pipeline: {
      description: "Retry a failed or canceled pipeline.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        pipeline_id: z.number().describe("Pipeline ID"),
      }),
      returns: z.object({ id: z.number(), status: z.string() }),
      execute: async (params, ctx) => {
        const p = await glPost(
          ctx,
          `/projects/${enc(params.project_id)}/pipelines/${params.pipeline_id}/retry`,
          {},
        );
        return { id: p.id, status: p.status };
      },
    },

    cancel_pipeline: {
      description: "Cancel a running pipeline.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        pipeline_id: z.number().describe("Pipeline ID"),
      }),
      returns: z.object({ id: z.number(), status: z.string() }),
      execute: async (params, ctx) => {
        const p = await glPost(
          ctx,
          `/projects/${enc(params.project_id)}/pipelines/${params.pipeline_id}/cancel`,
          {},
        );
        return { id: p.id, status: p.status };
      },
    },

    // ── Branches ─────────────────────────────────────────────────────────

    list_branches: {
      description: "List branches for a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        search: z.string().optional().describe("Filter by branch name"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          name: z.string(),
          merged: z.boolean(),
          protected: z.boolean(),
          default: z.boolean(),
          web_url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = { per_page: String(params.per_page) };
        if (params.search) q.search = params.search;
        const data = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/repository/branches`,
          q,
        );
        return data.map((b: any) => ({
          name: b.name,
          merged: b.merged ?? false,
          protected: b.protected ?? false,
          default: b.default ?? false,
          web_url: b.web_url,
        }));
      },
    },

    create_branch: {
      description: "Create a new branch from a ref.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        branch: z.string().describe("New branch name"),
        ref: z.string().describe("Source branch, tag, or SHA"),
      }),
      returns: z.object({ name: z.string(), web_url: z.string() }),
      execute: async (params, ctx) => {
        const b = await glPost(
          ctx,
          `/projects/${enc(params.project_id)}/repository/branches`,
          { branch: params.branch, ref: params.ref },
        );
        return { name: b.name, web_url: b.web_url };
      },
    },

    delete_branch: {
      description: "Delete a branch from a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        branch: z.string().describe("Branch name to delete"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await glDelete(
          ctx,
          `/projects/${enc(params.project_id)}/repository/branches/${enc(params.branch)}`,
        );
        return { success: true };
      },
    },

    // ── Tags ─────────────────────────────────────────────────────────────

    list_tags: {
      description: "List tags for a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        search: z.string().optional().describe("Filter by tag name"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({ name: z.string(), message: z.string().nullable(), commit_sha: z.string() }),
      ),
      execute: async (params, ctx) => {
        const q: Record<string, string> = { per_page: String(params.per_page) };
        if (params.search) q.search = params.search;
        const data = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/repository/tags`,
          q,
        );
        return data.map((t: any) => ({
          name: t.name,
          message: t.message ?? null,
          commit_sha: t.commit?.id ?? "",
        }));
      },
    },

    // ── Search ───────────────────────────────────────────────────────────

    search: {
      description: "Search across GitLab (projects, issues, merge requests, blobs).",
      params: z.object({
        scope: z
          .enum(["projects", "issues", "merge_requests", "blobs"])
          .describe("Search scope"),
        search: z.string().describe("Search query"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        return glFetch(ctx, "/search", {
          scope: params.scope,
          search: params.search,
          per_page: String(params.per_page),
        });
      },
    },

    // ── Members ──────────────────────────────────────────────────────────

    list_members: {
      description: "List members of a project.",
      params: z.object({
        project_id: z.union([z.string(), z.number()]).describe("Project ID or path"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({ id: z.number(), username: z.string(), access_level: z.number() }),
      ),
      execute: async (params, ctx) => {
        const data = await glFetch(
          ctx,
          `/projects/${enc(params.project_id)}/members`,
          { per_page: String(params.per_page) },
        );
        return data.map((m: any) => ({
          id: m.id,
          username: m.username,
          access_level: m.access_level,
        }));
      },
    },
  },
});
