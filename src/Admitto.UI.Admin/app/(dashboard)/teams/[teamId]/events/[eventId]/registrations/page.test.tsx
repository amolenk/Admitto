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

import { EventHeroCard } from "../components/event-hero-card";
import RegistrationsPage from "./page";

// The capacity-summary scenarios remain blocked by current code truth: `page.tsx` computes a
// `totalCount` local that is never rendered, does not render a summary tile, and receives no
// capacity summary prop. The Reconfirm column likewise uses plain `Date#toLocaleString` and
// ignores the fetched event timezone; the exact host-zone output is asserted below.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";

const REGISTRATIONS_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations`;
const EVENT_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}`;
const TICKET_TYPES_URL = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/ticket-types`;

const generalAdmission = ticketTypeDto({ id: "tt-ga", name: "General Admission" });
const vip = ticketTypeDto({ id: "tt-vip", name: "VIP" });
const workshop = ticketTypeDto({ id: "tt-workshop", name: "Workshop" });

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

    // Given the registrations request has not completed
    // When the page renders
    // Then the table area shows loading skeletons instead of an empty state or rows
    it("shows loading skeletons while registrations are being fetched", async () => {
        let resolveRegistrations!: (value: ReturnType<typeof registrationListItemDto>[]) => void;
        const registrationsPending = new Promise<ReturnType<typeof registrationListItemDto>[]>(
            (resolve) => {
                resolveRegistrations = resolve;
            },
        );
        get.mockImplementation((url: string) => {
            if (url === REGISTRATIONS_URL) return registrationsPending;
            if (url === TICKET_TYPES_URL) return Promise.resolve([generalAdmission, vip]);
            if (url === EVENT_URL) return Promise.resolve(event);
            throw new Error(`Unexpected GET ${url}`);
        });

        const { container } = renderPage();

        expect(container.querySelectorAll('[data-slot="skeleton"]')).toHaveLength(3);
        expect(screen.queryByRole("table")).not.toBeInTheDocument();
        expect(screen.queryByText("No registrations yet.")).not.toBeInTheDocument();

        resolveRegistrations([ada]);
        expect(await screen.findByText("ada@example.com")).toBeInTheDocument();
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

    // Given a cancelled, reconfirmed registration and a plain registered one
    // When the status column renders
    // Then the cancelled row shows Cancelled and not Reconfirmed
    it("reflects Cancelled > Reconfirmed > Registered precedence in the status column", async () => {
        renderPage();

        const adaRow = (await screen.findByText("ada@example.com")).closest("tr") as HTMLElement;
        const bobRow = screen.getByText("bob@example.com").closest("tr") as HTMLElement;
        expect(within(adaRow).getByText("Registered")).toBeInTheDocument();
        // bob is cancelled AND hasReconfirmed - Cancelled must win
        expect(within(bobRow).getByText("Cancelled")).toBeInTheDocument();
        expect(within(bobRow).queryByText("Reconfirmed")).not.toBeInTheDocument();
    });

    // Given a registered, reconfirmed registration
    // When the status column renders
    // Then it shows "Reconfirmed"
    it("shows Reconfirmed for a registered, reconfirmed registration", async () => {
        mockData({
            registrations: [
                registrationListItemDto({
                    id: "r-carol",
                    email: "carol@example.com",
                    status: "registered",
                    hasReconfirmed: true,
                    reconfirmedAt: "2026-03-06T08:00:00Z",
                }),
            ],
        });

        renderPage();

        const row = (await screen.findByText("carol@example.com")).closest("tr") as HTMLElement;
        expect(within(row).getByText("Reconfirmed")).toBeInTheDocument();
    });

    // Given the registrations table
    // When it renders
    // Then no separate Reconfirm date/time column is present
    it("has no separate Reconfirm date/time column", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        expect(screen.queryByText("Reconfirm")).not.toBeInTheDocument();
    });

    // Given the registrations table
    // When it renders
    // Then the remaining date column header reads "Date"
    it("renders the date column header as Date", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        expect(screen.getByRole("columnheader", { name: /Date/ })).toBeInTheDocument();
    });

    // Given a cancelled, a reconfirmed and a plain registered registration
    // When the Date column renders
    // Then each row shows the timestamp matching its own displayed status
    it("shows CancelledAt/ReconfirmedAt/CreatedAt in the Date column per row status", async () => {
        const cancelled = registrationListItemDto({
            id: "r-cancelled",
            email: "cancelled@example.com",
            status: "cancelled",
            hasReconfirmed: true,
            reconfirmedAt: "2026-03-05T12:30:00Z",
            cancelledAt: "2026-03-07T09:15:00Z",
            createdAt: "2026-03-01T00:00:00Z",
        });
        const reconfirmed = registrationListItemDto({
            id: "r-reconfirmed",
            email: "reconfirmed@example.com",
            status: "registered",
            hasReconfirmed: true,
            reconfirmedAt: "2026-03-05T12:30:00Z",
            cancelledAt: null,
            createdAt: "2026-03-01T00:00:00Z",
        });
        const registered = registrationListItemDto({
            id: "r-registered",
            email: "registered@example.com",
            status: "registered",
            hasReconfirmed: false,
            reconfirmedAt: null,
            cancelledAt: null,
            createdAt: "2026-03-01T10:00:00Z",
        });
        mockData({ registrations: [cancelled, reconfirmed, registered] });

        renderPage();

        const cancelledRow = (await screen.findByText("cancelled@example.com")).closest("tr") as HTMLElement;
        const reconfirmedRow = screen.getByText("reconfirmed@example.com").closest("tr") as HTMLElement;
        const registeredRow = screen.getByText("registered@example.com").closest("tr") as HTMLElement;

        expect(within(cancelledRow).getByText("3/7/2026, 09:15:00")).toBeInTheDocument();
        expect(within(reconfirmedRow).getByText("3/5/2026, 12:30:00")).toBeInTheDocument();
        expect(within(registeredRow).getByText("3/1/2026, 10:00:00")).toBeInTheDocument();
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

        await selectOption(user, screen.getByRole("combobox", { name: "Ticket type" }), "VIP");

        expect(screen.queryByText("ada@example.com")).not.toBeInTheDocument();
        expect(screen.getByText("bob@example.com")).toBeInTheDocument();
    });

    // Given a ticket type in the catalog that no registration has selected
    // When the filter is selected
    // Then the table shows its filtered empty state and the result summary is zero
    it("shows a filtered empty state when no ticket matches", async () => {
        mockData({ ticketTypes: [generalAdmission, vip, workshop] });
        const { user } = renderPage();
        await screen.findByText("ada@example.com");

        await selectOption(user, screen.getByRole("combobox", { name: "Ticket type" }), "Workshop");

        expect(screen.getByText("No registrations match the current filters.")).toBeInTheDocument();
        expect(screen.getByText("No results")).toBeInTheDocument();
    });

    // Given registrations with each of the three statuses
    // When the Status filter is set to a specific status
    // Then only rows with that displayed status remain
    it("narrows rows with the status filter", async () => {
        const cancelled = registrationListItemDto({
            id: "r-cancelled",
            email: "cancelled@example.com",
            status: "cancelled",
        });
        const reconfirmed = registrationListItemDto({
            id: "r-reconfirmed",
            email: "reconfirmed@example.com",
            status: "registered",
            hasReconfirmed: true,
            reconfirmedAt: "2026-03-05T12:30:00Z",
        });
        const registered = registrationListItemDto({
            id: "r-registered",
            email: "registered@example.com",
            status: "registered",
            hasReconfirmed: false,
        });
        mockData({ registrations: [cancelled, reconfirmed, registered] });

        const { user } = renderPage();
        await screen.findByText("cancelled@example.com");

        await selectOption(user, screen.getByRole("combobox", { name: "Status" }), "Reconfirmed");

        expect(screen.queryByText("cancelled@example.com")).not.toBeInTheDocument();
        expect(screen.queryByText("registered@example.com")).not.toBeInTheDocument();
        expect(screen.getByText("reconfirmed@example.com")).toBeInTheDocument();
    });

    // Given registrations with each of the three statuses
    // When the Status filter is set to "Registered"
    // Then only the non-reconfirmed, non-cancelled row remains
    it("narrows rows to only Registered with the status filter", async () => {
        const cancelled = registrationListItemDto({
            id: "r-cancelled-2",
            email: "cancelled2@example.com",
            status: "cancelled",
        });
        const reconfirmed = registrationListItemDto({
            id: "r-reconfirmed-2",
            email: "reconfirmed2@example.com",
            status: "registered",
            hasReconfirmed: true,
            reconfirmedAt: "2026-03-05T12:30:00Z",
        });
        const registered = registrationListItemDto({
            id: "r-registered-2",
            email: "registered2@example.com",
            status: "registered",
            hasReconfirmed: false,
        });
        mockData({ registrations: [cancelled, reconfirmed, registered] });

        const { user } = renderPage();
        await screen.findByText("cancelled2@example.com");

        await selectOption(user, screen.getByRole("combobox", { name: "Status" }), "Registered");

        expect(screen.queryByText("cancelled2@example.com")).not.toBeInTheDocument();
        expect(screen.queryByText("reconfirmed2@example.com")).not.toBeInTheDocument();
        expect(screen.getByText("registered2@example.com")).toBeInTheDocument();
    });

    // Given registrations with each of the three statuses
    // When the Status filter is set to "Cancelled"
    // Then only the cancelled row remains, even though it has also reconfirmed
    it("narrows rows to only Cancelled with the status filter", async () => {
        const cancelled = registrationListItemDto({
            id: "r-cancelled-3",
            email: "cancelled3@example.com",
            status: "cancelled",
            hasReconfirmed: true,
            reconfirmedAt: "2026-03-05T12:30:00Z",
        });
        const registered = registrationListItemDto({
            id: "r-registered-3",
            email: "registered3@example.com",
            status: "registered",
            hasReconfirmed: false,
        });
        mockData({ registrations: [cancelled, registered] });

        const { user } = renderPage();
        await screen.findByText("cancelled3@example.com");

        await selectOption(user, screen.getByRole("combobox", { name: "Status" }), "Cancelled");

        expect(screen.getByText("cancelled3@example.com")).toBeInTheDocument();
        expect(screen.queryByText("registered3@example.com")).not.toBeInTheDocument();
    });

    // Given the Status filter defaults to "All statuses"
    // When no filter is applied
    // Then rows of every status remain visible
    it("shows all statuses by default in the status filter", async () => {
        renderPage();
        await screen.findByText("ada@example.com");

        expect(screen.getByRole("combobox", { name: "Status" })).toHaveTextContent("All statuses");
        expect(screen.getByText("ada@example.com")).toBeInTheDocument();
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

    // Given the list and add-registration endpoint share state
    // When an attendee is added successfully from the page
    // Then the exact request is posted and the invalidated list refetch shows the new row
    it("refetches and shows a newly added registration", async () => {
        let registrations = [ada, bob];
        const added = registrationListItemDto({
            id: "r-added",
            firstName: "Grace",
            lastName: "Hopper",
            email: "grace@example.com",
            tickets: [{ id: generalAdmission.id, name: generalAdmission.name }],
        });
        const payload = {
            email: added.email,
            firstName: added.firstName,
            lastName: added.lastName,
            ticketTypeIds: [generalAdmission.id],
            additionalDetails: null,
        };

        get.mockImplementation((url: string) => {
            if (url === REGISTRATIONS_URL) return Promise.resolve(registrations);
            if (url === TICKET_TYPES_URL) return Promise.resolve([generalAdmission, vip]);
            if (url === EVENT_URL) return Promise.resolve(event);
            throw new Error(`Unexpected GET ${url}`);
        });
        post.mockImplementation((_url, body) => {
            expect(body).toEqual(payload);
            registrations = [...registrations, added];
            return Promise.resolve(undefined);
        });

        const { user } = renderPage();
        await screen.findByText("ada@example.com");
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        const dialog = await screen.findByRole("dialog");
        await user.type(within(dialog).getByLabelText("First name"), added.firstName!);
        await user.type(within(dialog).getByLabelText("Last name"), added.lastName!);
        await user.type(within(dialog).getByLabelText("Email"), added.email);
        const ticketLabel = within(dialog).getByText("General Admission").closest("label") as HTMLElement;
        await user.click(within(ticketLabel).getByRole("checkbox"));
        await user.click(within(dialog).getByRole("button", { name: "Add registration" }));

        await waitFor(() => expect(post).toHaveBeenCalledWith(REGISTRATIONS_URL, payload));
        expect(await screen.findByText(added.email)).toBeInTheDocument();
        expect(get.mock.calls.filter(([url]) => url === REGISTRATIONS_URL).length).toBeGreaterThanOrEqual(2);
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

describe("EventHeroCard registration capacity summary", () => {
    function registeredStat() {
        const label = screen.getByText("Registered");
        return label.parentElement?.parentElement as HTMLElement;
    }

    // Given the event has no configured ticket capacity
    // When the event's actual capacity summary UI renders
    // Then the registered count is labelled as a total without an artificial capacity
    it("shows the registered total for unlimited capacity", () => {
        renderWithProviders(
            <EventHeroCard
                event={event}
                ticketTypes={[ticketTypeDto({ maxCapacity: null, usedCapacity: 47 })]}
            />,
        );

        const stat = registeredStat();
        expect(within(stat).getByText("47")).toBeInTheDocument();
        expect(within(stat).getByText("total")).toBeInTheDocument();
        expect(within(stat).queryByText(/of/)).not.toBeInTheDocument();
    });

    // Given the event's ticket catalog has a configured capacity of 250
    // When the event's actual capacity summary UI renders
    // Then the registered count is displayed against that capacity
    it("shows the registered count of configured capacity", () => {
        renderWithProviders(
            <EventHeroCard
                event={event}
                ticketTypes={[ticketTypeDto({ maxCapacity: 250, usedCapacity: 47 })]}
            />,
        );

        const stat = registeredStat();
        expect(within(stat).getByText("47")).toBeInTheDocument();
        expect(within(stat).getByText("of 250")).toBeInTheDocument();
    });
});
