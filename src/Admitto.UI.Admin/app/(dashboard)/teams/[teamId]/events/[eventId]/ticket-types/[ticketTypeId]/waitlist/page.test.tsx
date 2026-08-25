import { screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import {
    pendingNotificationRow,
    ticketTypeDto,
    waitlistDetailsDto,
    waitlistEntryRow,
} from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import WaitlistPage from "./page";

// Harvested from openspec `admin-ui-waitlist`. Position shifting and coupon expiry are
// backend concerns (see TESTING-BACKLOG.md); this page's own job is to render the snapshot
// it is given, mask emails, and let an organizer remove an active entry.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const del = vi.mocked(apiClient.delete);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";
const TICKET_TYPE_ID = "33333333-3333-3333-3333-333333333333";
const TICKET_TYPES_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types`;
const WAITLIST_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types/${TICKET_TYPE_ID}/waitlist`;

const ticketType = ticketTypeDto({ id: TICKET_TYPE_ID, name: "Workshop Pass" });

function mockEndpoints(waitlist: ReturnType<typeof waitlistDetailsDto>) {
    get.mockImplementation((url: string) => {
        if (url === WAITLIST_URL) return Promise.resolve(waitlist);
        if (url === TICKET_TYPES_URL) return Promise.resolve([ticketType]);
        return Promise.reject(new Error(`unexpected GET ${url}`));
    });
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID, ticketTypeId: TICKET_TYPE_ID } });
    return renderWithProviders(<WaitlistPage />);
}

function statContent(label: string): HTMLElement {
    const statLabel = screen.getAllByText(label).find((element) =>
        element.closest('[data-slot="card-content"]'),
    );

    if (!statLabel) throw new Error(`Could not find stat card for ${label}`);
    return statLabel.closest('[data-slot="card-content"]') as HTMLElement;
}

