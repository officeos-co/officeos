import { defineSkill, z } from "@harro/skill-sdk";

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
  name: "jenkins",
  title: "Jenkins",
  logo: "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M2.872 24h-.975a3.866 3.866 0 01-.07-.197c-.215-.666-.594-1.49-.692-2.154-.146-.984.78-1.039 1.374-1.465.915-.66 1.635-1.025 2.627-1.62.295-.179 1.182-.624 1.281-.829.201-.408-.345-.982-.49-1.3-.225-.507-.345-.937-.376-1.435-.824-.13-1.455-.627-1.844-1.185-.63-.925-1.066-2.635-.525-3.936.045-.103.254-.305.285-.463.06-.308-.105-.72-.12-1.048-.06-1.692.284-3.15 1.425-3.66.463-1.84 2.113-2.453 3.673-3.367.58-.342 1.224-.562 1.89-.807 2.372-.877 6.027-.712 7.994.783.836.633 2.176 1.97 2.656 2.939 1.262 2.555 1.17 6.825.287 9.934-.12.421-.29 1.032-.533 1.533-.168.35-.689 1.05-.625 1.36.064.314 1.19 1.17 1.432 1.395.434.422 1.26.975 1.324 1.5.07.557-.248 1.336-.41 1.875-.217.721-.436 1.441-.654 2.131H2.87zm11.104-3.54c-.545-.3-1.361-.622-2.065-.757-.87-.164-.78 1.188-.75 1.994.03.643.36 1.316.51 1.744.076.197.09.41.256.449.3.068 1.29-.326 1.575-.479.6-.328 1.064-.844 1.574-1.189.016-.17.016-.34.03-.508a2.648 2.648 0 00-1.095-.277c.314-.15.75-.15 1.035-.332l.016-.193c-.496-.03-.69-.254-1.021-.436zm7.454 2.935a17.78 17.78 0 00.465-1.752c.06-.287.215-.918.178-1.176-.059-.459-.684-.799-1.004-1.086-.584-.525-.95-.975-1.56-1.469-.249.375-.78.615-.983.914 1.447-.689 1.71 2.625 1.141 3.69.09.329.391.45.514.735l-.086.166h1.29c.013 0 .03 0 .044.014zm-6.634-.012c-.05-.074-.1-.135-.15-.209l-.301.195h.45zm2.77 0c.008-.209.018-.404.03-.598-.53.029-.825-.48-1.196-.527-.324-.045-.6.361-1.02.195-.095.105-.183.227-.284.316.154.18.295.375.424.584h.815c.014-.164.135-.285.3-.285.165 0 .284.121.284.27h.66zm2.116 0c-.314-.479-.947-.898-1.68-.555l-.03.541h1.71zm-8.51 0l-.104-.344c-.225-.72-.36-1.26-.405-1.68-.914-.436-1.875-.87-2.654-1.426-.15-.105-1.109-1.35-1.23-1.305-1.739.676-3.359 1.86-4.814 2.984.256.557.48 1.141.69 1.74h8.505zm8.265-2.113c-.029-.512-.164-1.56-.48-1.74-.66-.39-1.846.78-2.34.943.045.15.135.271.15.48.285-.074.645-.029.898.092-.299.03-.629.03-.824.164-.074.195.016.48-.029.764.69.197 1.5.303 2.385.332.164-.227.225-.645.211-1.082zm-4.08-.36c-.044.375.046.51.12.943 1.26.391 1.034-1.74-.135-.959zM8.76 19.5c-.45.457 1.27 1.082 1.814 1.115 0-.29.165-.564.135-.77-.65-.118-1.502-.042-1.945-.347zm5.565.215c0 .043-.061.03-.068.064.58.451 1.014.545 1.802.51.354-.262.67-.563 1.043-.807-.855.074-1.931.607-2.774.23zm3.42-17.726c-1.606-.906-4.35-1.591-6.076-.731-1.38.692-3.27 1.84-3.899 3.292.6 1.402-.166 2.686-.226 4.109-.018.757.36 1.42.391 2.242-.2.338-.825.38-1.26.356-.146-.729-.4-1.549-1.155-1.63-1.064-.116-1.845.764-1.89 1.683-.06 1.08.833 2.864 2.085 2.745.488-.046.608-.54 1.139-.54.285.57-.445.75-.523 1.154-.016.105.06.511.104.705.233.944.744 2.16 1.245 2.88.635.9 1.884 1.051 3.229 1.141.24-.525 1.125-.48 1.706-.346-.691-.27-1.336-.945-1.875-1.529-.615-.676-1.23-1.41-1.261-2.28 1.155 1.604 2.1 3 4.2 3.704 1.59.525 3.45-.254 4.664-1.109.51-.359.811-.93 1.17-1.439 1.35-1.936 1.98-4.71 1.846-7.394-.06-1.111-.06-2.221-.436-2.955-.389-.781-1.695-1.471-2.475-.781-.15-.764.63-1.23 1.545-.96-.66-.854-1.336-1.858-2.266-2.384zM13.58 14.896c.615 1.544 2.724 1.363 4.505 1.323-.084.194-.256.435-.465.515-.57.232-2.145.408-2.937-.012-.506-.27-.824-.873-1.102-1.227-.137-.172-.795-.608-.012-.609zm.164-.87c.893.464 2.52.517 3.731.48.066.267.066.593.068.913-1.55.08-3.386-.304-3.794-1.395h-.005zm6.675-.586c-.473.9-1.145 1.897-2.539 1.928-.023-.284-.045-.735 0-.904 1.064-.103 1.727-.646 2.543-1.017zm-.649-.667c-1.02.66-2.154 1.375-3.824 1.21-.351-.31-.485-1-.14-1.458.181.313.06.885.57.97.944.165 2.038-.579 2.73-.84.42-.713-.046-.976-.42-1.433-.782-.93-1.83-2.1-1.802-3.51.314-.224.346.346.391.45.404.96 1.424 2.175 2.174 3 .18.21.48.39.51.524.092.39-.254.854-.209 1.11zm-13.439-.675c-.314-.184-.393-.99-.768-1.01-.535-.03-.438 1.05-.436 1.68-.37-.33-.435-1.365-.164-1.89-.308-.15-.445.164-.618.284.22-1.59 2.34-.734 1.99.96zM4.713 5.995c-.685.756-.54 2.174-.459 3.188 1.244-.785 2.898.06 2.883 1.394.595-.016.223-.744.115-1.215-.353-1.528.592-3.187.041-4.59-1.064.084-1.939.52-2.578 1.215zm9.12 1.113c.307.562.404 1.148.84 1.57.195.19.574.424.387.95-.045.121-.365.391-.551.45-.674.195-2.254.03-1.721-.81.563.015 1.314.36 1.732-.045-.314-.524-.885-1.53-.674-2.13zm6.198-.013h.068c.33.668.6 1.375 1.004 1.965-.27.628-2.053 1.19-2.023.057.39-.17 1.05-.035 1.395-.25-.193-.556-.48-1.006-.434-1.771zm-6.927-1.617c-1.422-.33-2.131.592-2.56 1.553-.384-.094-.231-.615-.135-.883.255-.701 1.28-1.633 2.119-1.506.359.057.848.386.576.834zM9.642 1.593c-1.56.44-3.56 1.574-4.2 2.974.495-.07.84-.321 1.33-.351.186-.016.428.074.641.015.424-.104.78-1.065 1.102-1.41.31-.345.685-.496.94-.81.167-.09.409-.074.42-.33-.073-.075-.15-.135-.232-.105v.017z\"/></svg>",
  emoji: "\uD83E\uDEAC",
  description:
    "Manage Jenkins jobs, builds, queue, nodes, and views via the Jenkins REST API with Basic auth.",
  doc,

  credentials: {
    url: {
      label: "Jenkins URL",
      kind: "text",
      placeholder: "https://jenkins.example.com",
      help: "Base URL of your Jenkins instance (no trailing slash).",
    },
    user: {
      label: "Username",
      kind: "text",
      placeholder: "admin",
      help: "Jenkins username.",
    },
    api_token: {
      label: "API Token",
      kind: "password",
      placeholder: "11a1b2c3…",
      help: "API token from User > Configure > API Token. Do NOT use your password.",
    },
  },

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
