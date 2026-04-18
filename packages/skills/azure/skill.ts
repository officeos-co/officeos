import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

async function azExec(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  args: string[],
): Promise<unknown> {
  const res = await ctx.fetch(ctx.credentials.proxy_url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command: "az", args }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Azure proxy ${res.status}: ${text}`);
  }
  const data = await res.json();
  if (data.exitCode !== 0) {
    throw new Error(`az exited ${data.exitCode}: ${data.stderr ?? ""}`);
  }
  try {
    return JSON.parse(data.stdout);
  } catch {
    return data.stdout;
  }
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Resource Groups ───────────────────────────────────────────────

    resource_groups_list: {
      description: "List all resource groups in the subscription.",
      params: z.object({
        subscription: z.string().optional().describe("Azure subscription ID or name"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["group", "list", "--output", "json"];
        if (params.subscription) args.push("--subscription", params.subscription);
        return azExec(ctx, args);
      },
    },

    resource_groups_create: {
      description: "Create a new resource group.",
      params: z.object({
        name: z.string().describe("Resource group name"),
        location: z.string().describe("Azure region (e.g. eastus, westeurope)"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, ["group", "create", "--name", params.name, "--location", params.location, "--output", "json"]),
    },

    resource_groups_delete: {
      description: "Delete a resource group and all its contained resources.",
      params: z.object({
        name: z.string().describe("Resource group name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, ["group", "delete", "--name", params.name, "--yes", "--output", "json"]),
    },

    // ── Virtual Machines ──────────────────────────────────────────────

    vms_list: {
      description: "List VMs, optionally filtered by resource group.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group name"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["vm", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    vms_describe: {
      description: "Show details of a specific VM.",
      params: z.object({
        name: z.string().describe("VM name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, ["vm", "show", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    vms_create: {
      description: "Create a new VM.",
      params: z.object({
        name: z.string().describe("VM name"),
        resource_group: z.string().describe("Resource group name"),
        image: z.string().default("Ubuntu2204").describe("VM image (e.g. Ubuntu2204, Win2022Datacenter)"),
        size: z.string().default("Standard_B1s").describe("VM size (e.g. Standard_B1s, Standard_D2s_v3)"),
        location: z.string().optional().describe("Azure region; defaults to resource group location"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) => {
        const args = [
          "vm", "create",
          "--name", params.name,
          "--resource-group", params.resource_group,
          "--image", params.image,
          "--size", params.size,
          "--output", "json",
        ];
        if (params.location) args.push("--location", params.location);
        return azExec(ctx, args);
      },
    },

    vms_start: {
      description: "Start a stopped VM.",
      params: z.object({
        name: z.string().describe("VM name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, ["vm", "start", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    vms_stop: {
      description: "Stop (but keep allocated) a VM.",
      params: z.object({
        name: z.string().describe("VM name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, ["vm", "stop", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    vms_deallocate: {
      description: "Deallocate a VM to stop billing for compute.",
      params: z.object({
        name: z.string().describe("VM name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, ["vm", "deallocate", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    // ── Web Apps ──────────────────────────────────────────────────────

    webapps_list: {
      description: "List App Service web apps.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["webapp", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    webapps_describe: {
      description: "Show details of a web app.",
      params: z.object({
        name: z.string().describe("Web app name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, ["webapp", "show", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    webapps_create: {
      description: "Create a new App Service web app.",
      params: z.object({
        name: z.string().describe("Web app name"),
        resource_group: z.string().describe("Resource group name"),
        plan: z.string().describe("App Service plan name"),
        runtime: z.string().describe("Runtime stack (e.g. NODE:20-lts, PYTHON:3.12, DOTNET:8.0)"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, [
          "webapp", "create",
          "--name", params.name,
          "--resource-group", params.resource_group,
          "--plan", params.plan,
          "--runtime", params.runtime,
          "--output", "json",
        ]),
    },

    webapps_deploy: {
      description: "Deploy a web app from a zip archive or other source.",
      params: z.object({
        name: z.string().describe("Web app name"),
        resource_group: z.string().describe("Resource group name"),
        src_path: z.string().describe("Path to the deployment source (zip file or directory)"),
        deploy_type: z.enum(["zip", "war", "jar", "ear", "static"]).default("zip").describe("Deployment type"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, [
          "webapp", "deploy",
          "--name", params.name,
          "--resource-group", params.resource_group,
          "--src-path", params.src_path,
          "--type", params.deploy_type,
          "--output", "json",
        ]),
    },

    // ── Storage ───────────────────────────────────────────────────────

    storage_accounts_list: {
      description: "List Azure storage accounts.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["storage", "account", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    storage_containers_list: {
      description: "List blob containers in a storage account.",
      params: z.object({
        account_name: z.string().describe("Storage account name"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) =>
        azExec(ctx, ["storage", "container", "list", "--account-name", params.account_name, "--output", "json"]),
    },

    storage_blobs_list: {
      description: "List blobs in a container.",
      params: z.object({
        account_name: z.string().describe("Storage account name"),
        container_name: z.string().describe("Container name"),
        prefix: z.string().optional().describe("Blob name prefix filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = [
          "storage", "blob", "list",
          "--account-name", params.account_name,
          "--container-name", params.container_name,
          "--output", "json",
        ];
        if (params.prefix) args.push("--prefix", params.prefix);
        return azExec(ctx, args);
      },
    },

    // ── Azure SQL ─────────────────────────────────────────────────────

    sql_servers_list: {
      description: "List Azure SQL servers.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["sql", "server", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    sql_databases_list: {
      description: "List databases on an Azure SQL server.",
      params: z.object({
        server: z.string().describe("SQL server name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) =>
        azExec(ctx, [
          "sql", "db", "list",
          "--server", params.server,
          "--resource-group", params.resource_group,
          "--output", "json",
        ]),
    },

    // ── AKS ───────────────────────────────────────────────────────────

    aks_list: {
      description: "List AKS clusters.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["aks", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    aks_describe: {
      description: "Show details of an AKS cluster.",
      params: z.object({
        name: z.string().describe("Cluster name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, ["aks", "show", "--name", params.name, "--resource-group", params.resource_group, "--output", "json"]),
    },

    aks_get_credentials: {
      description: "Download kubeconfig credentials for an AKS cluster.",
      params: z.object({
        name: z.string().describe("Cluster name"),
        resource_group: z.string().describe("Resource group name"),
      }),
      returns: z.unknown(),
      execute: async (params, ctx) =>
        azExec(ctx, ["aks", "get-credentials", "--name", params.name, "--resource-group", params.resource_group]),
    },

    // ── Azure Functions ───────────────────────────────────────────────

    functionapp_list: {
      description: "List Azure Function apps.",
      params: z.object({
        resource_group: z.string().optional().describe("Resource group filter"),
      }),
      returns: z.array(z.record(z.unknown())),
      execute: async (params, ctx) => {
        const args = ["functionapp", "list", "--output", "json"];
        if (params.resource_group) args.push("--resource-group", params.resource_group);
        return azExec(ctx, args);
      },
    },

    functionapp_create: {
      description: "Create an Azure Function app with consumption plan.",
      params: z.object({
        name: z.string().describe("Function app name"),
        resource_group: z.string().describe("Resource group name"),
        storage_account: z.string().describe("Storage account name for the function app"),
        location: z.string().describe("Azure region"),
        runtime: z.string().describe("Runtime (e.g. node, python, dotnet, java)"),
      }),
      returns: z.record(z.unknown()),
      execute: async (params, ctx) =>
        azExec(ctx, [
          "functionapp", "create",
          "--name", params.name,
          "--resource-group", params.resource_group,
          "--storage-account", params.storage_account,
          "--consumption-plan-location", params.location,
          "--runtime", params.runtime,
          "--output", "json",
        ]),
    },
  },
});
