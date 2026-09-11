import { screen } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { registrationListItemDto, ticketedEventDetailsDto, ticketTypeDto } from "@/test-utils/builders";
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

    // Given an event whose UTC start crosses into the previous date in its event time zone
    // When the check-in card renders
    // Then it shows the event-local start date and time instead of a relative day count
    it("CheckInCard_EventStartsAtCrossesUtcDateBoundary_ShowsEventLocalDateAndTime", () => {
        const boundaryEvent = ticketedEventDetailsDto({
            startsAt: "2027-06-13T01:30:00.000Z",
            timeZone: "America/Los_Angeles",
        });

        renderWithProviders(<CheckInCard event={boundaryEvent} />);

        expect(screen.getByText("Jun 12, 2027 · 18:30")).toBeInTheDocument();
        expect(screen.queryByText(/days/)).not.toBeInTheDocument();
    });

    // Given an event with expected attendees
    // When the check-in card renders
    // Then it exposes the scanner action and check-in counts without a share link
    it("CheckInCard_WithExpectedAttendees_ShowsScannerAndCounts", () => {
        renderWithProviders(<CheckInCard event={event} summary={{ checkedInCount: 0, expectedCount: 42 }} />);

        expect(screen.getByText("Check-in")).toBeInTheDocument();
        expect(screen.getByText("Event day")).toBeInTheDocument();
        expect(screen.getByText("20:00")).toBeInTheDocument();
        expect(screen.getByRole("button", { name: "Scanner" })).toBeInTheDocument();
        expect(screen.getByText("42")).toBeInTheDocument();
        expect(screen.queryByRole("link")).not.toBeInTheDocument();
        expect(screen.queryByText(/share link/i)).not.toBeInTheDocument();
    });

    // Given an event with no reconfirmed registrations
    // When the hero card renders
    // Then it does not show a Reconfirmed stat
    it("hides the Reconfirmed stat when there are no reconfirmations", () => {
        renderWithProviders(
            <EventHeroCard event={event} openStatus={{ isOpen: true }} ticketTypes={ticketTypes} registrations={[]} />,
        );

        expect(screen.queryByText("Reconfirmed", { selector: "div" })).not.toBeInTheDocument();
    });

    // Given two active reconfirmed registrations
    // When the hero card renders
    // Then it shows their count in the Reconfirmed stat
    it("shows the Reconfirmed stat for active reconfirmed registrations", () => {
        const registrations = [
            registrationListItemDto({ id: "reg-1", hasReconfirmed: true }),
            registrationListItemDto({ id: "reg-2", hasReconfirmed: true }),
            registrationListItemDto({ id: "reg-3", hasReconfirmed: false }),
        ];

        renderWithProviders(
            <EventHeroCard
                event={event}
                openStatus={{ isOpen: true }}
                ticketTypes={ticketTypes}
                registrations={registrations}
            />,
        );

        expect(screen.getByText("Reconfirmed", { selector: "div" })).toBeInTheDocument();
        expect(screen.getByText("2", { selector: "span" })).toBeInTheDocument();
    });

    // Given ticket capacity with no reconfirmed registrations
    // When the check-in card renders
    // Then Expected equals total used capacity
    it("uses total used capacity for Expected without reconfirmations", () => {
        renderWithProviders(<CheckInCard event={event} summary={{ checkedInCount: 0, expectedCount: 42 }} />);

        expect(screen.getByText("Expected")).toBeInTheDocument();
        expect(screen.getByText("42")).toBeInTheDocument();
    });

    // Given the attendance summary reports two expected attendees
    // When the check-in card renders
    // Then Expected uses summary.expectedCount
    it("CheckInCard_SummaryReportsExpectedAttendees_DisplaysSummaryCount", () => {
        renderWithProviders(
            <CheckInCard
                event={event}
                summary={{ checkedInCount: 0, expectedCount: 2 }}
            />,
        );

        expect(screen.getByText("Expected")).toBeInTheDocument();
        expect(screen.getByText("2")).toBeInTheDocument();
        expect(screen.queryByText("1")).not.toBeInTheDocument();
    });

    // Given a cancelled registration that was previously reconfirmed
    // When the hero card renders
    // Then it is not counted as Reconfirmed
    it("does not count cancelled reconfirmed registrations", () => {
        const registrations = [
            registrationListItemDto({ status: "cancelled", hasReconfirmed: true }),
        ];

        renderWithProviders(
            <EventHeroCard event={event} openStatus={{ isOpen: true }} ticketTypes={ticketTypes} registrations={registrations} />,
        );

        expect(screen.queryByText("Reconfirmed", { selector: "div" })).not.toBeInTheDocument();
    });
});
