import { execFile as execFileCallback } from "node:child_process";
import { existsSync } from "node:fs";
import path from "node:path";

export interface ExecResult {
  stdout: string;
  stderr: string;
}

export type ExecFileLike = (
  file: string,
  args: readonly string[],
  options: { cwd?: string; env?: NodeJS.ProcessEnv; maxBuffer: number },
) => Promise<ExecResult>;

export interface OfficeOsCliOptions {
  extensionPath: string;
  configuredCliPath?: string;
  env?: NodeJS.ProcessEnv;
  execFile?: ExecFileLike;
}

export interface CliInvocation {
  file: string;
  argsPrefix: readonly string[];
}

export const ResourceKinds = [
  "agents",
  "runs",
  "channels",
  "routines",
  "browsers",
  "memorystores",
  "engines",
  "providers",
  "models",
] as const;

export type ResourceKind = (typeof ResourceKinds)[number];

export class OfficeOsCli {
  private readonly extensionPath: string;
  private readonly configuredCliPath?: string;
  private readonly env?: NodeJS.ProcessEnv;
  private readonly execFile: ExecFileLike;

  constructor(options: OfficeOsCliOptions) {
    this.extensionPath = options.extensionPath;
    this.configuredCliPath = normalizeConfiguredPath(options.configuredCliPath);
    this.env = options.env;
    this.execFile = options.execFile ?? defaultExecFile;
  }

  async listResources(kind: ResourceKind): Promise<unknown[]> {
    const args =
      kind === "providers" || kind === "models"
        ? [kind, "-o", "json"]
        : ["get", kind, "-o", "json"];
    const value = await this.runJson<unknown>(args, kind);
    return Array.isArray(value) ? value : [value];
  }

  async describeResource(kind: string, name: string): Promise<unknown> {
    if (kind === "providers" || kind === "models") {
      const resources = await this.listResources(kind);
      return (
        resources.find(
          (resource) =>
            resourceName(resource) === name || resourceId(resource) === name,
        ) ?? { kind: singularKind(kind), name }
      );
    }

    return await this.runJson<unknown>(
      ["describe", `${kind}/${name}`, "-o", "json"],
      `${kind}/${name}`,
    );
  }

  async deleteResource(kind: string, name: string): Promise<void> {
    if (kind === "models") {
      throw new Error(
        "OfficeOS models are discovered from providers and cannot be deleted.",
      );
    }

    await this.runText(["delete", kind, name]);
  }

  async getContexts(): Promise<string[]> {
    const output = await this.runText(["config", "get-contexts"]);
    return output
      .split(/\r?\n/)
      .map((line) => line.replace(/^\*\s*/, "").trim())
      .filter((line) => line.length > 0);
  }

  async currentContext(): Promise<string> {
    return (await this.runText(["config", "current-context"])).trim();
  }

  async useContext(name: string): Promise<void> {
    await this.runText(["config", "use-context", name]);
  }

  async terminalCommand(args: readonly string[]): Promise<string> {
    const invocation = await this.resolveTerminalInvocation();
    return [
      quoteShell(invocation.file),
      ...invocation.argsPrefix.map(quoteShell),
      ...args.map(quoteShell),
    ].join(" ");
  }

  async runText(args: readonly string[]): Promise<string> {
    const result = await this.runRaw(args);
    return result.stdout.trimEnd();
  }

  async runJson<T>(args: readonly string[], label: string): Promise<T> {
    const output = await this.runText(args);
    return parseJsonOutput<T>(output, label);
  }

  private async runRaw(args: readonly string[]): Promise<ExecResult> {
    const primary = this.primaryInvocation();
    try {
      return await this.execInvocation(primary, args);
    } catch (error) {
      if (this.configuredCliPath || !isCommandNotFound(error)) {
        throw toCliError(error);
      }

      const fallback = buildDevFallback(this.extensionPath);
      if (!fallback) {
        throw toCliError(error);
      }

      try {
        return await this.execInvocation(fallback, args);
      } catch (fallbackError) {
        throw toCliError(fallbackError);
      }
    }
  }

