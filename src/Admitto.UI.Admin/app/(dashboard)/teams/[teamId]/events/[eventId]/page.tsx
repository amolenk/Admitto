"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDetailsDto, TicketTypeDto, RegistrationListItemDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Skeleton } from "@/components/ui/skeleton";
import { EventHeroCard } from "./components/event-hero-card";
import { TicketBreakdownCard } from "./components/ticket-breakdown-card";
import { CheckInCard } from "./components/check-in-card";
import { SalesTrendCard } from "./components/sales-trend-card";

type EventWithStatus = TicketedEventDetailsDto & { isRegistrationOpen?: boolean };

async function fetchEvent(teamId: string, eventId: string): Promise<EventWithStatus> {
    return apiClient.get<EventWithStatus>(`/api/teams/${teamId}/events/${eventId}`);
}

async function fetchTicketTypes(teamId: string, eventId: string): Promise<TicketTypeDto[]> {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`);
}

async function fetchRegistrations(teamId: string, eventId: string): Promise<RegistrationListItemDto[]> {
    return apiClient.get<RegistrationListItemDto[]>(`/api/teams/${teamId}/events/${eventId}/registrations`);
}

export default function EventDashboardPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

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

    const registrations = useQuery({
        queryKey: ["registrations", teamId, eventId],
        queryFn: () => fetchRegistrations(teamId, eventId),
        throwOnError: false,
    });

    if (event.isLoading) {
        return (
            <PageLayout>
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
            <PageLayout>
                <p className="text-destructive">Failed to load event details.</p>
            </PageLayout>
        );
    }

    return (
        <PageLayout>
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
                <SalesTrendCard
                    registrations={registrations.data}
                    isLoading={registrations.isLoading}
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
