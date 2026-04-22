"use client";

import { useRouter, useParams, usePathname } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
    LayoutDashboard,
    Users,
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
    { label: "Registrations", href: "/registrations", icon: Users },
    { label: "Ticket types", href: "/ticket-types", icon: Ticket },
    { label: "Emails", href: "/settings/email", icon: Mail },
    { label: "Settings", href: "/settings", icon: Settings },
];

async function fetchEvent(teamSlug: string, eventSlug: string): Promise<TicketedEventDetailsDto> {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamSlug}/events/${eventSlug}`);
}

export function NavEventPages({ teamSlug }: { teamSlug: string }) {
    const router = useRouter();
    const params = useParams<{ eventSlug?: string }>();
    const pathname = usePathname();
    const activeEventSlug = params.eventSlug ?? null;

    const { data: event } = useQuery({
        queryKey: ["event", teamSlug, activeEventSlug],
        queryFn: () => fetchEvent(teamSlug, activeEventSlug!),
        enabled: !!activeEventSlug,
        throwOnError: false,
    });

    if (!activeEventSlug) return null;

    const basePath = `/teams/${teamSlug}/events/${activeEventSlug}`;

    function isPageActive(pageHref: string): boolean {
        const fullPath = `${basePath}${pageHref}`;
        if (pageHref === "") {
            return pathname === basePath;
        }
        if (pageHref === "/settings") {
            // Settings is active for /settings and all sub-pages except /settings/email
            return pathname.startsWith(fullPath) && !pathname.startsWith(`${basePath}/settings/email`);
        }
        return pathname.startsWith(fullPath);
    }

    const eventName = event?.name ?? activeEventSlug;

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel className="uppercase tracking-wider">{eventName}</SidebarGroupLabel>
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
