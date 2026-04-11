import { readdir, writeFile, mkdir, readFile } from "node:fs/promises";
import { resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { execSync } from "node:child_process";
import type { SkillDefinition } from "@harro/skill-sdk";
import { SkillExecutor, type ExecuteRequest } from "./executor.js";

const __dirname = dirname(fileURLToPath(import.meta.url));

const PLATFORM_URL = process.env.PLATFORM_URL?.replace(/\/$/, "");
const REGISTRATION_TOKEN = process.env.REGISTRATION_TOKEN;
const POLL_INTERVAL_MS = 3000;
const HEARTBEAT_INTERVAL_MS = 30_000;
const SKILL_SYNC_INTERVAL_MS = 60_000;

if (!PLATFORM_URL || !REGISTRATION_TOKEN) {
  console.error("PLATFORM_URL and REGISTRATION_TOKEN env vars are required");
  process.exit(1);
}

const executor = new SkillExecutor();
const skillsDir = resolve(__dirname, "skills");
const syncStateFile = resolve(__dirname, ".skill-sync-state.json");

// Track when each skill was last synced
let syncState: Record<string, string> = {};

async function loadSyncState(): Promise<void> {
  try {
    const raw = await readFile(syncStateFile, "utf-8");
    syncState = JSON.parse(raw);
  } catch {
    syncState = {};
  }
}

async function saveSyncState(): Promise<void> {
  await writeFile(syncStateFile, JSON.stringify(syncState, null, 2), "utf-8");
}

async function loadSkills(): Promise<void> {
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

async function apiCall(path: string, opts: RequestInit = {}): Promise<Response> {
  return fetch(`${PLATFORM_URL}${path}`, {
    ...opts,
    headers: {
      "content-type": "application/json",
      ...opts.headers,
    },
  });
}

async function register(): Promise<string> {
  console.log(`Registering with ${PLATFORM_URL}...`);

  const res = await apiCall("/api/runner/register", {
    method: "POST",
    body: JSON.stringify({
      registrationToken: REGISTRATION_TOKEN,
      version: "0.1.0",
    }),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Registration failed (${res.status}): ${text}`);
  }

  const data = (await res.json()) as {
    authToken: string;
    runnerId: string;
    name: string;
  };
  console.log(`Registered as runner "${data.name}" (${data.runnerId})`);
  return data.authToken;
}

/**
 * Sync custom skills from the platform.
 * Fetches the skill list, compares updatedAt timestamps, downloads new/updated zips,
 * extracts source files, builds with esbuild, and hot-loads into the executor.
 */
async function syncSkills(authToken: string): Promise<void> {
  const headers = { Authorization: `Bearer ${authToken}` };

  const res = await apiCall("/api/runner/skills", { headers });
  if (!res.ok) {
    console.error(`Skill sync list failed (${res.status})`);
    return;
  }

  const remoteSkills = (await res.json()) as {
    name: string;
    updatedAt: string;
  }[];

  let synced = 0;
  for (const remote of remoteSkills) {
    const localUpdatedAt = syncState[remote.name];
    if (localUpdatedAt === remote.updatedAt) continue;

    console.log(`Syncing skill: ${remote.name}...`);
    try {
      // Download the zip
      const dlRes = await apiCall(`/api/runner/skills/${remote.name}/download`, {
        headers: { ...headers, "content-type": "" },
      });
      if (!dlRes.ok) {
        console.error(
          `Failed to download skill ${remote.name}: ${dlRes.status}`
        );
        continue;
      }

      const zipBuffer = Buffer.from(await dlRes.arrayBuffer());

      // Extract zip to a temp directory
      const tmpDir = resolve(__dirname, ".skill-tmp", remote.name);
      await mkdir(tmpDir, { recursive: true });

      // Write zip and extract using node:zlib + tar, or simpler: use unzip
      const zipPath = resolve(tmpDir, `${remote.name}.zip`);
      await writeFile(zipPath, zipBuffer);

      // Extract using system unzip (available in node:22-slim)
      try {
        execSync(`unzip -o "${zipPath}" -d "${tmpDir}"`, { stdio: "pipe" });
      } catch {
        // Fallback: try to parse as a simple archive
        console.error(`Failed to extract ${remote.name}.zip — skipping`);
        continue;
      }

      // Read extracted files for esbuild
      const { readdir: rd } = await import("node:fs/promises");
      const extractedFiles = await collectFiles(tmpDir);

      // Build with esbuild (dynamic import to avoid bundling issues)
      const { buildSkill } = await import("./builder.js");
      await mkdir(skillsDir, { recursive: true });
      await buildSkill(remote.name, tmpDir, skillsDir);

      // Hot-load the built skill
      const bundlePath = resolve(skillsDir, `${remote.name}.js`);
      const mod = await import(
        pathToFileURL(bundlePath).href + `?t=${Date.now()}`
      );
      const def = mod.default?.default ?? mod.default;
      if (def?.name && def?.actions) {
        executor.register(def);
        console.log(
          `Synced and loaded skill: ${def.name} (${Object.keys(def.actions).length} actions)`
        );
      }

      syncState[remote.name] = remote.updatedAt;
      synced++;
    } catch (err) {
      console.error(`Failed to sync skill ${remote.name}:`, err);
    }
  }

  if (synced > 0) {
    await saveSyncState();
    console.log(`Skill sync complete: ${synced} skill(s) updated`);
  }
}

async function collectFiles(dir: string): Promise<string[]> {
  const { readdir: rd, stat } = await import("node:fs/promises");
  const entries = await rd(dir, { withFileTypes: true });
  const files: string[] = [];
  for (const entry of entries) {
    const full = resolve(dir, entry.name);
    if (entry.isDirectory()) {
      files.push(...(await collectFiles(full)));
    } else {
      files.push(full);
    }
  }
  return files;
}

async function pollAndExecute(authToken: string): Promise<void> {
  const headers = { Authorization: `Bearer ${authToken}` };

  const res = await apiCall("/api/runner/jobs", { headers });

  if (res.status === 204) return;
  if (!res.ok) {
    console.error(`Poll failed (${res.status}):`, await res.text());
    return;
  }

  const job = (await res.json()) as { id: string; payload: ExecuteRequest };
  console.log(
    `Executing job ${job.id}: ${job.payload.skill}/${job.payload.action}`
  );

  let result;
  try {
    result = await executor.execute(job.payload);
  } catch (err) {
    result = {
      success: false,
      error: err instanceof Error ? err.message : String(err),
    };
  }

  const postRes = await apiCall(`/api/runner/jobs/${job.id}/result`, {
    method: "POST",
    headers,
    body: JSON.stringify(result),
  });

  if (!postRes.ok) {
    console.error(
      `Failed to post result for job ${job.id}:`,
      await postRes.text()
    );
  } else {
    console.log(`Job ${job.id} completed (success: ${result.success})`);
  }
}

async function heartbeat(authToken: string): Promise<void> {
  const res = await apiCall("/api/runner/heartbeat", {
    method: "POST",
    headers: { Authorization: `Bearer ${authToken}` },
  });
  if (!res.ok) {
    console.error(`Heartbeat failed (${res.status})`);
  }
}

async function main(): Promise<void> {
  await loadSyncState();
  await loadSkills();
  const authToken = await register();

  // Initial skill sync from platform
  console.log("Syncing skills from platform...");
  await syncSkills(authToken);

  let lastHeartbeat = Date.now();
  let lastSkillSync = Date.now();

  console.log("Runner started. Polling for jobs...");
  while (true) {
    try {
      await pollAndExecute(authToken);
    } catch (err) {
      console.error("Poll error:", err);
    }

    // Send heartbeat every 30s
    if (Date.now() - lastHeartbeat > HEARTBEAT_INTERVAL_MS) {
      try {
        await heartbeat(authToken);
        lastHeartbeat = Date.now();
      } catch (err) {
        console.error("Heartbeat error:", err);
      }
    }

    // Sync skills every 60s
    if (Date.now() - lastSkillSync > SKILL_SYNC_INTERVAL_MS) {
      try {
        await syncSkills(authToken);
        lastSkillSync = Date.now();
      } catch (err) {
        console.error("Skill sync error:", err);
      }
    }

    await new Promise((r) => setTimeout(r, POLL_INTERVAL_MS));
  }
}

main().catch((err) => {
  console.error("Fatal error:", err);
  process.exit(1);
});
