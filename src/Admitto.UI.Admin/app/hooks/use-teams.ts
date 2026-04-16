"use client";

import { useQuery } from "@tanstack/react-query";
import { TeamListItemDto } from "@/lib/admitto-api/generated/types.gen";
import { useTeamStore } from "@/stores/team-store";
import { useEffect } from "react";
import { apiClient } from "@/lib/api-client";

async function fetchTeams(): Promise<TeamListItemDto[]> {
    return apiClient.get<TeamListItemDto[]>("/api/teams");
}

export function useTeams() {
    const selectedTeamSlug = useTeamStore((s) => s.selectedTeamSlug);
    const setSelectedTeamSlug = useTeamStore((s) => s.setSelectedTeamSlug);

    const { data: teams = [], isLoading, isSuccess } = useQuery({
        queryKey: ["teams"],
        queryFn: fetchTeams,
        throwOnError: false,
    });

    // Auto-select the first team when teams load and nothing is selected
    useEffect(() => {
        if (isSuccess && teams.length > 0 && !selectedTeamSlug) {
            setSelectedTeamSlug(teams[0].slug);
        }
    }, [isSuccess, teams, selectedTeamSlug, setSelectedTeamSlug]);

    const selectedTeam = teams.find((t) => t.slug === selectedTeamSlug) ?? null;

    return { teams, selectedTeam, isLoading, setSelectedTeamSlug };
}
