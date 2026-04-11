import { build } from "esbuild";
import { existsSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));

// In the bundled runner, the SDK may not be at the same relative path.
// Use a no-op resolve plugin that strips the import (skills already bundle their own SDK copy).
const sdkResolvePlugin = {
  name: "sdk-resolve",
  setup(b: any) {
    // Try the monorepo path first, fall back to marking as external
    const monorepoSdk = resolve(__dirname, "../../skill-sdk/src/index.ts");
    if (existsSync(monorepoSdk)) {
      b.onResolve({ filter: /^@harro\/skill-sdk$/ }, () => ({
        path: monorepoSdk,
      }));
    }
  },
};

/**
 * Build a single skill from source directory into a standalone .js bundle.
 */
export async function buildSkill(
  name: string,
  sourceDir: string,
  outDir: string
): Promise<string> {
  // Find skill.ts — might be at root or nested one level
  let skillEntry = resolve(sourceDir, "skill.ts");
  if (!existsSync(skillEntry)) {
    // Check one level deep (zip might have a subdirectory)
    const { readdirSync } = await import("fs");
    for (const entry of readdirSync(sourceDir, { withFileTypes: true })) {
      if (entry.isDirectory()) {
        const nested = resolve(sourceDir, entry.name, "skill.ts");
        if (existsSync(nested)) {
          skillEntry = nested;
          break;
        }
      }
    }
  }

  if (!existsSync(skillEntry)) {
    throw new Error(`skill.ts not found in ${sourceDir}`);
  }

  const outfile = resolve(outDir, `${name}.js`);

  await build({
    entryPoints: [skillEntry],
    bundle: true,
    platform: "node",
    target: "node22",
    format: "esm",
    outfile,
    sourcemap: true,
    external: ["node:*"],
    plugins: [sdkResolvePlugin],
    loader: { ".md": "text" },
  });

  return outfile;
}
