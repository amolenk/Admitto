import { screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { ticketedEventDetailsDto, ticketTypeDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";

import { CheckInCard } from "./check-in-card";
import { EventHeroCard } from "./event-hero-card";

const event = ticketedEventDetailsDto({
    name: "Acme Summit",
    startsAt: "2027-06-12T18:00:00.000Z",
    endsAt: "2027-06-13T18:00:00.000Z",
    timeZone: "Europe/Amsterdam",
    registrationPolicy: {
        opensAt: "2027-06-01T08:00:00.000Z",
        closesAt: "2027-06-12T16:00:00.000Z",
        allowedEmailDomain: null,
    },
});

const ticketTypes = [ticketTypeDto({ id: "tt-1", name: "General Admission", usedCapacity: 42 })];

describe("event dashboard cards", () => {
    beforeEach(() => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date("2027-06-01T10:00:00.000Z"));
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    // Given an event with metadata and a public website
    // When the hero card renders
    // Then its metadata is visible without exposing action buttons
    it("shows hero metadata without action buttons", () => {
        renderWithProviders(<EventHeroCard event={event} openStatus={{ isOpen: true }} ticketTypes={ticketTypes} />);

        expect(screen.getByText("Acme Summit")).toBeInTheDocument();
        expect(screen.getByText(/June 12, 2027/)).toBeInTheDocument();
        expect(screen.getByText(/Europe\/Amsterdam \(UTC\+02:00\)/)).toBeInTheDocument();
        expect(screen.getByRole("link", { name: "example.com" })).toHaveAttribute(
            "href",
            "https://example.com",
        );
        expect(screen.getByText("Registration open")).toBeInTheDocument();
        expect(screen.getByText("12 days to go")).toBeInTheDocument();
        expect(screen.queryByRole("button")).not.toBeInTheDocument();
    });

    // Given an event with expected attendees
    // When the check-in card renders
    // Then it exposes the scanner action and check-in metadata without a share link
    it("shows the scanner without a share link", () => {
        renderWithProviders(<CheckInCard event={event} ticketTypes={ticketTypes} />);

        expect(screen.getByText("Check-in")).toBeInTheDocument();
        expect(screen.getByText("Event day")).toBeInTheDocument();
        expect(screen.getByText("20:00")).toBeInTheDocument();
        expect(screen.getByText("12 days")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Scanner" })).toBeInTheDocument();
        expect(screen.getByText("42")).toBeInTheDocument();
        expect(screen.queryByRole("link")).not.toBeInTheDocument();
        expect(screen.queryByText(/share link/i)).not.toBeInTheDocument();
    });
});
