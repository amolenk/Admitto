"use client";

import { useQuery } from "@tanstack/react-query";
import { TeamListItemDto } from "@/lib/admitto-api/generated/types.gen";
import { useTeamStore } from "@/stores/team-store";
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
    // Prefer the teamId from the current URL (e.g. /teams/[teamId]/...)
    // so that a hard refresh lands on the correct team.
    useEffect(() => {
        if (isSuccess && teams.length > 0 && !selectedTeamId) {
            const urlTeamId = pathname.match(/^\/teams\/([^/]+)/)?.[1] ?? null;
            const teamIdToSelect = urlTeamId && teams.some((t) => t.teamId === urlTeamId)
                ? urlTeamId
                : teams[0].teamId;
            setSelectedTeamId(teamIdToSelect);
        }
    }, [isSuccess, teams, selectedTeamId, setSelectedTeamId, pathname]);

    const selectedTeam = teams.find((t) => t.teamId === selectedTeamId) ?? null;

    return { teams, selectedTeam, isLoading, setSelectedTeamId };
}
