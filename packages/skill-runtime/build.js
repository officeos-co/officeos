import { build } from "esbuild";
import { readdirSync, existsSync } from "fs";
import { resolve, dirname } from "path";
import { fileURLToPath } from "url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const skillsDir = resolve(__dirname, "../skills");
const sdkEntry = resolve(__dirname, "../skill-sdk/src/index.ts");

// Plugin to resolve @harro/skill-sdk to the local SDK source
const sdkResolvePlugin = {
  name: "sdk-resolve",
  setup(build) {
    build.onResolve({ filter: /^@harro\/skill-sdk$/ }, () => ({
      path: sdkEntry,
    }));
  },
};

// Bundle each first-party skill into dist/skills/{name}.js
const skillDirs = readdirSync(skillsDir).filter((d) =>
  existsSync(resolve(skillsDir, d, "skill.ts"))
);

// Bundle the runtime server
await build({
  entryPoints: ["src/server.ts"],
  bundle: true,
  platform: "node",
  target: "node22",
  format: "esm",
  outfile: "dist/server.js",
  sourcemap: true,
  external: ["node:*", "playwright-core"],
  plugins: [sdkResolvePlugin],
  banner: {
    js: 'import { createRequire } from "node:module"; const require = createRequire(import.meta.url);',
  },
});

// Bundle each skill as a standalone file (.md files inlined as text)
for (const name of skillDirs) {
  await build({
    entryPoints: [resolve(skillsDir, name, "skill.ts")],
    bundle: true,
    platform: "node",
    target: "node22",
    format: "esm",
    outfile: `dist/skills/${name}.js`,
    sourcemap: true,
    external: ["node:*", "playwright-core"],
    plugins: [sdkResolvePlugin],
    loader: { ".md": "text" },
  });
}

console.log(
  `Built runtime + ${skillDirs.length} skills: ${skillDirs.join(", ")}`
);
