import { describe, expect, test } from "bun:test";
import {
  CUSTOM_MCP_EXAMPLE_JSON,
  buildInitialCustomMcpServersJson,
  buildCustomMcpServersJson,
  isUnchangedCustomMcpExample,
  parseCustomMcpServersJson,
} from "@/features/agents";

describe("parseCustomMcpServersJson", () => {
  test("parses Claude-style stdio MCP servers", () => {
    const [server] = parseCustomMcpServersJson(
      JSON.stringify({
        mcpServers: {
          "my-server": {
            command: "npx",
            args: ["-y", "some-mcp-server"],
            env: { API_KEY: "secret" },
          },
        },
      }),
    );

    expect(server.name).toBe("my-server");
    expect(server.title).toBe("My Server");
    expect(server.input.transportType).toBe("stdio");
    expect(server.input.command).toBe("npx");
    expect(server.input.args).toBe(JSON.stringify(["-y", "some-mcp-server"]));
    expect(server.credentials).toEqual({ API_KEY: "secret" });
    expect(server.input.credentialFieldsJson).toContain("API_KEY");
  });

  test("keeps blank env keys as credential fields without overwriting secrets", () => {
    const [server] = parseCustomMcpServersJson(
      JSON.stringify({
        mcpServers: {
          "my-server": {
            command: "npx",
            env: { API_KEY: "" },
          },
        },
      }),
    );

    expect(server.credentials).toEqual({});
    expect(server.input.credentialFieldsJson).toContain("API_KEY");
  });

  test("allows an empty custom server source of truth", () => {
    expect(parseCustomMcpServersJson(JSON.stringify({ mcpServers: {} }))).toEqual(
      [],
    );
  });

  test("rejects invalid JSON", () => {
    expect(() => parseCustomMcpServersJson("{")).toThrow("Invalid JSON.");
  });

  test("rejects missing mcpServers object", () => {
    expect(() => parseCustomMcpServersJson("{}")).toThrow("mcpServers");
  });

  test("rejects invalid server names", () => {
    expect(() =>
      parseCustomMcpServersJson(
        JSON.stringify({ mcpServers: { "Bad Name": { command: "npx" } } }),
      ),
    ).toThrow("Invalid MCP server name");
  });

  test("rejects non-string args", () => {
    expect(() =>
      parseCustomMcpServersJson(
        JSON.stringify({
          mcpServers: { server: { command: "npx", args: [1] } },
        }),
      ),
    ).toThrow("args must be a string array");
  });

  test("rejects non-string env values", () => {
    expect(() =>
      parseCustomMcpServersJson(
        JSON.stringify({
          mcpServers: { server: { command: "npx", env: { TOKEN: 1 } } },
        }),
      ),
    ).toThrow("env values must be strings");
  });
});

describe("buildCustomMcpServersJson", () => {
  test("builds a source-of-truth JSON document from custom servers", () => {
    const json = buildCustomMcpServersJson([
      {
        name: "github",
        command: "npx",
        args: ["-y", "@modelcontextprotocol/server-github"],
        credentialFields: [{ name: "TOKEN" }],
        isBuiltin: true,
      },
      {
        name: "custom-server",
        command: "uvx",
        args: ["custom-mcp"],
        credentialFields: [{ name: "API_KEY" }],
        isBuiltin: false,
      },
    ]);

    expect(JSON.parse(json)).toEqual({
      mcpServers: {
        "custom-server": {
          command: "uvx",
          args: ["custom-mcp"],
          env: { API_KEY: "" },
        },
      },
    });
  });
});

describe("buildInitialCustomMcpServersJson", () => {
  test("uses a concrete example when there are no custom servers", () => {
    expect(buildInitialCustomMcpServersJson([])).toBe(CUSTOM_MCP_EXAMPLE_JSON);
    expect(isUnchangedCustomMcpExample(CUSTOM_MCP_EXAMPLE_JSON)).toBe(true);
  });

  test("uses saved custom servers when any exist", () => {
    const json = buildInitialCustomMcpServersJson([
      {
        name: "custom-server",
        command: "uvx",
        args: ["custom-mcp"],
        credentialFields: [],
        isBuiltin: false,
      },
    ]);

    expect(JSON.parse(json).mcpServers).toHaveProperty("custom-server");
    expect(isUnchangedCustomMcpExample(json)).toBe(false);
  });
});
