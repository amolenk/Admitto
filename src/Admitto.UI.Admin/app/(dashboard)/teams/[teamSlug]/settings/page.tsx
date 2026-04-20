"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TeamDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { TeamSettingsForm } from "./team-settings-form";
import { Skeleton } from "@/components/ui/skeleton";

async function fetchTeam(teamSlug: string): Promise<TeamDto> {
    return apiClient.get<TeamDto>(`/api/teams/${teamSlug}`);
}

export default function TeamSettingsPage() {
    const params = useParams<{ teamSlug: string }>();

    const { data: team, isLoading, error } = useQuery({
        queryKey: ["team", params.teamSlug],
        queryFn: () => fetchTeam(params.teamSlug),
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

    return <TeamSettingsForm key={`${team.slug}-${team.version}`} team={team} />;
}
