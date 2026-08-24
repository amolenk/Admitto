import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";

import { renderWithProviders } from "@/test-utils/render";
import { routerMock, setRoute } from "@/test-utils/router";

import { NavSettings } from "./nav-settings";

// Harvested from openspec `admin-ui-team-crud`. The sidebar entry is a plain button that
// pushes the settings route on click (not an anchor), and it is gated on
// `canManageTeamSettings` — a permissions rule expressed purely in the UI.

const TEAM_ID = "11111111-1111-1111-1111-111111111111";

describe("NavSettings", () => {
    // Given an owner viewing the dashboard
    // When they click the Team Settings entry
    // Then they are navigated to that team's settings page
    it("navigates to the team's settings page", async () => {
        setRoute({ pathname: `/teams/${TEAM_ID}/events` });
        const { user } = renderWithProviders(
            <NavSettings teamId={TEAM_ID} canManageTeamSettings={true} />,
        );

        await user.click(screen.getByRole("button", { name: "Team Settings" }));

        expect(routerMock.push).toHaveBeenCalledWith(`/teams/${TEAM_ID}/settings`);
    });

    // Given the owner is already somewhere under the team's settings area
    // When the nav renders
    // Then the entry is marked active
    it("marks the entry active while under the settings path", () => {
        setRoute({ pathname: `/teams/${TEAM_ID}/settings/members` });
        renderWithProviders(<NavSettings teamId={TEAM_ID} canManageTeamSettings={true} />);

        expect(screen.getByRole("button", { name: "Team Settings" })).toHaveAttribute(
            "data-active",
            "true",
        );
    });

    // Given a crew member who cannot manage team settings
    // When the sidebar renders
    // Then the Team Settings entry does not appear at all
    it("is hidden for non-owners", () => {
        setRoute({ pathname: `/teams/${TEAM_ID}/events` });
        renderWithProviders(<NavSettings teamId={TEAM_ID} canManageTeamSettings={false} />);

        expect(screen.queryByRole("button", { name: "Team Settings" })).not.toBeInTheDocument();
    });
});
