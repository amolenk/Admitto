import { beforeEach, describe, expect, it } from "vitest";

import { forgetRememberedTeam } from "@/test-utils/selected-team-cookie";

import { readSelectedTeamCookie, useTeamStore } from "./team-store";

// The store is the single place that writes the "last selected team" cookie, so every
// path that changes selection (explicit switch, URL-derived init, fallback-to-first-team)
// persists it without each caller having to remember to.

const alphaTeamId = "aaaa1111-0000-0000-0000-000000000000";

describe("team-store", () => {
    beforeEach(() => {
        useTeamStore.setState({ selectedTeamId: null });
        forgetRememberedTeam();
    });

    // Given no team has ever been selected
    // When the cookie is read
    // Then there is nothing to find
    it("has no cookie before a team is ever selected", () => {
        expect(readSelectedTeamCookie()).toBeNull();
    });

    // Given a team is selected
    // When the selection is set
    // Then the choice is written to the cookie so it survives a refresh
    it("persists the selected team to a cookie", () => {
        useTeamStore.getState().setSelectedTeamId(alphaTeamId);

        expect(readSelectedTeamCookie()).toBe(alphaTeamId);
    });

    // Given a team was previously selected and persisted
    // When the selection is cleared back to null
    // Then the cookie is removed too, rather than left pointing at a stale team
    it("clears the cookie when the selection is cleared", () => {
        useTeamStore.getState().setSelectedTeamId(alphaTeamId);

        useTeamStore.getState().setSelectedTeamId(null);

        expect(readSelectedTeamCookie()).toBeNull();
    });
});
