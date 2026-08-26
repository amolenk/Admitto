import { fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

import { wallClockToUtcIso } from "@/lib/time-zones";
import { renderWithProviders } from "@/test-utils/render";

import { ZonedDateTimePicker } from "./zoned-date-time-picker";

describe("ZonedDateTimePicker", () => {
    // Given a UTC instant and an event time zone
    // When the picker renders
    // Then it shows the event-zone wall clock value and an explicit zone caption
    it("reads UTC and displays local event time with its caption", () => {
        renderWithProviders(
            <ZonedDateTimePicker
                value="2027-06-01T16:00:00.000Z"
                timeZone="Europe/Amsterdam"
                onChange={vi.fn()}
            />,
        );

        expect(screen.getByRole("button")).toHaveTextContent("Jun 01, 2027 · 18:00");
        expect(screen.getByText(/Europe\/Amsterdam \(UTC[+-]\d{2}:\d{2}\)/)).toBeInTheDocument();
    });

    // Given a picker with a selected date
    // When the local wall-clock time changes
    // Then the component writes the corresponding UTC instant
    it("writes wall-clock edits in the selected event zone", async () => {
        const onChange = vi.fn();
        const { user } = renderWithProviders(
            <ZonedDateTimePicker
                value="2027-06-01T16:00:00.000Z"
                timeZone="Europe/Amsterdam"
                onChange={onChange}
            />,
        );

        await user.click(screen.getByRole("button"));
        const timeInput = document.querySelector<HTMLInputElement>('input[type="time"]');
        expect(timeInput).not.toBeNull();
        fireEvent.change(timeInput!, { target: { value: "20:30" } });

        expect(onChange).toHaveBeenCalledWith(
            wallClockToUtcIso("2027-06-01T20:30", "Europe/Amsterdam"),
        );
    });
});
