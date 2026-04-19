"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { Skeleton } from "@/components/ui/skeleton";
import { GeneralSettingsForm } from "./general-settings-form";

async function fetchEvent(teamSlug: string, eventSlug: string): Promise<TicketedEventDto> {
    return apiClient.get<TicketedEventDto>(`/api/teams/${teamSlug}/events/${eventSlug}`);
}

export default function EventGeneralSettingsPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    const { data, isLoading, error } = useQuery({
        queryKey: ["event", teamSlug, eventSlug],
        queryFn: () => fetchEvent(teamSlug, eventSlug),
        throwOnError: false,
    });

    if (isLoading) {
        return (
            <div className="space-y-6 max-w-lg">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
            </div>
        );
    }

    if (error || !data) {
        return <p className="text-destructive">Failed to load event details.</p>;
    }

    return <GeneralSettingsForm key={`${data.slug}-${data.version}`} event={data} />;
}
