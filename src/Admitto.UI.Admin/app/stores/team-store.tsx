import { create } from "zustand";

// Mirrors the sidebar's `sidebar_state` cookie pattern (see components/ui/sidebar.tsx):
// a client-set cookie is enough to remember a per-browser UI preference across visits
// without introducing server-side, per-account state for what is just a convenience.
export const SELECTED_TEAM_COOKIE_NAME = "selected_team_id";
const SELECTED_TEAM_COOKIE_MAX_AGE = 60 * 60 * 24 * 30;

export function readSelectedTeamCookie(): string | null {
    if (typeof document === "undefined") {
        return null;
    }

    const match = document.cookie.match(
        new RegExp(`(?:^|; )${SELECTED_TEAM_COOKIE_NAME}=([^;]*)`),
    );
    return match ? decodeURIComponent(match[1]!) : null;
}

function writeSelectedTeamCookie(teamId: string | null): void {
    if (typeof document === "undefined") {
        return;
    }

    document.cookie = teamId
        ? `${SELECTED_TEAM_COOKIE_NAME}=${encodeURIComponent(teamId)}; path=/; max-age=${SELECTED_TEAM_COOKIE_MAX_AGE}`
        : `${SELECTED_TEAM_COOKIE_NAME}=; path=/; max-age=0`;
}

type TeamStore = {
    selectedTeamId: string | null;
    setSelectedTeamId: (teamId: string | null) => void;
};

// Every path that changes the selection — explicit switch, URL-derived init, or fallback
// to the first team — goes through this one setter, so the "last selected team" cookie
// stays correct no matter which path the user took.
export const useTeamStore = create<TeamStore>((set) => ({
    selectedTeamId: null,
    setSelectedTeamId: (teamId: string | null) =>
    {
        writeSelectedTeamCookie(teamId);
        set({ selectedTeamId: teamId });
    },
}));
