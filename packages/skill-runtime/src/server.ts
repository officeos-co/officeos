import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { readdir, writeFile, mkdir, rm } from "node:fs/promises";
import { resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { SkillExecutor, type ExecuteRequest } from "./executor.js";
import { buildSkill } from "./builder.js";

const __dirname = dirname(fileURLToPath(import.meta.url));
const PORT = parseInt(process.env.PORT ?? "3001", 10);
const BACKEND_URL = process.env.BACKEND_URL ?? "http://localhost:5000";

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

/**
 * Load skills from the backend's skill registry.
 * Fetches the registry list and attempts to install any active skills
 * that have a bundleUrl or npmPackage and aren't already loaded.
 */
async function loadSkillsFromRegistry(): Promise<void> {
  try {
    const resp = await fetch(`${BACKEND_URL}/api/skill-registry`);
    if (!resp.ok) {
      console.warn(`Failed to fetch skill registry (HTTP ${resp.status})`);
      return;
    }
    const entries = (await resp.json()) as Array<{
      name: string;
      npmPackage?: string;
      bundleUrl?: string;
      status: string;
    }>;

    for (const entry of entries) {
      if (entry.status !== "active") continue;
      // Skip if already loaded (bundled skill takes precedence)
      if (executor.getManifest(entry.name)) continue;

      try {
        await installSkillPackage(entry.name, entry.npmPackage, entry.bundleUrl);
      } catch (err) {
        console.error(`Failed to load registry skill ${entry.name}:`, err);
      }
    }
  } catch (err) {
    console.warn("Could not reach backend skill registry:", err instanceof Error ? err.message : String(err));
  }
}

/**
 * Install a skill from an npm package or bundle URL into the runtime.
 */
async function installSkillPackage(
  name: string,
  npmPackage?: string | null,
  bundleUrl?: string | null,
): Promise<void> {
  const skillsDir = resolve(__dirname, "skills");
  await mkdir(skillsDir, { recursive: true });

  if (bundleUrl) {
    // Download the bundle JS file directly
    const resp = await fetch(bundleUrl);
    if (!resp.ok) throw new Error(`Failed to download bundle from ${bundleUrl}: HTTP ${resp.status}`);
    const content = await resp.text();
    const bundlePath = resolve(skillsDir, `${name}.js`);
    await writeFile(bundlePath, content, "utf-8");

    const mod = await import(pathToFileURL(bundlePath).href + `?t=${Date.now()}`);
    const def = mod.default?.default ?? mod.default;
    if (def?.name && def?.actions) {
      executor.register(def);
      console.log(`Installed registry skill from bundle: ${def.name}`);
    } else {
      throw new Error(`Invalid skill module from bundle: ${name}`);
    }
  } else if (npmPackage) {
    // For npm packages, we use dynamic import (package must be installed in node_modules)
    // In production this would trigger npm install; for now we try direct import
    try {
      const mod = await import(npmPackage);
      const def = mod.default?.default ?? mod.default;
      if (def?.name && def?.actions) {
        executor.register(def);
        console.log(`Installed registry skill from npm: ${def.name}`);
      }
    } catch {
      console.warn(`npm package ${npmPackage} not found in node_modules — skipping`);
    }
  }
}

/**
 * On-demand: fetch a skill bundle from the backend and load it.
 * Called when /execute receives a request for an unknown skill.
 */
async function loadSkillFromBackend(skillName: string): Promise<boolean> {
  try {
    const resp = await fetch(`${BACKEND_URL}/api/skills/${encodeURIComponent(skillName)}/bundle`);
    if (!resp.ok) {
      console.warn(`No bundle available for skill ${skillName} (HTTP ${resp.status})`);
      return false;
    }

    const content = await resp.text();
    const skillsDir = resolve(__dirname, "skills");
    await mkdir(skillsDir, { recursive: true });
    const bundlePath = resolve(skillsDir, `${skillName}.js`);
    await writeFile(bundlePath, content, "utf-8");

    const mod = await import(pathToFileURL(bundlePath).href + `?t=${Date.now()}`);
    const def = mod.default?.default ?? mod.default;
    if (def?.name && def?.actions) {
      executor.register(def);
      console.log(`On-demand loaded skill from backend: ${def.name}`);
      return true;
    }
    console.warn(`Invalid skill module from backend bundle: ${skillName}`);
    return false;
  } catch (err) {
    console.error(`Failed to load skill ${skillName} from backend:`, err);
    return false;
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

    // Try on-demand loading if the skill is not registered
    if (!executor.getManifest(body.skill)) {
      const loaded = await loadSkillFromBackend(body.skill);
      if (!loaded) {
        json(res, 422, { success: false, error: `Unknown skill: ${body.skill}` });
        return;
      }
    }

    const result = await executor.execute(body);
    json(res, result.success ? 200 : 422, result);
    return;
  }

  // POST /build — build a skill from source files and hot-load it
  if (url.pathname === "/build" && req.method === "POST") {
    let body: { name: string; files: { path: string; content: string }[] };
    try {
      body = JSON.parse(await readBody(req));
    } catch {
      json(res, 400, { error: "Invalid JSON body" });
      return;
    }

    if (!body.name || !body.files?.length) {
      json(res, 400, { error: "Missing required fields: name, files" });
      return;
    }

    try {
      const tmpDir = resolve(__dirname, ".build-tmp", body.name);
      await mkdir(tmpDir, { recursive: true });

      // Write source files to temp dir
      for (const file of body.files) {
        const filePath = resolve(tmpDir, file.path);
        await mkdir(dirname(filePath), { recursive: true });
        await writeFile(filePath, file.content, "utf-8");
      }

      // Build the skill
      const skillsDir = resolve(__dirname, "skills");
      await mkdir(skillsDir, { recursive: true });
      await buildSkill(body.name, tmpDir, skillsDir);

      // Hot-load the built skill
      const bundlePath = resolve(skillsDir, `${body.name}.js`);
      const mod = await import(pathToFileURL(bundlePath).href + `?t=${Date.now()}`);
      const def = mod.default?.default ?? mod.default;
      if (def?.name && def?.actions) {
        executor.register(def);
        console.log(`Built and loaded skill: ${def.name}`);
      }

      const manifest = executor.getManifest(body.name);
      json(res, 200, { ok: true, manifest });
    } catch (err) {
      json(res, 500, { error: `Build failed: ${err instanceof Error ? err.message : String(err)}` });
    }
    return;
  }

  // POST /reload/:name — hot-reload a skill from dist/skills/
  const reloadMatch = url.pathname.match(/^\/reload\/([a-z0-9_-]+)$/);
  if (reloadMatch && req.method === "POST") {
    const name = reloadMatch[1];
    try {
      const bundlePath = resolve(__dirname, "skills", `${name}.js`);
      const mod = await import(pathToFileURL(bundlePath).href + `?t=${Date.now()}`);
      const def = mod.default?.default ?? mod.default;
      if (def?.name && def?.actions) {
        executor.register(def);
        json(res, 200, { ok: true, manifest: executor.getManifest(name) });
      } else {
        json(res, 400, { error: `Invalid skill module: ${name}` });
      }
    } catch (err) {
      json(res, 500, { error: `Reload failed: ${err instanceof Error ? err.message : String(err)}` });
    }
    return;
  }

  // POST /install — install a skill from registry (npm package or bundle URL)
  if (url.pathname === "/install" && req.method === "POST") {
    let body: { name: string; npmPackage?: string; bundleUrl?: string };
    try {
      body = JSON.parse(await readBody(req));
    } catch {
      json(res, 400, { error: "Invalid JSON body" });
      return;
    }

    if (!body.name) {
      json(res, 400, { error: "Missing required field: name" });
      return;
    }

    try {
      await installSkillPackage(body.name, body.npmPackage, body.bundleUrl);
      const manifest = executor.getManifest(body.name);
      json(res, 200, { ok: true, manifest });
    } catch (err) {
      json(res, 500, { error: `Install failed: ${err instanceof Error ? err.message : String(err)}` });
    }
    return;
  }

  // POST /uninstall — remove a loaded skill
  if (url.pathname === "/uninstall" && req.method === "POST") {
    let body: { name: string };
    try {
      body = JSON.parse(await readBody(req));
    } catch {
      json(res, 400, { error: "Invalid JSON body" });
      return;
    }

    if (!body.name) {
      json(res, 400, { error: "Missing required field: name" });
      return;
    }

    executor.unregister(body.name);

    // Remove the bundle file if it exists
    const bundlePath = resolve(__dirname, "skills", `${body.name}.js`);
    try {
      await rm(bundlePath, { force: true });
    } catch {
      // ignore — file may not exist
    }

    console.log(`Uninstalled skill: ${body.name}`);
    json(res, 200, { ok: true });
    return;
  }

  // 404
  json(res, 404, { error: "Not found" });
}

await loadSkills();
await loadSkillsFromRegistry();

// Start browser session cleanup loop
const { browserSessions } = await import("./browser-session-manager.js");
browserSessions.startCleanup();

const server = createServer((req, res) => {
  handleRequest(req, res).catch((err) => {
    console.error("Unhandled error:", err);
    json(res, 500, { success: false, error: "Internal server error" });
  });
});

async function shutdown() {
  const { browserSessions } = await import("./browser-session-manager.js");
  await browserSessions.shutdown();
  server.close();
  process.exit(0);
}
process.on("SIGTERM", shutdown);
process.on("SIGINT", shutdown);

server.listen(PORT, () => {
  console.log(`Skill runtime listening on :${PORT}`);
  console.log(`Skills loaded: ${executor.getAllManifests().map((m) => m.name).join(", ")}`);
});
