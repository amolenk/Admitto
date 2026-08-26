import { screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { additionalDetailFieldDto, ticketTypeDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";

import { AddRegistrationSheet } from "./add-registration-sheet";

// `TicketTypeDto` (generated/types.gen.ts) has no cancellation flag (`status`, `cancelledAt`, or
// `isCancelled`). `GetTicketTypesHandler` returns every ticket type verbatim and this component
// renders every supplied item as a checkbox, so the cancelled-type requirement has no
// representable state in the current DTO/component. The catalog tests below document that code
// truth rather than inventing an unsupported cancelled fixture.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const post = vi.mocked(apiClient.post);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const EVENT_ID = "33333333-3333-3333-3333-333333333333";
const ENDPOINT = `/api/teams/${TEAM_ID}/events/${EVENT_ID}/registrations`;

const generalAdmission = ticketTypeDto({ id: "tt-1", name: "General Admission" });
const workshop = ticketTypeDto({ id: "tt-2", name: "Workshop A" });

function renderSheet(opts: {
    ticketTypes?: ReturnType<typeof ticketTypeDto>[];
    additionalDetailSchema?: ReturnType<typeof additionalDetailFieldDto>[];
} = {}) {
    const onOpenChange = vi.fn();
    const onAdded = vi.fn();

    const result = renderWithProviders(
        <AddRegistrationSheet
            open
            onOpenChange={onOpenChange}
            teamId={TEAM_ID}
            eventId={EVENT_ID}
            eventName="DevConf 2026"
            ticketTypes={opts.ticketTypes ?? [generalAdmission]}
            additionalDetailSchema={opts.additionalDetailSchema ?? []}
            onAdded={onAdded}
        />,
    );

    return { ...result, onOpenChange, onAdded };
}

/** The checkbox nested in the label for a given ticket-type name. */
function ticketCheckbox(name: string) {
    const label = screen.getByText(name).closest("label") as HTMLElement;
    return within(label).getByRole("checkbox");
}

async function fillAttendee(
    user: ReturnType<typeof renderSheet>["user"],
    { firstName = "Ada", lastName = "Lovelace", email = "ada@example.com" } = {},
) {
    if (firstName) await user.type(screen.getByLabelText("First name"), firstName);
    if (lastName) await user.type(screen.getByLabelText("Last name"), lastName);
    if (email) await user.type(screen.getByLabelText("Email"), email);
}

/** The payload the form built, once it has submitted. */
async function submittedPayload() {
    await waitFor(() => expect(post).toHaveBeenCalled());
    return post.mock.calls[0]![1] as Record<string, unknown>;
}

describe("AddRegistrationSheet", () => {
    beforeEach(() => {
        post.mockResolvedValue(undefined);
    });

    // Given a valid attendee and a selected ticket type
    // When the admin submits the form
    // Then the registration is posted with the entered details and the sheet closes
    it("adds a registration", async () => {
        const { user, onOpenChange, onAdded } = renderSheet();

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        const payload = {
            email: "ada@example.com",
            firstName: "Ada",
            lastName: "Lovelace",
            ticketTypeIds: ["tt-1"],
            additionalDetails: null,
        };
        expect(await submittedPayload()).toEqual(payload);
        expect(post).toHaveBeenCalledWith(ENDPOINT, payload);
        await waitFor(() => expect(onOpenChange).toHaveBeenCalledWith(false));
        expect(onAdded).toHaveBeenCalled();
    });

    // Given the first name is left blank
    // When the admin submits
    // Then the field error is shown and nothing is posted
    it("blocks submission when the first name is missing", async () => {
        const { user } = renderSheet();

        await fillAttendee(user, { firstName: "" });
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByText("First name is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given the last name is left blank
    // When the admin submits
    // Then the field error is shown and nothing is posted
    it("blocks submission when the last name is missing", async () => {
        const { user } = renderSheet();

        await fillAttendee(user, { lastName: "" });
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByText("Last name is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given the email is left blank
    // When the admin submits
    // Then the field error is shown and nothing is posted
    it("blocks submission when the email is missing", async () => {
        const { user } = renderSheet();

        await fillAttendee(user, { email: "" });
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByText("Email is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given an attendee enters an invalid email address
    // When the admin submits
    // Then the email format error is shown and nothing is posted
    it("blocks submission when the email is invalid", async () => {
        const { user } = renderSheet();

        await fillAttendee(user, { email: "not-an-email" });
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByText("Enter a valid email")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given an attendee has not selected a ticket type
    // When the admin submits
    // Then the ticket selection validation error is shown and nothing is posted
    it("blocks submission when no ticket type is selected", async () => {
        const { user } = renderSheet();

        await fillAttendee(user);
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await screen.findByText("Select at least one ticket type")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
    });

    // Given the backend rejects an already-registered email with its actual conflict response
    // When the form handles the failure
    // Then the conflict is shown in the general error banner and the form remains open
    it("shows the actual duplicate-registration conflict in the general banner", async () => {
        post.mockRejectedValue(
            new FormError({
                status: 409,
                title: "Conflict",
                detail: "Registration already exists.",
            }),
        );
        const { user } = renderSheet();

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        const alert = await screen.findByRole("alert");
        expect(within(alert).getByText("Conflict")).toBeInTheDocument();
        expect(within(alert).getByText("Registration already exists.")).toBeInTheDocument();
        expect(screen.getByLabelText("Email")).toBeInTheDocument();
    });

    // Given the backend rejects the submission because the event is not active
    // When the form handles the failure
    // Then the reason is shown as a general banner rather than a field error
    it("shows a general banner when the event is not active", async () => {
        post.mockRejectedValue(
            new FormError({
                status: 409,
                title: "Registration closed",
                detail: "This event is not open for registration.",
            }),
        );
        const { user } = renderSheet();

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        const alert = await screen.findByRole("alert");
        expect(within(alert).getByText("Registration closed")).toBeInTheDocument();
        expect(within(alert).getByText("This event is not open for registration.")).toBeInTheDocument();
    });

    // Given the event's ticket catalog has more than one type
    // When the sheet renders the ticket-type selector
    // Then every type in the catalog is listed, and only those types
    it("lists every ticket type from the event's catalog", async () => {
        renderSheet({ ticketTypes: [generalAdmission, workshop] });

        expect(screen.getByText("General Admission")).toBeInTheDocument();
        expect(screen.getByText("Workshop A")).toBeInTheDocument();
        expect(screen.getAllByRole("checkbox")).toHaveLength(2);
    });

    // Given two ticket types in the event's catalog
    // When both are selected and the form is submitted
    // Then both catalog ids are sent to the registration endpoint
    it("submits the selected ticket types from the catalog", async () => {
        const { user } = renderSheet({ ticketTypes: [generalAdmission, workshop] });

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.click(ticketCheckbox("Workshop A"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await submittedPayload()).toMatchObject({
            ticketTypeIds: [generalAdmission.id, workshop.id],
        });
    });

    // Given the event has no ticket types configured
    // When the sheet renders
    // Then it says so rather than showing an empty checkbox list
    it("shows a message when the catalog has no ticket types", async () => {
        renderSheet({ ticketTypes: [] });

        expect(
            screen.getByText("No ticket types available for this event."),
        ).toBeInTheDocument();
        expect(screen.queryByRole("checkbox")).not.toBeInTheDocument();
    });

    // Given the event's additional-detail schema declares two fields with different length
    // limits
    // When the sheet renders and the admin fills them in
    // Then each field is rendered with its own label and maxLength, and the values are sent
    // keyed by their schema key
    it("renders and submits schema-driven additional-detail fields", async () => {
        const dietary = additionalDetailFieldDto({ key: "dietary", name: "Dietary requirements", maxLength: 200 });
        const tshirt = additionalDetailFieldDto({ key: "tshirt", name: "T-shirt size", maxLength: 5 });
        const { user } = renderSheet({ additionalDetailSchema: [dietary, tshirt] });

        const dietaryInput = screen.getByLabelText("Dietary requirements");
        const tshirtInput = screen.getByLabelText("T-shirt size");
        expect(dietaryInput).toHaveAttribute("maxLength", "200");
        expect(tshirtInput).toHaveAttribute("maxLength", "5");

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.type(dietaryInput, "Vegetarian");
        await user.type(tshirtInput, "L");
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await submittedPayload()).toMatchObject({
            additionalDetails: { dietary: "Vegetarian", tshirt: "L" },
        });
    });

    // Given the additional-detail schema has fields that are left blank
    // When the form is submitted
    // Then the blank entries are dropped rather than sent as empty strings
    it("omits blank additional-detail values from the payload", async () => {
        const dietary = additionalDetailFieldDto({ key: "dietary", name: "Dietary requirements", maxLength: 200 });
        const { user } = renderSheet({ additionalDetailSchema: [dietary] });

        await fillAttendee(user);
        await user.click(ticketCheckbox("General Admission"));
        await user.click(screen.getByRole("button", { name: "Add registration" }));

        expect(await submittedPayload()).toMatchObject({ additionalDetails: {} });
    });
});
