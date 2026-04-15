import { defineSkill, z } from "@harro/skill-sdk";
import doc from "./SKILL.md";

async function gcpExec(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  args: string[],
): Promise<unknown> {
  const res = await ctx.fetch(ctx.credentials.proxy_url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command: "gcloud", args }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`GCP proxy ${res.status}: ${text}`);
  }
  const data = await res.json();
  if (data.exitCode !== 0) {
    throw new Error(`gcloud exited ${data.exitCode}: ${data.stderr ?? ""}`);
  }
  try {
    return JSON.parse(data.stdout);
  } catch {
    return data.stdout;
  }
}

export default defineSkill({
  name: "gcp",
  title: "Google Cloud Platform",
  emoji: "☁️",
  description:
    "Manage GCP resources via the gcloud CLI proxy: Compute Engine, GKE, Cloud Run, Cloud Functions, Cloud Storage, and BigQuery.",
  doc,

  credentials: {
    proxy_url: {
      label: "Proxy URL",
      kind: "url",
      placeholder: "https://gcloud-proxy.internal/exec",
      help: "URL of the gcloud proxy endpoint that executes gcloud commands with service account credentials.",
    },
  },

  actions: {
    // ── Compute Engine ────────────────────────────────────────────────

    instances_list: {
      description: "List VM instances in a project.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().optional().describe("Zone filter (e.g. us-central1-a); omit for all zones"),
        filter: z.string().optional().describe("gcloud filter expression"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["compute", "instances", "list", `--project=${params.project}`, "--format=json"];
        if (params.zone) args.push(`--zones=${params.zone}`);
        if (params.filter) args.push(`--filter=${params.filter}`);
        return gcpExec(ctx, args);
      },
    },

    instances_describe: {
      description: "Get details of a specific VM instance.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().describe("Zone (e.g. us-central1-a)"),
        instance: z.string().describe("Instance name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "compute", "instances", "describe", params.instance,
          `--project=${params.project}`, `--zone=${params.zone}`, "--format=json",
        ]),
    },

    instances_create: {
      description: "Create a new VM instance.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().describe("Zone (e.g. us-central1-a)"),
        name: z.string().describe("Instance name"),
        machine_type: z.string().default("e2-micro").describe("Machine type (e.g. e2-micro, n1-standard-2)"),
        image_family: z.string().default("debian-12").describe("Image family"),
        image_project: z.string().default("debian-cloud").describe("Image project"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "compute", "instances", "create", params.name,
          `--project=${params.project}`, `--zone=${params.zone}`,
          `--machine-type=${params.machine_type}`,
          `--image-family=${params.image_family}`,
          `--image-project=${params.image_project}`,
          "--format=json",
        ]),
    },

    instances_delete: {
      description: "Delete a VM instance.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().describe("Zone"),
        instance: z.string().describe("Instance name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "compute", "instances", "delete", params.instance,
          `--project=${params.project}`, `--zone=${params.zone}`,
          "--quiet", "--format=json",
        ]),
    },

    instances_start: {
      description: "Start a stopped VM instance.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().describe("Zone"),
        instance: z.string().describe("Instance name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "compute", "instances", "start", params.instance,
          `--project=${params.project}`, `--zone=${params.zone}`, "--format=json",
        ]),
    },

    instances_stop: {
      description: "Stop a running VM instance.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        zone: z.string().describe("Zone"),
        instance: z.string().describe("Instance name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "compute", "instances", "stop", params.instance,
          `--project=${params.project}`, `--zone=${params.zone}`, "--format=json",
        ]),
    },

    // ── GKE ──────────────────────────────────────────────────────────

    clusters_list: {
      description: "List GKE clusters in a project.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().optional().describe("Region filter (e.g. us-central1)"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["container", "clusters", "list", `--project=${params.project}`, "--format=json"];
        if (params.region) args.push(`--region=${params.region}`);
        return gcpExec(ctx, args);
      },
    },

    clusters_describe: {
      description: "Get details of a GKE cluster.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region (e.g. us-central1)"),
        cluster: z.string().describe("Cluster name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "container", "clusters", "describe", params.cluster,
          `--project=${params.project}`, `--region=${params.region}`, "--format=json",
        ]),
    },

    // ── Cloud Run ─────────────────────────────────────────────────────

    services_list: {
      description: "List Cloud Run services.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region (e.g. us-central1)"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "run", "services", "list",
          `--project=${params.project}`, `--region=${params.region}`, "--format=json",
        ]),
    },

    services_describe: {
      description: "Describe a Cloud Run service.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region"),
        service: z.string().describe("Service name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "run", "services", "describe", params.service,
          `--project=${params.project}`, `--region=${params.region}`, "--format=json",
        ]),
    },

    services_deploy: {
      description: "Deploy or update a Cloud Run service from a container image.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region"),
        service: z.string().describe("Service name"),
        image: z.string().describe("Container image URL (e.g. gcr.io/project/image:tag)"),
        allow_unauthenticated: z.boolean().default(false).describe("Allow unauthenticated requests"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const args = [
          "run", "deploy", params.service,
          `--image=${params.image}`,
          `--project=${params.project}`, `--region=${params.region}`,
          "--platform=managed", "--format=json",
        ];
        if (params.allow_unauthenticated) args.push("--allow-unauthenticated");
        else args.push("--no-allow-unauthenticated");
        return gcpExec(ctx, args);
      },
    },

    // ── Cloud Functions ───────────────────────────────────────────────

    functions_list: {
      description: "List Cloud Functions in a project.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().optional().describe("Region filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["functions", "list", `--project=${params.project}`, "--format=json"];
        if (params.region) args.push(`--regions=${params.region}`);
        return gcpExec(ctx, args);
      },
    },

    functions_describe: {
      description: "Describe a Cloud Function.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region"),
        function: z.string().describe("Function name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "functions", "describe", params.function,
          `--project=${params.project}`, `--region=${params.region}`, "--format=json",
        ]),
    },

    functions_deploy: {
      description: "Deploy a Cloud Function.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        region: z.string().describe("Region"),
        function: z.string().describe("Function name"),
        runtime: z.string().describe("Runtime (e.g. nodejs20, python312)"),
        source: z.string().describe("Source directory or GCS path"),
        entry_point: z.string().optional().describe("Entry point function name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const args = [
          "functions", "deploy", params.function,
          `--project=${params.project}`, `--region=${params.region}`,
          `--runtime=${params.runtime}`, `--source=${params.source}`,
          "--trigger-http", "--format=json",
        ];
        if (params.entry_point) args.push(`--entry-point=${params.entry_point}`);
        return gcpExec(ctx, args);
      },
    },

    // ── Cloud Storage ─────────────────────────────────────────────────

    buckets_list: {
      description: "List GCS buckets in a project.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) =>
        gcpExec(ctx, ["storage", "buckets", "list", `--project=${params.project}`, "--format=json"]),
    },

    objects_list: {
      description: "List objects in a GCS bucket.",
      params: z.object({
        bucket: z.string().describe("Bucket name (without gs://)"),
        prefix: z.string().optional().describe("Object prefix filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const uri = `gs://${params.bucket}/${params.prefix ?? ""}`;
        return gcpExec(ctx, ["storage", "objects", "list", uri, "--format=json"]);
      },
    },

    // ── BigQuery ──────────────────────────────────────────────────────

    datasets_list: {
      description: "List BigQuery datasets in a project.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) =>
        gcpExec(ctx, ["bq", "ls", "--format=json", `--project_id=${params.project}`]),
    },

    query: {
      description: "Run a BigQuery SQL query and return results.",
      params: z.object({
        project: z.string().describe("GCP project ID"),
        query: z.string().describe("Standard SQL query string"),
        max_results: z.number().int().min(1).max(10000).default(100).describe("Maximum rows to return"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        gcpExec(ctx, [
          "bq", "query",
          `--project_id=${params.project}`,
          "--use_legacy_sql=false",
          `--max_rows=${params.max_results}`,
          "--format=json",
          params.query,
        ]),
    },
  },
});
