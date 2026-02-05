import type { NextConfig } from "next";

const nextConfig: NextConfig = {
    output: "standalone",
    // TODO : Remove this once ESLint issues are resolved
    eslint: { ignoreDuringBuilds: true }
};

export default nextConfig;
