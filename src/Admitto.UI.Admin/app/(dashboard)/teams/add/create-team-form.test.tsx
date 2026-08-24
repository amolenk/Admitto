import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { useTeamStore } from "@/stores/team-store";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock } from "@/test-utils/router";

import { CreateTeamForm } from "./create-team-form";

// Harvested from openspec `admin-ui-team-crud`. Redirects to team settings, not the events
// page — see TESTING-BACKLOG.md D3, confirmed as the current, correct behaviour.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const post = vi.mocked(apiClient.post);

const NEW_TEAM_ID = "44444444-4444-4444-4444-444444444444";

describe("CreateTeamForm", () => {
    beforeEach(() => {
        useTeamStore.setState({ selectedTeamId: null });
        post.mockResolvedValue({ teamId: NEW_TEAM_ID });
    });

    // Given a valid team name
    // When the form is submitted
    // Then the team is created, selected, and the owner lands on its settings page
    it("creates the team and redirects to its settings page", async () => {
        const { user } = renderWithProviders(<CreateTeamForm />);

        await user.type(screen.getByPlaceholderText("e.g. My Team"), "Contoso Crew");
        await user.click(screen.getByRole("button", { name: "Create team" }));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith("/api/teams", { name: "Contoso Crew" }),
        );
        await waitFor(() =>
            expect(routerMock.push).toHaveBeenCalledWith(`/teams/${NEW_TEAM_ID}/settings`),
        );
        expect(useTeamStore.getState().selectedTeamId).toBe(NEW_TEAM_ID);
    });

    // Given the name field is left empty
    // When the form is submitted
    // Then the field error is shown and no request is made
    it("blocks submission and shows an error when the name is empty", async () => {
        const { user } = renderWithProviders(<CreateTeamForm />);

        await user.click(screen.getByRole("button", { name: "Create team" }));

        expect(await screen.findByText("Name is required")).toBeInTheDocument();
        expect(post).not.toHaveBeenCalled();
        expect(routerMock.push).not.toHaveBeenCalled();
    });
});
