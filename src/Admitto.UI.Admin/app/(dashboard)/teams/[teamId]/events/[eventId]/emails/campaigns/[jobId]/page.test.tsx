import { screen, waitFor, within } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import type { BulkEmailJobDetailDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import BulkEmailDetailPage from "./page";

// Harvested from openspec `admin-ui-bulk-emails`. Cancel proxy forwarding is covered
// generically by `lib/admitto-api/admitto-client.node.test.ts`; this file covers the
// job-detail rendering and the cancel affordance's client-side wiring.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";
const JOB_ID = "33333333-3333-3333-3333-333333333333";

/** A bulk email job as the detail endpoint returns it. Sending (active) unless overridden. */
function bulkEmailJobDetailDto(overrides: Partial<BulkEmailJobDetailDto> = {}): BulkEmailJobDetailDto {
    return {
        id: JOB_ID,
        teamId: TEAM_ID,
        ticketedEventId: EVENT_ID,
        emailType: "bulk-custom",
        subject: "Important update",
        textBody: "Hello there",
        htmlBody: "<p>Hello there</p>",
        attendeeFilter: {},
        status: "sending",
        recipientCount: 20,
        sentCount: 12,
        failedCount: 1,
        cancelledCount: 0,
        lastError: null,
        isSystemTriggered: false,
        triggeredBy: "owner@example.com",
        createdAt: "2026-03-01T10:00:00Z",
        startedAt: "2026-03-01T10:01:00Z",
        completedAt: null,
        cancellationRequestedAt: null,
        cancelledAt: null,
        version: 1,
        recipients: [],
        ...overrides,
    };
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID, jobId: JOB_ID } });
    return renderWithProviders(<BulkEmailDetailPage />);
}

describe("BulkEmailDetailPage", () => {
    // Given a bulk email job
    // When its detail page loads
    // Then the summary section shows its status, type and recipient/sent/failed counts
    it("shows the job summary", async () => {
        get.mockResolvedValue(
            bulkEmailJobDetailDto({
                status: "partiallyFailed",
                recipientCount: 20,
                sentCount: 18,
                failedCount: 2,
            }),
        );

        renderPage();

        expect(await screen.findByText("Partial failure")).toBeInTheDocument();
        expect(screen.getByText("bulk-custom")).toBeInTheDocument();
        expect(screen.getByText("20")).toBeInTheDocument();
        expect(screen.getByText("18")).toBeInTheDocument();
        expect(screen.getByText("2")).toBeInTheDocument();
    });

    // Given a job whose attendee filter narrows by ticket type, status and reconfirmation
    // When the detail page loads
    // Then the recipient criteria used for the job are shown
    it("shows the attendee filter used for the job", async () => {
        get.mockResolvedValue(
            bulkEmailJobDetailDto({
                attendeeFilter: {
                    ticketTypeIds: ["tt-1", "tt-2"],
                    registrationStatus: "registered",
                    hasReconfirmed: true,
                },
            }),
        );

        renderPage();

        // Scoped to the Recipients section: several of its labels ("Status", "Recipients")
        // are reused verbatim by the Summary section above it.
        const recipients = (await screen.findByRole("heading", { name: "Recipients" })).closest(
            "div",
        )!;
        expect(within(recipients).getByText("Registered attendees")).toBeInTheDocument();
        expect(within(recipients).getByText("Ticket types")).toBeInTheDocument();
        expect(within(recipients).getByText("2")).toBeInTheDocument();
        expect(within(recipients).getByText("Status")).toBeInTheDocument();
        expect(within(recipients).getByText("registered")).toBeInTheDocument();
        expect(within(recipients).getByText("Reconfirmed")).toBeInTheDocument();
        expect(within(recipients).getByText("Yes")).toBeInTheDocument();
    });

    // Given a job that is still pending, resolving or sending
    // When its detail page loads
    // Then a Cancel button is available
    it("shows a cancel button for an active job", async () => {
        get.mockResolvedValue(bulkEmailJobDetailDto({ status: "sending" }));

        renderPage();

        expect(await screen.findByRole("button", { name: "Cancel job" })).toBeInTheDocument();
    });

    // Given a job that has already reached a terminal state
    // When its detail page loads
    // Then there is no Cancel button, since the job can no longer be stopped
    it("hides the cancel button for a terminal job", async () => {
        get.mockResolvedValue(bulkEmailJobDetailDto({ status: "completed" }));

        const { container } = renderPage();

        await waitFor(() =>
            expect(container.querySelector('[data-slot="badge"]')).toHaveTextContent("Completed"),
        );
        expect(screen.queryByRole("button", { name: "Cancel job" })).not.toBeInTheDocument();
    });

    // Given an active job, and an organizer who confirms the cancel dialog
    // When the cancel request succeeds
    // Then the job detail is refetched and reflects the new (terminal) status
    it("refreshes the job status after a successful cancel", async () => {
        get.mockResolvedValueOnce(bulkEmailJobDetailDto({ status: "sending" }));
        get.mockResolvedValueOnce(bulkEmailJobDetailDto({ status: "cancelled" }));
        post.mockResolvedValue(undefined);
        const { user, container } = renderPage();

        await user.click(await screen.findByRole("button", { name: "Cancel job" }));
        const dialog = await screen.findByRole("alertdialog");
        await user.click(within(dialog).getByRole("button", { name: "Cancel job" }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/bulk-emails/${JOB_ID}/cancel`,
            ),
        );
        await waitFor(() =>
            expect(container.querySelector('[data-slot="badge"]')).toHaveTextContent("Cancelled"),
        );
    });
});
