import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { renderWithProviders } from "@/test-utils/render";

import type { AdditionalDetailField, TicketedEventDetails } from "../event-detail-types";
import { AdditionalDetailsEditor } from "./additional-details-editor";

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);
const ENDPOINT = "/api/teams/t1/events/e1/additional-detail-schema";

const existingField: AdditionalDetailField = {
    key: "dietary",
    name: "Dietary requirements",
    maxLength: 200,
};

const event: TicketedEventDetails = {
    id: "e1",
    teamId: "t1",
    slug: "evt",
    name: "Test Event",
    startsAt: "2027-06-12T18:00:00.000Z",
    endsAt: "2027-06-13T18:00:00.000Z",
    timeZone: "UTC",
    status: "active",
    version: 7,
    isRegistrationOpen: false,
    registrationPolicy: null,
    reconfirmPolicy: null,
    waitlistPolicy: { quietHoursStart: "22:00:00", quietHoursEnd: "07:00:00" },
    additionalDetailSchema: [],
};

function renderEditor(
    additionalDetailSchema: AdditionalDetailField[] = [],
    disabled = false,
) {
    return renderWithProviders(
        <AdditionalDetailsEditor
            event={{ ...event, additionalDetailSchema }}
            teamId="t1"
            eventId="e1"
            disabled={disabled}
        />,
    );
}

async function save(user: ReturnType<typeof renderEditor>["user"]) {
    await user.click(screen.getByRole("button", { name: "Save changes" }));
    await waitFor(() => expect(put).toHaveBeenCalled());
}

describe("AdditionalDetailsEditor", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    // Given an event with no additional detail fields
    // When an organizer adds and fills a field
    // Then the auto-generated kebab-case key and default max length are persisted
    it("adds a field with an auto-generated key", async () => {
        const { user } = renderEditor();

        await user.click(screen.getByRole("button", { name: "Add field" }));
        const name = screen.getByPlaceholderText("Display name");
        await user.type(name, "Dietary Requirements");

        expect(screen.getByPlaceholderText("key")).toHaveValue("dietary-requirements");
        expect(screen.getByRole("spinbutton")).toHaveValue(200);

        await save(user);

        expect(put).toHaveBeenCalledWith(ENDPOINT, {
            fields: [{ key: "dietary-requirements", name: "Dietary Requirements", maxLength: 200 }],
            expectedVersion: 7,
        });
    });

    // Given a new field whose display name generated a key
    // When the organizer overrides the key before saving
    // Then the explicit key is persisted instead of the generated one
    it("persists an overridden key", async () => {
        const { user } = renderEditor();

        await user.click(screen.getByRole("button", { name: "Add field" }));
        await user.type(screen.getByPlaceholderText("Display name"), "T-shirt size");
        const key = screen.getByPlaceholderText("key");
        await user.clear(key);
        await user.type(key, "shirt-size");
        await save(user);

        expect(put).toHaveBeenCalledWith(ENDPOINT, {
            fields: [{ key: "shirt-size", name: "T-shirt size", maxLength: 200 }],
            expectedVersion: 7,
        });
    });

    // Given two configured fields
    // When the editor renders
    // Then no drag or keyboard reorder affordance is exposed by this component
    it("does not expose an unsupported reorder control", () => {
        renderEditor([
            existingField,
            { key: "shirt-size", name: "T-shirt size", maxLength: 5 },
        ]);

        expect(screen.queryByRole("button", { name: /^(?:reorder|move(?: up| down)?)$/i })).not.toBeInTheDocument();
        expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    });

    // Given an existing field
    // When the organizer renames its display name
    // Then the persisted key remains unchanged and the key input is read-only
    it("renames a field without changing its key", async () => {
        const { user } = renderEditor([existingField]);

        const name = screen.getByPlaceholderText("Display name");
        const key = screen.getByPlaceholderText("key");
        expect(key).toHaveValue("dietary");
        expect(key).toHaveAttribute("readonly");
        await user.clear(name);
        await user.type(name, "Food preferences");
        await save(user);

        expect(put).toHaveBeenCalledWith(ENDPOINT, {
            fields: [{ key: "dietary", name: "Food preferences", maxLength: 200 }],
            expectedVersion: 7,
        });
    });

    // Given an original field with historical registration values
    // When the organizer requests removal
    // Then confirmation explains that historical values are preserved before removal proceeds
    it("requires confirmation before removing an original field", async () => {
        const remainingField = { key: "shirt-size", name: "T-shirt size", maxLength: 5 };
        const { user } = renderEditor([existingField, remainingField]);

        await user.click(screen.getAllByRole("button", { name: "Remove field" })[0]!);

        expect(screen.getByRole("heading", { name: "Remove field?" })).toBeInTheDocument();
        expect(screen.getByText(/Historical values on existing registrations are preserved/)).toBeInTheDocument();
        expect(screen.getByDisplayValue("Dietary requirements")).toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();

        await user.click(screen.getByRole("button", { name: "Cancel" }));
        expect(screen.getByDisplayValue("Dietary requirements")).toBeInTheDocument();

        await user.click(screen.getAllByRole("button", { name: "Remove field" })[0]!);
        await user.click(screen.getByRole("button", { name: /^Remove$/ }));
        expect(screen.queryByDisplayValue("Dietary requirements")).not.toBeInTheDocument();
        expect(put).not.toHaveBeenCalled();

        await save(user);
        expect(put).toHaveBeenCalledWith(ENDPOINT, {
            fields: [remainingField],
            expectedVersion: 7,
        });
    });

    // Given an archived event
    // When the additional-details editor renders
    // Then fields, removal, add, and save controls are read-only
    it("is read-only for archived events", () => {
        renderEditor([existingField], true);

        expect(screen.getByRole("button", { name: "Add field" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Save changes" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Discard" })).toBeDisabled();
        expect(screen.getByPlaceholderText("Display name")).toBeDisabled();
        expect(screen.getByPlaceholderText("key")).toBeDisabled();
        expect(screen.getByRole("spinbutton")).toBeDisabled();
        expect(screen.getByRole("button", { name: "Remove field" })).toBeDisabled();
    });

    // Given another organizer has changed the schema
    // When this organizer saves a valid new field
    // Then the schema concurrency conflict is shown in the general error alert
    it("surfaces an additional-details concurrency conflict", async () => {
        const concurrencyConflict = {
            status: 409,
            title: "Conflict",
            detail: "The resource was modified by another operation.",
            errors: {},
            code: "concurrency_conflict",
        };
        put.mockRejectedValue(
            new FormError(concurrencyConflict),
        );
        const { user } = renderEditor();

        await user.click(screen.getByRole("button", { name: "Add field" }));
        await user.type(screen.getByPlaceholderText("Display name"), "Company");
        await save(user);

        const alert = await screen.findByRole("alert");
        expect(alert).toHaveTextContent("Concurrency conflict");
    });
});