describe("WaitlistPage", () => {
    beforeEach(() => {
        // The page formats joined/expiry timestamps with the browser's local timezone. Keep
        // those assertions deterministic rather than depending on the runner's timezone.
        vi.stubEnv("TZ", "UTC");
    });

    afterEach(() => {
        vi.useRealTimers();
        vi.unstubAllEnvs();
    });

    // Given a ticket type with an active waitlist entry and a pending claim notification
    // When the page loads
    // Then the header shows the ticket type name, the stats reflect the snapshot, the entry's
    // email is shown already masked, and the pending notification shows a relative countdown
    // to its expiry alongside the absolute time
    it("renders entries with masked emails, joined dates and a claim-time countdown", async () => {
        vi.useFakeTimers({ toFake: ["Date"] });
        vi.setSystemTime(new Date("2026-08-01T10:00:00Z"));

        mockEndpoints(
            waitlistDetailsDto({
                activeEntries: [
                    waitlistEntryRow({
                        entryId: "aaaa1111-0000-0000-0000-000000000001",
                        position: 1,
                        maskedEmail: "ali***@example.com",
                        joinedAt: "2026-08-01T08:00:00Z",
                    }),
                    waitlistEntryRow({
                        entryId: "aaaa1111-0000-0000-0000-000000000002",
                        position: 2,
                        maskedEmail: "bea***@example.com",
                        joinedAt: "2026-07-31T08:00:00Z",
                    }),
                    waitlistEntryRow({
                        entryId: "aaaa1111-0000-0000-0000-000000000003",
                        position: 3,
                        maskedEmail: "cam***@example.com",
                        joinedAt: "2026-07-30T08:00:00Z",
                    }),
                ],
                pendingNotifications: [
                    pendingNotificationRow({
                        couponId: "bbbb2222-0000-0000-0000-000000000001",
                        maskedEmail: "bob***@example.com",
                        expiresAt: "2026-08-01T15:00:00Z",
                    }),
                ],
                stats: { totalWaiting: 3, totalPending: 1, sentToday: 4 },
            }),
        );

        renderPage();

        expect(await screen.findByRole("heading", { name: "Workshop Pass" })).toBeInTheDocument();

        const waitingStat = statContent("Waiting");
        const pendingStat = statContent("Pending notifications");
        const sentTodayStat = statContent("Sent today");
        expect(within(waitingStat).getByText("3")).toBeInTheDocument();
        expect(within(pendingStat).getByText("1")).toBeInTheDocument();
        expect(within(sentTodayStat).getByText("4")).toBeInTheDocument();

        const [activeTable, pendingTable] = screen.getAllByRole("table");
        const activeRows = within(activeTable!).getAllByRole("row").slice(1);
        const expectedEntries = [
            ["1", "ali***@example.com", "1 Aug 2026"],
            ["2", "bea***@example.com", "31 Jul 2026"],
            ["3", "cam***@example.com", "30 Jul 2026"],
        ];

        expect(activeRows).toHaveLength(expectedEntries.length);
        expectedEntries.forEach(([position, email, joined], index) => {
            const cells = within(activeRows[index]!).getAllByRole("cell");
            expect(cells[0]).toHaveTextContent(position);
            expect(cells[1]).toHaveTextContent(email);
            expect(cells[2]).toHaveTextContent(joined);
        });

        const pendingRows = within(pendingTable!).getAllByRole("row").slice(1);
        expect(pendingRows).toHaveLength(1);
        const pendingCells = within(pendingRows[0]!).getAllByRole("cell");
        expect(pendingCells[0]).toHaveTextContent("bob***@example.com");
        expect(pendingCells[1]).toHaveTextContent("1 Aug 2026, 15:00");
        expect(pendingCells[2]).toHaveTextContent("in about 5 hours");
    });

    // Given a ticket type with nobody on its waitlist
    // When the page loads
    // Then both tables show their empty-state copy instead of an empty table
    it("shows the empty state when nobody is on the waitlist", async () => {
        mockEndpoints(
            waitlistDetailsDto({
                activeEntries: [],
                pendingNotifications: [],
                stats: { totalWaiting: 0, totalPending: 0, sentToday: 0 },
            }),
        );

        renderPage();

        expect(
            await screen.findByText("No one is currently on the waitlist."),
        ).toBeInTheDocument();
        expect(screen.getByText("No pending notifications.")).toBeInTheDocument();
    });

    // Given an active waitlist entry
    // When the organizer removes it
    // Then a DELETE is sent for that entry and the list is refreshed to reflect the removal —
    // the position shift itself is a backend concern, not asserted here
    it("removes an active entry and refreshes the list", async () => {
        let waitlist = waitlistDetailsDto({
            activeEntries: [
                waitlistEntryRow({ entryId: "aaaa1111-0000-0000-0000-000000000001" }),
                waitlistEntryRow({
                    entryId: "aaaa1111-0000-0000-0000-000000000002",
                    position: 2,
                    maskedEmail: "bea***@example.com",
                }),
            ],
        });
        get.mockImplementation((url: string) => {
            if (url === WAITLIST_URL) return Promise.resolve(waitlist);
            if (url === TICKET_TYPES_URL) return Promise.resolve([ticketType]);
            return Promise.reject(new Error(`unexpected GET ${url}`));
        });
        del.mockImplementation(async () => {
            waitlist = waitlistDetailsDto({
                activeEntries: [
                    waitlistEntryRow({
                        entryId: "aaaa1111-0000-0000-0000-000000000002",
                        position: 1,
                        maskedEmail: "bea***@example.com",
                    }),
                ],
                stats: { totalWaiting: 1, totalPending: 0, sentToday: 0 },
            });
        });

        const { user } = renderPage();

        const initialAlice = await screen.findByText("ali***@example.com");
        expect(within(initialAlice.closest("tr")!).getAllByRole("cell")[0]).toHaveTextContent("1");

        const row = initialAlice.closest("tr")!;
        await user.click(within(row).getByRole("button"));

        await waitFor(() =>
            expect(del).toHaveBeenCalledWith(
                `${WAITLIST_URL}/aaaa1111-0000-0000-0000-000000000001`,
            ),
        );
        await waitFor(() =>
            expect(screen.queryByText("ali***@example.com")).not.toBeInTheDocument(),
        );
        const remainingBob = screen.getByText("bea***@example.com");
        const remainingRow = remainingBob.closest("tr")!;
        const remainingCells = within(remainingRow).getAllByRole("cell");
        expect(remainingCells[0]).toHaveTextContent("1");
        expect(remainingCells[1]).toHaveTextContent("bea***@example.com");
        expect(screen.queryByText("No one is currently on the waitlist.")).not.toBeInTheDocument();

        const waitingStat = statContent("Waiting");
        const pendingStat = statContent("Pending notifications");
        const sentTodayStat = statContent("Sent today");
        expect(within(waitingStat).getByText("1")).toBeInTheDocument();
        expect(within(pendingStat).getByText("0")).toBeInTheDocument();
        expect(within(sentTodayStat).getByText("0")).toBeInTheDocument();
    });
});
