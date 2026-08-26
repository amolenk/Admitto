import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { apiClient } from "@/lib/api-client";
import { useTeamStore } from "@/stores/team-store";
import { teamListItemDto } from "@/test-utils/builders";
import { createQueryWrapper } from "@/test-utils/render";
import { setRoute } from "@/test-utils/router";

import { useTeams } from "./use-teams";

// `useTeams` drives the team switcher and, through it, which team every dashboard page is
// scoped to. Its one non-trivial behaviour is picking the initial selection: the team in the
// URL wins, so a hard refresh of /teams/{id}/... lands on that team rather than the first one.

vi.mock("@/lib/api-client", () => ({
    apiClient: { get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

const get = vi.mocked(apiClient.get);

const alpha = teamListItemDto({ teamId: "aaaa1111-0000-0000-0000-000000000000", name: "Alpha" });
const beta = teamListItemDto({ teamId: "bbbb2222-0000-0000-0000-000000000000", name: "Beta" });

function renderUseTeams() {
    return renderHook(() => useTeams(), { wrapper: createQueryWrapper() });
}

describe("useTeams", () => {
    beforeEach(() => {
        // The zustand store is a module singleton, so the selection survives between tests.
        useTeamStore.setState({ selectedTeamId: null });
        get.mockResolvedValue([alpha, beta]);
    });

    // Given a deep link into a team that the user is a member of
    // When the team list loads
    // Then that team is selected, not simply the first in the list
    it("selects the team named in the URL", async () => {
        setRoute({ pathname: `/teams/${beta.teamId}/events/xyz/registrations` });

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.selectedTeam).toEqual(beta));
    });

    // Given a URL whose teamId is not one the user can see — a stale bookmark, or a team
    // they were removed from
    // When the team list loads
    // Then it falls back to the first team rather than selecting nothing
    it("falls back to the first team when the URL names an unknown team", async () => {
        setRoute({ pathname: "/teams/99999999-0000-0000-0000-000000000000/settings" });

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.selectedTeam).toEqual(alpha));
    });

    // Given a route with no team in it, such as the dashboard root
    // When the team list loads
    // Then the first team is selected
    it("falls back to the first team when the route has no team", async () => {
        setRoute({ pathname: "/" });

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.selectedTeam).toEqual(alpha));
    });

    // Given the user has already switched teams
    // When the hook re-runs on a route that names a different team
    // Then the existing selection stands — auto-selection only fills an empty slot
    it("does not override a selection that is already made", async () => {
        useTeamStore.setState({ selectedTeamId: beta.teamId });
        setRoute({ pathname: `/teams/${alpha.teamId}/events` });

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.teams).toHaveLength(2));
        expect(result.current.selectedTeam).toEqual(beta);
    });

    // Given a selection left over for a team that is no longer in the list
    // When the teams load
    // Then no team is resolved, rather than a stale object being returned
    it("resolves no team when the selected id is not in the list", async () => {
        useTeamStore.setState({ selectedTeamId: "cccc3333-0000-0000-0000-000000000000" });

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.teams).toHaveLength(2));
        expect(result.current.selectedTeam).toBeNull();
    });

    // Given the request is still in flight
    // When the hook first renders
    // Then it reports loading with an empty list, so consumers can show a skeleton
    it("reports loading with an empty list before the teams arrive", async () => {
        get.mockReturnValue(new Promise(() => {}));

        const { result } = renderUseTeams();

        expect(result.current.isLoading).toBe(true);
        expect(result.current.teams).toEqual([]);
        expect(result.current.selectedTeam).toBeNull();
    });

    // Given the teams request fails
    // When the hook settles
    // Then it degrades to an empty list instead of throwing to the error boundary —
    // the switcher sits in the dashboard shell, so throwing would blank the whole page
    it("degrades to an empty list when the request fails", async () => {
        get.mockRejectedValue(new Error("offline"));

        const { result } = renderUseTeams();

        await waitFor(() => expect(result.current.isLoading).toBe(false));
        expect(result.current.teams).toEqual([]);
        expect(result.current.selectedTeam).toBeNull();
    });

    // Given a loaded team list
    // When the user picks another team from the switcher
    // Then the selection follows
    it("switches the selected team", async () => {
        const { result } = renderUseTeams();
        await waitFor(() => expect(result.current.selectedTeam).toEqual(alpha));

        act(() => {
            result.current.setSelectedTeamId(beta.teamId);
        });

        expect(result.current.selectedTeam).toEqual(beta);
    });
});
