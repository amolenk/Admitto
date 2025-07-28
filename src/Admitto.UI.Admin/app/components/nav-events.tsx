"use client"

import { useRouter } from "next/navigation"

import { SquarePlus, } from "lucide-react"
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    useSidebar,
} from "@/components/ui/sidebar"

export function NavEvents({
                              teamId,
                              events,
                          }: {
    teamId: string,
    events: {
        id: string
        name: string
    }[]
}) {
    const {isMobile} = useSidebar()
    const router = useRouter()

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel>Events</SidebarGroupLabel>
            <SidebarMenu>
                {events.map((item) => (
                    <SidebarMenuItem key={item.id}>
                        <SidebarMenuButton
                            asChild
                            onClick={() => router.push(`/teams/${teamId}/events/${item.id}`)}
                        >
                            <a href="#">{item.name}</a>
                        </SidebarMenuButton>
                    </SidebarMenuItem>
                ))}
                <SidebarMenuItem>
                    <SidebarMenuButton
                        className="text-sidebar-foreground/70"
                        onClick={() => router.push(`/teams/${teamId}/events/add`)}
                    >
                        <SquarePlus className="text-sidebar-foreground/70"/>
                        <a href="#">New event</a>
                    </SidebarMenuButton>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarGroup>
    )
}
