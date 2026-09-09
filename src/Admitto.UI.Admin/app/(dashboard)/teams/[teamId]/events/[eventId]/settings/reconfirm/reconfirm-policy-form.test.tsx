import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";

import { TicketedEventDetails } from "../event-detail-types";
import { ReconfirmPolicyForm } from "./reconfirm-policy-form";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);

const TEAM_ID = "22222222-2222-2222-2222-222222222222";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";

function baseEvent(overrides: Partial<TicketedEventDetails> = {}): TicketedEventDetails {
    return {
        id: EVENT_ID,
        teamId: TEAM_ID,
        slug: "acme-summit",
        name: "Acme Summit",
        startsAt: "2025-06-01T09:00:00Z",
        endsAt: "2025-06-03T17:00:00Z",
        timeZone: "Europe/Amsterdam",
        status: "active",
        version: 4,
        isRegistrationOpen: true,
        registrationPolicy: null,
        reconfirmPolicy: {
            opensAt: "2025-05-01T08:00:00Z",
            closesAt: "2025-05-25T08:00:00Z",
            minEmailIntervalHours: 1,
            quietHoursStart: null,
            quietHoursEnd: null,
        },
        waitlistPolicy: { quietHoursStart: "22:00:00", quietHoursEnd: "08:00:00" },
        ...overrides,
    };
}

function renderForm(
    eventOverrides: Partial<TicketedEventDetails> = {},
    disabled = false,
) {
    return renderWithProviders(
        <ReconfirmPolicyForm
            event={baseEvent(eventOverrides)}
            teamId={TEAM_ID}
            eventId={EVENT_ID}
            disabled={disabled}
        />,
    );
}

function numberInputs() {
    return screen.getAllByRole("spinbutton") as HTMLInputElement[];
}

