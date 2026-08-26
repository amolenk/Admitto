import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";

import { SendBulkEmailSheet } from "./send-bulk-email-sheet";

// The sheet enforces required fields and the attendee-only, template-free recipient model;
// generic API proxy forwarding is covered separately.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";

function renderSheet(onClose = vi.fn()) {
    const result = renderWithProviders(
        <SendBulkEmailSheet teamId={TEAM_ID} eventId={EVENT_ID} open={true} onClose={onClose} />,
    );
    return { ...result, onClose };
}

/** Fills in the three direct-content fields, in DOM order: subject, text body, HTML body. */
async function fillContent(user: ReturnType<typeof renderSheet>["user"]) {
    const [subject, textBody, htmlBody] = screen.getAllByRole("textbox");
    await user.type(subject!, "Important update");
    await user.type(textBody!, "Hello there");
    await user.type(htmlBody!, "<p>Hello there</p>");
}

describe("SendBulkEmailSheet", () => {
    beforeEach(() => {
        get.mockResolvedValue([]);
        post.mockResolvedValue({ bulkEmailJobId: "job-1" });
    });

    // Given the sheet has just opened
    // When no content has been entered
    // Then Send is disabled, and it only enables once subject, text body and HTML body are
    // all filled in
    it("requires subject, text body and HTML body before Send is enabled", async () => {
        const { user } = renderSheet();
        const send = screen.getByRole("button", { name: "Send" });
        expect(send).toBeDisabled();

        const [subject, textBody, htmlBody] = screen.getAllByRole("textbox");
        await user.type(subject!, "Important update");
        expect(send).toBeDisabled();

        await user.type(textBody!, "Hello there");
        expect(send).toBeDisabled();

        await user.type(htmlBody!, "<p>Hello there</p>");
        expect(send).toBeEnabled();
    });

    // Given complete direct content
    // When the organizer sends
    // Then the job is created with the typed content and an attendee-based recipient filter
    it("submits the direct content and attendee filter on send", async () => {
        const { user } = renderSheet();
        await fillContent(user);

        await user.click(screen.getByRole("button", { name: "Send" }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/bulk-emails`,
                {
                    emailType: "bulk-custom",
                    subject: "Important update",
                    textBody: "Hello there",
                    htmlBody: "<p>Hello there</p>",
                    attendeeFilter: { ticketTypeIds: null, registrationStatus: null },
                },
            ),
        );
    });

    // Given a successful send
    // When the mutation resolves
    // Then the sheet closes
    it("closes the sheet on successful submission", async () => {
        const { user, onClose } = renderSheet();
        await fillContent(user);

        await user.click(screen.getByRole("button", { name: "Send" }));

        await waitFor(() => expect(onClose).toHaveBeenCalled());
    });

    // Given the sheet only ever targets registered attendees
    // When it renders
    // Then there is no file-upload input anywhere for an external recipient list
    it("has no file-upload input, since recipients are attendee-only", () => {
        renderSheet();

        expect(document.querySelector('input[type="file"]')).not.toBeInTheDocument();
    });

    // Given the bulk-email feature only supports direct content
    // When the sheet renders
    // Then there is no template selection control
    it("has no template selection", () => {
        renderSheet();

        expect(screen.queryByText(/template/i)).not.toBeInTheDocument();
    });
});
