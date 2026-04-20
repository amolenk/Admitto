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

async function fetchEvents(teamSlug: string): Promise<TicketedEventListItemDto[]> {
    return apiClient.get<TicketedEventListItemDto[]>(`/api/teams/${teamSlug}/events`);
}

export function NavEvents({
                              teamSlug,
                          }: {
    teamSlug: string,
}) {
    const router = useRouter()
    const params = useParams<{ eventSlug?: string }>();
    const activeEventSlug = params.eventSlug ?? null;

    const { data: events = [] } = useQuery({
        queryKey: ["events", teamSlug],
        queryFn: () => fetchEvents(teamSlug),
        throwOnError: false,
    });

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel className="uppercase tracking-wider">Events</SidebarGroupLabel>
            <SidebarMenu>
                {events.map((ticketedEvent) => {
                    const isActive = ticketedEvent.slug === activeEventSlug;
                    const eventDate = new Date(ticketedEvent.startsAt);
                    const dateLabel = eventDate
                        .toLocaleDateString("en-US", { month: "short", day: "numeric" })
                        .toUpperCase();

                    return (
                        <SidebarMenuItem key={ticketedEvent.slug}>
                            <button
                                onClick={() => router.push(`/teams/${teamSlug}/events/${ticketedEvent.slug}`)}
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
                <SidebarMenuItem>
                    <button
                        className="side-item text-muted-foreground"
                        onClick={() => router.push(`/teams/${teamSlug}/events/new`)}
                    >
                        <Plus className="size-3.5" />
                        <span>New event</span>
                    </button>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarGroup>
    )
}
