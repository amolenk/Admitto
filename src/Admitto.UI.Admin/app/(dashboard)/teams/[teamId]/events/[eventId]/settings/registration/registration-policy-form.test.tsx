import { fireEvent, screen, waitFor } from "@testing-library/react";
import { addDays, format } from "date-fns";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { wallClockToUtcIso } from "@/lib/time-zones";
import { renderWithProviders } from "@/test-utils/render";

import { EventStatusBanner } from "../event-status-banner";
import type { TicketedEventDetails } from "../event-detail-types";
import { RegistrationPolicyForm } from "./registration-policy-form";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);
const ENDPOINT = "/api/teams/t1/events/e1/registration-policy";

const policy = {
    opensAt: "2027-06-10T09:00:00.000Z",
    closesAt: "2027-06-11T17:00:00.000Z",
    allowedEmailDomain: null,
};

const event: TicketedEventDetails = {
    id: "e1",
    teamId: "t1",
    slug: "evt",
    name: "Test Event",
    startsAt: "2027-06-12T18:00:00.000Z",
    endsAt: "2027-06-13T18:00:00.000Z",
    timeZone: "UTC",
    status: "active",
    version: 3,
    isRegistrationOpen: false,
    registrationPolicy: null,
    reconfirmPolicy: null,
    waitlistPolicy: { quietHoursStart: "22:00:00", quietHoursEnd: "07:00:00" },
    additionalDetailSchema: [],
};

function renderPolicy(overrides: Partial<TicketedEventDetails> = {}, disabled = false) {
    const result = renderWithProviders(
        <RegistrationPolicyForm
            event={{ ...event, ...overrides }}
            teamId="t1"
            eventId="e1"
            disabled={disabled}
        />,
    );
    return result;
}

async function pickDateTime(
    user: ReturnType<typeof renderPolicy>["user"],
    trigger: HTMLElement,
    targetDate: Date,
    time: string,
) {
    await user.click(trigger);
    const current = new Date();
    const currentMonth = current.getFullYear() * 12 + current.getMonth();
    const targetMonth = targetDate.getFullYear() * 12 + targetDate.getMonth();
    const direction = targetMonth >= currentMonth ? /next month/i : /previous month/i;
    for (let i = 0; i < Math.abs(targetMonth - currentMonth); i++) {
        await user.click(screen.getByRole("button", { name: direction }));
    }

    const day = (await screen.findAllByRole("gridcell", { name: String(targetDate.getDate()) }))
        .find((cell) => !cell.className.includes("day-outside"));
    expect(day).toBeDefined();
    await user.click(day!);

    const timeInput = document.querySelector<HTMLInputElement>('input[type="time"]');
    expect(timeInput).not.toBeNull();
    fireEvent.change(timeInput!, { target: { value: time } });
    await user.keyboard("{Escape}");
}

describe("RegistrationPolicyForm", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    // Given an event without a registration policy
    // When an organizer configures the opening and closing times
    // Then the policy endpoint receives the exact UTC values and event version
    it("configures the registration window", async () => {
        const { user } = renderPolicy();
        // Keep the fixture and selected calendar month aligned with the fixed 2027 event.
        const openDate = new Date(2027, 5, 10);
        const closeDate = addDays(openDate, 1);

        const pickers = screen.getAllByRole("button", { name: /Pick a date/i });
        await pickDateTime(user, pickers[0]!, openDate, "09:15");
        await pickDateTime(user, pickers[1]!, closeDate, "17:45");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(ENDPOINT, {
                opensAt: wallClockToUtcIso(`${format(openDate, "yyyy-MM-dd")}T09:15`, "UTC"),
                closesAt: wallClockToUtcIso(`${format(closeDate, "yyyy-MM-dd")}T17:45`, "UTC"),
                allowedEmailDomain: null,
                expectedVersion: 3,
            }),
        );
    });

    // Given an existing registration window
    // When the organizer enables domain restriction and enters a domain
    // Then the exact domain is included in the policy update
    it("configures an email-domain restriction", async () => {
        const { user } = renderPolicy({ registrationPolicy: policy });

        await user.click(screen.getByRole("switch"));
        await user.type(screen.getByPlaceholderText("acme.org"), "acme.org");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(ENDPOINT, {
                opensAt: policy.opensAt,
                closesAt: policy.closesAt,
                allowedEmailDomain: "acme.org",
                expectedVersion: 3,
            }),
        );
    });

    // Given the registration policy form
    // When it renders
    // Then it does not expose legacy Open or Close controls
    it("has no legacy Open or Close controls", () => {
        renderPolicy();

        expect(screen.queryByRole("button", { name: /^Open$/i })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: /^Close$/i })).not.toBeInTheDocument();
    });

    // Given an archived event
    // When the policy page renders its status banner and form
    // Then the banner explains the read-only state and all policy controls are disabled
    it("is read-only for archived events", () => {
        renderWithProviders(
            <>
                <EventStatusBanner status="archived" />
                <RegistrationPolicyForm
                    event={{ ...event, status: "archived", registrationPolicy: policy }}
                    teamId="t1"
                    eventId="e1"
                    disabled
                />
            </>,
        );

        const alert = screen.getByRole("alert");
        expect(alert).toHaveTextContent("Event archived");
        expect(alert).toHaveTextContent("Policies are read-only and cannot be modified.");
        expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Discard" })).toBeDisabled();
        expect(screen.getByRole("switch")).toBeDisabled();
        expect(screen.getByRole("button", { name: "Remove policy" })).toBeDisabled();
        for (const dateButton of screen.getAllByRole("button", { name: /Jun 10|Jun 11/ })) {
            expect(dateButton).toBeDisabled();
        }
    });

    // Given a configured policy changed by another organizer
    // When this organizer saves the stale form
    // Then the concurrency conflict is shown to the user
    it("surfaces a concurrency conflict", async () => {
        const concurrencyConflict = {
            status: 409,
            title: "Conflict",
            detail: "The resource was modified by another operation.",
            errors: {},
            code: "concurrency_conflict",
        };
        put.mockRejectedValue(
            new FormError(concurrencyConflict),
        );
        const { user } = renderPolicy({ registrationPolicy: policy });

        await user.click(screen.getByRole("switch"));
        await user.type(screen.getByPlaceholderText("acme.org"), "acme.org");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const alert = await screen.findByRole("alert");
        expect(alert).toHaveTextContent("Conflict");
        expect(alert).toHaveTextContent("The resource was modified by another operation.");
    });
});
