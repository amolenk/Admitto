import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { ticketTypeDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";

import { EditTicketTypeForm } from "./edit-ticket-type-form";

// The edit form mirrors the add form's capacity-dependent waitlist toggle while starting from
// a persisted ticket type rather than a blank one.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);

const TEAM_ID = "66666666-6666-6666-6666-666666666666";
const EVENT_ID = "77777777-7777-7777-7777-777777777777";
const TICKET_TYPE_ID = "cccccccc-0000-0000-0000-000000000001";
const EDIT_ENDPOINT =
    `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types/${TICKET_TYPE_ID}`;

function renderForm(overrides: Parameters<typeof ticketTypeDto>[0] = {}) {
    const onSaved = vi.fn();
    const onCancel = vi.fn();

    const result = renderWithProviders(
        <EditTicketTypeForm
            teamId={TEAM_ID}
            eventId={EVENT_ID}
            ticketType={ticketTypeDto(overrides)}
            onSaved={onSaved}
            onCancel={onCancel}
        />,
    );

    return { ...result, onSaved, onCancel };
}

describe("EditTicketTypeForm waitlist cascade", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    // Given a ticket type with no capacity limit
    // When the edit form renders
    // Then neither the max-capacity field nor the waitlist toggle is shown
    it("hides capacity and waitlist fields when the ticket type has no capacity limit", () => {
        renderForm({ maxCapacity: null, waitlistEnabled: true });

        expect(screen.queryByPlaceholderText("e.g. 100")).not.toBeInTheDocument();
        expect(screen.queryByRole("switch", { name: /enable waitlist/i })).not.toBeInTheDocument();
        expect(screen.queryByPlaceholderText("e.g. 8")).not.toBeInTheDocument();
        expect(screen.queryByText("Schedule")).not.toBeInTheDocument();
    });

    // Given a ticket type with no capacity limit
    // When the organizer switches capacity limiting on
    // Then the max-capacity field and the waitlist toggle both appear
    it("reveals the waitlist toggle once capacity limiting is switched on", async () => {
        const { user } = renderForm({ maxCapacity: null, waitlistEnabled: false });

        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));

        expect(screen.getByPlaceholderText("e.g. 100")).toBeInTheDocument();
        expect(screen.getByRole("switch", { name: /enable waitlist/i })).toBeInTheDocument();
        expect(screen.queryByPlaceholderText("e.g. 8")).not.toBeInTheDocument();

        await user.click(screen.getByRole("switch", { name: /enable waitlist/i }));

        expect(screen.getByPlaceholderText("e.g. 8")).toBeInTheDocument();
    });

    // Given a ticket type that already has a capacity limit and an active waitlist
    // When the edit form renders
    // Then the waitlist toggle is shown and pre-checked, matching the persisted state
    it("shows the waitlist toggle already enabled for a ticket type with capacity and waitlist configured", () => {
        renderForm({ maxCapacity: 100, waitlistEnabled: true, claimWindowHours: 6 });

        expect(screen.getByRole("switch", { name: /limit capacity/i })).toBeChecked();
        expect(screen.getByRole("switch", { name: /enable waitlist/i })).toBeChecked();
        expect(screen.getByPlaceholderText("e.g. 8")).toHaveValue(6);
    });

    // Given a ticket type that already has capacity and an active waitlist configured
    // When the organizer switches capacity limiting back off
    // Then the waitlist toggle disappears along with the capacity field, and the abandoned
    // waitlist configuration is not sent when the form is saved
    it("discards the waitlist configuration when capacity limiting is switched back off", async () => {
        const { user } = renderForm({
            maxCapacity: 100,
            waitlistEnabled: true,
            claimWindowHours: 6,
        });

        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));

        expect(screen.queryByRole("switch", { name: /enable waitlist/i })).not.toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, {
                name: "General Admission",
                selfServiceEnabled: true,
                maxCapacity: null,
                waitlistEnabled: false,
                claimWindowHours: undefined,
                maxReconfirmAttempts: null,
            }),
        );
        expect(put.mock.calls[0]![1]).not.toHaveProperty("timeSlots");
    });

    // Given a ticket type with persisted time slots
    // When the edit form renders and is saved
    // Then the slots are displayed as immutable history and omitted from the update payload
    it("shows time slots as non-editable and omits them from the update payload", async () => {
        const { user } = renderForm({
            timeSlots: ["morning", "afternoon"],
            maxCapacity: 100,
        });

        expect(screen.getByText("Schedule")).toBeInTheDocument();
        expect(screen.getByText("morning")).toBeInTheDocument();
        expect(screen.getByText("afternoon")).toBeInTheDocument();
        expect(screen.getByText("Time slots can't be changed after creation.")).toBeInTheDocument();
        expect(screen.queryByPlaceholderText("e.g. morning, afternoon")).not.toBeInTheDocument();

        const nameInput = screen.getAllByRole("textbox")[0]!;
        await user.clear(nameInput);
        await user.type(nameInput, "Updated Pass");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, {
                name: "Updated Pass",
                selfServiceEnabled: true,
                maxCapacity: 100,
                waitlistEnabled: false,
                claimWindowHours: undefined,
                maxReconfirmAttempts: null,
            }),
        );
        expect(put.mock.calls[0]![1]).not.toHaveProperty("timeSlots");
    });
});
