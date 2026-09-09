import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { ticketedEventDetailsDto, ticketedEventListItemDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock, setRoute } from "@/test-utils/router";

import { NavEvents } from "./nav-events";
import { NavEventPages } from "./sidebar/nav-event-pages";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";

describe("NavEvents", () => {
    beforeEach(() => {
        get.mockReset();
        routerMock.push.mockReset();
        setRoute({ params: { teamId: TEAM_ID }, pathname: `/teams/${TEAM_ID}` });
    });

    it("hides New event when the selected team member cannot create events", async () => {
        get.mockResolvedValue([ticketedEventListItemDto({ id: EVENT_ID })]);

        renderWithProviders(<NavEvents teamId={TEAM_ID} canCreateEvents={false} />);

        await waitFor(() => expect(get).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/events`));
        expect(screen.queryByRole("button", { name: "New event" })).not.toBeInTheDocument();
    });

    it("routes the New event option to the create page for an owner", async () => {
        get.mockResolvedValue([]);

        const { user } = renderWithProviders(<NavEvents teamId={TEAM_ID} canCreateEvents />);

        await user.click(await screen.findByRole("button", { name: "New event" }));

        expect(routerMock.push).toHaveBeenCalledWith(`/teams/${TEAM_ID}/events/new`);
    });
});

describe("NavEventPages", () => {
    beforeEach(() => {
        get.mockReset();
        routerMock.push.mockReset();
        get.mockResolvedValue(ticketedEventDetailsDto({ id: EVENT_ID, teamId: TEAM_ID }));
    });

    function renderPages(pathname: string) {
        setRoute({
            params: { teamId: TEAM_ID, eventId: EVENT_ID },
            pathname,
        });
        return renderWithProviders(<NavEventPages teamId={TEAM_ID} />);
    }

    it("does not expose campaign create, list, detail, or cancel entry points", () => {
        renderPages(`/teams/${TEAM_ID}/events/${EVENT_ID}`);

        expect(screen.queryByRole("button", { name: "Emails" })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Campaigns" })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: /create campaign/i })).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: /campaign detail/i })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: /send bulk email/i })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: /cancel bulk email/i })).not.toBeInTheDocument();
    });

    it("targets the General edit page from the Edit event sidebar entry", async () => {
        const { user } = renderPages(`/teams/${TEAM_ID}/events/${EVENT_ID}`);

        await user.click(screen.getByRole("button", { name: "Edit event" }));

        expect(routerMock.push).toHaveBeenCalledWith(
            `/teams/${TEAM_ID}/events/${EVENT_ID}/edit/general`,
        );
    });

    it("does not expose email navigation on the event edit page", () => {
        renderPages(`/teams/${TEAM_ID}/events/${EVENT_ID}/edit/policies`);

        expect(screen.queryByRole("button", { name: "Emails" })).not.toBeInTheDocument();
    });
});
