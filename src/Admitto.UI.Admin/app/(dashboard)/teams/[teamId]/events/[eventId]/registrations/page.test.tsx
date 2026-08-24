import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import {
    registrationListItemDto,
    ticketedEventDetailsDto,
    ticketTypeDto,
} from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import RegistrationsPage from "./page";

// Harvested from openspec `admin-ui-registrations`. Two scenarios from the spec ("Summary
// shows total registrations only when the ticket type has no capacity" / "shows X of
// capacity") have no surface in the code: `page.tsx` computes a `totalCount` local that is
// never rendered, and there is no capacity-aware summary anywhere on the page. Not tested —
// there is nothing to assert. Also note the Reconfirm column formats `reconfirmedAt` with
// plain `Date#toLocaleString`, not the event-timezone helpers in `lib/time-zones.ts` — tested
// here only for presence/absence, not for a specific timezone conversion.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";

const REGISTRATIONS_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations`;
const EVENT_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}`;
const TICKET_TYPES_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types`;

const generalAdmission = ticketTypeDto({ id: "tt-ga", name: "General Admission" });
const vip = ticketTypeDto({ id: "tt-vip", name: "VIP" });

const ada = registrationListItemDto({
    id: "r-ada",
    firstName: "Ada",
    lastName: "Anderson",
    email: "ada@example.com",
    tickets: [{ id: generalAdmission.id, name: generalAdmission.name }],
    status: "registered",
    hasReconfirmed: false,
    reconfirmedAt: null,
    createdAt: "2026-03-01T10:00:00Z",
});

const bob = registrationListItemDto({
    id: "r-bob",
    firstName: "Bob",
    lastName: "Zephyr",
    email: "bob@example.com",
    tickets: [
        { id: vip.id, name: vip.name },
        { id: generalAdmission.id, name: generalAdmission.name },
    ],
    status: "cancelled",
    hasReconfirmed: true,
    reconfirmedAt: "2026-03-05T12:30:00Z",
    createdAt: "2026-03-02T09:00:00Z",
});

const event = ticketedEventDetailsDto({ id: EVENT_ID, teamId: TEAM_ID, name: "DevConf 2026" });

function mockData(overrides: {
    registrations?: ReturnType<typeof registrationListItemDto>[];
    ticketTypes?: ReturnType<typeof ticketTypeDto>[];
} = {}) {
    const registrations = overrides.registrations ?? [ada, bob];
    const ticketTypes = overrides.ticketTypes ?? [generalAdmission, vip];

    get.mockImplementation((url: string) => {
        if (url === REGISTRATIONS_URL) return Promise.resolve(registrations);
        if (url === TICKET_TYPES_URL) return Promise.resolve(ticketTypes);
        if (url === EVENT_URL) return Promise.resolve(event);
        throw new Error(`Unexpected GET ${url}`);
    });
}

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID, eventId: EVENT_ID } });
    return renderWithProviders(<RegistrationsPage />);
}

/** The data rows in the registrations table, excluding the header row. */
function dataRows() {
    return screen.getAllByRole("row").slice(1);
}

/** Picks an option from a Radix Select trigger. */
async function selectOption(
    user: ReturnType<typeof renderPage>["user"],
    trigger: HTMLElement,
    optionName: string,
) {
    await user.click(trigger);
    await user.click(await screen.findByRole("option", { name: optionName }));
}

describe("RegistrationsPage", () => {
    beforeEach(() => {
        mockData();
    });

    // Given an event with registrations
    // When the page loads
    // Then the registrations are fetched and shown
    it("loads and shows registrations", async () => {
        renderPage();

        expect(await screen.findByText("ada@example.com")).toBeInTheDocument();
        expect(screen.getByText("bob@example.com")).toBeInTheDocument();
        expect(get).toHaveBeenCalledWith(REGISTRATIONS_URL);
    });

    // Given an event with no registrations
    // When the page loads
    // Then an empty-state message is shown instead of a table
    it("shows an empty-state row for an event with no registrations", async () => {
        mockData({ registrations: [] });

        renderPage();

        expect(await screen.findByText("No registrations yet.")).toBeInTheDocument();
    });

    // Given a registration with a first and last name
    // When the attendee column renders
    // Then the full name is shown
    it("shows the attendee's first and last name in the attendee column", async () => {
        renderPage();

        expect(await screen.findByText("Ada Anderson")).toBeInTheDocument();
        expect(screen.getByText("Bob Zephyr")).toBeInTheDocument();
    });

    // Given a registration with two tickets
    // When the ticket column renders
    // Then one badge is shown per ticket
    it("shows one badge per ticket in the ticket column", async () => {
        renderPage();

        const bobRow = (await screen.findByText("bob@example.com")).closest("tr") as HTMLElement;
        expect(within(bobRow).getByText("VIP")).toBeInTheDocument();
        expect(within(bobRow).getByText("General Admission")).toBeInTheDocument();
    });

    // Given registrations with different statuses
    // When the status column renders
    // Then each row reflects its own registration status
    it("reflects the registration status in the status column", async () => {
        renderPage();

        const adaRow = (await screen.findByText("ada@example.com")).closest("tr") as HTMLElement;
        const bobRow = screen.getByText("bob@example.com").closest("tr") as HTMLElement;
        expect(within(adaRow).getByText("Registered")).toBeInTheDocument();
        expect(within(bobRow).getByText("Cancelled")).toBeInTheDocument();
    });

    // Given one attendee who has not reconfirmed and one who has
    // When the reconfirm column renders
    // Then the unreconfirmed row shows a placeholder and the reconfirmed row shows a formatted
    // timestamp
    it("reflects HasReconfirmed in the reconfirm column", async () => {
        renderPage();

        const adaRow = (await screen.findByText("ada@example.com")).closest("tr") as HTMLElement;
        const bobRow = screen.getByText("bob@example.com").closest("tr") as HTMLElement;
        expect(within(adaRow).getByText("—")).toBeInTheDocument();
        expect(within(bobRow).queryByText("—")).not.toBeInTheDocument();
    });

    // Given attendees with different names and emails
    // When the search box is used
    // Then rows are filtered across both name and email
    it("filters rows by search across name and email", async () => {
        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await user.type(screen.getByPlaceholderText("Search email…"), "zephyr");

        expect(screen.queryByText("ada@example.com")).not.toBeInTheDocument();
        expect(screen.getByText("bob@example.com")).toBeInTheDocument();
    });

    // Given attendees with different ticket types
    // When the ticket-type filter is set
    // Then only rows with a matching ticket remain
    it("narrows rows with the ticket-type filter", async () => {
        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await selectOption(user, screen.getByRole("combobox"), "VIP");

        expect(screen.queryByText("ada@example.com")).not.toBeInTheDocument();
        expect(screen.getByText("bob@example.com")).toBeInTheDocument();
    });

    // Given attendees whose surnames sort differently than insertion order
    // When the page loads with no sort applied yet
    // Then rows are ordered by attendee name ascending by default
    it("defaults to attendee name ascending sort", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        const rows = dataRows();
        expect(within(rows[0]!).getByText("Ada Anderson")).toBeInTheDocument();
        expect(within(rows[1]!).getByText("Bob Zephyr")).toBeInTheDocument();
    });

    // Given the default ascending sort on the attendee column
    // When the column header is clicked
    // Then the sort direction toggles
    it("toggles sort direction when a column header is clicked", async () => {
        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await user.click(screen.getByRole("button", { name: /Attendee/ }));

        const rows = dataRows();
        expect(within(rows[0]!).getByText("Bob Zephyr")).toBeInTheDocument();
        expect(within(rows[1]!).getByText("Ada Anderson")).toBeInTheDocument();
    });

    // Given the "Add registration" control
    // When it is clicked
    // Then the add-registration sheet opens
    it("opens the add-registration sheet from the Add registration control", async () => {
        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByRole("heading", { name: "Add registration" })).toBeInTheDocument();
        expect(screen.getByLabelText("First name")).toBeInTheDocument();
    });

    // Given the registrations table
    // When it renders
    // Then there is no multi-select checkbox column
    it("has no multi-select checkbox column", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
    });

    // Given the registrations page
    // When it renders
    // Then no status tabs are shown above the table
    it("has no status tabs above the table", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        expect(screen.queryByRole("tab")).not.toBeInTheDocument();
        expect(screen.queryByRole("tablist")).not.toBeInTheDocument();
    });

    // Given a registration row
    // When the attendee cell is inspected
    // Then it links to that attendee's detail page
    it("links a row to the attendee detail page", async () => {
        renderPage();

        const link = (await screen.findByText("ada@example.com")).closest("a");
        expect(link).toHaveAttribute(
            "href",
            `/teams/${TEAM_ID}/events/${EVENT_ID}/registrations/${ada.id}`,
        );
    });

    // Given the Export CSV control
    // When it is clicked
    // Then the browser is navigated to the export proxy route, which streams the download
    it("navigates to the export proxy route when Export CSV is clicked", async () => {
        const location = { href: "http://localhost:3000/teams" };
        Object.defineProperty(window, "location", {
            configurable: true,
            writable: true,
            value: location,
        });

        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await user.click(screen.getByRole("button", { name: "Export CSV" }));

        expect(location.href).toBe(`/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations/export`);
    });
});

describe("RegistrationsPage pagination", () => {
    const many = Array.from({ length: 30 }, (_, i) => {
        const n = (i + 1).toString().padStart(2, "0");
        return registrationListItemDto({
            id: `r-${n}`,
            firstName: "Attendee",
            lastName: n,
            email: `attendee${n}@example.com`,
            tickets: [{ id: generalAdmission.id, name: generalAdmission.name }],
        });
    });

    beforeEach(() => {
        mockData({ registrations: many });
    });

    // Given more registrations than one page holds
    // When the page loads
    // Then only the first 25 rows are shown
    it("shows 25 rows per page", async () => {
        renderPage();
        await screen.findByText("attendee01@example.com");

        expect(dataRows()).toHaveLength(25);
        expect(screen.getByText("Showing 1–25 of 30")).toBeInTheDocument();
    });

    // Given more registrations than one page holds
    // When the Next control is used
    // Then the window advances to the remaining rows
    it("advances the window with the next-page control", async () => {
        const { user } = renderPage();
        await screen.findByText("attendee01@example.com");

        await user.click(screen.getByRole("button", { name: /Next/ }));

        expect(await screen.findByText("Showing 26–30 of 30")).toBeInTheDocument();
        expect(dataRows()).toHaveLength(5);
        expect(screen.getByText("Attendee 26")).toBeInTheDocument();
    });
});
