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
    const selectedTeamSlug = useTeamStore((s) => s.selectedTeamSlug);
    const setSelectedTeamSlug = useTeamStore((s) => s.setSelectedTeamSlug);
    const pathname = usePathname();

    const { data: teams = [], isLoading, isSuccess } = useQuery({
        queryKey: ["teams"],
        queryFn: fetchTeams,
        throwOnError: false,
    });

    // Auto-select a team when teams load and nothing is selected yet.
    // Prefer the team slug from the current URL (e.g. /teams/[teamSlug]/...)
    // so that a hard refresh lands on the correct team.
    useEffect(() => {
        if (isSuccess && teams.length > 0 && !selectedTeamSlug) {
            const urlSlug = pathname.match(/^\/teams\/([^/]+)/)?.[1] ?? null;
            const slugToSelect = urlSlug && teams.some((t) => t.slug === urlSlug)
                ? urlSlug
                : teams[0].slug;
            setSelectedTeamSlug(slugToSelect);
        }
    }, [isSuccess, teams, selectedTeamSlug, setSelectedTeamSlug, pathname]);

    const selectedTeam = teams.find((t) => t.slug === selectedTeamSlug) ?? null;

    return { teams, selectedTeam, isLoading, setSelectedTeamSlug };
}
