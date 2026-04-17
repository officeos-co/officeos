import type { NextConfig } from "next";

const backendUrl =
  process.env.NODE_ENV === "production"
    ? "https://api.officeos.co"
    : "http://localhost:5000";

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
