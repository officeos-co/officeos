import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Standalone build is what the Dockerfile copies from .next/standalone.
  output: "standalone",
};

export default nextConfig;
