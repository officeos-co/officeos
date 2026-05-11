import { mkdir, writeFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import {
  buildClientSchema,
  getIntrospectionQuery,
  printSchema,
} from "graphql";

const DEFAULT_SCHEMA_URL = "http://localhost:5000/api/dashboard/graphql";
const DEFAULT_OUTPUT_PATH = "/tmp/dashboard-schema.graphql";

type Args = {
  url: string;
  outputPath: string;
};

function parseArgs(): Args {
  let url = process.env.GRAPHQL_SCHEMA_URL ?? DEFAULT_SCHEMA_URL;
  let outputPath =
    process.env.GRAPHQL_SCHEMA_OUTPUT ?? DEFAULT_OUTPUT_PATH;

  for (const arg of process.argv.slice(2)) {
    if (arg.startsWith("--url=")) {
      url = arg.slice("--url=".length);
      continue;
    }
    if (arg.startsWith("--output=")) {
      outputPath = arg.slice("--output=".length);
      continue;
    }
    if (arg === "--help" || arg === "-h") {
      printUsageAndExit();
    }
    outputPath = arg;
  }

  return { url, outputPath };
}

function printUsageAndExit(): never {
  console.log(
    [
      "Usage: bun run schema:fetch [output-path] [--url=http://localhost:5000/api/dashboard/graphql]",
      "",
      "Environment:",
      `  GRAPHQL_SCHEMA_URL      GraphQL endpoint. Defaults to ${DEFAULT_SCHEMA_URL}`,
      `  GRAPHQL_SCHEMA_OUTPUT   Output file. Defaults to ${DEFAULT_OUTPUT_PATH}`,
    ].join("\n"),
  );
  process.exit(0);
}

function sdlUrl(url: string): string {
  const parsed = new URL(url);
  parsed.searchParams.set("sdl", "");
  return parsed.toString();
}

async function fetchSdl(url: string): Promise<string | null> {
  const res = await fetch(sdlUrl(url), {
    headers: { accept: "application/graphql-response+json, text/plain" },
  });
  if (!res.ok) return null;

  const text = await res.text();
  if (text.trim().startsWith("{")) return null;
  return text;
}

async function fetchIntrospectionSdl(url: string): Promise<string> {
  const res = await fetch(url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ query: getIntrospectionQuery() }),
  });
  if (!res.ok) {
    throw new Error(`Schema introspection failed: ${res.status} ${res.statusText}`);
  }

  const json = (await res.json()) as {
    data?: Parameters<typeof buildClientSchema>[0];
    errors?: Array<{ message?: string }>;
  };
  if (!json.data) {
    const message =
      json.errors?.map((error) => error.message).filter(Boolean).join("; ") ??
      "No schema data returned.";
    throw new Error(`Schema introspection failed: ${message}`);
  }

  return printSchema(buildClientSchema(json.data));
}

async function main() {
  const { url, outputPath } = parseArgs();
  const absoluteOutputPath = resolve(outputPath);
  const schema = (await fetchSdl(url)) ?? (await fetchIntrospectionSdl(url));

  await mkdir(dirname(absoluteOutputPath), { recursive: true });
  await writeFile(absoluteOutputPath, `${schema.trimEnd()}\n`, "utf8");

  console.log(`Fetched GraphQL schema from ${url}`);
  console.log(`Wrote ${absoluteOutputPath}`);
}

await main();
