import { createHash, randomBytes } from "node:crypto";
import { createServer, type Server } from "node:http";
import {
  readConfig,
  requireContext,
  setContext,
  useContext,
} from "../../../lib/config-store";
import { print } from "../../../shell/output";
import {
  authenticateCodexProvider,
  deleteResource,
  describeResource,
  getResourceLogs,
  listModels,
  listProviders,
  listResources,
  sendAgentMessage,
} from "../api/control-plane-api";
import { openBrowser } from "../../../shell/browser";

const ResourceKinds = [
  { kind: "agents", aliases: "agent", description: "Agent resources" },
  { kind: "channels", aliases: "channel", description: "Channel connections" },
  { kind: "routines", aliases: "routine", description: "Agent routines" },
  { kind: "credentials", aliases: "credential", description: "Routine credentials" },
  { kind: "browsers", aliases: "browser", description: "Browser resources" },
  { kind: "integrations", aliases: "integration", description: "Integration deployments" },
  {
    kind: "memory-stores",
    aliases: "memorystore, memorystores",
    description: "Memory stores",
  },
  { kind: "providers", aliases: "provider", description: "Configured provider resources" },
  { kind: "engines", aliases: "engine", description: "Execution engines" },
] as const;

const DeletableResourceKinds = [
  "routines",
  "credentials",
  "channels",
  "integrations",
  "browsers",
  "memory-stores",
  "agents",
  "providers",
] as const;

interface LogsOptions {
  tail?: number;
  since?: string;
  sinceTime?: string;
  type?: string;
  severity?: string;
  follow?: boolean;
}

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
  if (args[0] === "--all") {
    let deleted = 0;
    for (const kind of DeletableResourceKinds) {
      for (;;) {
        const resources = await listResources(context.apiUrl, context.token, kind);
        if (resources.length === 0) break;

        let deletedInBatch = 0;
        for (const resource of resources) {
          const name = resourceDeleteIdentifier(kind, resource);
          if (!name) continue;
          try {
            await deleteResource(context.apiUrl, context.token, kind, name);
            deleted += 1;
            deletedInBatch += 1;
            print(`${kind}/${resourceDisplayName(resource) || name} deleted`);
          } catch (error) {
            if (!isNotFoundError(error)) throw error;
          }
        }

        if (deletedInBatch === 0) break;
      }
    }
    print(`${deleted} resources deleted`);
    return;
  }

  const kind = requireArg(args[0], "Usage: officeos delete <kind> <name>");
  const name = requireArg(args[1], "Usage: officeos delete <kind> <name>");
  await deleteResource(context.apiUrl, context.token, kind, name);
  print(`${kind}/${name} deleted`);
}

export async function runCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const agent = requireArg(
    args[0],
    "Usage: officeos run <agent> --task <text>",
  );
  const task = readRequiredTextOption(
    args,
    ["--task"],
    "Missing task. Use `--task <text>`.",
  );
  rejectRemovedRunOptions(args);
  const result = await sendAgentMessage(
    context.apiUrl,
    context.token,
    agent,
    task,
  );
  printAgentWork(result);
}

export async function sendCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const agent = requireArg(
    args[0],
    "Usage: officeos send <agent> --message <text>",
  );
  const message = readRequiredTextOption(
    args,
    ["--message", "-m"],
    "Missing message. Use `--message <text>`.",
  );
  const result = await sendAgentMessage(
    context.apiUrl,
    context.token,
    agent,
    message,
  );
  printAgentWork(result);
}

function printAgentWork(
  result: Awaited<ReturnType<typeof sendAgentMessage>>,
): void {
  print(`agent/${result.agentName}\twork/${result.workLogId}\t${result.status}`);
}

export async function logsCommand(args: string[]): Promise<void> {
  const context = await requireContext();
  const { kind, name, options } = readLogsTarget(args);
  const result = await getResourceLogs(
    context.apiUrl,
    context.token,
    kind,
    name,
    options,
  );
  if (result.length > 0) print(result);
}

export async function modelsCommand(args: string[] = []): Promise<void> {
  const context = await requireContext();
  const models = await listModels(context.apiUrl, context.token);
  printFormatted(models, readOutput(args));
}

