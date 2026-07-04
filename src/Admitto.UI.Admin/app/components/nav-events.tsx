"use client"

import { TicketedEventListItemDto } from "@/lib/admitto-api/generated";
import { useRouter, useParams } from "next/navigation"

import { Plus } from "lucide-react"
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuItem,
} from "@/components/ui/sidebar"
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { formatInEventZone } from "@/lib/time-zones";

async function fetchEvents(teamId: string): Promise<TicketedEventListItemDto[]> {
    return apiClient.get<TicketedEventListItemDto[]>(`/api/teams/${teamId}/events`);
}

export function NavEvents({
                               teamId,
                               canCreateEvents,
                           }: {
    teamId: string,
    canCreateEvents: boolean,
}) {
    const router = useRouter()
    const params = useParams<{ eventId?: string }>();
    const activeEventId = params.eventId ?? null;

    const { data: events = [] } = useQuery({
        queryKey: ["events", teamId],
        queryFn: () => fetchEvents(teamId),
        throwOnError: false,
    });

    const visibleEvents = events.filter(e => e.status !== "archived");

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel className="uppercase tracking-wider">Events</SidebarGroupLabel>
            <SidebarMenu>
                {visibleEvents.map((ticketedEvent) => {
                    const isActive = ticketedEvent.id === activeEventId;
                    const dateLabel = formatInEventZone(
                        ticketedEvent.startsAt,
                        ticketedEvent.timeZone,
                        "MMM d",
                    ).toUpperCase();

                    return (
                        <SidebarMenuItem key={ticketedEvent.id}>
                            <button
                                onClick={() => router.push(`/teams/${teamId}/events/${ticketedEvent.id}`)}
                                data-active={isActive ? "true" : "false"}
                                className="side-item"
                            >
                                <span
                                    className={`h-1.5 w-1.5 rounded-full shrink-0 ${
                                        isActive ? "bg-primary" : ""
                                    }`}
                                    style={!isActive ? { background: "var(--border)" } : undefined}
                                />
                                <span className="truncate flex-1 text-left">{ticketedEvent.name}</span>
                                {isActive && (
                                    <span className="text-[10px] text-muted-foreground font-mono">
                                        {dateLabel}
                                    </span>
                                )}
                            </button>
                        </SidebarMenuItem>
                    );
                })}
                {canCreateEvents && (
                    <SidebarMenuItem>
                        <button
                            className="side-item text-muted-foreground"
                            onClick={() => router.push(`/teams/${teamId}/events/new`)}
                        >
                            <Plus className="size-3.5" />
                            <span>New event</span>
                        </button>
                    </SidebarMenuItem>
                )}
            </SidebarMenu>
        </SidebarGroup>
    )
}
