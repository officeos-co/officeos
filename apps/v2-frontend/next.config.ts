import type { NextConfig } from "next";

const backendUrl =
  process.env.NODE_ENV === "production"
    ? "http://eaos-backend-prod:8000"
    : "http://localhost:5080";

const nextConfig: NextConfig = {
  output: "standalone",
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${backendUrl}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
