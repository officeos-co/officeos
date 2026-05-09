import { describe, test, expect, beforeAll } from "bun:test";
import {
  buildClientSchema,
  getIntrospectionQuery,
  getNamedType,
  isNonNullType,
  parse,
  validate,
} from "graphql";
import { readFileSync, readdirSync, statSync, existsSync } from "fs";
import { join, relative } from "path";

const SCHEMA_URL = "http://localhost:5000/api/dashboard/graphql";

const SRC_DIR = join(import.meta.dir, "..");

/** Extract all gql`` template bodies from a TypeScript source file. */
function extractGqlBodies(source: string): string[] {
  const bodies: string[] = [];
  const re = /gql\s*`([\s\S]*?)`/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(source)) !== null) {
    bodies.push(m[1]);
  }
  return bodies;
}

/** Recursively collect all files matching a predicate. */
function walkDir(dir: string, predicate: (f: string) => boolean): string[] {
  if (!existsSync(dir)) return [];
  const results: string[] = [];
  for (const entry of readdirSync(dir)) {
    const full = join(dir, entry);
    if (
      entry === "node_modules" ||
      entry === ".next" ||
      entry === "generated" ||
      entry === "__tests__"
    )
      continue;
    if (statSync(full).isDirectory()) {
      results.push(...walkDir(full, predicate));
    } else if (predicate(entry)) {
      results.push(full);
    }
  }
  return results;
}

/** Collect all GraphQL operations from gql`` tags in .ts/.tsx files and .graphql files. */
function collectOperations(): Array<{
  file: string;
  name: string;
  body: string;
}> {
  const ops: Array<{ file: string; name: string; body: string }> = [];

  // Scan all .ts/.tsx files for gql`` template literals
  const tsFiles = walkDir(
    SRC_DIR,
    (f) => f.endsWith(".ts") || f.endsWith(".tsx"),
  );
  for (const fullPath of tsFiles) {
    const file = relative(SRC_DIR, fullPath);
    const source = readFileSync(fullPath, "utf-8");
    const bodies = extractGqlBodies(source);
    for (const body of bodies) {
      const nameMatch = body.match(/(?:query|mutation|subscription)\s+(\w+)/);
      const name = nameMatch?.[1] ?? "anonymous";
      ops.push({ file, name, body });
    }
  }

  // Scan .graphql operation files
  const gqlFiles = walkDir(SRC_DIR, (f) => f.endsWith(".graphql"));
  for (const fullPath of gqlFiles) {
    const file = relative(SRC_DIR, fullPath);
    const source = readFileSync(fullPath, "utf-8");
    const nameMatch = source.match(/(?:query|mutation|subscription)\s+(\w+)/);
    const name = nameMatch?.[1] ?? "anonymous";
    ops.push({ file, name, body: source });
  }

  return ops;
}

const operations = collectOperations();

describe("GraphQL contract tests", () => {
  test("scanner finds operations (sanity check)", () => {
    expect(operations.length).toBeGreaterThan(0);
    const files = new Set(operations.map((o) => o.file));
    // Must scan beyond just hooks/ — features/agents/api/ has the bulk of operations
    expect(files.size).toBeGreaterThan(1);
  });

  let schema: ReturnType<typeof buildClientSchema>;

  beforeAll(async () => {
    const res = await fetch(SCHEMA_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ query: getIntrospectionQuery() }),
    });
    if (!res.ok)
      throw new Error(`Schema fetch failed: ${res.status} ${res.statusText}`);
    const json = (await res.json()) as {
      data: Parameters<typeof buildClientSchema>[0];
    };
    schema = buildClientSchema(json.data);
  });

  test("schema exposes setExtraUsageEnabled as a boolean toggle", () => {
    const field = schema.getMutationType()?.getFields().setExtraUsageEnabled;
    expect(field).toBeDefined();
    expect(isNonNullType(field!.type)).toBe(true);
    expect(getNamedType(field!.type).name).toBe("Boolean");

    const enabledArg = field!.args.find((arg) => arg.name === "enabled");
    expect(enabledArg).toBeDefined();
    expect(isNonNullType(enabledArg!.type)).toBe(true);
    expect(getNamedType(enabledArg!.type).name).toBe("Boolean");
  });

  for (const op of operations) {
    test(`${op.name} (${op.file})`, () => {
      const doc = parse(op.body);
      const errors = validate(schema, doc);
      if (errors.length > 0) {
        const msgs = errors.map((e) => e.message).join("\n  ");
        throw new Error(`${op.name} failed validation:\n  ${msgs}`);
      }
    });
  }
});
