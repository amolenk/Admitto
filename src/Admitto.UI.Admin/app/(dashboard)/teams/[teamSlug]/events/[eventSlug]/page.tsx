"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDto, TicketTypeDto, RegistrationOpenStatusDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Skeleton } from "@/components/ui/skeleton";
import { useTeams } from "@/hooks/use-teams";
import { EventHeroCard } from "./components/event-hero-card";
import { TicketBreakdownCard } from "./components/ticket-breakdown-card";
import { CheckInCard } from "./components/check-in-card";

async function fetchEvent(teamSlug: string, eventSlug: string): Promise<TicketedEventDto> {
    return apiClient.get<TicketedEventDto>(`/api/teams/${teamSlug}/events/${eventSlug}`);
}

async function fetchTicketTypes(teamSlug: string, eventSlug: string): Promise<TicketTypeDto[]> {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types`);
}

async function fetchOpenStatus(teamSlug: string, eventSlug: string): Promise<RegistrationOpenStatusDto> {
    return apiClient.get<RegistrationOpenStatusDto>(
        `/api/teams/${teamSlug}/events/${eventSlug}/registration/open-status`
    );
}

export default function EventDashboardPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const { selectedTeam } = useTeams();

    const event = useQuery({
        queryKey: ["event", teamSlug, eventSlug],
        queryFn: () => fetchEvent(teamSlug, eventSlug),
        throwOnError: false,
    });

    const ticketTypes = useQuery({
        queryKey: ["ticket-types", teamSlug, eventSlug],
        queryFn: () => fetchTicketTypes(teamSlug, eventSlug),
        throwOnError: false,
    });

    const openStatus = useQuery({
        queryKey: ["registration-open-status", teamSlug, eventSlug],
        queryFn: () => fetchOpenStatus(teamSlug, eventSlug),
        throwOnError: false,
    });

    const breadcrumbs = [
        { label: selectedTeam?.name ?? teamSlug, href: `/teams/${teamSlug}/settings` },
        { label: event.data?.name ?? eventSlug },
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
                    openStatus={openStatus.data}
                    ticketTypes={ticketTypes.data}
                />
                <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
                    <TicketBreakdownCard
                        teamSlug={teamSlug}
                        ticketTypes={ticketTypes.data ?? []}
                        isLoading={ticketTypes.isLoading}
                    />
                    <CheckInCard event={event.data} ticketTypes={ticketTypes.data ?? []} />
                </div>
            </div>
        </PageLayout>
    );
}
