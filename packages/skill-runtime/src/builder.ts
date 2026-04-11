import { build } from "esbuild";
import { existsSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const sdkEntry = resolve(__dirname, "../../skill-sdk/src/index.ts");

const sdkResolvePlugin = {
  name: "sdk-resolve",
  setup(b: any) {
    b.onResolve({ filter: /^@harro\/skill-sdk$/ }, () => ({
      path: sdkEntry,
    }));
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
  const skillEntry = resolve(sourceDir, "skill.ts");
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
