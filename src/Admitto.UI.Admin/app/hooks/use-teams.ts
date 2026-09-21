"use client";

import { useQuery } from "@tanstack/react-query";
import { TeamListItemDto } from "@/lib/admitto-api/generated/types.gen";
import { readSelectedTeamCookie, useTeamStore } from "@/stores/team-store";
import { useEffect } from "react";
import { usePathname } from "next/navigation";
import { apiClient } from "@/lib/api-client";

async function fetchTeams(): Promise<TeamListItemDto[]> {
    return apiClient.get<TeamListItemDto[]>("/api/teams");
}

export function useTeams() {
    const selectedTeamId = useTeamStore((s) => s.selectedTeamId);
    const setSelectedTeamId = useTeamStore((s) => s.setSelectedTeamId);
    const pathname = usePathname();

    const { data: teams = [], isLoading, isSuccess } = useQuery({
        queryKey: ["teams"],
        queryFn: fetchTeams,
        throwOnError: false,
    });

    // Auto-select a team when teams load and nothing is selected yet.
    // Prefer the teamId from the current URL (e.g. /teams/[teamId]/...) so that a hard
    // refresh of a deep link lands on the correct team; otherwise fall back to whichever
    // team the user last selected on a previous visit, remembered via a cookie; otherwise
    // the first team in the list.
    useEffect(() => {
        if (isSuccess && teams.length > 0 && !selectedTeamId) {
            const isKnownTeam = (teamId: string | null): teamId is string =>
                teamId !== null && teams.some((t) => t.teamId === teamId);

            const urlTeamId = pathname.match(/^\/teams\/([^/]+)/)?.[1] ?? null;
            const rememberedTeamId = readSelectedTeamCookie();
            const teamIdToSelect =
                (isKnownTeam(urlTeamId) ? urlTeamId : null) ??
                (isKnownTeam(rememberedTeamId) ? rememberedTeamId : null) ??
                teams[0].teamId;
            setSelectedTeamId(teamIdToSelect);
        }
    }, [isSuccess, teams, selectedTeamId, setSelectedTeamId, pathname]);

    const selectedTeam = teams.find((t) => t.teamId === selectedTeamId) ?? null;

    return { teams, selectedTeam, isLoading, setSelectedTeamId };
}
