import { build } from "esbuild";
import { readdirSync, existsSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../skills");
const sdkEntry = resolve(__dirname, "../skill-sdk/src/index.ts");

const sdkResolvePlugin = {
  name: "sdk-resolve",
  setup(build) {
    build.onResolve({ filter: /^@harro\/skill-sdk$/ }, () => ({
      path: sdkEntry,
    }));
  },
};

// Bundle the runner client
await build({
  entryPoints: ["src/client.ts"],
  bundle: true,
  platform: "node",
  target: "node22",
  format: "esm",
  outfile: "dist/client.js",
  sourcemap: true,
  external: ["node:*"],
  plugins: [sdkResolvePlugin],
  banner: {
    js: 'import { createRequire } from "node:module"; const require = createRequire(import.meta.url);',
  },
});

// Bundle each skill as a standalone file (same as skill-runtime)
const skillDirs = existsSync(skillsDir)
  ? readdirSync(skillsDir).filter((d) =>
      existsSync(resolve(skillsDir, d, "skill.ts"))
    )
  : [];

for (const name of skillDirs) {
  await build({
    entryPoints: [resolve(skillsDir, name, "skill.ts")],
    bundle: true,
    platform: "node",
    target: "node22",
    format: "esm",
    outfile: `dist/skills/${name}.js`,
    sourcemap: true,
    external: ["node:*"],
    plugins: [sdkResolvePlugin],
    loader: { ".md": "text" },
  });
}

console.log(
  `Built runner client + ${skillDirs.length} skills: ${skillDirs.join(", ")}`
);
