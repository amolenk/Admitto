import { fireEvent, screen, waitFor } from "@testing-library/react";
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
    // Then neither the public capacity field nor the waitlist toggle is shown
    it("hides capacity and waitlist fields when the ticket type has no capacity limit", () => {
        renderForm({ publicCapacity: null, waitlistEnabled: true });

        expect(screen.queryByPlaceholderText("e.g. 100")).not.toBeInTheDocument();
        expect(screen.queryByRole("switch", { name: /enable waitlist/i })).not.toBeInTheDocument();
        expect(screen.queryByPlaceholderText("e.g. 8")).not.toBeInTheDocument();
        expect(screen.queryByText("Schedule")).not.toBeInTheDocument();
    });

    // Given a ticket type with no capacity limit
    // When the organizer switches capacity limiting on
    // Then the max-capacity field and the waitlist toggle both appear
    it("reveals the waitlist toggle once capacity limiting is switched on", async () => {
        const { user } = renderForm({ publicCapacity: null, waitlistEnabled: false });

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
        renderForm({ publicCapacity: 100, waitlistEnabled: true, claimWindowHours: 6 });

        expect(screen.getByRole("switch", { name: /limit capacity/i })).toBeChecked();
        expect(screen.getByRole("switch", { name: /enable waitlist/i })).toBeChecked();
        expect(screen.getByPlaceholderText("e.g. 8")).toHaveValue(6);
    });

    // Given a ticket type with a public capacity
    // When the edit form renders
    // Then the capacity field is labelled public capacity with its value, explains that admin registrations and
    // coupons come on top, and there is no reserved capacity field
    it("shows the public capacity field without a reserved capacity field", () => {
        renderForm({ publicCapacity: 40, publicUsedCapacity: 40, adminUsedCount: 5 });

        expect(screen.getByLabelText("Public capacity")).toHaveValue(40);
        expect(screen.getByText(/Admin registrations and coupons come on top of this/)).toBeInTheDocument();
        expect(screen.queryByText(/reserved/i)).not.toBeInTheDocument();
    });

    // Given a ticket type that already has capacity and an active waitlist configured
    // When the organizer switches capacity limiting back off
    // Then the waitlist toggle disappears along with the capacity field, and the abandoned
    // waitlist configuration is not sent when the form is saved
    it("discards the waitlist configuration when capacity limiting is switched back off", async () => {
        const { user } = renderForm({
            publicCapacity: 100,
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
                publicCapacity: null,
                waitlistEnabled: false,
                claimWindowHours: undefined,
                maxReconfirmationEmails: null,
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
            publicCapacity: 100,
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
                publicCapacity: 100,
                waitlistEnabled: false,
                claimWindowHours: undefined,
                maxReconfirmationEmails: null,
            }),
        );
        expect(put.mock.calls[0]![1]).not.toHaveProperty("timeSlots");
    });
});

describe("EditTicketTypeForm waitlist disable and capacity-limit removal", () => {
    beforeEach(() => {
        put.mockReset();
        put.mockResolvedValue(undefined);
    });

    // Given a ticket type whose waitlist has people waiting
    // When the organizer switches the waitlist off and saves
    // Then a confirmation explains that everyone waiting is removed and must be informed, and nothing is saved yet
    it("asks for confirmation with the removal wording when explicitly disabling an active waitlist", async () => {
        const { user } = renderForm({ publicCapacity: 100, waitlistEnabled: true, waitlistMode: true });

        await user.click(screen.getByRole("switch", { name: /enable waitlist/i }));
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const dialog = await screen.findByRole("alertdialog");
        expect(dialog).toHaveTextContent("Disable waitlist?");
        expect(dialog).toHaveTextContent(
            "Disabling the waitlist removes everyone who is still waiting. You'll need to inform them yourself. " +
            "Offers already sent stay valid until they expire.",
        );
        expect(dialog).not.toHaveTextContent(/revoke/i);
        expect(put).not.toHaveBeenCalled();
    });

    // Given the disable confirmation is showing
    // When the organizer confirms
    // Then the ticket type is saved with its waitlist switched off
    it("saves the disabled waitlist once the organizer confirms", async () => {
        const { user } = renderForm({ publicCapacity: 100, waitlistEnabled: true, waitlistMode: true });

        await user.click(screen.getByRole("switch", { name: /enable waitlist/i }));
        await user.click(screen.getByRole("button", { name: "Save changes" }));
        await user.click(await screen.findByRole("button", { name: "Disable waitlist" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, expect.objectContaining({
                publicCapacity: 100,
                waitlistEnabled: false,
            })),
        );
    });

    // Given a ticket type whose waitlist has people waiting
    // When the organizer removes the capacity limit
    // Then an informational note says everyone waiting will receive an offer, and saving needs no confirmation
    it("shows an informational note instead of the disable dialog when removing the capacity limit", async () => {
        const { user } = renderForm({ publicCapacity: 100, waitlistEnabled: true, waitlistMode: true });

        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));

        const note = screen.getByRole("alert");
        expect(note).toHaveTextContent(/everyone waiting will receive an offer/i);

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, expect.objectContaining({
                publicCapacity: null,
                waitlistEnabled: false,
            })),
        );
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });

    // Given a ticket type with a waitlist but nobody waiting
    // When the organizer removes the capacity limit
    // Then no note about offers is shown
    it("does not show the offer note when nobody is waiting", async () => {
        const { user } = renderForm({ publicCapacity: 100, waitlistEnabled: true, waitlistMode: false });

        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));

        expect(screen.queryByText(/everyone waiting will receive an offer/i)).not.toBeInTheDocument();
    });
});

