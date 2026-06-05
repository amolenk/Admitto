"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useTeamStore } from "@/stores/team-store";
import { apiClient } from "@/lib/api-client";
import { TicketedEventListItemDto } from "@/lib/admitto-api/generated";

async function fetchEvents(teamId: string): Promise<TicketedEventListItemDto[]> {
    return apiClient.get<TicketedEventListItemDto[]>(`/api/teams/${teamId}/events`);
}

export default function Home() {
    const router = useRouter();
    const selectedTeamId = useTeamStore((s) => s.selectedTeamId);

    const { data: events } = useQuery({
        queryKey: ["events", selectedTeamId],
        queryFn: () => fetchEvents(selectedTeamId!),
        enabled: !!selectedTeamId,
        throwOnError: false,
    });

    useEffect(() => {
        if (!selectedTeamId || !events) return;

        const firstEvent = events.find(e => e.status !== "archived");
        if (firstEvent) {
            router.replace(`/teams/${selectedTeamId}/events/${firstEvent.id}`);
        }
    }, [selectedTeamId, events, router]);

    return null;
}