export async function providersCommand(args: string[] = []): Promise<void> {
  const context = await requireContext();
  const providers = await listProviders(context.apiUrl, context.token);
  printFormatted(providers, readOutput(args));
}

export async function providerCommand(args: string[]): Promise<void> {
  const sub = requireArg(args[0], "Usage: officeos provider auth codex");
  switch (sub) {
    case "auth":
      await providerAuthCommand(args.slice(1));
      break;
    default:
      throw new Error(`Unknown provider command '${sub}'.`);
  }
}

export async function credentialCommand(args: string[]): Promise<void> {
  const sub = requireArg(args[0], "Usage: officeos credential auth github");
  switch (sub) {
    case "auth":
      await credentialAuthCommand(args.slice(1));
      break;
    default:
      throw new Error(`Unknown credential command '${sub}'.`);
  }
}

async function providerAuthCommand(args: string[]): Promise<void> {
  const provider = requireArg(args[0], "Usage: officeos provider auth codex");
  if (provider !== "codex") {
    throw new Error(`Unsupported provider auth target '${provider}'.`);
  }

  const context = await requireContext();
  const oauth = codexOAuthOptions(args);
  const codeVerifier = base64Url(randomBytes(32));
  const codeChallenge = base64Url(createHash("sha256").update(codeVerifier).digest());
  const state = base64Url(randomBytes(24));
  const redirectUri = new URL(oauth.redirectUri);
  const callback = waitForOAuthCallback(redirectUri, state);

  const authUrl = new URL(oauth.authorizationUrl);
  authUrl.searchParams.set("response_type", "code");
  authUrl.searchParams.set("client_id", oauth.clientId);
  authUrl.searchParams.set("redirect_uri", oauth.redirectUri);
  authUrl.searchParams.set("code_challenge", codeChallenge);
  authUrl.searchParams.set("code_challenge_method", "S256");
  authUrl.searchParams.set("state", state);
  if (oauth.scope) authUrl.searchParams.set("scope", oauth.scope);

  print(`Open this URL to authenticate Codex: ${authUrl.toString()}`);
  if (!args.includes("--no-browser")) {
    await openBrowser(authUrl.toString()).catch(() => undefined);
  }

  try {
    const code = await callback.code;
    const token = await exchangeCodexCode(oauth, code, codeVerifier);
    const idClaims = token.id_token ? decodeJwtPayload(token.id_token) : {};
    const authClaims = objectValue(idClaims["https://api.openai.com/auth"]);
    const accountId = stringValue(authClaims?.chatgpt_account_id)
      ?? stringValue(authClaims?.account_id)
      ?? stringValue(idClaims.account_id);
    const accountEmail = stringValue(idClaims.email);
    const result = await authenticateCodexProvider(context.apiUrl, context.token, {
      accessToken: token.access_token,
      refreshToken: token.refresh_token,
      expiresAt: token.expires_in ? new Date(Date.now() + token.expires_in * 1000).toISOString() : undefined,
      accountEmail,
      accountId,
      clientId: oauth.clientId,
      tokenUrl: oauth.tokenUrl,
      scopes: oauth.scope ? oauth.scope.split(/\s+/).filter(Boolean) : undefined,
    });
    printFormatted(result, readOutput(args));
  } finally {
    await callback.close();
  }
}