  private primaryInvocation(): CliInvocation {
    return {
      file: this.configuredCliPath ?? "officeos",
      argsPrefix: [],
    };
  }

  private async resolveTerminalInvocation(): Promise<CliInvocation> {
    if (this.configuredCliPath) {
      return this.primaryInvocation();
    }

    try {
      await this.execInvocation(this.primaryInvocation(), ["help"]);
      return this.primaryInvocation();
    } catch (error) {
      if (!isCommandNotFound(error)) {
        return this.primaryInvocation();
      }

      return buildDevFallback(this.extensionPath) ?? this.primaryInvocation();
    }
  }

  private async execInvocation(
    invocation: CliInvocation,
    args: readonly string[],
  ): Promise<ExecResult> {
    return await this.execFile(
      invocation.file,
      [...invocation.argsPrefix, ...args],
      {
        env: this.env,
        maxBuffer: 10 * 1024 * 1024,
      },
    );
  }
}

export function parseJsonOutput<T>(output: string, label: string): T {
  const trimmed = output.trim();
  if (!trimmed) {
    throw new Error(`officeos returned no JSON for ${label}.`);
  }

  try {
    return JSON.parse(trimmed) as T;
  } catch (error) {
    throw new Error(
      `officeos returned invalid JSON for ${label}: ${(error as Error).message}`,
    );
  }
}

export function buildDevFallback(
  extensionPath: string,
): CliInvocation | undefined {
  const repoRoot = path.resolve(extensionPath, "../..");
  const cliEntry = path.join(repoRoot, "apps", "cli", "src", "app", "main.ts");
  return existsSync(cliEntry)
    ? { file: "bun", argsPrefix: [cliEntry] }
    : undefined;
}

export function resourceName(value: unknown): string {
  if (!value || typeof value !== "object") {
    return String(value);
  }

  const record = value as Record<string, unknown>;
  return String(record.name ?? record.id ?? record.displayName ?? "");
}

export function resourceId(value: unknown): string {
  if (!value || typeof value !== "object") {
    return "";
  }

  const record = value as Record<string, unknown>;
  return String(record.id ?? "");
}

export function singularKind(kind: string): string {
  const normalized = kind.toLowerCase();
  if (normalized === "memorystores") return "MemoryStore";
  return normalized.endsWith("s")
    ? `${normalized.slice(0, -1).charAt(0).toUpperCase()}${normalized.slice(1, -1)}`
    : `${normalized.charAt(0).toUpperCase()}${normalized.slice(1)}`;
}

function defaultExecFile(
  file: string,
  args: readonly string[],
  options: { cwd?: string; env?: NodeJS.ProcessEnv; maxBuffer: number },
): Promise<ExecResult> {
  return new Promise((resolve, reject) => {
    execFileCallback(file, [...args], options, (error, stdout, stderr) => {
      if (error) {
        reject(Object.assign(error, { stderr, stdout }));
        return;
      }

      resolve({ stdout, stderr });
    });
  });
}

function isCommandNotFound(error: unknown): boolean {
  return (
    typeof error === "object" &&
    error !== null &&
    "code" in error &&
    (error as { code?: unknown }).code === "ENOENT"
  );
}

function toCliError(error: unknown): Error {
  if (error instanceof Error) {
    const stderr =
      "stderr" in error
        ? String((error as { stderr?: unknown }).stderr ?? "").trim()
        : "";
    return stderr ? new Error(stderr) : error;
  }

  return new Error(String(error));
}

function normalizeConfiguredPath(value?: string): string | undefined {
  const trimmed = value?.trim();
  return trimmed && trimmed.length > 0 ? trimmed : undefined;
}

function quoteShell(value: string): string {
  if (/^[A-Za-z0-9_./:=@-]+$/.test(value)) {
    return value;
  }

  return `'${value.replace(/'/g, "'\\''")}'`;
}
