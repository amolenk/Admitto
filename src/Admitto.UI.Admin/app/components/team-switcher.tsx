"use client";

import * as React from "react";
import { useRouter } from "next/navigation";
import { ChevronsUpDown, Plus } from "lucide-react";
import { clsx } from "clsx";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar";
import { TeamDto } from "@/lib/admitto-api/generated/types.gen";
import { useTeamStore } from "@/stores/team-store";

export function TeamSwitcher(/*{teams}: TeamSwitcherProps*/)
{
    const router = useRouter();
    const { isMobile } = useSidebar();

    const { teams, selectedTeam, setSelectedTeamId, hasLoaded } = useTeamStore();

    if (!hasLoaded)
    {
        return (<div></div>);
    }

    return (
        <SidebarMenu>
            <SidebarMenuItem>
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <SidebarMenuButton
                            size="lg"
                            className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
                        >
                            <div className="grid flex-1 text-left text-sm leading-tight">
                                <span className="truncate font-semibold">
                                    {selectedTeam ? selectedTeam.name : "No teams found"}
                                </span>
                            </div>
                            <ChevronsUpDown className="ml-auto" />
                        </SidebarMenuButton>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent
                        className="w-[--radix-dropdown-menu-trigger-width] min-w-56 rounded-lg"
                        align="start"
                        side={isMobile ? "bottom" : "right"}
                        sideOffset={4}
                    >
                        {teams && teams.length > 0 ? (
                            <>
                                <DropdownMenuLabel className="text-xs text-muted-foreground">Teams</DropdownMenuLabel>
                                {teams.map((team: TeamDto) => (
                                    <DropdownMenuItem
                                        key={team.name}
                                        onClick={() => setSelectedTeamId(team.slug)}
                                        className="gap-2 p-2"
                                    >
                                        {team.name}
                                    </DropdownMenuItem>
                                ))}
                                <DropdownMenuSeparator />
                            </>
                        ) : null}
                        <DropdownMenuItem className="gap-2 p-2" onClick={() => router.push("/teams/add")}>
                            <div className="flex size-6 items-center justify-center rounded-md border bg-background">
                                <Plus className="size-4" />
                            </div>
                            <div
                                className={clsx("font-medium", { "text-muted-foreground": teams && teams.length > 0 })}>
                                Add team
                            </div>
                        </DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </SidebarMenuItem>
        </SidebarMenu>
    );
}