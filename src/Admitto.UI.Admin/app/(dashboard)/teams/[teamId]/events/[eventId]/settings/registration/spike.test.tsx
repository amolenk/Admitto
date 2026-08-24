import { fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { addDays, addYears } from "date-fns";

import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";

import { RegistrationPolicyForm } from "./registration-policy-form";
import { TicketedEventDetails } from "../event-detail-types";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);

const event: TicketedEventDetails = {
    id: "e1",
    teamId: "t1",
    slug: "evt",
    name: "Test Event",
    startsAt: addYears(new Date(), 1).toISOString(),
    endsAt: addYears(new Date(), 1).toISOString(),
    timeZone: "UTC",
    status: "active",
    version: 3,
    isRegistrationOpen: false,
    registrationPolicy: null,
    reconfirmPolicy: null,
    waitlistPolicy: { quietHoursStart: "22:00:00", quietHoursEnd: "07:00:00" },
    additionalDetailSchema: [],
};

async function pickDateTime(user: any, trigger: HTMLElement, targetDate: Date, time: string) {
    await user.click(trigger);
    const dayText = String(targetDate.getDate());
    const cells = await screen.findAllByRole("gridcell", { name: dayText });
    const day = cells.find((c) => !c.className.includes("day-outside"))!;
    await user.click(day);
    const timeInput = document.querySelector<HTMLInputElement>('input[type="time"]')!;
    fireEvent.change(timeInput, { target: { value: time } });
    await user.keyboard("{Escape}");
}

describe("spike", () => {
    it("configures the registration window", async () => {
        put.mockResolvedValue(undefined);
        const { user } = renderWithProviders(
            <RegistrationPolicyForm event={event} teamId="t1" eventId="e1" disabled={false} />,
        );

        const buttons = screen.getAllByRole("button", { name: /pick a date/i });
        expect(buttons.length).toBe(2);

        const today = new Date();
        let closeDate = addDays(today, 1);
        if (closeDate.getMonth() !== today.getMonth()) {
            // handle rollover in test later
        }

        await pickDateTime(user, buttons[0], today, "09:15");
        await pickDateTime(user, buttons[1], closeDate, "17:45");

        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() => expect(put).toHaveBeenCalled());
        console.log(JSON.stringify(put.mock.calls[0]));
    });
});