describe("ReconfirmPolicyForm", () => {
    beforeEach(() => {
        put.mockReset();
        put.mockResolvedValue(undefined);
    });

    it("configures the reconfirm policy with a minimum reminder interval", async () => {
        const { user } = renderForm();
        const [minEmailInterval] = numberInputs();

        fireEvent.change(minEmailInterval, { target: { value: "48", valueAsNumber: 48 } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/reconfirm-policy`,
                {
                    opensAt: "2025-05-01T08:00:00Z",
                    closesAt: "2025-05-25T08:00:00Z",
                    minEmailIntervalHours: 48,
                    quietHoursStart: null,
                    quietHoursEnd: null,
                    expectedVersion: 4,
                },
            ),
        );
    });

    it("removes a configured reconfirm policy", async () => {
        const { user } = renderForm();

        await user.click(screen.getByRole("button", { name: "Remove policy" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/reconfirm-policy`,
                {
                    opensAt: null,
                    closesAt: null,
                    minEmailIntervalHours: null,
                    quietHoursStart: null,
                    quietHoursEnd: null,
                    expectedVersion: 4,
                },
            ),
        );
    });

    it("shows no remove action when no policy is configured", () => {
        renderForm({ reconfirmPolicy: null });

        expect(screen.queryByRole("button", { name: "Remove policy" })).not.toBeInTheDocument();
    });

    it("sends optional overnight no-reminder quiet hours", async () => {
        const { user } = renderForm();
        await user.click(screen.getByRole("switch"));
        const quietTimes = Array.from(document.querySelectorAll<HTMLInputElement>('input[type="time"]'));
        fireEvent.change(quietTimes[0], { target: { value: "22:00" } });
        fireEvent.change(quietTimes[1], { target: { value: "06:00" } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() => expect(put).toHaveBeenCalledWith(expect.any(String), expect.objectContaining({ quietHoursStart: "22:00:00", quietHoursEnd: "06:00:00" })));
    });

    it("blocks equal quiet-hours boundaries before submit", async () => {
        const { user } = renderForm();
        await user.click(screen.getByRole("switch"));
        const quietTimes = Array.from(document.querySelectorAll<HTMLInputElement>('input[type="time"]'));
        fireEvent.change(quietTimes[0], { target: { value: "22:00" } });
        fireEvent.change(quietTimes[1], { target: { value: "22:00" } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));
        expect(await screen.findByText("Quiet hours must have different start and end times")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });

    it("blocks a close date before the open date after changing displayed event-zone values", async () => {
        const { user } = renderForm({
            timeZone: "America/Los_Angeles",
            reconfirmPolicy: {
                // Initially valid: May 25 at 22:30 and May 26 at 00:30 in the event zone.
                opensAt: "2025-05-26T05:30:00Z",
                closesAt: "2025-05-26T07:30:00Z",
                minEmailIntervalHours: 1,
                quietHoursStart: null,
                quietHoursEnd: null,
            },
        });

        const openPicker = screen.getByRole("button", { name: /May 25, 2025/ });
        await user.click(openPicker);
        const openTime = document.querySelector<HTMLInputElement>('input[type="time"]');
        expect(openTime).toHaveValue("22:30");
        fireEvent.change(openTime!, { target: { value: "23:30" } });
        await user.keyboard("{Escape}");

        const closePicker = screen.getByRole("button", { name: /May 26, 2025/ });
        await user.click(closePicker);
        const closeDay = screen
            .getAllByRole("gridcell", { name: "25" })
            .find((cell) => !cell.className.includes("day-outside"));
        expect(closeDay).toBeDefined();
        await user.click(closeDay!);

        const closeTime = document.querySelector<HTMLInputElement>('input[type="time"]');
        expect(closeTime).toHaveValue("00:30");
        fireEvent.change(closeTime!, { target: { value: "22:30" } });
        await user.keyboard("{Escape}");

        expect(screen.getByRole("button", { name: /May 25, 2025.*23:30/ })).toBeInTheDocument();
        expect(screen.getByRole("button", { name: /May 25, 2025.*22:30/ })).toBeInTheDocument();
        expect(screen.getAllByText(/America\/Los_Angeles/)).toHaveLength(2);

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        expect(await screen.findByText("Close date must be after open date")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });

    it("blocks a non-positive minimum email interval", async () => {
        const { user } = renderForm();
        const [minEmailInterval] = numberInputs();

        fireEvent.change(minEmailInterval, { target: { value: "0", valueAsNumber: 0 } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        expect(await screen.findByText("Must be at least 1 hour")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });

    it("submits a local 09:00 opening as the UTC instant for the event zone", async () => {
        const { user } = renderForm();
        const openPicker = screen.getByRole("button", { name: /May 01, 2025/ });

        await user.click(openPicker);
        const localTime = document.querySelector<HTMLInputElement>('input[type="time"]');
        expect(localTime).toBeInTheDocument();
        fireEvent.change(localTime!, { target: { value: "09:00" } });
        await user.keyboard("{Escape}");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(
                `/api/teams/${TEAM_ID}/events/${EVENT_ID}/reconfirm-policy`,
                expect.objectContaining({
                    opensAt: "2025-05-01T07:00:00.000Z",
                    expectedVersion: 4,
                }),
            ),
        );
        expect(screen.getAllByText(/Europe\/Amsterdam/)).toHaveLength(2);
    });

    it("makes the policy form read-only when its owner marks the event disabled", () => {
        renderForm({ status: "archived" }, true);

        expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Discard" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Remove policy" })).toBeDisabled();
        numberInputs().forEach((input) => expect(input).toBeDisabled());
        screen.getAllByRole("button", { name: /May/ }).forEach((button) =>
            expect(button).toBeDisabled(),
        );
    });

    it("surfaces a concurrency conflict from saving", async () => {
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
        const [minEmailInterval] = numberInputs();

        fireEvent.change(minEmailInterval, { target: { value: "48", valueAsNumber: 48 } });
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const alert = await screen.findByRole("alert");
        expect(alert).toHaveTextContent("Conflict");
        expect(alert).toHaveTextContent("The resource was modified by another operation.");
    });
});
