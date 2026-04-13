import { readdir, writeFile, mkdir } from "fs/promises";
import { resolve, dirname } from "path";
import { fileURLToPath, pathToFileURL } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "dist/skills");
const manifestsDir = resolve(__dirname, "dist/manifests");

const { extractManifest } = await import(
  pathToFileURL(resolve(__dirname, "dist/manifest.js")).href
);

await mkdir(manifestsDir, { recursive: true });

let files;
try {
  files = await readdir(skillsDir);
} catch {
  console.error(`No skills directory found at ${skillsDir}`);
  process.exit(1);
}

const manifests = [];
let failed = false;

for (const file of files) {
  if (!file.endsWith(".js")) continue;
  const name = file.replace(/\.js$/, "");
  try {
    const mod = await import(pathToFileURL(resolve(skillsDir, file)).href);
    const def = mod.default?.default ?? mod.default;
    if (!def?.name || !def?.actions) {
      console.error(`Skill ${name}: no valid definition found (missing name or actions)`);
      failed = true;
      continue;
    }
    const manifest = extractManifest(def);
    await writeFile(
      resolve(manifestsDir, `${name}.json`),
      JSON.stringify(manifest, null, 2)
    );
    manifests.push(manifest);
    console.log(`Extracted manifest: ${name} (${Object.keys(manifest.actions).length} actions)`);
  } catch (err) {
    console.error(`Failed to extract manifest for ${name}:`, err);
    failed = true;
  }
}

if (failed) {
  console.error("One or more manifests failed to extract");
  process.exit(1);
}

await writeFile(
  resolve(manifestsDir, "all.json"),
  JSON.stringify(manifests, null, 2)
);

console.log(`Extracted ${manifests.length} manifests to dist/manifests/`);
