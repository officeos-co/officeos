import { createMDX } from "fumadocs-mdx/next";
import path from "node:path";

const withMDX = createMDX();
const repoRoot = path.resolve("../..");

export default withMDX({
  output: "standalone",
  experimental: {
    externalDir: true,
  },
  turbopack: {
    root: repoRoot,
  },
});
