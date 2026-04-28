import type { NextConfig } from "next";
import { getEnvConfig } from "./src/lib/env";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    const { apiUrl } = getEnvConfig();
    return [
      {
        source: "/api/:path*",
        destination: `${apiUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
