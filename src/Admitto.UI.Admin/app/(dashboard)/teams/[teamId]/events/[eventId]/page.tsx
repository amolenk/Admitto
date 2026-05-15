"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Skeleton } from "@/components/ui/skeleton";
import { useTeams } from "@/hooks/use-teams";
import { EventHeroCard } from "./components/event-hero-card";
import { TicketBreakdownCard } from "./components/ticket-breakdown-card";
import { CheckInCard } from "./components/check-in-card";

type EventWithStatus = TicketedEventDetailsDto & { isRegistrationOpen?: boolean };

async function fetchEvent(teamId: string, eventId: string): Promise<EventWithStatus> {
    return apiClient.get<EventWithStatus>(`/api/teams/${teamId}/events/${eventId}`);
}

async function fetchTicketTypes(teamId: string, eventId: string): Promise<TicketTypeDto[]> {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`);
}

export default function EventDashboardPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const { selectedTeam } = useTeams();

    const event = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () => fetchEvent(teamId, eventId),
        throwOnError: false,
    });

    const ticketTypes = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () => fetchTicketTypes(teamId, eventId),
        throwOnError: false,
    });

    const breadcrumbs = [
        { label: selectedTeam?.name ?? teamId, href: `/teams/${teamId}/settings` },
        { label: event.data?.name ?? eventId },
        { label: "Dashboard" },
    ];

    if (event.isLoading) {
        return (
            <PageLayout title="Dashboard" breadcrumbs={breadcrumbs}>
                <Skeleton className="h-48 w-full" />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5 mt-5">
                    <Skeleton className="h-64 w-full" />
                    <Skeleton className="h-64 w-full" />
                </div>
            </PageLayout>
        );
    }

    if (event.error || !event.data) {
        return (
            <PageLayout title="Dashboard" breadcrumbs={breadcrumbs}>
                <p className="text-destructive">Failed to load event details.</p>
            </PageLayout>
        );
    }

    return (
        <PageLayout title="Dashboard" breadcrumbs={breadcrumbs}>
            <div className="flex flex-col gap-5">
                <EventHeroCard
                    event={event.data}
                    openStatus={
                        event.data.isRegistrationOpen !== undefined
                            ? { isOpen: event.data.isRegistrationOpen }
                            : null
                    }
                    ticketTypes={ticketTypes.data}
                />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <TicketBreakdownCard
                        teamId={teamId}
                        eventId={eventId}
                        ticketTypes={ticketTypes.data ?? []}
                        isLoading={ticketTypes.isLoading}
                    />
                    <CheckInCard event={event.data} ticketTypes={ticketTypes.data ?? []} />
                </div>
            </div>
        </PageLayout>
    );
}
