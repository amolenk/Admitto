'use client'

import { LogOut, ChevronsUpDown } from "lucide-react"

import {
    SidebarMenu,
    SidebarMenuItem,
} from "@/components/ui/sidebar"
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

import { authClient } from "@/lib/auth-client"
import { User } from "better-auth"
import { useRouter } from "next/navigation";

function getInitials(name: string): string {
    return name
        .split(/\s+/)
        .map((w) => w[0])
        .slice(0, 2)
        .join("")
        .toUpperCase();
}

export function NavUser({
    user,
}: {
    user: User
}) {

    const router = useRouter()

    return (
        <SidebarMenu>
            <SidebarMenuItem>
                <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                        <button className="side-item w-full">
                            <div className="h-6 w-6 rounded-full bg-primary/15 text-primary grid place-items-center text-[11px] font-semibold shrink-0">
                                {getInitials(user.name)}
                            </div>
                            <div className="flex flex-col leading-tight min-w-0 text-left flex-1">
                                <span className="text-[12.5px] font-medium truncate">{user.name}</span>
                                <span className="text-[10.5px] text-muted-foreground truncate">{user.email}</span>
                            </div>
                            <ChevronsUpDown className="size-3 text-muted-foreground shrink-0" />
                        </button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent
                        className="w-[--radix-dropdown-menu-trigger-width] min-w-48 rounded-lg"
                        align="start"
                        side="top"
                        sideOffset={4}
                    >
                        <div className="px-3 py-2">
                            <p className="text-sm font-medium">{user.name}</p>
                            <p className="text-xs text-muted-foreground">{user.email}</p>
                        </div>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                            onClick={async () => await authClient.signOut({
                                fetchOptions: {
                                    onSuccess: () => {
                                        router.push("/signin");
                                    },
                                },
                            })}
                        >
                            <LogOut className="size-3.5 mr-2" />
                            Sign out
                        </DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </SidebarMenuItem>
        </SidebarMenu>
    )
}
