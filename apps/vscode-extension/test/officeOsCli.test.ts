import assert from "node:assert/strict";
import path from "node:path";
import test from "node:test";
import {
  ExecFileLike,
  OfficeOsCli,
  buildDevFallback,
  parseJsonOutput,
  resolveBunExecutable,
  resourceName,
  singularKind,
} from "../src/officeOsCli";

test("parseJsonOutput parses valid JSON", () => {
  assert.deepEqual(
    parseJsonOutput<{ name: string }>('{"name":"alpha"}', "resource"),
    { name: "alpha" },
  );
});

test("parseJsonOutput rejects invalid JSON with context", () => {
  assert.throws(
    () => parseJsonOutput("{bad", "agents"),
    /officeos returned invalid JSON for agents/,
  );
});

test("listResources uses normal get command for control-plane resources", async () => {
  const calls: Array<{ file: string; args: readonly string[] }> = [];
  const execFile: ExecFileLike = async (file, args) => {
    calls.push({ file, args });
    return { stdout: '[{"kind":"Agent","name":"fix-ci"}]', stderr: "" };
  };

  const cli = new OfficeOsCli({
    extensionPath: process.cwd(),
    configuredCliPath: "/bin/officeos",
    execFile,
  });
  const resources = await cli.listResources("agents");

  assert.deepEqual(resources, [{ kind: "Agent", name: "fix-ci" }]);
  assert.deepEqual(calls, [
    { file: "/bin/officeos", args: ["get", "agents", "-o", "json"] },
  ]);
});

test("listResourceCatalog shells out through get without a target", async () => {
  const calls: Array<readonly string[]> = [];
  const execFile: ExecFileLike = async (_file, args) => {
    calls.push(args);
    return {
      stdout:
        '[{"kind":"widgets","singular":"widget","aliases":["widget"],"displayName":"Widgets","description":"Widget resources","icon":"package","capabilities":["list"],"displayFields":["name"]}]',
      stderr: "",
    };
  };

  const cli = new OfficeOsCli({
    extensionPath: process.cwd(),
    configuredCliPath: "/bin/officeos",
    execFile,
  });

  assert.deepEqual(await cli.listResourceCatalog(), [
    {
      kind: "widgets",
      singular: "widget",
      aliases: ["widget"],
      displayName: "Widgets",
      description: "Widget resources",
      icon: "package",
      capabilities: ["list"],
      displayFields: ["name"],
    },
  ]);
  assert.deepEqual(calls, [["get", "-o", "json"]]);
});

test("listResources uses generic get command for providers and models", async () => {
  const calls: Array<readonly string[]> = [];
  const execFile: ExecFileLike = async (_file, args) => {
    calls.push(args);
    return { stdout: '[{"id":"anthropic"}]', stderr: "" };
  };

  const cli = new OfficeOsCli({
    extensionPath: process.cwd(),
    configuredCliPath: "/bin/officeos",
    execFile,
  });
  await cli.listResources("providers");
  await cli.listResources("models");

  assert.deepEqual(calls, [
    ["get", "providers", "-o", "json"],
    ["get", "models", "-o", "json"],
  ]);
});

test("falls back to the repo Bun CLI in development when officeos is not installed", async () => {
  const calls: Array<{ file: string; args: readonly string[] }> = [];
  const execFile: ExecFileLike = async (file, args) => {
    calls.push({ file, args });
    if (file === "officeos") {
      const error = new Error("missing") as NodeJS.ErrnoException;
      error.code = "ENOENT";
      throw error;
    }

    return { stdout: '[{"name":"alpha"}]', stderr: "" };
  };

  const cli = new OfficeOsCli({ extensionPath: process.cwd(), execFile });
  await cli.listResources("agents");

  assert.equal(calls[0]?.file, "officeos");
  assert.equal(path.basename(calls[1]?.file ?? ""), "bun");
  assert.match(String(calls[1]?.args[0]), /apps\/cli\/src\/app\/main\.ts$/);
  assert.deepEqual(calls[1]?.args.slice(1), ["get", "agents", "-o", "json"]);
});

test("configured cli path disables development fallback", async () => {
  const calls: Array<{ file: string; args: readonly string[] }> = [];
  const execFile: ExecFileLike = async (file, args) => {
    calls.push({ file, args });
    const error = new Error("missing") as NodeJS.ErrnoException;
    error.code = "ENOENT";
    throw error;
  };

  const cli = new OfficeOsCli({
    extensionPath: process.cwd(),
    configuredCliPath: "/missing/officeos",
    execFile,
  });

  await assert.rejects(() => cli.listResources("agents"), /missing/);
  assert.deepEqual(calls, [
    { file: "/missing/officeos", args: ["get", "agents", "-o", "json"] },
  ]);
});

test("sendAgentMessage shells out through the send command", async () => {
  const calls: Array<{ file: string; args: readonly string[] }> = [];
  const execFile: ExecFileLike = async (file, args) => {
    calls.push({ file, args });
    return { stdout: "agent/fix-ci\twork/1\tqueued\n", stderr: "" };
  };

  const cli = new OfficeOsCli({
    extensionPath: process.cwd(),
    configuredCliPath: "/bin/officeos",
    execFile,
  });

  await cli.sendAgentMessage("fix-ci", "please check this");

  assert.deepEqual(calls, [
    {
      file: "/bin/officeos",
      args: ["send", "fix-ci", "--message", "please check this"],
    },
  ]);
});

test("buildDevFallback resolves the repository CLI entry from the extension path", () => {
  const fallback = buildDevFallback(path.resolve(process.cwd()));

  assert.equal(path.basename(fallback?.file ?? ""), "bun");
  assert.match(
    String(fallback?.argsPrefix[0]),
    /apps\/cli\/src\/app\/main\.ts$/,
  );
});

test("buildDevFallback resolves the repository CLI entry from workspace folders", () => {
  const fallback = buildDevFallback("/tmp/installed-extension", [
    path.resolve(process.cwd(), "../.."),
  ]);

  assert.equal(path.basename(fallback?.file ?? ""), "bun");
  assert.match(
    String(fallback?.argsPrefix[0]),
    /apps\/cli\/src\/app\/main\.ts$/,
  );
});

test("resolveBunExecutable falls back to common macOS install paths", () => {
  const fallback = resolveBunExecutable({ PATH: "/usr/bin:/bin" });
  assert.equal(path.basename(fallback), "bun");
});

test("resource helpers prefer stable names and canonical singular kinds", () => {
  assert.equal(resourceName({ displayName: "Claude", id: "claude" }), "claude");
  assert.equal(resourceName({ name: "fix-ci", id: "agent-1" }), "fix-ci");
  assert.equal(singularKind("memorystores"), "MemoryStore");
  assert.equal(singularKind("memory-stores"), "MemoryStore");
  assert.equal(singularKind("providers"), "Provider");
});
