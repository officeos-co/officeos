import type { NextConfig } from "next";
import { envConfig } from "./src/lib/env";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${envConfig.apiUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
