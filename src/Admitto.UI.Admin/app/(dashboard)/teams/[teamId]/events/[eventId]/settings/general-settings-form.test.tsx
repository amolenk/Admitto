import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { ticketedEventDetailsDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import { GeneralSettingsForm } from "./general-settings-form";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);
const TEAM_ID = "44444444-4444-4444-4444-444444444444";
const EVENT_ID = "55555555-5555-5555-5555-555555555555";
const ENDPOINT = `/api/teams/${TEAM_ID}/events/${EVENT_ID}`;

const event = ticketedEventDetailsDto({
    id: EVENT_ID,
    teamId: TEAM_ID,
    name: "Acme Summit",
    publicSlug: "acme-summit",
    timeZone: "UTC",
});

function renderForm(overrides: Parameters<typeof ticketedEventDetailsDto>[0] = {}) {
    const nextEvent = ticketedEventDetailsDto({ ...event, ...overrides });
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID } });
    return renderWithProviders(<GeneralSettingsForm event={nextEvent} />);
}

describe("GeneralSettingsForm", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    it("does not show waitlist quiet-hours controls", () => {
        const event = ticketedEventDetailsDto({
            id: "55555555-5555-5555-5555-555555555555",
            teamId: "44444444-4444-4444-4444-444444444444",
            waitlistPolicy: { quietHoursStart: "22:00", quietHoursEnd: "08:00" },
        });
        setRoute({ params: { teamId: event.teamId, eventId: event.id } });

        renderWithProviders(<GeneralSettingsForm event={event} />);

        expect(screen.queryByText(/quiet hours/i)).not.toBeInTheDocument();
        expect(screen.queryByText(/waitlist notifications/i)).not.toBeInTheDocument();
    });

    // Given an existing event
    // When the organizer changes its name
    // Then the general-event endpoint receives the complete exact update payload
    it("updates the event name", async () => {
        const { user } = renderForm();

        await user.clear(screen.getByPlaceholderText("e.g. Azure Fest"));
        await user.type(screen.getByPlaceholderText("e.g. Azure Fest"), "New Summit");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(ENDPOINT, {
                expectedVersion: event.version,
                name: "New Summit",
                publicSlug: event.publicSlug,
                websiteUrl: event.websiteUrl,
                baseUrl: event.baseUrl,
                timeZone: event.timeZone,
                startsAt: event.startsAt,
                endsAt: event.endsAt,
            }),
        );
    });

    // Given an event with a public slug
    // When the general tab renders
    // Then the current slug is shown in its editable field
    it("shows the current public slug", () => {
        renderForm();

        expect(screen.getByDisplayValue("acme-summit")).toBeInTheDocument();
    });

    // Given the general update is rejected by optimistic concurrency
    // When the organizer saves a changed name
    // Then the API conflict title and detail are shown in the form alert
    it("surfaces a concurrency conflict", async () => {
        const concurrencyConflict = {
            status: 409,
            title: "Conflict",
            detail: "The resource was modified by another operation.",
            errors: {},
            code: "concurrency_conflict",
        };
        put.mockRejectedValue(new FormError(concurrencyConflict));
        const { user } = renderForm();

        await user.clear(screen.getByPlaceholderText("e.g. Azure Fest"));
        await user.type(screen.getByPlaceholderText("e.g. Azure Fest"), "New Summit");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        const alert = await screen.findByRole("alert");
        expect(alert).toHaveTextContent("Conflict");
        expect(alert).toHaveTextContent("The resource was modified by another operation.");
    });

    // Given an event with no time zone
    // When the organizer attempts to save another general change
    // Then the required time-zone validation is shown and no request is made
    it("requires a time zone", async () => {
        const { user } = renderForm({ timeZone: "" });

        await user.clear(screen.getByPlaceholderText("e.g. Azure Fest"));
        await user.type(screen.getByPlaceholderText("e.g. Azure Fest"), "New Summit");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        expect(await screen.findByText("Time zone is required")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });

    // Given an event with a valid time zone
    // When the organizer selects a different IANA zone
    // Then the selected zone is persisted with the event update
    it("edits the time zone", async () => {
        const { user, queryClient } = renderForm();
        const invalidateQueries = vi.spyOn(queryClient, "invalidateQueries");

        await user.click(screen.getByRole("combobox"));
        await user.click(await screen.findByText("Europe/Amsterdam", { selector: "[cmdk-item]" }));
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(ENDPOINT, {
                expectedVersion: event.version,
                name: event.name,
                publicSlug: event.publicSlug,
                websiteUrl: event.websiteUrl,
                baseUrl: event.baseUrl,
                timeZone: "Europe/Amsterdam",
                startsAt: event.startsAt,
                endsAt: event.endsAt,
            }),
        );
        expect(invalidateQueries).toHaveBeenCalledWith({
            queryKey: ["event", TEAM_ID, EVENT_ID],
        });
        expect(invalidateQueries).toHaveBeenCalledWith({
            queryKey: ["events", TEAM_ID],
        });
    });

    // Given an event with a UTC time zone
    // When the general tab renders
    // Then the selector and both date-time pickers display the active zone caption
    it("shows the zone caption on all general date-time controls", () => {
        renderForm();

        expect(screen.getAllByText("UTC (UTC)")).toHaveLength(3);
    });

    // Given an event containing an unknown IANA zone
    // When another general field is saved
    // Then the inline time-zone validation rejects the unknown value
    it("rejects an unknown IANA time zone", async () => {
        const { user } = renderForm({ timeZone: "Mars/Olympus" });

        await user.clear(screen.getByPlaceholderText("e.g. Azure Fest"));
        await user.type(screen.getByPlaceholderText("e.g. Azure Fest"), "New Summit");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        expect(await screen.findByText("Unknown IANA time zone")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();
    });
});
