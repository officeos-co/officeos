import assert from "node:assert/strict";
import path from "node:path";
import test from "node:test";
import {
  ExecFileLike,
  OfficeOsCli,
  ResourceKinds,
  buildDevFallback,
  parseJsonOutput,
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

test("listResources uses top-level commands for providers and models", async () => {
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
    ["providers", "-o", "json"],
    ["models", "-o", "json"],
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
  assert.equal(calls[1]?.file, "bun");
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

test("buildDevFallback resolves the repository CLI entry from the extension path", () => {
  const fallback = buildDevFallback(path.resolve(process.cwd()));

  assert.equal(fallback?.file, "bun");
  assert.match(
    String(fallback?.argsPrefix[0]),
    /apps\/cli\/src\/app\/main\.ts$/,
  );
});

test("resource helpers prefer stable names and canonical singular kinds", () => {
  assert.equal(resourceName({ displayName: "Claude", id: "claude" }), "claude");
  assert.equal(resourceName({ name: "fix-ci", id: "agent-1" }), "fix-ci");
  assert.equal(singularKind("memorystores"), "MemoryStore");
  assert.equal(singularKind("providers"), "Provider");
});

test("resource kinds expose agents but not removed runs", () => {
  assert.ok(ResourceKinds.includes("agents"));
  assert.ok(!(ResourceKinds as readonly string[]).includes("runs"));
});
