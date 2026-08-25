import { screen, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { ticketedEventDetailsDto, ticketTypeDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import TicketTypesPage from "./page";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "22222222-2222-2222-2222-222222222222";
const EVENT_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}`;
const TICKET_TYPES_URL = `${EVENT_URL}/ticket-types`;

const event = ticketedEventDetailsDto({
    id: EVENT_ID,
    teamId: TEAM_ID,
    name: "DevConf 2026",
    publicSlug: "devconf-2026",
});

const workshop = ticketTypeDto({
    id: "33333333-3333-3333-3333-333333333333",
    name: "Workshop Pass",
    timeSlots: ["morning", "afternoon"],
    maxCapacity: 100,
    usedCapacity: 25,
    selfServiceEnabled: true,
});

const unlimited = ticketTypeDto({
    id: "44444444-4444-4444-4444-444444444444",
    name: "Staff Pass",
    timeSlots: [],
    maxCapacity: null,
    usedCapacity: 7,
    selfServiceEnabled: false,
});

function mockData(ticketTypes = [workshop, unlimited]) {
    get.mockImplementation((url: string) => {
        if (url === EVENT_URL) return Promise.resolve(event);
        if (url === TICKET_TYPES_URL) return Promise.resolve(ticketTypes);
        throw new Error(`Unexpected GET ${url}`);
    });
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID } });
    return renderWithProviders(<TicketTypesPage />);
}

function cardFor(name: string): HTMLElement {
    return screen.getByRole("heading", { name }).closest('[data-slot="card"]') as HTMLElement;
}

describe("TicketTypesPage", () => {
    beforeEach(() => mockData());

    it("shows the event header and ticket card summaries", async () => {
        renderPage();

        expect(await screen.findByRole("heading", { name: "DevConf 2026" })).toBeInTheDocument();

        const workshopCard = cardFor("Workshop Pass");
        expect(within(workshopCard).getByText("Available")).toBeInTheDocument();
        expect(within(workshopCard).getByText("Registered")).toBeInTheDocument();
        expect(within(workshopCard).getByText("25")).toBeInTheDocument();
        expect(within(workshopCard).getByText("morning")).toBeInTheDocument();
        expect(within(workshopCard).getByText("afternoon")).toBeInTheDocument();
        expect(within(workshopCard).queryByText("devconf-2026")).not.toBeInTheDocument();

        const staffCard = cardFor("Staff Pass");
        expect(within(staffCard).getByText("Available")).toBeInTheDocument();
        expect(within(staffCard).getByText("Registered")).toBeInTheDocument();
        expect(within(staffCard).getByText("7")).toBeInTheDocument();
        expect(within(staffCard).queryByText("morning")).not.toBeInTheDocument();
        expect(within(staffCard).queryByText("afternoon")).not.toBeInTheDocument();
        expect(within(staffCard).queryByText("devconf-2026")).not.toBeInTheDocument();

        expect(screen.queryByRole("contentinfo")).not.toBeInTheDocument();
    });
});
