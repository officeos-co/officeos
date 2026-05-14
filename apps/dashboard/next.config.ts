import type { NextConfig } from "next";
import { getEnvConfig } from "./src/lib/env";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    const { apiUrl } = getEnvConfig();
    return [
      {
        source: "/api/v1/:path*",
        destination: `${apiUrl}/api/v1/:path*`,
      },
    ];
  },
};

export default nextConfig;
