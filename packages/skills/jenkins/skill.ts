import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

type Ctx = { fetch: typeof globalThis.fetch; credentials: Record<string, string> };

function jenkinsBase(credentials: Record<string, string>) {
  return credentials.url.replace(/\/$/, "");
}

function basicAuth(user: string, token: string) {
  return "Basic " + btoa(`${user}:${token}`);
}

function jenHeaders(credentials: Record<string, string>) {
  return {
    Authorization: basicAuth(credentials.user, credentials.api_token),
    Accept: "application/json",
    "User-Agent": "eaos-skill-runtime/1.0",
  };
}

async function fetchCrumb(ctx: Ctx): Promise<Record<string, string>> {
  const base = jenkinsBase(ctx.credentials);
  try {
    const res = await ctx.fetch(`${base}/crumbIssuer/api/json`, {
      headers: jenHeaders(ctx.credentials),
    });
    if (!res.ok) return {};
    const data = await res.json();
    return { [data.crumbRequestField]: data.crumb };
  } catch {
    return {};
  }
}

async function jenGet(ctx: Ctx, path: string, params?: Record<string, string>) {
  const base = jenkinsBase(ctx.credentials);
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    headers: jenHeaders(ctx.credentials),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Jenkins API ${res.status}: ${body}`);
  }
  return res.json();
}

async function jenPost(ctx: Ctx, path: string, formBody?: Record<string, string>) {
  const base = jenkinsBase(ctx.credentials);
  const crumb = await fetchCrumb(ctx);
  const res = await ctx.fetch(`${base}${path}`, {
    method: "POST",
    headers: {
      ...jenHeaders(ctx.credentials),
      "Content-Type": "application/x-www-form-urlencoded",
      ...crumb,
    },
    body: formBody ? new URLSearchParams(formBody).toString() : undefined,
  });
  if (!res.ok && res.status !== 201 && res.status !== 302) {
    const text = await res.text();
    throw new Error(`Jenkins API ${res.status}: ${text}`);
  }
  return { success: true };
}

async function jenText(ctx: Ctx, path: string, params?: Record<string, string>) {
  const base = jenkinsBase(ctx.credentials);
  const qs = params ? "?" + new URLSearchParams(params).toString() : "";
  const res = await ctx.fetch(`${base}${path}${qs}`, {
    headers: {
      ...jenHeaders(ctx.credentials),
      Accept: "text/plain",
    },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Jenkins API ${res.status}: ${body}`);
  }
  return res.text();
}

function enc(s: string | number) {
  return encodeURIComponent(String(s));
}

