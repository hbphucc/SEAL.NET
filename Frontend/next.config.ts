import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Allow HTTPS backend with self-signed cert in dev
  experimental: {
    turbopackFileSystemCacheForDev: false,
  },
};

export default nextConfig;
