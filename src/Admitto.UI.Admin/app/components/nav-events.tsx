"use client"

import {TicketedEventDto} from "@/lib/admitto-api/generated";
import { useRouter } from "next/navigation"

import { SquarePlus, } from "lucide-react"
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
} from "@/components/ui/sidebar"
import {useEffect, useState} from "react";

export function NavEvents({
                              teamSlug,
                          }: {
    teamSlug: string,
}) {
    const router = useRouter()

    const [events, setEvents] = useState<Array<TicketedEventDto>>([]);

    useEffect(() =>
    {
        async function fetchEvents()
        {
            try
            {
                const response = await fetch(`/api/teams/${teamSlug}/events`, { method: "GET" });
                if (!response.ok)
                {
                    throw new Error("Failed to fetch events");
                }

                const data = (await response.json()) as Array<TicketedEventDto>;
                setEvents(data);
            }
            catch (error)
            {
                console.error("Error fetching events:", error);
            }
        }

        fetchEvents();
    }, [teamSlug]);

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
