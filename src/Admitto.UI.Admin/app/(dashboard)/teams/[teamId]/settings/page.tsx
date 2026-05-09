"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TeamDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { TeamSettingsForm } from "./team-settings-form";
import { Skeleton } from "@/components/ui/skeleton";

async function fetchTeam(teamId: string): Promise<TeamDto> {
    return apiClient.get<TeamDto>(`/api/teams/${teamId}`);
}

export default function TeamSettingsPage() {
    const params = useParams<{ teamId: string }>();

    const { data: team, isLoading, error } = useQuery({
        queryKey: ["team", params.teamId],
        queryFn: () => fetchTeam(params.teamId),
        throwOnError: false,
    });

    if (isLoading) {
        return (
            <div className="space-y-6">
                <Skeleton className="h-10 w-48" />
                <Skeleton className="h-64 w-full" />
            </div>
        );
    }

    if (error || !team) {
        return <p className="text-destructive">Failed to load team details.</p>;
    }

    return <TeamSettingsForm key={`${team.teamId}-${team.version}`} team={team} />;
}
