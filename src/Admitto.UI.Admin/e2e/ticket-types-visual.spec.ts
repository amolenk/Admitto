import { expect, test, type Locator, type Page } from "@playwright/test";
import { resolveDemoContext } from "./support/demo-data";
import { gotoTicketTypes } from "./support/navigation";

const GENERAL_ADMISSION = "General Admission";
const STABILITY_WINDOW_MS = 2_500;

type Geometry = {
    card: { x: number; y: number; width: number; height: number };
    stats: { x: number; y: number; width: number; height: number };
};

function generalAdmissionCard(page: Page): Locator {
    return page.locator(".ticket-card").filter({
        has: page.getByRole("heading", { name: GENERAL_ADMISSION, exact: true }),
    });
}

async function waitForMeaningfulCardContent(page: Page, card: Locator): Promise<void> {
    await expect(card).toBeVisible();
    await expect(card.getByRole("heading", { name: GENERAL_ADMISSION, exact: true })).toBeVisible();
    await expect(card.getByText("Registered", { exact: true })).toBeVisible();
    await expect(card.getByText("Remaining", { exact: true })).toBeVisible();
    await expect(card.getByText("Capacity", { exact: true })).toBeVisible();

    await page.evaluate(async () => {
        await document.fonts.ready;
    });
    await expect
        .poll(() => page.evaluate(() => document.fonts.status), {
            message: "Waiting for document fonts to finish loading",
        })
        .toBe("loaded");
    await expect
        .poll(
            () =>
                card.evaluate((element) => {
                    const text = element.textContent ?? "";
                    const { width, height } = element.getBoundingClientRect();
                    return (
                        text.includes("General Admission") &&
                        text.includes("Registered") &&
                        text.includes("Remaining") &&
                        text.includes("Capacity") &&
                        width > 0 &&
                        height > 0
                    );
                }),
            { message: "Waiting for meaningful General Admission card content" },
        )
        .toBe(true);
}

async function startLayoutShiftTracking(page: Page): Promise<void> {
    await page.evaluate(() => {
        const browserWindow = window as Window & {
            __ticketLayoutShiftState?: {
                entries: PerformanceEntry[];
                observer?: PerformanceObserver;
            };
        };
        const state: { entries: PerformanceEntry[]; observer?: PerformanceObserver } = {
            entries: [],
        };
        browserWindow.__ticketLayoutShiftState = state;

        if (PerformanceObserver.supportedEntryTypes.includes("layout-shift")) {
            state.observer = new PerformanceObserver((list) => {
                state.entries.push(...list.getEntries());
            });
            state.observer.observe({ type: "layout-shift", buffered: true });
        }
    });
}

async function readGeometry(card: Locator): Promise<Geometry> {
    return card.evaluate((element) => {
        const stats = element.querySelector(".grid.grid-cols-3");
        if (!stats) {
            throw new Error("Ticket card stats block was not found");
        }

        const rect = (node: Element) => {
            const { x, y, width, height } = node.getBoundingClientRect();
            return { x, y, width, height };
        };

        return { card: rect(element), stats: rect(stats) };
    });
}

async function assertStableGeometry(card: Locator): Promise<void> {
    const baseline = await readGeometry(card);
    const startedAt = Date.now();
    let changed = false;

    await expect
        .poll(
            async () => {
                const current = await readGeometry(card);
                if (JSON.stringify(current) !== JSON.stringify(baseline)) {
                    changed = true;
                }

                return !changed && Date.now() - startedAt >= STABILITY_WINDOW_MS;
            },
            {
                timeout: 5_000,
                intervals: [100, 250, 500, 1_000],
                message: `General Admission card geometry changed during the ${STABILITY_WINDOW_MS}ms stability window`,
            },
        )
        .toBe(true);
}

async function assertNoPostReadinessLayoutShift(card: Locator): Promise<void> {
    const shifts = await card.evaluate((element) => {
        const readyAt = (window as Window & { __ticketCardReadyAt?: number }).__ticketCardReadyAt;
        if (readyAt === undefined) {
            throw new Error("Ticket card readiness was not marked");
        }

        const browserWindow = window as Window & {
            __ticketLayoutShiftState?: {
                entries: PerformanceEntry[];
                observer?: PerformanceObserver;
            };
        };
        const state = browserWindow.__ticketLayoutShiftState;
        state?.entries.push(...(state.observer?.takeRecords() ?? []));
        state?.observer?.disconnect();

        return [
            ...performance.getEntriesByType("layout-shift"),
            ...(state?.entries ?? []),
        ]
            .map((entry) => {
                const layoutShift = entry as PerformanceEntry & {
                    hadRecentInput?: boolean;
                    sources?: Array<{ node?: Node | null }>;
                };
                const attributable = (layoutShift.sources ?? []).some(({ node }) => {
                    return node instanceof Element && (node === element || element.contains(node));
                });

                return {
                    startTime: entry.startTime,
                    value: (layoutShift as { value?: number }).value ?? 0,
                    hadRecentInput: layoutShift.hadRecentInput ?? false,
                    attributable,
                };
            })
            .filter(
                (entry) =>
                    entry.startTime >= readyAt && !entry.hadRecentInput && entry.attributable,
            );
    });

    expect(
        shifts,
        `Non-input layout shifts attributable to the General Admission card after readiness: ${JSON.stringify(shifts)}`,
    ).toEqual([]);
}

test.describe("ticket type card browser scenarios", () => {
    test("renders the perforated divider on the General Admission card", async ({ page }) => {
        const { team, event } = await resolveDemoContext(page);
        await gotoTicketTypes(page, team.teamId, event.id);

        const card = generalAdmissionCard(page);
        await expect(card).toHaveCount(1);
        await waitForMeaningfulCardContent(page, card);

        const perforation = card.locator(".ticket-perf");
        await expect(perforation).toHaveCount(1);
        await expect(perforation).toHaveAttribute("aria-hidden", "true");
        await expect
            .poll(() =>
                perforation.evaluate((element) => {
                    const style = getComputedStyle(element);
                    return {
                        borderTopStyle: style.borderTopStyle,
                        borderTopWidth: style.borderTopWidth,
                        height: style.height,
                    };
                }),
            )
            .toEqual({ borderTopStyle: "dashed", borderTopWidth: "1px", height: "1px" });

        await expect(card).toHaveScreenshot("ticket-type-card-perforated.png", {
            animations: "disabled",
        });
    });

    test("keeps the General Admission card stable after readiness", async ({ page }) => {
        const { team, event } = await resolveDemoContext(page);
        await gotoTicketTypes(page, team.teamId, event.id);
        await startLayoutShiftTracking(page);

        const card = generalAdmissionCard(page);
        await expect(card).toHaveCount(1);
        await waitForMeaningfulCardContent(page, card);
        await page.evaluate(() => {
            (window as Window & { __ticketCardReadyAt?: number }).__ticketCardReadyAt =
                performance.now();
        });

        await assertStableGeometry(card);
        await assertNoPostReadinessLayoutShift(card);
    });
});
