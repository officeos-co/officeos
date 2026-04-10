import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { readdir } from "node:fs/promises";
import { resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { SkillExecutor, type ExecuteRequest } from "./executor.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
const PORT = parseInt(process.env.PORT ?? "3001", 10);

const executor = new SkillExecutor();

/**
 * Load all bundled skills from dist/skills/*.js
 */
async function loadSkills(): Promise<void> {
  const skillsDir = resolve(__dirname, "skills");
  let files: string[];
  try {
    files = await readdir(skillsDir);
  } catch {
    console.warn(`No skills directory found at ${skillsDir}`);
    return;
  }

  for (const file of files) {
    if (!file.endsWith(".js")) continue;
    try {
      const mod = await import(pathToFileURL(resolve(skillsDir, file)).href);
      const def = mod.default?.default ?? mod.default;
      if (def?.name && def?.actions) {
        executor.register(def);
        console.log(`Loaded skill: ${def.name} (${Object.keys(def.actions).length} actions)`);
      }
    } catch (err) {
      console.error(`Failed to load skill ${file}:`, err);
    }
  }
}

function readBody(req: IncomingMessage): Promise<string> {
  return new Promise((resolve, reject) => {
    const chunks: Buffer[] = [];
    req.on("data", (chunk) => chunks.push(chunk));
    req.on("end", () => resolve(Buffer.concat(chunks).toString()));
    req.on("error", reject);
  });
}

function json(res: ServerResponse, status: number, data: unknown): void {
  res.writeHead(status, { "Content-Type": "application/json" });
  res.end(JSON.stringify(data));
}

async function handleRequest(
  req: IncomingMessage,
  res: ServerResponse
): Promise<void> {
  const url = new URL(req.url ?? "/", `http://localhost:${PORT}`);

  // Health check
  if (url.pathname === "/health" && req.method === "GET") {
    json(res, 200, { ok: true });
    return;
  }

  // GET /manifests — list all skill manifests
  if (url.pathname === "/manifests" && req.method === "GET") {
    json(res, 200, executor.getAllManifests());
    return;
  }

  // GET /manifest/:skill — single skill manifest
  const manifestMatch = url.pathname.match(/^\/manifest\/([a-z0-9_-]+)$/);
  if (manifestMatch && req.method === "GET") {
    const manifest = executor.getManifest(manifestMatch[1]);
    if (!manifest) {
      json(res, 404, { error: `Skill not found: ${manifestMatch[1]}` });
      return;
    }
    json(res, 200, manifest);
    return;
  }

  // POST /execute — execute a skill action
  if (url.pathname === "/execute" && req.method === "POST") {
    let body: ExecuteRequest;
    try {
      body = JSON.parse(await readBody(req));
    } catch {
      json(res, 400, { success: false, error: "Invalid JSON body" });
      return;
    }

    if (!body.skill || !body.action) {
      json(res, 400, {
        success: false,
        error: "Missing required fields: skill, action",
      });
      return;
    }

    const result = await executor.execute(body);
    json(res, result.success ? 200 : 422, result);
    return;
  }

  // 404
  json(res, 404, { error: "Not found" });
}

await loadSkills();

const server = createServer((req, res) => {
  handleRequest(req, res).catch((err) => {
    console.error("Unhandled error:", err);
    json(res, 500, { success: false, error: "Internal server error" });
  });
});

server.listen(PORT, () => {
  console.log(`Skill runtime listening on :${PORT}`);
  console.log(`Skills loaded: ${executor.getAllManifests().map((m) => m.name).join(", ")}`);
});
