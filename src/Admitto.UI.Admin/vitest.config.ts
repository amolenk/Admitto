import { resolve } from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// `tsconfig.json` uses `baseUrl: "app/"` + `paths: { "@/*": ["./*"] }`. Declaring the
// alias by hand rather than via vite-tsconfig-paths keeps resolution predictable.
const alias = { "@": resolve(__dirname, "app") };

// Two environments, split by filename:
//   *.node.test.ts  -> server-side code (proxy routes, callAdmittoApi). No DOM, no setup file.
//   *.test.ts(x)    -> hooks, forms, pages. jsdom + the shared setup.
// The jsdom setup imports jest-dom, which needs a DOM, so the projects cannot be merged.
export default defineConfig({
    plugins: [react()],
    resolve: { alias },
    test: {
        projects: [
            {
                extends: true,
                test: {
                    name: "node",
                    environment: "node",
                    include: ["app/**/*.node.test.ts"],
                },
            },
            {
                extends: true,
                test: {
                    name: "jsdom",
                    environment: "jsdom",
                    setupFiles: ["./app/test-utils/setup.ts"],
                    include: ["app/**/*.test.ts", "app/**/*.test.tsx"],
                    exclude: ["app/**/*.node.test.ts"],
                },
            },
        ],
    },
});
