import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { teamDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";

import { TeamSettingsForm } from "./team-settings-form";

// Partial updates send only changed fields plus the loaded version, so stale writes are rejected
// rather than silently overwriting concurrent edits.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const put = vi.mocked(apiClient.put);

const team = teamDto({
    teamId: "11111111-1111-1111-1111-111111111111",
    name: "Contoso Crew",
    accentColor: "#3b82f6",
    version: 5,
});

function renderForm() {
    return renderWithProviders(<TeamSettingsForm team={team} />);
}

describe("TeamSettingsForm", () => {
    beforeEach(() => {
        put.mockResolvedValue(undefined);
    });

    // Given the owner changes only the team name
    // When they save
    // Then the update carries the new name and the loaded version, not the unchanged color
    it("updates the team name", async () => {
        const { user } = renderForm();

        const nameInput = screen.getByPlaceholderText("e.g. My Team");
        await user.clear(nameInput);
        await user.type(nameInput, "Fabrikam Fold");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(`/api/teams/${team.teamId}`, {
                expectedVersion: 5,
                name: "Fabrikam Fold",
            }),
        );
    });

    // Given the owner changes only the accent color
    // When they save
    // Then the update carries the new color and the loaded version, not the unchanged name
    it("updates the team accent color", async () => {
        const { user } = renderForm();

        const textColorInput = screen.getByPlaceholderText("#0f766e");
        await user.clear(textColorInput);
        await user.type(textColorInput, "#00ff00");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        await waitFor(() =>
            expect(put).toHaveBeenCalledWith(`/api/teams/${team.teamId}`, {
                expectedVersion: 5,
                accentColor: "#00ff00",
            }),
        );
    });

    // Given the team was modified by someone else since the page loaded
    // When the owner saves their own change
    // Then the concurrency message is surfaced rather than a silent failure
    it("surfaces a concurrency conflict", async () => {
        put.mockRejectedValue(
            new FormError({
                status: 409,
                title: "Conflict",
                detail: "The team was modified by someone else. Please reload.",
            }),
        );
        const { user } = renderForm();

        const nameInput = screen.getByPlaceholderText("e.g. My Team");
        await user.clear(nameInput);
        await user.type(nameInput, "Fabrikam Fold");
        await user.click(screen.getByRole("button", { name: "Save changes" }));

        expect(
            await screen.findByText("The team was modified by someone else. Please reload."),
        ).toBeInTheDocument();
    });
});
