import type { NextConfig } from "next";

const backendUrl =
  process.env.NODE_ENV === "production"
    ? "https://api.harrokrog.com"
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
