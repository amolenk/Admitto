import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import type { BulkEmailListItemDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import CampaignsPage from "./page";

// The page exercises status filtering, empty state, and row navigation; generic API proxy
// forwarding is covered separately.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";

/** A bulk email job as `/bulk-emails` lists it. Completed unless overridden. */
function bulkEmailListItemDto(overrides: Partial<BulkEmailListItemDto> = {}): BulkEmailListItemDto {
    return {
        id: "aaaaaaaa-0000-0000-0000-000000000001",
        emailType: "bulk-custom",
        status: "completed",
        recipientCount: 10,
        sentCount: 10,
        failedCount: 0,
        cancelledCount: 0,
        isSystemTriggered: false,
        triggeredBy: "owner@example.com",
        createdAt: "2026-03-01T10:00:00Z",
        startedAt: "2026-03-01T10:01:00Z",
        completedAt: "2026-03-01T10:05:00Z",
        cancellationRequestedAt: null,
        cancelledAt: null,
        ...overrides,
    };
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID } });
    return renderWithProviders(<CampaignsPage />);
}

/** Opens the (only) status filter and picks `label`. */
async function selectStatus(user: ReturnType<typeof renderPage>["user"], label: string) {
    await user.click(screen.getByRole("combobox"));
    await user.click(await screen.findByRole("option", { name: label }));
}

describe("CampaignsPage", () => {
    beforeEach(() => {
        get.mockResolvedValue([]);
    });

    // Given the bulk-emails endpoint returns jobs newest first
    // When the campaigns list renders
    // Then the rows preserve that order rather than re-sorting them
    it("renders jobs in the order the API returns them", async () => {
        const newest = bulkEmailListItemDto({
            id: "job-newest",
            emailType: "newest-job",
            createdAt: "2026-03-10T10:00:00Z",
        });
        const oldest = bulkEmailListItemDto({
            id: "job-oldest",
            emailType: "oldest-job",
            createdAt: "2026-01-01T10:00:00Z",
        });
        get.mockResolvedValue([newest, oldest]);

        renderPage();

        const rows = await screen.findAllByRole("row");
        // rows[0] is the header row.
        expect(within(rows[1]!).getByText("newest-job")).toBeInTheDocument();
        expect(within(rows[2]!).getByText("oldest-job")).toBeInTheDocument();
    });

    // Given an event with no bulk email jobs
    // When the campaigns list renders
    // Then an empty state is shown instead of a table
    it("shows an empty state when there are no jobs", async () => {
        get.mockResolvedValue([]);

        renderPage();

        expect(await screen.findByText("No bulk emails yet")).toBeInTheDocument();
        expect(screen.queryByRole("table")).not.toBeInTheDocument();
    });

    // Given jobs in active, completed and cancelled statuses
    // When the organizer filters to "Active"
    // Then only pending/resolving/sending jobs remain visible
    it("narrows the list to active jobs", async () => {
        get.mockResolvedValue([
            bulkEmailListItemDto({ id: "1", emailType: "active-job", status: "sending" }),
            bulkEmailListItemDto({ id: "2", emailType: "completed-job", status: "completed" }),
            bulkEmailListItemDto({ id: "3", emailType: "cancelled-job", status: "cancelled" }),
        ]);
        const { user } = renderPage();
        await screen.findByText("active-job");

        await selectStatus(user, "Active");

        expect(screen.getByText("active-job")).toBeInTheDocument();
        expect(screen.queryByText("completed-job")).not.toBeInTheDocument();
        expect(screen.queryByText("cancelled-job")).not.toBeInTheDocument();
    });

    // Given the same mixed jobs
    // When the organizer filters to "Completed"
    // Then only completed/partially-failed jobs remain visible
    it("narrows the list to completed jobs", async () => {
        get.mockResolvedValue([
            bulkEmailListItemDto({ id: "1", emailType: "active-job", status: "sending" }),
            bulkEmailListItemDto({ id: "2", emailType: "completed-job", status: "partiallyFailed" }),
            bulkEmailListItemDto({ id: "3", emailType: "cancelled-job", status: "cancelled" }),
        ]);
        const { user } = renderPage();
        await screen.findByText("active-job");

        await selectStatus(user, "Completed");

        expect(screen.getByText("completed-job")).toBeInTheDocument();
        expect(screen.queryByText("active-job")).not.toBeInTheDocument();
        expect(screen.queryByText("cancelled-job")).not.toBeInTheDocument();
    });

    // Given the same mixed jobs
    // When the organizer filters to "Failed & Cancelled"
    // Then only failed/cancelled jobs remain visible
    it("narrows the list to failed and cancelled jobs", async () => {
        get.mockResolvedValue([
            bulkEmailListItemDto({ id: "1", emailType: "active-job", status: "sending" }),
            bulkEmailListItemDto({ id: "2", emailType: "completed-job", status: "completed" }),
            bulkEmailListItemDto({ id: "3", emailType: "failed-job", status: "failed" }),
        ]);
        const { user } = renderPage();
        await screen.findByText("active-job");

        await selectStatus(user, "Failed & Cancelled");

        expect(screen.getByText("failed-job")).toBeInTheDocument();
        expect(screen.queryByText("active-job")).not.toBeInTheDocument();
        expect(screen.queryByText("completed-job")).not.toBeInTheDocument();
    });

    // Given a rendered list with one job
    // When the organizer clicks its row
    // Then the browser is sent to that job's detail page
    it("navigates to job detail when a row is clicked", async () => {
        const job = bulkEmailListItemDto({ id: "job-1", emailType: "bulk-custom" });
        get.mockResolvedValue([job]);
        const originalLocation = window.location;
        Object.defineProperty(window, "location", { configurable: true, value: { href: "" } });

        const { user } = renderPage();
        await user.click(await screen.findByText("bulk-custom"));

        expect(window.location.href).toBe(
            `/teams/${TEAM_ID}/events/${EVENT_ID}/emails/campaigns/${job.id}`,
        );

        Object.defineProperty(window, "location", { configurable: true, value: originalLocation });
    });
});