async function credentialAuthCommand(args: string[]): Promise<void> {
  const provider = requireArg(args[0], "Usage: officeos credential auth github");
  if (provider !== "github") {
    throw new Error(`Unsupported credential auth target '${provider}'.`);
  }

  const context = await requireContext();
  const name = readOption(args, "--name") ?? "github";
  const returnTo = readOption(args, "--return-to") ?? `/credentials/${encodeURIComponent(name)}`;
  const authUrl = new URL("/api/auth/github", context.apiUrl);
  authUrl.searchParams.set("returnTo", returnTo);

  print(`Open this URL to authenticate GitHub: ${authUrl.toString()}`);
  if (!args.includes("--no-browser")) {
    await openBrowser(authUrl.toString()).catch(() => undefined);
  }
  print(`Complete the GitHub OAuth flow in the dashboard. The credential is saved as credential/${name}.`);
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

function readLogsTarget(args: string[]): {
  kind: string;
  name: string;
  options: LogsOptions;
} {
  const target = requireArg(
    args[0],
    "Usage: officeos logs <kind/name> [--tail <n>] [--since <duration>] [--type <type>] [--severity <level>]",
  );
  let optionStart = 1;
  let kind: string;
  let name: string | undefined;

  if (target.includes("/")) {
    [kind, name] = splitResource(target);
  } else {
    kind = target;
    if (args[1] && !args[1].startsWith("-")) {
      name = args[1];
      optionStart = 2;
    }
  }

  if (!name) {
    throw new Error("Logs requires <kind/name>.");
  }

  const options: LogsOptions = {};
  for (let index = optionStart; index < args.length; index += 1) {
    const arg = args[index];
    switch (arg) {
      case "--tail": {
        const value = requireArg(args[++index], "Missing --tail value.");
        const tail = Number(value);
        if (!Number.isInteger(tail) || tail <= 0) {
          throw new Error("--tail must be a positive integer.");
        }
        options.tail = tail;
        break;
      }
      case "--since":
        options.since = requireArg(args[++index], "Missing --since value.");
        break;
      case "--since-time":
        options.sinceTime = requireArg(
          args[++index],
          "Missing --since-time value.",
        );
        break;
      case "--type":
        options.type = requireArg(args[++index], "Missing --type value.");
        break;
      case "--severity":
        options.severity = requireArg(
          args[++index],
          "Missing --severity value.",
        );
        break;
      case "-f":
      case "--follow":
        options.follow = true;
        break;
      default:
        throw new Error(`Unknown logs option '${arg}'.`);
    }
  }

  return { kind, name, options };
}

interface CodexOAuthOptions {
  authorizationUrl: string;
  tokenUrl: string;
  clientId: string;
  redirectUri: string;
  scope: string;
}

interface CodexTokenResponse {
  access_token: string;
  refresh_token: string;
  id_token?: string;
  expires_in?: number;
}

function codexOAuthOptions(args: string[]): CodexOAuthOptions {
  return {
    authorizationUrl: readOption(args, "--auth-url")
      ?? process.env.CODEX_OAUTH_AUTHORIZATION_URL
      ?? "https://auth.openai.com/oauth/authorize",
    tokenUrl: readOption(args, "--token-url")
      ?? process.env.CODEX_OAUTH_TOKEN_URL
      ?? "https://auth.openai.com/oauth/token",
    clientId: readOption(args, "--client-id")
      ?? process.env.CODEX_OAUTH_CLIENT_ID
      ?? "app_EMoamEEZ73f0CkXaXp7hrann",
    redirectUri: readOption(args, "--redirect-uri")
      ?? process.env.CODEX_OAUTH_REDIRECT_URI
      ?? "http://localhost:1455/auth/callback",
    scope: readOption(args, "--scope")
      ?? process.env.CODEX_OAUTH_SCOPE
      ?? "openid profile email offline_access",
  };
}

function waitForOAuthCallback(redirectUri: URL, expectedState: string, label = "Codex"): {
  code: Promise<string>;
  close: () => Promise<void>;
} {
  let server: Server | undefined;
  const code = new Promise<string>((resolve, reject) => {
    server = createServer((req, res) => {
      const requestUrl = new URL(req.url ?? "/", redirectUri.origin);
      if (requestUrl.pathname !== redirectUri.pathname) {
        res.writeHead(404).end("Not found");
        return;
      }

      const error = requestUrl.searchParams.get("error");
      const state = requestUrl.searchParams.get("state");
      const authCode = requestUrl.searchParams.get("code");
      if (error) {
        res.writeHead(400).end(`${label} authentication failed. Return to the terminal.`);
        reject(new Error(`${label} authentication failed: ${error}`));
        return;
      }
      if (state !== expectedState) {
        res.writeHead(400).end("Invalid OAuth state. Return to the terminal.");
        reject(new Error("Invalid OAuth state."));
        return;
      }
      if (!authCode) {
        res.writeHead(400).end("Missing authorization code. Return to the terminal.");
        reject(new Error("Missing authorization code."));
        return;
      }

      res.writeHead(200, { "content-type": "text/plain" }).end(`${label} authenticated. You can close this tab.`);
      resolve(authCode);
    });

    const port = Number(redirectUri.port);
    if (!Number.isInteger(port) || port <= 0) {
      reject(new Error(`${label} redirect URI must include a fixed localhost port.`));
      return;
    }
    server.once("error", (error) => reject(error));
    server.listen(port, redirectUri.hostname);
  });

  return {
    code,
    close: () => new Promise((resolve) => server?.close(() => resolve()) ?? resolve()),
  };
}

async function exchangeCodexCode(options: CodexOAuthOptions, code: string, codeVerifier: string): Promise<CodexTokenResponse> {
  const body = new URLSearchParams({
    grant_type: "authorization_code",
    code,
    redirect_uri: options.redirectUri,
    code_verifier: codeVerifier,
    client_id: options.clientId,
  });
  const response = await fetch(options.tokenUrl, {
    method: "POST",
    headers: { "content-type": "application/x-www-form-urlencoded" },
    body,
  });
  if (!response.ok) {
    throw new Error(`Codex token exchange failed: ${response.status} ${response.statusText}`);
  }

  const token = (await response.json()) as Partial<CodexTokenResponse>;
  if (!token.access_token || !token.refresh_token) {
    throw new Error("Codex token exchange did not return access and refresh tokens.");
  }
  return token as CodexTokenResponse;
}

function decodeJwtPayload(jwt: string): Record<string, unknown> {
  const [, payload] = jwt.split(".");
  if (!payload) return {};
  try {
    return JSON.parse(Buffer.from(payload, "base64url").toString("utf8")) as Record<string, unknown>;
  } catch {
    return {};
  }
}

function objectValue(value: unknown): Record<string, unknown> | undefined {
  return value && typeof value === "object" && !Array.isArray(value)
    ? value as Record<string, unknown>
    : undefined;
}

function stringValue(value: unknown): string | undefined {
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

function base64Url(bytes: Uint8Array): string {
  return Buffer.from(bytes)
    .toString("base64")
    .replaceAll("+", "-")
    .replaceAll("/", "_")
    .replaceAll("=", "");
}

function rejectRemovedRunOptions(args: string[]): void {
  for (const option of ["--engine", "--wait"]) {
    if (args.includes(option)) {
      throw new Error(`${option} was removed with run resources; agent work now uses the agent's configured execution path.`);
    }
  }
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
  const health = record.health;
  const healthRecord = health && typeof health === "object" ? health as Record<string, unknown> : null;
  const status = healthRecord?.state
    ? `${healthRecord.state}:${healthRecord.reason ?? record.status ?? ""}`
    : record.status ?? record.phase ?? record.enabled ?? "";
  return [kind, name, status]
    .filter((part) => String(part).length > 0)
    .join("\t");
}

function resourceName(value: unknown): string {
  if (!value || typeof value !== "object") return String(value);
  const record = value as Record<string, unknown>;
  return `${String(record.kind ?? "").toLowerCase()}/${record.name ?? record.id ?? ""}`;
}

function resourceDeleteIdentifier(kind: typeof DeletableResourceKinds[number], value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const record = value as Record<string, unknown>;
  const name = kind === "integrations" || kind === "providers"
    ? record.name ?? record.id
    : record.id ?? record.name;
  return typeof name === "string" || typeof name === "number" ? String(name) : "";
}

function resourceDisplayName(value: unknown): string {
  if (!value || typeof value !== "object") return "";
  const record = value as Record<string, unknown>;
  const name = record.name ?? record.displayName ?? record.id;
  return typeof name === "string" || typeof name === "number" ? String(name) : "";
}

function isNotFoundError(error: unknown): boolean {
  return error instanceof Error && /was not found|404\b/i.test(error.message);
}

function readOption(args: string[], name: string): string | undefined {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
}

function readRequiredTextOption(
  args: string[],
  names: readonly string[],
  missingMessage: string,
): string {
  const value = names.map((name) => readOption(args, name)).find(Boolean);
  const trimmed = value?.trim();
  if (!trimmed) throw new Error(missingMessage);
  return trimmed;
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
