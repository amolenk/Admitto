"use client";

import { useRouter, useParams, usePathname } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
    IdCard,
    LayoutDashboard,
    ClipboardList,
    Ticket,
    Mail,
    Settings,
} from "lucide-react";
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuItem,
} from "@/components/ui/sidebar";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";

const eventPages = [
    { label: "Dashboard", href: "", icon: LayoutDashboard },
    { label: "Registrations", href: "/registrations", icon: ClipboardList },
    { label: "Ticket types", href: "/ticket-types", icon: Ticket },
    { label: "Badges", href: "/badge-types", icon: IdCard },
    { label: "Emails", href: "/emails", icon: Mail },
    { label: "Settings", href: "/settings", icon: Settings },
];

async function fetchEvent(teamId: string, eventId: string): Promise<TicketedEventDetailsDto> {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`);
}

export function NavEventPages({ teamId }: { teamId: string }) {
    const router = useRouter();
    const params = useParams<{ eventId?: string }>();
    const pathname = usePathname();
    const activeEventId = params.eventId ?? null;

    const { data: event } = useQuery({
        queryKey: ["event", teamId, activeEventId],
        queryFn: () => fetchEvent(teamId, activeEventId!),
        enabled: !!activeEventId,
        throwOnError: false,
    });

    if (!activeEventId) return null;

    const basePath = `/teams/${teamId}/events/${activeEventId}`;

    function isPageActive(pageHref: string): boolean {
        const fullPath = `${basePath}${pageHref}`;
        if (pageHref === "") {
            return pathname === basePath;
        }
        if (pageHref === "/settings") {
            return pathname.startsWith(fullPath);
        }
        return pathname.startsWith(fullPath);
    }

    const eventName = event?.name;

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel className="uppercase tracking-wider">
                {eventName ?? <span className="inline-block w-24 h-3 rounded bg-muted animate-pulse" />}
            </SidebarGroupLabel>
            <SidebarMenu>
                {eventPages.map((page) => {
                    const Icon = page.icon;
                    const active = isPageActive(page.href);
                    return (
                        <SidebarMenuItem key={page.label}>
                            <button
                                onClick={() => router.push(`${basePath}${page.href}`)}
                                data-active={active ? "true" : "false"}
                                className="side-item"
                            >
                                <Icon className="size-3.5" />
                                <span>{page.label}</span>
                            </button>
                        </SidebarMenuItem>
                    );
                })}
            </SidebarMenu>
        </SidebarGroup>
    );
}
