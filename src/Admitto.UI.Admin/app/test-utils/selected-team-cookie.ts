import { SELECTED_TEAM_COOKIE_NAME } from "@/stores/team-store";

/** Sets the "remembered team" cookie directly, bypassing the store, to simulate a returning visitor. */
export function rememberTeam(teamId: string): void {
    document.cookie = `${SELECTED_TEAM_COOKIE_NAME}=${teamId}; path=/`;
}

/** Clears the "remembered team" cookie so tests don't leak state into each other. */
export function forgetRememberedTeam(): void {
    document.cookie = `${SELECTED_TEAM_COOKIE_NAME}=; path=/; max-age=0`;
}
