import { defineConfig, devices } from "@playwright/test";

const isListCommand = process.argv.some(
    (argument) => argument === "--list" || argument.startsWith("--list="),
);

const baseURL = process.env.E2E_BASE_URL ?? (isListCommand ? undefined : missingBaseUrl());

function missingBaseUrl(): never {
    throw new Error(
        "E2E_BASE_URL is required when executing Playwright tests. " +
        "The stack must be started separately; Playwright does not start a web server.",
    );
}

const authState = "playwright/.auth/alice.json";

export default defineConfig({
    testDir: "./e2e",
    outputDir: "playwright/test-results",
    reporter: [["list"], ["html", { outputFolder: "playwright/report", open: "never" }]],
    retries: process.env.CI ? 2 : 0,
    use: {
        baseURL,
        viewport: { width: 1440, height: 900 },
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
        video: "retain-on-failure",
    },
    projects: [
        {
            name: "setup",
            testMatch: /auth\.setup\.ts/,
            use: { ...devices["Desktop Chrome"] },
        },
        {
            name: "chromium",
            testMatch: /.*\.spec\.ts/,
            testIgnore: /unauthenticated\.spec\.ts/,
            dependencies: ["setup"],
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 1440, height: 900 },
                storageState: authState,
            },
        },
        {
            name: "chromium-unauthenticated",
            testMatch: /unauthenticated\.spec\.ts/,
            use: {
                ...devices["Desktop Chrome"],
                viewport: { width: 1440, height: 900 },
                storageState: { cookies: [], origins: [] },
            },
        },
    ],
});
