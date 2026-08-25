import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";

import { TicketedEventDetails } from "../event-detail-types";
import { WaitlistPolicyForm } from "./waitlist-policy-form";

// Harvested from openspec `admin-ui-waitlist`: "Organizer sets quiet hours on event". Only the
// form half is in scope — the coupon-expiry extension that quiet hours drive is a backend rule
// (see TESTING-BACKLOG.md).

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);

const TEAM_ID = "44444444-4444-4444-4444-444444444444";
const EVENT_ID = "55555555-5555-5555-5555-555555555555";

function baseEvent(overrides: Partial<TicketedEventDetails> = {}): TicketedEventDetails {
    return {
        id: EVENT_ID,
        teamId: TEAM_ID,
        slug: "acme-summit",
        name: "Acme Summit",
        startsAt: "2026-09-01T09:00:00Z",
        endsAt: "2026-09-03T17:00:00Z",
        timeZone: "Europe/Amsterdam",
        status: "active",
        version: 4,
        isRegistrationOpen: true,
        registrationPolicy: null,
        reconfirmPolicy: null,
        waitlistPolicy: { quietHoursStart: "22:00:00", quietHoursEnd: "08:00:00" },
        ...overrides,
    };
}

function renderForm(
    overrides: Partial<TicketedEventDetails> = {},
    disabled = false,
) {
    return renderWithProviders(
        <WaitlistPolicyForm
            event={baseEvent(overrides)}
            teamId={TEAM_ID}
            eventId={EVENT_ID}
            disabled={disabled}
        />,
    );
}

/** The two `<input type="time">` fields, in document order (start, then end). */
function timeInputs() {
    return screen.getAllByDisplayValue(/^\d{2}:\d{2}$/) as HTMLInputElement[];
}

describe("WaitlistPolicyForm", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    // Given an event with a configured quiet-hours window
    // When the form renders
    // Then the current start and end times are shown, trimmed to HH:MM
    it("shows the event's current quiet hours", () => {
        renderForm();

        const [start, end] = timeInputs();
        expect(start).toHaveValue("22:00");
        expect(end).toHaveValue("08:00");
        expect(
            screen.getByText(
                "Waitlist notifications are sent immediately. Quiet hours extend the claim deadline so attendees still get the full claim window during waking hours.",
            ),
        ).toBeInTheDocument();
    });

    // Given an organizer changing the quiet-hours window
    // When they submit the form
    // Then the new times are sent with trailing seconds and the event's loaded version, so a
    // concurrent edit elsewhere is still detected
    it("submits the updated quiet hours with the event's version", async () => {
        const { user } = renderForm({ version: 4 });
        const [start, end] = timeInputs();

        fireEvent.change(start, { target: { value: "23:00" } });
        fireEvent.change(end, { target: { value: "07:30" } });

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/waitlist-policy`,
                {
                    quietHoursStart: "23:00:00",
                    quietHoursEnd: "07:30:00",
                    expectedVersion: 4,
                },
            ),
        );
    });

    // Given the save button is disabled until something changes
    // When the organizer edits a field
    // Then the button becomes enabled, so an unmodified form cannot be resubmitted
    it("enables Save changes only once a field is edited", async () => {
        renderForm();
        const [start] = timeInputs();

        expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();

        fireEvent.change(start, { target: { value: "23:00" } });

        await waitFor(() =>
            expect(screen.getByRole("button", { name: "Save changes" })).toBeEnabled(),
        );
    });

    // Given the organizer changed a quiet-hours field but changed their mind
    // When they click Discard
    // Then the field reverts to the loaded value and nothing is submitted
    it("discards an edit without submitting", async () => {
        const { user } = renderForm();
        const [start] = timeInputs();

        fireEvent.change(start, { target: { value: "23:00" } });
        await user.click(screen.getByRole("button", { name: "Discard" }));

        expect(timeInputs()[0]).toHaveValue("22:00");
        expect(put).not.toHaveBeenCalled();
    });

    // Given an archived event
    // When the policy form renders
    // Then its quiet-hours controls and actions are read-only
    it("is read-only for an archived event", () => {
        renderForm({ status: "archived" }, true);

        const [start, end] = timeInputs();
        expect(start).toBeDisabled();
        expect(end).toBeDisabled();
        expect(screen.getByRole("button", { name: "Discard" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();
    });

    // Given the event was modified after the policy page loaded
    // When the organizer saves their quiet-hours change
    // Then the concurrency conflict is shown instead of being swallowed
    it("surfaces a concurrency conflict", async () => {
        put.mockRejectedValue(
            new FormError({
                type: "about:blank",
                status: 409,
                title: "Conflict",
                detail: "The resource was modified by another operation.",
                errors: {},
                // These are ProblemDetails extensions emitted with the real concurrency_conflict
                // error. FormError only consumes the standard title/detail fields.
                code: "concurrency_conflict",
                expectedVersion: 4,
                actualVersion: 5,
            } as ConstructorParameters<typeof FormError>[0]),
        );
        const { user } = renderForm();
        const [start] = timeInputs();

        fireEvent.change(start, { target: { value: "23:00" } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const alert = await screen.findByRole("alert");
        expect(alert).toHaveTextContent("Conflict");
        expect(alert).toHaveTextContent("The resource was modified by another operation.");
    });
});
