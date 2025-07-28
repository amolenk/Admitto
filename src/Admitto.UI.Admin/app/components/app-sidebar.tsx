"use client";

import * as React from "react";
import { useEffect, useState } from "react";

import { useRouter } from "next/navigation";

import { NavUser } from "@/components/nav-user";

import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarRail } from "@/components/ui/sidebar";
import { Session } from "next-auth";
import { TeamSwitcher } from "@/components/team-switcher";
import { NavEvents } from "@/components/nav-events";
import { NavTeam } from "@/components/nav-team";
import { useTeamStore } from "@/stores/team-store";

type AppSidebarProps = React.ComponentProps<typeof Sidebar> & {
    session: Session | null
}

export function AppSidebar({ ...props }: AppSidebarProps)
{

    const fetchTeams = useTeamStore((s) => s.fetchTeams);
    const selectedTeam = useTeamStore((s) => s.selectedTeam);
    const [events, setEvents] = useState([]);
    const router = useRouter();

    useEffect(() =>
    {
        fetchTeams();
    }, [fetchTeams]);

    useEffect(() =>
    {
        async function fetchEvents()
        {
            if (selectedTeam)
            {
                try
                {

                    const response = await fetch(`/api/teams/${selectedTeam.id}/events`, { method: "GET" });
                    if (!response.ok)
                    {
                        console.log(response);
                        throw new Error("Failed to fetch events");
                    }
                    const data = await response.json();
                    setEvents(data);
                }
                catch (error)
                {
                    console.error("Error fetching events:", error);
                }

                // router.push("/")
            }
        }

        fetchEvents();
    }, [selectedTeam, router]);

    return (
        <Sidebar collapsible="offcanvas" {...props}>
            <SidebarHeader>
                <TeamSwitcher />
            </SidebarHeader>
            <SidebarContent>
                {selectedTeam && (
                    <>
                        <NavEvents teamId={selectedTeam.id} events={events} />
                        <NavTeam />
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
