const MCP_SERVER_NAME_RE = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const CUSTOM_MCP_LOGO =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="#52525B" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="m7 8 4 4-4 4"/><path d="M13 16h4"/><rect width="18" height="18" x="3" y="3" rx="2"/></svg>';
export const CUSTOM_MCP_EXAMPLE_JSON = JSON.stringify(
  {
    mcpServers: {
      "company-postgres": {
        command: "npx",
        args: ["-y", "@modelcontextprotocol/server-postgres"],
        env: {
          POSTGRES_CONNECTION_STRING: "",
        },
      },
    },
  },
  null,
  2,
);

type CustomMcpServerSource = {
  name: string;
  command: string;
  args: string[];
  credentialFields: Array<{ name: string }>;
  isBuiltin: boolean;
};

export type ParsedCustomMcpServer = {
  name: string;
  title: string;
  credentials: Record<string, string>;
  input: {
    name: string;
    title: string;
    description: string;
    subtitle: string;
    authorName: string;
    authorUrl: string;
    documentationUrl: string;
    repositoryUrl: string;
    tools?: Array<{ name: string; description: string }> | null;
    transportType: string;
    command?: string | null;
    args?: string | null;
    url?: string | null;
    category: string;
    credentialFieldsJson?: string | null;
    logo?: string | null;
  };
};

export function parseCustomMcpServersJson(source: string): ParsedCustomMcpServer[] {
  let parsed: unknown;
  try {
    parsed = JSON.parse(source);
  } catch {
    throw new Error("Invalid JSON.");
  }

  if (!isRecord(parsed) || !isRecord(parsed.mcpServers)) {
    throw new Error('Expected a JSON object with an "mcpServers" object.');
  }

  const servers = Object.entries(parsed.mcpServers).map(([name, value]) =>
    parseServer(name, value),
  );

  return servers;
}

export function buildCustomMcpServersJson(servers: CustomMcpServerSource[]): string {
  const mcpServers = Object.fromEntries(
    servers
      .filter((server) => !server.isBuiltin)
      .sort((a, b) => a.name.localeCompare(b.name))
      .map((server) => [
        server.name,
        {
          command: server.command || "npx",
          args: server.args,
          env: Object.fromEntries(
            server.credentialFields.map((field) => [field.name, ""]),
          ),
        },
      ]),
  );

  return JSON.stringify({ mcpServers }, null, 2);
}

export function buildInitialCustomMcpServersJson(
  servers: CustomMcpServerSource[],
): string {
  const hasCustomServers = servers.some((server) => !server.isBuiltin);
  return hasCustomServers
    ? buildCustomMcpServersJson(servers)
    : CUSTOM_MCP_EXAMPLE_JSON;
}

export function isUnchangedCustomMcpExample(source: string): boolean {
  try {
    return normalizeJson(source) === normalizeJson(CUSTOM_MCP_EXAMPLE_JSON);
  } catch {
    return false;
  }
}

function parseServer(name: string, value: unknown): ParsedCustomMcpServer {
  if (!MCP_SERVER_NAME_RE.test(name)) {
    throw new Error(
      `Invalid MCP server name "${name}". Use a lowercase slug up to 64 characters.`,
    );
  }

  if (!isRecord(value)) {
    throw new Error(`MCP server "${name}" must be an object.`);
  }

  const command = value.command;
  if (typeof command !== "string" || command.trim().length === 0) {
    throw new Error(`MCP server "${name}" requires a command.`);
  }

  const args = parseArgs(name, value.args);
  const credentials = parseEnv(name, value.env);
  const credentialFields = Object.keys(credentials.all).map((key) => ({
    name: key,
    label: toLabel(key),
    type: "password",
    required: true,
  }));
  const title = toTitle(name);

  return {
    name,
    title,
    credentials: credentials.withValues,
    input: {
      name,
      title,
      description: "",
      subtitle: "Custom MCP server",
      authorName: "Custom",
      authorUrl: "",
      documentationUrl: "",
      repositoryUrl: "",
      tools: null,
      transportType: "stdio",
      command: command.trim(),
      args: args.length > 0 ? JSON.stringify(args) : null,
      url: null,
      category: "custom",
      credentialFieldsJson:
        credentialFields.length > 0 ? JSON.stringify(credentialFields) : null,
      logo: CUSTOM_MCP_LOGO,
    },
  };
}

function parseArgs(name: string, value: unknown): string[] {
  if (value === undefined) return [];
  if (!Array.isArray(value) || value.some((item) => typeof item !== "string")) {
    throw new Error(`MCP server "${name}" args must be a string array.`);
  }
  return value;
}

function parseEnv(
  name: string,
  value: unknown,
): { all: Record<string, string>; withValues: Record<string, string> } {
  if (value === undefined) return { all: {}, withValues: {} };
  if (!isRecord(value)) {
    throw new Error(`MCP server "${name}" env must be an object.`);
  }

  const entries = Object.entries(value);
  const invalid = entries.find(([, envValue]) => typeof envValue !== "string");
  if (invalid) {
    throw new Error(`MCP server "${name}" env values must be strings.`);
  }

  const stringEntries = entries as Array<[string, string]>;
  const all = Object.fromEntries(stringEntries) as Record<string, string>;
  const withValues = Object.fromEntries(
    stringEntries.filter(([, envValue]) => envValue.trim().length > 0),
  ) as Record<string, string>;

  return { all, withValues };
}

function toTitle(name: string): string {
  return name
    .split(/[._-]+/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function toLabel(name: string): string {
  return name
    .split("_")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
    .join(" ");
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function normalizeJson(source: string): string {
  return JSON.stringify(JSON.parse(source));
}
