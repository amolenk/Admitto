"use client";

import * as React from "react";
import { useEffect, useState } from "react";

import { useRouter } from "next/navigation";

import { NavUser } from "@/components/nav-user";

import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarRail } from "@/components/ui/sidebar";
import { TeamSwitcher } from "@/components/team-switcher";
import { NavEvents } from "@/components/nav-events";
import { NavTeam } from "@/components/nav-team";
import { useTeamStore } from "@/stores/team-store";
import { Session } from "@/lib/auth";

type AppSidebarProps = React.ComponentProps<typeof Sidebar> & {
    session: Session | null
}

export function AppSidebar({ ...props }: AppSidebarProps)
{
    const fetchTeams = useTeamStore((s) => s.fetchTeams);
    const selectedTeam = useTeamStore((s) => s.selectedTeam);
    const router = useRouter();

    useEffect(() =>
    {
        fetchTeams();
    }, [fetchTeams]);

    return (
        <Sidebar collapsible="offcanvas" {...props}>
            <SidebarHeader>
                 <TeamSwitcher />
            </SidebarHeader>
            <SidebarContent>
                {selectedTeam && (
                    <>
                        <NavEvents teamSlug={selectedTeam.slug} />
                        {/*<NavTeam />*/}
                    </>
                )}
            </SidebarContent>
            <SidebarFooter>
                {props.session?.user && (
                    <>
                        <NavUser user={props.session.user} />
                    </>
                )}
            </SidebarFooter>
            <SidebarRail />
        </Sidebar>
    );
}
