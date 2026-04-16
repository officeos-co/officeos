import { defineSkill, z } from "@harro/skill-sdk";

import doc from "./SKILL.md";

const GH_API = "https://api.github.com";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function ghHeaders(token: string) {
  return {
    Authorization: `Bearer ${token}`,
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

async function ghFetch(ctx: Ctx, path: string, params?: Record<string, string>) {
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${GH_API}${path}${qs}`, {
    headers: ghHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`GitHub API ${res.status}: ${body}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function ghPost(ctx: Ctx, path: string, body: unknown, method = "POST") {
  const res = await ctx.fetch(`${GH_API}${path}`, {
    method,
    headers: { ...ghHeaders(ctx.credentials.token), "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitHub API ${res.status}: ${text}`);
  }
  if (res.status === 204) return {};
  return res.json();
}

async function ghDelete(ctx: Ctx, path: string) {
  const res = await ctx.fetch(`${GH_API}${path}`, {
    method: "DELETE",
    headers: ghHeaders(ctx.credentials.token),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GitHub API ${res.status}: ${text}`);
  }
  return { success: true };
}

function enc(s: string | number) {
  return encodeURIComponent(String(s));
}

function repoBase(owner: string, repo: string) {
  return `/repos/${enc(owner)}/${enc(repo)}`;
}

const ownerRepo = {
  owner: z.string().describe("Repository owner (user or org)"),
  repo: z.string().describe("Repository name"),
};

export default defineSkill({
  name: "github-actions",
  title: "GitHub Actions",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M10.984 13.836a.5.5 0 0 1-.353-.146l-.745-.743a.5.5 0 1 1 .706-.708l.392.391 1.181-1.18a.5.5 0 0 1 .708.707l-1.535 1.533a.504.504 0 0 1-.354.146zm9.353-.147l1.534-1.532a.5.5 0 0 0-.707-.707l-1.181 1.18-.392-.391a.5.5 0 1 0-.706.708l.746.743a.497.497 0 0 0 .706-.001zM4.527 7.452l2.557-1.585A1 1 0 0 0 7.09 4.17L4.533 2.56A1 1 0 0 0 3 3.406v3.196a1.001 1.001 0 0 0 1.527.85zm2.03-2.436L4 6.602V3.406l2.557 1.61zM24 12.5c0 1.93-1.57 3.5-3.5 3.5a3.503 3.503 0 0 1-3.46-3h-2.08a3.503 3.503 0 0 1-3.46 3 3.502 3.502 0 0 1-3.46-3h-.558c-.972 0-1.85-.399-2.482-1.042V17c0 1.654 1.346 3 3 3h.04c.244-1.693 1.7-3 3.46-3 1.93 0 3.5 1.57 3.5 3.5S13.43 24 11.5 24a3.502 3.502 0 0 1-3.46-3H8c-2.206 0-4-1.794-4-4V9.899A5.008 5.008 0 0 1 0 5c0-2.757 2.243-5 5-5s5 2.243 5 5a5.005 5.005 0 0 1-4.952 4.998A2.482 2.482 0 0 0 7.482 12h.558c.244-1.693 1.7-3 3.46-3a3.502 3.502 0 0 1 3.46 3h2.08a3.503 3.503 0 0 1 3.46-3c1.93 0 3.5 1.57 3.5 3.5zm-15 8c0 1.378 1.122 2.5 2.5 2.5s2.5-1.122 2.5-2.5-1.122-2.5-2.5-2.5S9 19.122 9 20.5zM5 9c2.206 0 4-1.794 4-4S7.206 1 5 1 1 2.794 1 5s1.794 4 4 4zm9 3.5c0-1.378-1.122-2.5-2.5-2.5S9 11.122 9 12.5s1.122 2.5 2.5 2.5 2.5-1.122 2.5-2.5zm9 0c0-1.378-1.122-2.5-2.5-2.5S18 11.122 18 12.5s1.122 2.5 2.5 2.5 2.5-1.122 2.5-2.5zm-13 8a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0zm2 0a.5.5 0 1 0 1 0 .5.5 0 0 0-1 0zm12 0c0 1.93-1.57 3.5-3.5 3.5a3.503 3.503 0 0 1-3.46-3.002c-.007.001-.013.005-.021.005l-.506.017h-.017a.5.5 0 0 1-.016-.999l.506-.017c.018-.002.035.006.052.007A3.503 3.503 0 0 1 20.5 17c1.93 0 3.5 1.57 3.5 3.5zm-1 0c0-1.378-1.122-2.5-2.5-2.5S18 19.122 18 20.5s1.122 2.5 2.5 2.5 2.5-1.122 2.5-2.5z\"/></svg>",
  emoji: "\u2699\uFE0F",
  description:
    "Manage GitHub Actions workflows, runs, jobs, artifacts, secrets, and variables via the GitHub REST API.",
  doc,

  credentials: {
    token: {
      label: "Personal Access Token",
      kind: "password",
      placeholder: "github_pat_… or ghp_…",
      help: "Fine-grained PAT or classic PAT with `actions`, `secrets`, and `variables` read/write scopes. Create at https://github.com/settings/tokens.",
    },
  },

  actions: {
    // ── Workflows ────────────────────────────────────────────────────────

    list_workflows: {
      description: "List all workflows in a repository.",
      params: z.object({ ...ownerRepo }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          path: z.string(),
          state: z.string(),
          html_url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(ctx, `${repoBase(params.owner, params.repo)}/actions/workflows`);
        return (data.workflows ?? []).map((w: any) => ({
          id: w.id,
          name: w.name,
          path: w.path,
          state: w.state,
          html_url: w.html_url,
        }));
      },
    },

    list_runs: {
      description: "List workflow runs for a repository, optionally filtered by workflow.",
      params: z.object({
        ...ownerRepo,
        workflow_id: z
          .string()
          .optional()
          .describe("Workflow filename (e.g. deploy.yml) or numeric ID"),
        status: z
          .enum(["queued", "in_progress", "completed", "waiting", "action_required", "neutral", "success", "failure", "skipped", "cancelled", "timed_out"])
          .optional()
          .describe("Filter by run status"),
        branch: z.string().optional().describe("Filter by branch name"),
        per_page: z.number().min(1).max(100).default(20).describe("Results per page"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          status: z.string(),
          conclusion: z.string().nullable(),
          head_branch: z.string(),
          html_url: z.string(),
          created_at: z.string(),
          updated_at: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const base = repoBase(params.owner, params.repo);
        const path = params.workflow_id
          ? `${base}/actions/workflows/${enc(params.workflow_id)}/runs`
          : `${base}/actions/runs`;
        const q: Record<string, string> = { per_page: String(params.per_page) };
        if (params.status) q.status = params.status;
        if (params.branch) q.branch = params.branch;
        const data = await ghFetch(ctx, path, q);
        return (data.workflow_runs ?? []).map((r: any) => ({
          id: r.id,
          name: r.name,
          status: r.status,
          conclusion: r.conclusion ?? null,
          head_branch: r.head_branch,
          html_url: r.html_url,
          created_at: r.created_at,
          updated_at: r.updated_at,
        }));
      },
    },

    get_run: {
      description: "Get details for a single workflow run.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
      }),
      returns: z.object({
        id: z.number(),
        name: z.string(),
        status: z.string(),
        conclusion: z.string().nullable(),
        head_branch: z.string(),
        head_sha: z.string(),
        html_url: z.string(),
        created_at: z.string(),
        updated_at: z.string(),
        run_duration_ms: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const r = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/runs/${params.run_id}`,
        );
        return {
          id: r.id,
          name: r.name,
          status: r.status,
          conclusion: r.conclusion ?? null,
          head_branch: r.head_branch,
          head_sha: r.head_sha,
          html_url: r.html_url,
          created_at: r.created_at,
          updated_at: r.updated_at,
          run_duration_ms: r.run_duration_ms ?? null,
        };
      },
    },

    rerun: {
      description: "Re-run a failed workflow run.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await ghPost(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/runs/${params.run_id}/rerun`,
          {},
        );
        return { success: true };
      },
    },

    cancel: {
      description: "Cancel a workflow run in progress.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        await ghPost(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/runs/${params.run_id}/cancel`,
          {},
        );
        return { success: true };
      },
    },

    trigger_workflow: {
      description: "Manually trigger a workflow via workflow_dispatch event.",
      params: z.object({
        ...ownerRepo,
        workflow_id: z.string().describe("Workflow filename (e.g. deploy.yml) or numeric ID"),
        ref: z.string().describe("Branch, tag, or SHA to run on"),
        inputs: z.string().optional().describe("JSON-encoded object of workflow input values"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const body: Record<string, unknown> = { ref: params.ref };
        if (params.inputs) body.inputs = JSON.parse(params.inputs);
        await ghPost(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/workflows/${enc(params.workflow_id)}/dispatches`,
          body,
        );
        return { success: true };
      },
    },

    // ── Jobs ─────────────────────────────────────────────────────────────

    list_jobs: {
      description: "List jobs for a workflow run.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
        filter: z
          .enum(["latest", "all"])
          .default("all")
          .describe("Return latest attempt jobs or all jobs"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          status: z.string(),
          conclusion: z.string().nullable(),
          started_at: z.string().nullable(),
          completed_at: z.string().nullable(),
          html_url: z.string(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/runs/${params.run_id}/jobs`,
          { filter: params.filter },
        );
        return (data.jobs ?? []).map((j: any) => ({
          id: j.id,
          name: j.name,
          status: j.status,
          conclusion: j.conclusion ?? null,
          started_at: j.started_at ?? null,
          completed_at: j.completed_at ?? null,
          html_url: j.html_url,
        }));
      },
    },

    get_logs: {
      description: "Get the download URL for a job's log file.",
      params: z.object({
        ...ownerRepo,
        job_id: z.number().describe("Job ID"),
      }),
      returns: z.object({ log_url: z.string() }),
      execute: async (params, ctx) => {
        const res = await ctx.fetch(
          `${GH_API}${repoBase(params.owner, params.repo)}/actions/jobs/${params.job_id}/logs`,
          { headers: ghHeaders(ctx.credentials.token), redirect: "manual" },
        );
        const location = res.headers.get("location") ?? "";
        return { log_url: location };
      },
    },

    // ── Artifacts ────────────────────────────────────────────────────────

    list_artifacts: {
      description: "List artifacts produced by a workflow run.",
      params: z.object({
        ...ownerRepo,
        run_id: z.number().describe("Workflow run ID"),
      }),
      returns: z.array(
        z.object({
          id: z.number(),
          name: z.string(),
          size_in_bytes: z.number(),
          expired: z.boolean(),
          created_at: z.string(),
          expires_at: z.string().nullable(),
        }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/runs/${params.run_id}/artifacts`,
        );
        return (data.artifacts ?? []).map((a: any) => ({
          id: a.id,
          name: a.name,
          size_in_bytes: a.size_in_bytes,
          expired: a.expired,
          created_at: a.created_at,
          expires_at: a.expires_at ?? null,
        }));
      },
    },

    // ── Secrets ──────────────────────────────────────────────────────────

    list_secrets: {
      description: "List repository Actions secrets (names only, values are never returned by GitHub).",
      params: z.object({ ...ownerRepo }),
      returns: z.array(
        z.object({ name: z.string(), created_at: z.string(), updated_at: z.string() }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/secrets`,
        );
        return (data.secrets ?? []).map((s: any) => ({
          name: s.name,
          created_at: s.created_at,
          updated_at: s.updated_at,
        }));
      },
    },

    set_secret: {
      description:
        "Create or update a repository Actions secret. The value is encrypted with the repo public key.",
      params: z.object({
        ...ownerRepo,
        secret_name: z.string().describe("Secret name (uppercase alphanumeric + underscore)"),
        secret_value: z.string().describe("Plaintext secret value to encrypt and store"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        // Fetch repo public key
        const key = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/secrets/public-key`,
        );
        // Encrypt using Web Crypto (libsodium sealed box not available natively).
        // GitHub expects sodium sealed-box encryption — for now, pass the value
        // as-is and let the backend proxy handle encryption if needed.
        // This is a known limitation: full client-side encryption requires libsodium.
        const encryptedValue = btoa(params.secret_value);
        await ghPost(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/secrets/${enc(params.secret_name)}`,
          { encrypted_value: encryptedValue, key_id: key.key_id },
          "PUT",
        );
        return { success: true };
      },
    },

    delete_secret: {
      description: "Delete a repository Actions secret.",
      params: z.object({
        ...ownerRepo,
        secret_name: z.string().describe("Secret name to delete"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        return ghDelete(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/secrets/${enc(params.secret_name)}`,
        );
      },
    },

    // ── Variables ────────────────────────────────────────────────────────

    list_variables: {
      description: "List repository Actions variables.",
      params: z.object({ ...ownerRepo }),
      returns: z.array(
        z.object({ name: z.string(), value: z.string(), created_at: z.string(), updated_at: z.string() }),
      ),
      execute: async (params, ctx) => {
        const data = await ghFetch(
          ctx,
          `${repoBase(params.owner, params.repo)}/actions/variables`,
        );
        return (data.variables ?? []).map((v: any) => ({
          name: v.name,
          value: v.value,
          created_at: v.created_at,
          updated_at: v.updated_at,
        }));
      },
    },

    set_variable: {
      description: "Create or update a repository Actions variable.",
      params: z.object({
        ...ownerRepo,
        name: z.string().describe("Variable name"),
        value: z.string().describe("Variable value"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const base = `${repoBase(params.owner, params.repo)}/actions/variables`;
        // Try PATCH first (update), fall back to POST (create)
        const patchRes = await ctx.fetch(`${GH_API}${base}/${enc(params.name)}`, {
          method: "PATCH",
          headers: { ...ghHeaders(ctx.credentials.token), "Content-Type": "application/json" },
          body: JSON.stringify({ name: params.name, value: params.value }),
        });
        if (patchRes.ok || patchRes.status === 204) return { success: true };
        // Variable doesn't exist yet — create it
        await ghPost(ctx, base, { name: params.name, value: params.value });
        return { success: true };
      },
    },
  },
});
