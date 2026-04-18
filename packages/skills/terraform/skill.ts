import { defineSkill, z } from "@harro/skill-sdk";
import manifest from "./skill.json" with { type: "json" };
import doc from "./SKILL.md";

async function tfExec(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  args: string[],
) {
  const res = await ctx.fetch(ctx.credentials.proxy_url, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ command: "terraform", args }),
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(`Terraform proxy ${res.status}: ${body}`);
  }
  return res.json();
}

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Init ───────────────────────────────────────────────────────────

    init: {
      description: "Initialize a Terraform working directory.",
      params: z.object({
        directory: z.string().default(".").describe("Path to the configuration directory"),
        backend_config: z
          .string()
          .optional()
          .describe("Backend config key=value (e.g. bucket=my-bucket)"),
        upgrade: z.boolean().default(false).describe("Upgrade modules and plugins to latest"),
        reconfigure: z
          .boolean()
          .default(false)
          .describe("Reconfigure backend, ignoring saved config"),
      }),
      returns: z.object({
        output: z.string().describe("Init output"),
      }),
      execute: async (params, ctx) => {
        const args = ["init"];
        if (params.backend_config) args.push(`-backend-config=${params.backend_config}`);
        if (params.upgrade) args.push("-upgrade");
        if (params.reconfigure) args.push("-reconfigure");
        if (params.directory !== ".") args.push(params.directory);
        return tfExec(ctx, args);
      },
    },

    // ── Plan ───────────────────────────────────────────────────────────

    plan: {
      description: "Create an execution plan.",
      params: z.object({
        directory: z.string().default(".").describe("Path to the configuration directory"),
        var: z.string().optional().describe("Variable in key=value format"),
        var_file: z.string().optional().describe("Path to a .tfvars file"),
        target: z.string().optional().describe("Resource address to target"),
        out: z.string().optional().describe("Save plan to file"),
        destroy: z.boolean().default(false).describe("Plan a destroy operation"),
      }),
      returns: z.object({
        output: z.string().describe("Plan output"),
        has_changes: z.boolean().describe("Whether changes are planned"),
      }),
      execute: async (params, ctx) => {
        const args = ["plan"];
        if (params.var) args.push(`-var=${params.var}`);
        if (params.var_file) args.push(`-var-file=${params.var_file}`);
        if (params.target) args.push(`-target=${params.target}`);
        if (params.out) args.push(`-out=${params.out}`);
        if (params.destroy) args.push("-destroy");
        if (params.directory !== ".") args.push(params.directory);
        return tfExec(ctx, args);
      },
    },

    // ── Apply ──────────────────────────────────────────────────────────

    apply: {
      description: "Apply changes to infrastructure.",
      params: z.object({
        directory: z.string().default(".").describe("Path to the configuration directory"),
        auto_approve: z.boolean().default(false).describe("Skip interactive approval"),
        var: z.string().optional().describe("Variable in key=value format"),
        var_file: z.string().optional().describe("Path to a .tfvars file"),
        target: z.string().optional().describe("Resource address to target"),
        plan_file: z.string().optional().describe("Path to a saved plan file"),
      }),
      returns: z.object({
        output: z.string().describe("Apply output"),
      }),
      execute: async (params, ctx) => {
        const args = ["apply"];
        if (params.auto_approve) args.push("-auto-approve");
        if (params.var) args.push(`-var=${params.var}`);
        if (params.var_file) args.push(`-var-file=${params.var_file}`);
        if (params.target) args.push(`-target=${params.target}`);
        if (params.plan_file) args.push(params.plan_file);
        else if (params.directory !== ".") args.push(params.directory);
        return tfExec(ctx, args);
      },
    },

    // ── Destroy ────────────────────────────────────────────────────────

    destroy: {
      description: "Destroy managed infrastructure.",
      params: z.object({
        directory: z.string().default(".").describe("Path to the configuration directory"),
        auto_approve: z.boolean().default(false).describe("Skip interactive approval"),
        var: z.string().optional().describe("Variable in key=value format"),
        var_file: z.string().optional().describe("Path to a .tfvars file"),
        target: z.string().optional().describe("Resource address to target"),
      }),
      returns: z.object({
        output: z.string().describe("Destroy output"),
      }),
      execute: async (params, ctx) => {
        const args = ["destroy"];
        if (params.auto_approve) args.push("-auto-approve");
        if (params.var) args.push(`-var=${params.var}`);
        if (params.var_file) args.push(`-var-file=${params.var_file}`);
        if (params.target) args.push(`-target=${params.target}`);
        if (params.directory !== ".") args.push(params.directory);
        return tfExec(ctx, args);
      },
    },

    // ── State ──────────────────────────────────────────────────────────

    list_resources: {
      description: "List all resources tracked in state.",
      params: z.object({}),
      returns: z.array(
        z.object({
          address: z.string().describe("Resource address"),
        }),
      ),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["state", "list"]);
      },
    },

    show_resource: {
      description: "Show attributes of a single resource in state.",
      params: z.object({
        address: z.string().describe("Resource address to inspect"),
      }),
      returns: z.object({
        address: z.string().describe("Resource address"),
        attributes: z.record(z.any()).describe("Resource attributes"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["state", "show", params.address]);
      },
    },

    mv_resource: {
      description: "Move a resource to a new address in state.",
      params: z.object({
        source: z.string().describe("Current resource address"),
        destination: z.string().describe("New resource address"),
      }),
      returns: z.object({
        output: z.string().describe("Move result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["state", "mv", params.source, params.destination]);
      },
    },

    rm_resource: {
      description: "Remove a resource from state (does not destroy the real resource).",
      params: z.object({
        address: z.string().describe("Resource address to remove"),
      }),
      returns: z.object({
        output: z.string().describe("Remove result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["state", "rm", params.address]);
      },
    },

    pull_state: {
      description: "Pull remote state and output it as JSON.",
      params: z.object({}),
      returns: z.object({
        state: z.any().describe("Raw state JSON"),
      }),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["state", "pull"]);
      },
    },

    push_state: {
      description: "Push a local state file to the configured backend.",
      params: z.object({
        state_file: z.string().describe("Path to local state file"),
      }),
      returns: z.object({
        output: z.string().describe("Push result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["state", "push", params.state_file]);
      },
    },

    // ── Output ─────────────────────────────────────────────────────────

    list_outputs: {
      description: "List all outputs from the state.",
      params: z.object({}),
      returns: z.record(z.any()).describe("Map of output names to values"),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["output", "-json"]);
      },
    },

    get_output: {
      description: "Get a single output value.",
      params: z.object({
        name: z.string().describe("Output name"),
      }),
      returns: z.object({
        name: z.string().describe("Output name"),
        value: z.any().describe("Output value"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["output", "-json", params.name]);
      },
    },

    // ── Workspace ──────────────────────────────────────────────────────

    list_workspaces: {
      description: "List all workspaces.",
      params: z.object({}),
      returns: z.array(
        z.object({
          name: z.string().describe("Workspace name"),
          current: z.boolean().describe("Is the currently selected workspace"),
        }),
      ),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["workspace", "list"]);
      },
    },

    new_workspace: {
      description: "Create a new workspace.",
      params: z.object({
        name: z.string().describe("Workspace name"),
      }),
      returns: z.object({
        name: z.string().describe("Created workspace name"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["workspace", "new", params.name]);
      },
    },

    select_workspace: {
      description: "Switch to an existing workspace.",
      params: z.object({
        name: z.string().describe("Workspace name"),
      }),
      returns: z.object({
        name: z.string().describe("Selected workspace name"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["workspace", "select", params.name]);
      },
    },

    delete_workspace: {
      description: "Delete a workspace.",
      params: z.object({
        name: z.string().describe("Workspace name"),
      }),
      returns: z.object({
        name: z.string().describe("Deleted workspace name"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["workspace", "delete", params.name]);
      },
    },

    show_workspace: {
      description: "Show the currently selected workspace.",
      params: z.object({}),
      returns: z.object({
        name: z.string().describe("Current workspace name"),
      }),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["workspace", "show"]);
      },
    },

    // ── Import ─────────────────────────────────────────────────────────

    import_resource: {
      description: "Import existing infrastructure into Terraform state.",
      params: z.object({
        address: z.string().describe("Resource address in configuration"),
        id: z.string().describe("Provider-specific resource ID"),
      }),
      returns: z.object({
        output: z.string().describe("Import result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["import", params.address, params.id]);
      },
    },

    // ── Validate ───────────────────────────────────────────────────────

    validate: {
      description: "Validate Terraform configuration files.",
      params: z.object({
        directory: z.string().default(".").describe("Path to the configuration directory"),
      }),
      returns: z.object({
        valid: z.boolean().describe("Whether configuration is valid"),
        error_count: z.number().describe("Number of errors"),
        warning_count: z.number().describe("Number of warnings"),
        diagnostics: z.array(z.any()).describe("Validation diagnostics"),
      }),
      execute: async (params, ctx) => {
        const args = ["validate", "-json"];
        if (params.directory !== ".") args.push(params.directory);
        return tfExec(ctx, args);
      },
    },

    // ── Format ─────────────────────────────────────────────────────────

    fmt: {
      description: "Format Terraform configuration files.",
      params: z.object({
        check: z
          .boolean()
          .default(false)
          .describe("Check if files are formatted (exit code only)"),
        diff: z.boolean().default(false).describe("Display diffs of formatting changes"),
        recursive: z.boolean().default(false).describe("Also process subdirectories"),
      }),
      returns: z.object({
        output: z.string().describe("Fmt output"),
        files: z.array(z.string()).describe("Files that were formatted or need formatting"),
      }),
      execute: async (params, ctx) => {
        const args = ["fmt"];
        if (params.check) args.push("-check");
        if (params.diff) args.push("-diff");
        if (params.recursive) args.push("-recursive");
        return tfExec(ctx, args);
      },
    },

    // ── Providers ──────────────────────────────────────────────────────

    list_providers: {
      description: "List providers required by the configuration.",
      params: z.object({}),
      returns: z.array(
        z.object({
          namespace: z.string().describe("Provider namespace"),
          name: z.string().describe("Provider name"),
          version: z.string().describe("Version constraint"),
        }),
      ),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["providers"]);
      },
    },

    lock_providers: {
      description: "Write provider dependency hashes to the lock file.",
      params: z.object({}),
      returns: z.object({
        output: z.string().describe("Lock result"),
      }),
      execute: async (_params, ctx) => {
        return tfExec(ctx, ["providers", "lock"]);
      },
    },

    // ── Graph ──────────────────────────────────────────────────────────

    graph: {
      description: "Generate a visual dependency graph in DOT format.",
      params: z.object({
        type: z
          .enum(["plan", "apply"])
          .default("plan")
          .describe("Graph type: plan or apply"),
      }),
      returns: z.object({
        dot: z.string().describe("Graph in DOT format"),
      }),
      execute: async (params, ctx) => {
        const args = ["graph", `-type=${params.type}`];
        return tfExec(ctx, args);
      },
    },

    // ── Taint / Untaint ────────────────────────────────────────────────

    taint: {
      description:
        "Mark a resource as tainted, forcing recreation on the next apply. Deprecated in Terraform 1.5+; prefer -replace on plan/apply.",
      params: z.object({
        address: z.string().describe("Resource address to taint"),
      }),
      returns: z.object({
        output: z.string().describe("Taint result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["taint", params.address]);
      },
    },

    untaint: {
      description: "Remove the taint from a resource.",
      params: z.object({
        address: z.string().describe("Resource address to untaint"),
      }),
      returns: z.object({
        output: z.string().describe("Untaint result"),
      }),
      execute: async (params, ctx) => {
        return tfExec(ctx, ["untaint", params.address]);
      },
    },
  },
});
