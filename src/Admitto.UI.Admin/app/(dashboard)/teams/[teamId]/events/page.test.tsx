import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { ticketedEventDetailsDto, ticketedEventListItemDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock, setRoute } from "@/test-utils/router";

import Home from "../../../page";
import EditDangerPage from "./[eventId]/edit/danger/page";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock("@/stores/team-store", () => ({
    useTeamStore: (selector: (state: { selectedTeamId: string }) => unknown) =>
        selector({ selectedTeamId: "team-1" }),
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);
const EVENTS_URL = "/api/teams/team-1/events";
const EVENT_ARCHIVE_URL = "/api/teams/team-1/events/active/archive";

describe("dashboard event selection", () => {
    beforeEach(() => {
        setRoute({ pathname: "/" });
    });

    // Given the events response contains archived and active events
    // When the dashboard selects an event
    // Then archived events are filtered out before navigation
    it("skips archived events when selecting the first event", async () => {
        const archived = ticketedEventListItemDto({ id: "archived", status: "archived" });
        const active = ticketedEventListItemDto({ id: "active", status: "active" });
        get.mockResolvedValue([archived, active]);

        renderWithProviders(<Home />);

        await waitFor(() =>
            expect(routerMock.replace).toHaveBeenCalledWith("/teams/team-1/events/active"),
        );
        expect(get).toHaveBeenCalledWith(EVENTS_URL);
    });

    // Given the dashboard and the event danger page share the same query cache
    // When the event is archived through the danger page
    // Then the dashboard's event collection is refetched and no longer contains it
    it("refetches and removes an event after archiving it through the danger page", async () => {
        const active = ticketedEventListItemDto({ id: "active", status: "active" });
        const details = ticketedEventDetailsDto({
            id: "active",
            teamId: "team-1",
            name: "Admitto Summit",
            version: 4,
        });
        const eventResponses = [[active], []];
        post.mockResolvedValue(undefined);
        get.mockImplementation(async (url) => {
            if (url === EVENTS_URL) return eventResponses.shift() ?? [];
            if (url === "/api/teams/team-1/events/active") return details;
            throw new Error(`Unexpected GET ${url}`);
        });

        setRoute({
            params: { teamId: "team-1", eventId: "active" },
            pathname: "/teams/team-1/events/active/edit/danger",
        });

        const { queryClient, user } = renderWithProviders(
            <>
                <Home />
                <EditDangerPage />
            </>,
        );
        await waitFor(() => expect(get).toHaveBeenCalledWith(EVENTS_URL));

        await user.click(await screen.findByRole("button", { name: "Archive" }));
        await user.type(screen.getByPlaceholderText(details.name), details.name);
        await user.click(
            screen.getByRole("button", { name: "I understand, archive this event" }),
        );

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(EVENT_ARCHIVE_URL, { expectedVersion: 4 }),
        );
        await waitFor(() =>
            expect(get.mock.calls.filter(([url]) => url === EVENTS_URL)).toHaveLength(2),
        );
        await waitFor(() =>
            expect(queryClient.getQueryData(["events", "team-1"])).toEqual([]),
        );
        expect(routerMock.push).toHaveBeenCalledWith("/teams/team-1/settings");
    });
});