describe("EditTicketTypeForm raising capacity while people are waiting", () => {
    // 7 public seats taken, 8 admin tickets on top, 6 people waiting: the situation where an organizer, thinking
    // of 15 attendees, raised the public capacity to 15 and unexpectedly sent 6 offers.
    const waitingTicketType = {
        publicCapacity: 7,
        publicUsedCapacity: 7,
        adminUsedCount: 8,
        waitlistEnabled: true,
        waitlistMode: true,
        waitlistQueuedCount: 6,
    };

    beforeEach(() => {
        put.mockReset();
        put.mockResolvedValue(undefined);
    });

    function setPublicCapacity(value: string) {
        fireEvent.change(screen.getByPlaceholderText("e.g. 100"), { target: { value } });
    }

    // Given a ticket type with public, admin and waitlisted attendees
    // When the edit form renders
    // Then the capacity field shows that admin tickets come on top and how many people are waiting
    it("shows the public, admin and waitlist counts next to the public capacity", () => {
        renderForm(waitingTicketType);

        expect(screen.getByTestId("capacity-breakdown")).toHaveTextContent(
            "Now: 7 public tickets taken, plus 8 admin tickets on top (not counted); 6 people on the waitlist.",
        );
    });

    // Given 6 people waiting and 8 admin tickets on top of 7 public seats
    // When the organizer raises the public capacity to 15
    // Then a note says all 6 will receive an offer and 2 seats open once the offers are settled
    it("previews the offers the new capacity will send", async () => {
        renderForm(waitingTicketType);

        setPublicCapacity("15");

        const note = screen.getByRole("alert");
        expect(note).toHaveTextContent("6 people on the waitlist will receive an offer");
        expect(note).toHaveTextContent("frees 8 public seats; admin tickets don't count towards it");
        expect(note).toHaveTextContent("The other 2 seats open for self-service once every offer");
    });

    // Given the organizer raised the public capacity while people are waiting
    // When they save
    // Then nothing is saved until they confirm sending the offers
    it("asks for confirmation before saving a capacity that sends offers", async () => {
        const { user } = renderForm(waitingTicketType);

        setPublicCapacity("15");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const dialog = await screen.findByRole("alertdialog");
        expect(dialog).toHaveTextContent("Send 6 waitlist offers?");
        expect(put).not.toHaveBeenCalled();

        await user.click(screen.getByRole("button", { name: "Save and send offers" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, expect.objectContaining({ publicCapacity: 15 })),
        );
    });

    // Given the offer confirmation is showing
    // When the organizer cancels
    // Then nothing is saved
    it("saves nothing when the organizer cancels the offer confirmation", async () => {
        const { user } = renderForm(waitingTicketType);

        setPublicCapacity("15");
        await user.click(screen.getByRole("button", { name: "Save changes" }));
        await user.click(await screen.findByRole("button", { name: "Cancel" }));

        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });

    // Given people are waiting
    // When the organizer saves a capacity change that frees no public seat
    // Then it saves without a note or confirmation
    it("saves without confirmation when the change sends no offers", async () => {
        const { user } = renderForm(waitingTicketType);

        setPublicCapacity("6");
        expect(screen.queryByRole("alert")).not.toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(EDIT_ENDPOINT, expect.objectContaining({ publicCapacity: 6 })),
        );
        expect(screen.queryByRole("alertdialog")).not.toBeInTheDocument();
    });
});
