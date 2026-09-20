import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Next.js requires every route (page.tsx) to resolve, through its filesystem
 * ancestor chain, to a layout.tsx that renders <html>. This repo has no single
 * top-level app/layout.tsx: instead each route group ((auth), (dashboard),
 * (public)) declares its own root <html> shell. A page added outside any
 * group - or inside a group whose layout doesn't render <html> - builds fine
 * locally under `next dev` but fails `next build` with "doesn't have a root
 * layout", which is only ever run during the Docker/Aspire publish step.
 *
 * This test walks app/ (excluding api/, which has no layouts) and asserts
 * that invariant directly, so the mistake surfaces in the normal test run
 * instead of at deploy time.
 */

const APP_DIR = join(__dirname);
const EXCLUDED_TOP_LEVEL = new Set(["api"]);

function isDirectory(path: string): boolean {
    return statSync(path).isDirectory();
}

function hasOwnPage(dir: string): boolean {
    return readdirSync(dir).some((f) => f === "page.tsx" || f === "page.ts");
}

function findPageDirectories(dir: string, acc: string[] = []): string[] {
    if (hasOwnPage(dir)) acc.push(dir);

    for (const entry of readdirSync(dir)) {
        const entryPath = join(dir, entry);
        if (!isDirectory(entryPath)) continue;
        if (dir === APP_DIR && EXCLUDED_TOP_LEVEL.has(entry)) continue;

        findPageDirectories(entryPath, acc);
    }
    return acc;
}

function hasRootLayoutInAncestry(startDir: string): boolean {
    let current = startDir;

    while (current.startsWith(APP_DIR)) {
        const layoutPath = join(current, "layout.tsx");
        try {
            const contents = readFileSync(layoutPath, "utf-8");
            if (contents.includes("<html")) return true;
        } catch {
            // no layout.tsx at this level; keep walking up
        }

        if (current === APP_DIR) break;
        current = dirname(current);
    }

    return false;
}

describe("app route structure", () => {
    // Given every page.tsx under app/ (excluding api/)
    // When walking its ancestor directory chain for a layout.tsx
    // Then at least one ancestor layout must render <html>, or `next build` fails
    it("everyPage_hasAncestorLayoutRenderingHtml", () => {
        const pageDirectories = findPageDirectories(APP_DIR);

        expect(pageDirectories.length).toBeGreaterThan(0);

        const offenders = pageDirectories.filter((dir) => !hasRootLayoutInAncestry(dir));

        expect(offenders).toEqual([]);
    });
});
