import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { validationProblemDetails } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";

import { AddTicketTypeForm } from "./add-ticket-type-form";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const post = vi.mocked(apiClient.post);

const TEAM_ID = "22222222-2222-2222-2222-222222222222";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";
const ENDPOINT = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types`;

function renderForm(suggestions: string[] = []) {
    const onAdded = vi.fn();
    const onCancel = vi.fn();

    const result = renderWithProviders(
        <AddTicketTypeForm
            teamId={TEAM_ID}
            eventId={EVENT_ID}
            suggestions={suggestions}
            onAdded={onAdded}
            onCancel={onCancel}
        />,
    );

    return { ...result, onAdded, onCancel };
}

/** The payload the form built, once it has submitted. */
async function submittedPayload() {
    await waitFor(() => expect(post).toHaveBeenCalled());
    return post.mock.calls[0]![1] as Record<string, unknown>;
}

describe("AddTicketTypeForm", () => {
    beforeEach(() => {
        post.mockResolvedValue(undefined);
    });

    // Given a ticket type with capacity limiting left off
    // When the form is submitted
    // Then capacity is sent as null and the waitlist stays off, regardless of defaults
    it("sends a null capacity and no waitlist when capacity is not limited", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("Early Bird"), "General Admission");
        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(await submittedPayload()).toEqual({
            name: "General Admission",
            selfServiceEnabled: true,
            maxCapacity: null,
            waitlistEnabled: false,
            claimWindowHours: undefined,
            maxReconfirmAttempts: null,
            timeSlots: [],
        });
        expect(post).toHaveBeenCalledWith(ENDPOINT, expect.anything());
    });

    // Given capacity limiting and the waitlist are both enabled
    // When the form is submitted without touching the claim window
    // Then the capacity, the waitlist flag, and the default 8-hour claim window are sent
    it("sends capacity, waitlist and the default claim window when limiting is on", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("Early Bird"), "Early Bird");
        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));
        await user.type(screen.getByPlaceholderText("e.g. 100"), "100");
        await user.click(screen.getByRole("switch", { name: /enable waitlist/i }));
        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(await submittedPayload()).toMatchObject({
            maxCapacity: 100,
            waitlistEnabled: true,
            claimWindowHours: 8,
        });
    });

    // Given a capacity and waitlist were configured while capacity was limited
    // When capacity limiting is switched back off before submitting
    // Then the entered capacity is discarded and the waitlist is forced off, rather than
    // the abandoned values leaking into the request
    it("discards capacity and waitlist when capacity limiting is switched back off", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("Early Bird"), "Early Bird");
        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));
        await user.type(screen.getByPlaceholderText("e.g. 100"), "100");
        await user.click(screen.getByRole("switch", { name: /enable waitlist/i }));
        await user.click(screen.getByRole("switch", { name: /limit capacity/i }));

        // The dependent fields disappear with the toggle.
        expect(screen.queryByRole("switch", { name: /enable waitlist/i })).not.toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(await submittedPayload()).toMatchObject({
            maxCapacity: null,
            waitlistEnabled: false,
            claimWindowHours: undefined,
        });
    });

    // Given a form with no name
    // When it is submitted
    // Then the field error is shown and no request is made
    it("blocks submission and shows an error when the name is empty", async () => {
        const { user } = renderForm();

        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(await screen.findByText("Name is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given the backend rejects the submission with a field-level validation error
    // When the form handles the failure
    // Then the server message is surfaced on the offending field
    it("surfaces a server-side field error on the field", async () => {
        post.mockRejectedValue(
            new FormError(
                validationProblemDetails({
                    name: ["A ticket type with this name already exists"],
                }),
            ),
        );

        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("Early Bird"), "Early Bird");
        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(
            await screen.findByText("A ticket type with this name already exists"),
        ).toBeInTheDocument();
    });
});

describe("AddTicketTypeForm time slots", () => {
    beforeEach(() => {
        post.mockResolvedValue(undefined);
    });

    // Given a time slot typed in mixed case with surrounding whitespace
    // When it is committed with Enter
    // Then it is normalised to a lowercase slug and submitted
    it("normalises a slot to a lowercase slug", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("Early Bird"), "Early Bird");
        await user.type(screen.getByPlaceholderText("e.g. morning, afternoon"), "  Morning  {Enter}");

        expect(screen.getByText("morning")).toBeInTheDocument();

        await user.click(screen.getByRole("button", { name: "Add ticket type" }));

        expect(await submittedPayload()).toMatchObject({ timeSlots: ["morning"] });
    });

    // Given a slot that is already in the list
    // When it is added again
    // Then it is rejected as a duplicate rather than added twice
    it("rejects a duplicate slot", async () => {
        const { user } = renderForm();
        const slots = screen.getByPlaceholderText("e.g. morning, afternoon");

        await user.type(slots, "morning{Enter}");
        await user.type(slots, "morning{Enter}");

        expect(await screen.findByText("Already added")).toBeInTheDocument();
        expect(screen.getAllByText("morning")).toHaveLength(1);
    });

    // Given a slot containing characters outside the slug alphabet
    // When it is committed
    // Then the slug rule is explained and nothing is added
    it("rejects a slot that is not a valid slug", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("e.g. morning, afternoon"), "Bad Slot!{Enter}");

        expect(await screen.findByText("Lowercase letters, digits, hyphens")).toBeInTheDocument();
    });

    // Given a committed slot and an empty draft
    // When Backspace is pressed
    // Then the most recently added slot is removed
    it("removes the last slot on Backspace when the draft is empty", async () => {
        const { user } = renderForm();
        const slots = screen.getByPlaceholderText("e.g. morning, afternoon");

        await user.type(slots, "morning{Enter}");
        await user.type(slots, "afternoon{Enter}");
        await user.type(slots, "{Backspace}");

        expect(screen.queryByText("afternoon")).not.toBeInTheDocument();
        expect(screen.getByText("morning")).toBeInTheDocument();
    });

    // Given a comma-separated entry
    // When the comma is typed
    // Then it commits the slot, matching the on-screen hint
    it("commits a slot when a comma is typed", async () => {
        const { user } = renderForm();

        await user.type(screen.getByPlaceholderText("e.g. morning, afternoon"), "morning,");

        expect(screen.getByText("morning")).toBeInTheDocument();
    });

    // Given other ticket types in the event already use some slots
    // When one of those suggestions is clicked
    // Then it is added and disappears from the remaining suggestions
    it("adds a slot from the event's existing suggestions", async () => {
        const { user } = renderForm(["morning", "afternoon"]);

        await user.click(screen.getByRole("button", { name: "+ morning" }));

        expect(screen.queryByRole("button", { name: "+ morning" })).not.toBeInTheDocument();
        expect(screen.getByRole("button", { name: "+ afternoon" })).toBeInTheDocument();
    });
});
