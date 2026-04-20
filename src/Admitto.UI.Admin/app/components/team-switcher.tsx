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
import { SidebarMenu, SidebarMenuItem, useSidebar } from "@/components/ui/sidebar";
import { TeamListItemDto } from "@/lib/admitto-api/generated/types.gen";
import { useTeams } from "@/hooks/use-teams";

function getInitials(name: string): string {
    return name
        .split(/\s+/)
        .map((w) => w[0])
        .slice(0, 2)
        .join("")
        .toUpperCase();
}

export function TeamSwitcher()
{
    const router = useRouter();
    const { isMobile } = useSidebar();

    const { teams, selectedTeam, isLoading, setSelectedTeamSlug } = useTeams();

    if (isLoading)
    {
        return (<div></div>);
    }

    function handleSelectTeam(slug: string) {
        if (slug !== selectedTeam?.slug) {
            setSelectedTeamSlug(slug);
            router.push("/");
        }
    }

    return (
        <SidebarMenu>
            <SidebarMenuItem>
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <button className="flex items-center justify-between w-full rounded-md px-2.5 py-2 text-left text-sm transition-colors hover:bg-sidebar-accent border border-transparent hover:border-border">
                            <div className="flex items-center gap-2.5 min-w-0">
                                <div className="grid place-items-center h-7 w-7 rounded-md bg-primary/15 text-primary font-display font-semibold text-[13px] shrink-0">
                                    {selectedTeam ? getInitials(selectedTeam.name) : "?"}
                                </div>
                                <span className="text-[13px] font-medium truncate">
                                    {selectedTeam ? selectedTeam.name : "No teams found"}
                                </span>
                            </div>
                            <ChevronsUpDown className="size-3.5 text-muted-foreground shrink-0" />
                        </button>
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
                                {teams.map((team: TeamListItemDto) => (
                                    <DropdownMenuItem
                                        key={team.name}
                                        onClick={() => handleSelectTeam(team.slug)}
                                        className="gap-2 p-2"
                                    >
                                        <div className="grid place-items-center h-6 w-6 rounded-md bg-primary/15 text-primary font-semibold text-[11px] shrink-0">
                                            {getInitials(team.name)}
                                        </div>
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
