"use client"

import {TicketedEventListItemDto} from "@/lib/admitto-api/generated";
import { useRouter } from "next/navigation"

import { SquarePlus, } from "lucide-react"
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
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

    const { data: events = [] } = useQuery({
        queryKey: ["events", teamSlug],
        queryFn: () => fetchEvents(teamSlug),
        throwOnError: false,
    });

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel>Events</SidebarGroupLabel>
            <SidebarMenu>
                {events.map((ticketedEvent) => (
                    <SidebarMenuItem key={ticketedEvent.slug}>
                        <SidebarMenuButton
                            asChild
                            onClick={() => router.push(`/teams/${teamSlug}/events/${ticketedEvent.slug}`)}
                        >
                            <a href="#">{ticketedEvent.name}</a>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                ))}
                <SidebarMenuItem>
                    <SidebarMenuButton
                        className="text-sidebar-foreground/70"
                        onClick={() => router.push(`/teams/${teamSlug}/events/add`)}
                    >
                        <SquarePlus className="text-sidebar-foreground/70"/>
                        <a href="#">New event</a>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarGroup>
    )
}
