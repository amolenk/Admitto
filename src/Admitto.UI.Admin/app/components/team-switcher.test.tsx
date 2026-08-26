import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { useTeamStore } from "@/stores/team-store";
import { teamListItemDto } from "@/test-utils/builders";
import { renderWithProviders } from "@/test-utils/render";
import { routerMock } from "@/test-utils/router";

import { SidebarProvider } from "@/components/ui/sidebar";
import { TeamSwitcher } from "./team-switcher";

// The switcher updates the current team through `useTeams`/`useTeamStore`; navigation occurs
// only when the selection changes.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const alpha = teamListItemDto({ teamId: "aaaa1111-0000-0000-0000-000000000000", name: "Alpha" });
const beta = teamListItemDto({ teamId: "bbbb2222-0000-0000-0000-000000000000", name: "Beta" });

function renderSwitcher() {
    return renderWithProviders(
        <SidebarProvider>
            <TeamSwitcher />
        </SidebarProvider>,
    );
}

describe("TeamSwitcher", () => {
    beforeEach(() => {
        useTeamStore.setState({ selectedTeamId: alpha.teamId });
        get.mockResolvedValue([alpha, beta]);
    });

    // Given the switcher is open
    // When the owner clicks "Add team"
    // Then they are sent to the create-team page
    it("navigates to the create-team page", async () => {
        const { user } = renderSwitcher();

        await user.click(await screen.findByRole("button", { name: /Alpha/ }));
        await user.click(await screen.findByText("Add team"));

        expect(routerMock.push).toHaveBeenCalledWith("/teams/add");
    });

    // Given the owner is on Alpha
    // When they pick a different team from the switcher
    // Then the selection changes and they are sent back to the dashboard root, so pages
    // scoped to the old team are not left showing
    it("switches team and resets to the dashboard root", async () => {
        const { user } = renderSwitcher();

        await user.click(await screen.findByRole("button", { name: /Alpha/ }));
        await user.click(await screen.findByText("Beta"));

        expect(useTeamStore.getState().selectedTeamId).toBe(beta.teamId);
        await waitFor(() => expect(routerMock.push).toHaveBeenCalledWith("/"));
    });

    // Given the owner is already on Alpha
    // When they pick Alpha again from the switcher
    // Then nothing navigates, since the selection did not actually change
    it("does not navigate when the current team is selected again", async () => {
        const { user } = renderSwitcher();

        await user.click(await screen.findByRole("button", { name: /Alpha/ }));
        await user.click(await screen.findAllByText("Alpha").then((els) => els[els.length - 1]!));

        expect(routerMock.push).not.toHaveBeenCalled();
        expect(useTeamStore.getState().selectedTeamId).toBe(alpha.teamId);
    });
});
