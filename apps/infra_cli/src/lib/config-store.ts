import { mkdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { homedir } from "node:os";
import { resolveApiUrl } from "./env";

export interface EaosContext {
  apiUrl: string;
  token: string;
}

export interface EaosConfig {
  currentContext: string;
  contexts: Record<string, EaosContext>;
}

const configPath = join(homedir(), ".eaos", "config.yaml");

export async function readConfig(): Promise<EaosConfig | null> {
  try {
    const raw = await readFile(configPath, "utf8");
    return parseConfig(raw);
  } catch (error) {
    if ((error as NodeJS.ErrnoException).code === "ENOENT") return null;
    throw error;
  }
}

export async function writeContext(name: string, apiUrl: string, token: string): Promise<void> {
  const existing = await readConfig();
  const next: EaosConfig = {
    currentContext: name,
    contexts: {
      ...(existing?.contexts ?? {}),
      [name]: { apiUrl: resolveApiUrl(apiUrl), token },
    },
  };
  await mkdir(dirname(configPath), { recursive: true });
  await writeFile(configPath, serializeConfig(next), { mode: 0o600 });
}

function parseConfig(raw: string): EaosConfig {
  const lines = raw.split(/\r?\n/);
  const currentContext = valueAfter(lines.find((line) => line.startsWith("currentContext:")) ?? "");
  const contexts: Record<string, EaosContext> = {};
  let active: string | null = null;
  for (const line of lines) {
    const contextMatch = line.match(/^  ([^:\s]+):\s*$/);
    if (contextMatch) {
      active = contextMatch[1];
      contexts[active] = { apiUrl: "", token: "" };
      continue;
    }
    if (!active) continue;
    if (line.startsWith("    apiUrl:")) contexts[active].apiUrl = valueAfter(line);
    if (line.startsWith("    token:")) contexts[active].token = valueAfter(line);
  }
  return { currentContext, contexts };
}

function serializeConfig(config: EaosConfig): string {
  const lines = [`currentContext: ${config.currentContext}`, "contexts:"];
  for (const [name, context] of Object.entries(config.contexts)) {
    lines.push(`  ${name}:`);
    lines.push(`    apiUrl: ${context.apiUrl}`);
    lines.push(`    token: ${context.token}`);
  }
  return `${lines.join("\n")}\n`;
}

function valueAfter(line: string): string {
  return line.slice(line.indexOf(":") + 1).trim();
}

export async function requireContext(): Promise<EaosContext> {
  const config = await readConfig();
  const context = config?.contexts[config.currentContext];
  if (!context) {
    throw new Error("Not logged in. Run `eaos login` first.");
  }
  return context;
}