function jobPath(name: string, folderPath?: string) {
  if (folderPath) {
    const parts = folderPath.split("/").map(enc);
    return `/job/${parts.join("/job/")}/${enc(name)}`;
  }
  return `/job/${enc(name)}`;
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Jobs ─────────────────────────────────────────────────────────────

    list_jobs: {
      description: "List top-level jobs or jobs inside a folder.",
      params: z.object({
        folder_path: z
          .string()
          .optional()
          .describe("Folder path, e.g. MyFolder/SubFolder. Omit for root jobs."),
      }),
      returns: z.array(
        z.object({ name: z.string(), url: z.string(), color: z.string() }),
      ),
      execute: async (params, ctx) => {
        let apiPath: string;
        if (params.folder_path) {
          const parts = params.folder_path.split("/").map(enc);
          apiPath = `/job/${parts.join("/job/")}/api/json`;
        } else {
          apiPath = "/api/json";
        }
        const data = await jenGet(ctx, apiPath, { tree: "jobs[name,url,color]" });
        return (data.jobs ?? []).map((j: any) => ({
          name: j.name,
          url: j.url,
          color: j.color ?? "notbuilt",
        }));
      },
    },

    get_job: {
      description: "Get detailed information about a specific job.",
      params: z.object({
        name: z.string().describe("Job name"),
        folder_path: z.string().optional().describe("Folder containing the job"),
      }),
      returns: z.object({
        name: z.string(),
        url: z.string(),
        color: z.string(),
        description: z.string().nullable(),
        buildable: z.boolean(),
        last_build_number: z.number().nullable(),
        last_build_result: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const path = jobPath(params.name, params.folder_path) + "/api/json";
        const j = await jenGet(ctx, path);
        return {
          name: j.name,
          url: j.url,
          color: j.color ?? "notbuilt",
          description: j.description ?? null,
          buildable: j.buildable ?? false,
          last_build_number: j.lastBuild?.number ?? null,
          last_build_result: j.lastCompletedBuild?.result ?? null,
        };
      },
    },

    build_job: {
      description: "Trigger a build for a job, optionally with parameters.",
      params: z.object({
        name: z.string().describe("Job name"),
        folder_path: z.string().optional().describe("Folder containing the job"),
        params: z
          .string()
          .optional()
          .describe("JSON object of build parameters, e.g. {\"BRANCH\":\"main\"}"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const base = jobPath(params.name, params.folder_path);
        if (params.params) {
          const parsed = JSON.parse(params.params) as Record<string, string>;
          return jenPost(ctx, `${base}/buildWithParameters`, parsed);
        }
        return jenPost(ctx, `${base}/build`);
      },
    },

    get_build: {
      description: "Get details for a specific build.",
      params: z.object({
        name: z.string().describe("Job name"),
        build_number: z.number().describe("Build number (use 0 for lastBuild)"),
        folder_path: z.string().optional().describe("Folder containing the job"),
      }),
      returns: z.object({
        number: z.number(),
        result: z.string().nullable(),
        duration: z.number(),
        timestamp: z.number(),
        url: z.string(),
        building: z.boolean(),
      }),
      execute: async (params, ctx) => {
        const buildRef = params.build_number === 0 ? "lastBuild" : String(params.build_number);
        const path = `${jobPath(params.name, params.folder_path)}/${enc(buildRef)}/api/json`;
        const b = await jenGet(ctx, path);
        return {
          number: b.number,
          result: b.result ?? null,
          duration: b.duration,
          timestamp: b.timestamp,
          url: b.url,
          building: b.building ?? false,
        };
      },
    },

    get_log: {
      description: "Get the console log for a build. Supports progressive (incremental) reading.",
      params: z.object({
        name: z.string().describe("Job name"),
        build_number: z.number().describe("Build number"),
        folder_path: z.string().optional().describe("Folder containing the job"),
        start: z.number().default(0).describe("Byte offset for progressive log reading"),
      }),
      returns: z.object({ text: z.string(), more_data: z.boolean() }),
      execute: async (params, ctx) => {
        const base = jenkinsBase(ctx.credentials);
        const buildRef = String(params.build_number);
        const path = `${jobPath(params.name, params.folder_path)}/${enc(buildRef)}/logText/progressiveText`;
        const qs = "?" + new URLSearchParams({ start: String(params.start) }).toString();
        const res = await ctx.fetch(`${base}${path}${qs}`, {
          headers: { ...jenHeaders(ctx.credentials), Accept: "text/plain" },
        });
        if (!res.ok) {
          const body = await res.text();
          throw new Error(`Jenkins API ${res.status}: ${body}`);
        }
        const text = await res.text();
        const moreData = res.headers.get("X-More-Data") === "true";
        return { text, more_data: moreData };
      },
    },

    stop_build: {
      description: "Stop (abort) a running build.",
      params: z.object({
        name: z.string().describe("Job name"),
        build_number: z.number().describe("Build number to stop"),
        folder_path: z.string().optional().describe("Folder containing the job"),
      }),
      returns: z.object({ success: z.boolean() }),
      execute: async (params, ctx) => {
        const path = `${jobPath(params.name, params.folder_path)}/${enc(params.build_number)}/stop`;
        return jenPost(ctx, path);
      },
    },

    list_builds: {
      description: "List recent builds for a job.",
      params: z.object({
        name: z.string().describe("Job name"),
        folder_path: z.string().optional().describe("Folder containing the job"),
        limit: z.number().min(1).max(100).default(10).describe("Number of builds to return"),
      }),
      returns: z.array(
        z.object({
          number: z.number(),
          result: z.string().nullable(),
          duration: z.number(),
          timestamp: z.number(),
          url: z.string(),
          building: z.boolean(),
        }),
      ),
      execute: async (params, ctx) => {
        const path = jobPath(params.name, params.folder_path) + "/api/json";
        const tree = `builds[number,result,duration,timestamp,url,building]{0,${params.limit}}`;
        const data = await jenGet(ctx, path, { tree });
        return (data.builds ?? []).map((b: any) => ({
          number: b.number,
          result: b.result ?? null,
          duration: b.duration,
          timestamp: b.timestamp,
          url: b.url,
          building: b.building ?? false,
        }));
      },
    },

    // ── Queue ────────────────────────────────────────────────────────────

    get_queue: {
      description: "Get the current build queue (pending/blocked items).",
      params: z.object({}),
      returns: z.array(
        z.object({
          id: z.number(),
          why: z.string().nullable(),
          task_name: z.string(),
          in_queue_since: z.number(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await jenGet(ctx, "/queue/api/json");
        return (data.items ?? []).map((item: any) => ({
          id: item.id,
          why: item.why ?? null,
          task_name: item.task?.name ?? "",
          in_queue_since: item.inQueueSince ?? 0,
        }));
      },
    },

    // ── Nodes ────────────────────────────────────────────────────────────

    list_nodes: {
      description: "List all Jenkins nodes (agents/executors).",
      params: z.object({}),
      returns: z.array(
        z.object({
          display_name: z.string(),
          offline: z.boolean(),
          num_executors: z.number(),
          offline_cause: z.string().nullable(),
        }),
      ),
      execute: async (_params, ctx) => {
        const data = await jenGet(ctx, "/computer/api/json");
        return (data.computer ?? []).map((n: any) => ({
          display_name: n.displayName,
          offline: n.offline ?? false,
          num_executors: n.numExecutors ?? 0,
          offline_cause: n.offlineCauseReason ?? null,
        }));
      },
    },

    // ── Views ────────────────────────────────────────────────────────────

    list_views: {
      description: "List all views (dashboards) configured in Jenkins.",
      params: z.object({}),
      returns: z.array(z.object({ name: z.string(), url: z.string() })),
      execute: async (_params, ctx) => {
        const data = await jenGet(ctx, "/api/json", { tree: "views[name,url]" });
        return (data.views ?? []).map((v: any) => ({ name: v.name, url: v.url }));
      },
    },

    // ── Pipeline Stages ──────────────────────────────────────────────────

    get_stages: {
      description: "Get pipeline stage results for a build (requires Pipeline plugin).",
      params: z.object({
        name: z.string().describe("Pipeline job name"),
        build_number: z.number().describe("Build number"),
        folder_path: z.string().optional().describe("Folder containing the job"),
      }),
      returns: z.array(
        z.object({
          name: z.string(),
          status: z.string(),
          duration_millis: z.number(),
          start_time_millis: z.number(),
        }),
      ),
      execute: async (params, ctx) => {
        const path = `${jobPath(params.name, params.folder_path)}/${enc(params.build_number)}/wfapi/describe`;
        const data = await jenGet(ctx, path);
        return (data.stages ?? []).map((s: any) => ({
          name: s.name,
          status: s.status,
          duration_millis: s.durationMillis ?? 0,
          start_time_millis: s.startTimeMillis ?? 0,
        }));
      },
    },
  },
});
