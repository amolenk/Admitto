"use client";

import * as React from "react";

import { NavUser } from "@/components/nav-user";

import {
    Sidebar,
    SidebarContent,
    SidebarFooter,
    SidebarHeader,
    SidebarRail,
    SidebarSeparator,
} from "@/components/ui/sidebar";
import { Wordmark } from "@/components/sidebar/wordmark";
import { TeamSwitcher } from "@/components/team-switcher";
import { NavEvents } from "@/components/nav-events";
import { NavEventPages } from "@/components/sidebar/nav-event-pages";
import { NavSettings } from "@/components/nav-settings";
import { useTeams } from "@/hooks/use-teams";
import { Session } from "@/lib/auth";

type AppSidebarProps = React.ComponentProps<typeof Sidebar> & {
    session: Session | null
}

export function AppSidebar({ ...props }: AppSidebarProps)
{
    const { selectedTeam } = useTeams();

    return (
        <Sidebar collapsible="offcanvas" {...props}>
            <SidebarHeader>
                <Wordmark />
                <TeamSwitcher />
            </SidebarHeader>
            <SidebarContent>
                {selectedTeam && (
                    <>
                        <NavEvents teamSlug={selectedTeam.slug} />
                        <NavEventPages teamSlug={selectedTeam.slug} />
                        <NavSettings teamSlug={selectedTeam.slug} />
                    </>
                )}
            </SidebarContent>
            <SidebarFooter>
                <SidebarSeparator />
                {props.session?.user && (
                    <NavUser user={props.session.user} />
                )}
            </SidebarFooter>
            <SidebarRail />
        </Sidebar>
    );
}
