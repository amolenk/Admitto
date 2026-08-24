import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { useTeamStore } from "@/stores/team-store";
import { teamDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock, setRoute } from "@/test-utils/router";

import DangerZonePage from "./page";

// Harvested from openspec `admin-ui-team-danger-zone`. Note the spec says the confirmation
// input takes the team *slug*; the implementation compares against the team *name* and the
// dialog copy says "type the team name". The code is authoritative — see TESTING-BACKLOG.md.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);
const post = vi.mocked(apiClient.post);

const TEAM_ID = "11111111-1111-1111-1111-111111111111";
const team = teamDto({ teamId: TEAM_ID, name: "Contoso Crew", version: 7 });

function renderPage() {
    setRoute({ params: { teamId: TEAM_ID } });
    return renderWithProviders(<DangerZonePage />);
}

/** Opens the confirmation dialog and types `text` into the confirmation input. */
async function openDialogAndType(user: ReturnType<typeof renderPage>["user"], text: string) {
    await user.click(await screen.findByRole("button", { name: "Archive team" }));
    if (text) {
        await user.type(screen.getByPlaceholderText(team.name), text);
    }
    return screen.getByRole("button", { name: "I understand, archive this team" });
}

describe("DangerZonePage", () => {
    beforeEach(() => {
        useTeamStore.setState({ selectedTeamId: TEAM_ID });
        get.mockResolvedValue(team);
        post.mockResolvedValue(undefined);
    });

    // Given a team owner has typed the team name to confirm
    // When they archive the team
    // Then the archive is sent with the loaded version, the team switcher selection is
    // cleared, and they land back on the dashboard root — the archived team's pages are gone
    it("archives the team, clears the selection and returns to the dashboard root", async () => {
        const { user } = renderPage();

        await user.click(await openDialogAndType(user, team.name));

        await waitFor(() =>
            expect(post).toHaveBeenCalledWith(`/api/teams/${TEAM_ID}/archive`, {
                expectedVersion: 7,
            }),
        );
        await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/"));
        expect(useTeamStore.getState().selectedTeamId).toBeNull();
    });

    // Given the confirmation input does not match the team name
    // When the owner tries to confirm
    // Then the confirm button stays disabled, so a mistyped name cannot archive a team
    it("keeps the confirm button disabled until the typed name matches", async () => {
        const { user } = renderPage();

        const confirm = await openDialogAndType(user, "Contoso");
        expect(confirm).toBeDisabled();

        await user.type(screen.getByPlaceholderText(team.name), " Crew");

        expect(confirm).toBeEnabled();
    });

    // Given the confirmation dialog is open with a valid name typed
    // When the owner cancels instead of confirming
    // Then nothing is archived and they stay on the page
    it("does not archive when the dialog is cancelled", async () => {
        const { user } = renderPage();
        await openDialogAndType(user, team.name);

        await user.click(screen.getByRole("button", { name: "Cancel" }));

        expect(post).not.toHaveBeenCalled();
        expect(routerMock.push).not.toHaveBeenCalled();
    });

    // Given the owner typed the name, cancelled, and reopened the dialog
    // When the dialog reopens
    // Then the confirmation input is empty again, so the guard cannot be bypassed by
    // reopening a dialog that still holds a matching value
    it("clears the typed confirmation when the dialog is reopened", async () => {
        const { user } = renderPage();
        await openDialogAndType(user, team.name);
        await user.click(screen.getByRole("button", { name: "Cancel" }));

        await user.click(await screen.findByRole("button", { name: "Archive team" }));

        expect(screen.getByPlaceholderText(team.name)).toHaveValue("");
        expect(screen.getByRole("button", { name: "I understand, archive this team" })).toBeDisabled();
    });

    // Given the team still has active events, so the backend refuses
    // When the owner confirms the archive
    // Then the backend's reason is shown and they are not navigated away
    it("shows the backend's reason when the team cannot be archived", async () => {
        post.mockRejectedValue(
            new FormError({
                status: 409,
                title: "Conflict",
                detail: "Team cannot be archived because it has active events.",
            }),
        );
        const { user } = renderPage();

        await user.click(await openDialogAndType(user, team.name));

        expect(
            await screen.findByText("Team cannot be archived because it has active events."),
        ).toBeInTheDocument();
        expect(routerMock.push).not.toHaveBeenCalled();
    });

    // Given the team was modified since the page loaded, so the version is stale
    // When the owner confirms the archive
    // Then the concurrency message is surfaced rather than a silent failure
    it("surfaces a concurrency conflict", async () => {
        post.mockRejectedValue(
            new FormError({
                status: 409,
                title: "Conflict",
                detail: "The team was modified by someone else. Please reload.",
            }),
        );
        const { user } = renderPage();

        await user.click(await openDialogAndType(user, team.name));

        expect(
            await screen.findByText("The team was modified by someone else. Please reload."),
        ).toBeInTheDocument();
    });

    // Given an error that is not a FormError, such as a dropped connection
    // When the archive fails
    // Then the raw message is still surfaced rather than the page failing silently
    it("surfaces a non-FormError failure", async () => {
        post.mockRejectedValue(new Error("Network request failed"));
        const { user } = renderPage();

        await user.click(await openDialogAndType(user, team.name));

        expect(await screen.findByText("Network request failed")).toBeInTheDocument();
    });

    // Given the team details cannot be loaded
    // When the page settles
    // Then a load failure is reported instead of an archive form with no team behind it
    it("reports a failure to load the team", async () => {
        get.mockRejectedValue(new Error("offline"));

        renderPage();

        expect(await screen.findByText("Failed to load team details.")).toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Archive team" })).not.toBeInTheDocument();
    });
});
