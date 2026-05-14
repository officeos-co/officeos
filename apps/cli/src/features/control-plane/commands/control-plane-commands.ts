import {
  readConfig,
  requireContext,
  setContext,
  useContext,
} from "../../../lib/config-store";
import { print } from "../../../shell/output";
import {
  createRun,
  deleteResource,
  describeResource,
  getRun,
  getRunLogs,
  listModels,
  listProviders,
  listResources,
  listRuns,
} from "../api/control-plane-api";

const ResourceKinds = [
  { kind: "agents", aliases: "agent", description: "Agent resources" },
  { kind: "runs", aliases: "run", description: "Run resources" },
  { kind: "channels", aliases: "channel", description: "Channel connections" },
  { kind: "routines", aliases: "routine", description: "Agent routines" },
  {
    kind: "memory-stores",
    aliases: "memorystore, memorystores",
    description: "Memory stores",
  },
  { kind: "engines", aliases: "engine", description: "Execution engines" },
] as const;

export async function getCommand(args: string[]): Promise<void> {
  const output = readOutput(args);
  const target = readResourceTarget(args);
  if (!target) {
    printResourceKinds(output);
    return;
  }

  const context = await requireContext();
  const [kind, name] = splitResource(target);
  const result = name
    ? await describeResource(context.apiUrl, context.token, kind, name)
    : await listResources(context.apiUrl, context.token, kind);
  printFormatted(result, output);
}

export async function describeCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const target = requireArg(args[0], "Usage: officeos describe <kind/name>");
  const [kind, name] = splitResource(target);
  if (!name) throw new Error("Describe requires <kind/name>.");
  printFormatted(
    await describeResource(context.apiUrl, context.token, kind, name),
    readOutput(args),
  );
}

export async function deleteCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const kind = requireArg(args[0], "Usage: officeos delete <kind> <name>");
  const name = requireArg(args[1], "Usage: officeos delete <kind> <name>");
  await deleteResource(context.apiUrl, context.token, kind, name);
  print(`${kind}/${name} deleted`);
}

export async function runCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const agent = requireArg(
    args[0],
    "Usage: officeos run <agent> --task <text> [--engine opencode] [--wait]",
  );
  const task = readOption(args, "--task");
  if (!task) throw new Error("Missing task. Use `--task <text>`.");
  const engine = readOption(args, "--engine") ?? "opencode";
  const wait = args.includes("--wait");
  const result = await createRun(
    context.apiUrl,
    context.token,
    agent,
    task,
    engine,
    wait,
  );
  const run = result.run ?? (result as unknown as { id: string });
  print(`run/${run.id}`);
}

export async function logsCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const id = normalizeRunId(
    requireArg(args[0], "Usage: officeos logs run/<id>"),
  );
  const result = await getRunLogs(context.apiUrl, context.token, id);
  for (const entry of result.entries)
    print(`${entry.time} ${entry.type} ${entry.content}`);
}

