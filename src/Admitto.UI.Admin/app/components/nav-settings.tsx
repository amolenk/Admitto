"use client";

import { useRouter, usePathname } from "next/navigation";
import { Settings } from "lucide-react";
import {
    SidebarGroup,
    SidebarGroupLabel,
    SidebarMenu,
    SidebarMenuItem,
} from "@/components/ui/sidebar";

export function NavSettings({
    teamId,
    canManageTeamSettings,
}: {
    teamId: string;
    canManageTeamSettings: boolean;
}) {
    const router = useRouter();
    const pathname = usePathname();
    const isActive = pathname.startsWith(`/teams/${teamId}/settings`);

    if (!canManageTeamSettings) {
        return null;
    }

    return (
        <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel className="uppercase tracking-wider">Team</SidebarGroupLabel>
            <SidebarMenu>
                <SidebarMenuItem>
                    <button
                        onClick={() => router.push(`/teams/${teamId}/settings`)}
                        data-active={isActive ? "true" : "false"}
                        className="side-item"
                    >
                        <Settings className="size-3.5" />
                        <span>Team Settings</span>
                    </button>
                </SidebarMenuItem>
            </SidebarMenu>
        </SidebarGroup>
    );
}
