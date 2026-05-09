"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { Skeleton } from "@/components/ui/skeleton";
import { GeneralSettingsForm } from "./general-settings-form";

async function fetchEvent(teamId: string, eventId: string): Promise<TicketedEventDetailsDto> {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`);
}

export default function EventGeneralSettingsPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

    const { data, isLoading, error } = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () => fetchEvent(teamId, eventId),
        throwOnError: false,
    });

    if (isLoading) {
        return (
            <div className="space-y-6">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
            </div>
        );
    }

    if (error || !data) {
        return <p className="text-destructive">Failed to load event details.</p>;
    }

    return <GeneralSettingsForm key={`${data.version}`} event={data} />;
}