export async function waitCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const id = normalizeRunId(
    requireArg(
      args[0],
      "Usage: officeos wait run/<id> --for complete --timeout <duration>",
    ),
  );
  const timeout = parseDuration(readOption(args, "--timeout") ?? "10m");
  const deadline = Date.now() + timeout;
  for (;;) {
    const run = await getRun(context.apiUrl, context.token, id);
    if (["completed", "failed", "canceled"].includes(run.status)) {
      if (run.status !== "completed") {
        process.exitCode = 12;
        throw new Error(
          `Run ${run.status}${run.error ? `: ${run.error}` : ""}`,
        );
      }
      print(`run/${run.id} completed`);
      return;
    }
    if (Date.now() >= deadline) {
      process.exitCode = 10;
      throw new Error("Timed out waiting for run.");
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
}

export async function modelsCommand(args: string[] = []): Promise<void> {
  const context = await requireContext();
  printFormatted(
    await listModels(context.apiUrl, context.token),
    readOutput(args),
  );
}

export async function providersCommand(args: string[] = []): Promise<void> {
  const context = await requireContext();
  printFormatted(
    await listProviders(context.apiUrl, context.token),
    readOutput(args),
  );
}

export async function configCommand(args: string[]): Promise<void> {
  const sub = requireArg(
    args[0],
    "Usage: officeos config get-contexts|current-context|use-context|set-context",
  );
  const config = await readConfig();
  switch (sub) {
    case "get-contexts":
      for (const name of Object.keys(config?.contexts ?? {}))
        print(name === config?.currentContext ? `* ${name}` : `  ${name}`);
      break;
    case "current-context":
      print(config?.currentContext ?? "");
      break;
    case "use-context":
      await useContext(
        requireArg(args[1], "Usage: officeos config use-context <name>"),
      );
      break;
    case "set-context":
      await setContext(
        requireArg(
          args[1],
          "Usage: officeos config set-context <name> --api-url <url> --token <token>",
        ),
        requireArg(readOption(args, "--api-url"), "Missing --api-url."),
        requireArg(readOption(args, "--token"), "Missing --token."),
      );
      break;
    default:
      throw new Error(`Unknown config command '${sub}'.`);
  }
}

function splitResource(value: string): [string, string?] {
  const [kind, name] = value.split("/", 2);
  return [kind, name];
}

function normalizeRunId(value: string): string {
  return value.startsWith("run/") ? value.slice("run/".length) : value;
}

function readOutput(args: string[]): "table" | "json" | "yaml" | "name" {
  const value =
    readOption(args, "-o") ?? readOption(args, "--output") ?? "table";
  if (["table", "json", "yaml", "name"].includes(value))
    return value as "table" | "json" | "yaml" | "name";
  throw new Error(`Unknown output '${value}'.`);
}

function printFormatted(
  value: unknown,
  output: "table" | "json" | "yaml" | "name",
): void {
  if (output === "json" || output === "yaml") {
    print(JSON.stringify(value, null, 2));
    return;
  }
  const rows = Array.isArray(value) ? value : [value];
  if (output === "name") {
    for (const row of rows) print(resourceName(row));
    return;
  }
  for (const row of rows) print(formatRow(row));
}

function formatRow(value: unknown): string {
  if (!value || typeof value !== "object") return String(value);
  const record = value as Record<string, unknown>;
  const kind = record.kind ?? "";
  const name = record.name ?? record.id ?? "";
  const status = record.status ?? record.phase ?? record.enabled ?? "";
  return [kind, name, status]
    .filter((part) => String(part).length > 0)
    .join("\t");
}

function resourceName(value: unknown): string {
  if (!value || typeof value !== "object") return String(value);
  const record = value as Record<string, unknown>;
  return `${String(record.kind ?? "").toLowerCase()}/${record.name ?? record.id ?? ""}`;
}

function readOption(args: string[], name: string): string | undefined {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
}

function readResourceTarget(args: string[]): string | undefined {
  for (let index = 0; index < args.length; index += 1) {
    const arg = args[index];
    if (arg === "-o" || arg === "--output") {
      index += 1;
      continue;
    }

    if (arg.startsWith("-")) {
      throw new Error(`Unknown option '${arg}'.`);
    }

    return arg;
  }

  return undefined;
}

function printResourceKinds(output: "table" | "json" | "yaml" | "name"): void {
  if (output === "json" || output === "yaml") {
    print(JSON.stringify(ResourceKinds, null, 2));
    return;
  }

  if (output === "name") {
    for (const resource of ResourceKinds) print(resource.kind);
    return;
  }

  print("KIND\tALIASES\tDESCRIPTION");
  for (const resource of ResourceKinds) {
    print(`${resource.kind}\t${resource.aliases}\t${resource.description}`);
  }
}

function requireArg(value: string | undefined, message: string): string {
  if (!value) throw new Error(message);
  return value;
}

function parseDuration(value: string): number {
  const match = value.match(/^(\d+)(ms|s|m)?$/);
  if (!match) throw new Error(`Invalid duration '${value}'.`);
  const amount = Number(match[1]);
  const unit = match[2] ?? "ms";
  return unit === "m" ? amount * 60_000 : unit === "s" ? amount * 1000 : amount;
}
