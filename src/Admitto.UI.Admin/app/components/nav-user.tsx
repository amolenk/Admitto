'use client'

import { LogOut } from "lucide-react"

import {
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem
} from "@/components/ui/sidebar"

import { authClient } from "@/lib/auth-client"
import { User } from "better-auth"
import {useRouter} from "next/navigation";

export function NavUser({
    user,
}: {
    user: User
}) {

    const router = useRouter()

    return (
        <SidebarMenu>
            <SidebarMenuItem>
                <SidebarMenuButton
                    size="lg"
                    onClick={async () => await authClient.signOut({
                        fetchOptions: {
                            onSuccess: () => {
                                router.push("/signin");
                            },
                        },
                    })}>
                    <div className="grid flex-1 text-left text-sm leading-tight">
                        <span className="truncate font-semibold">{user.name}</span>
                        <span className="truncate text-xs">{user.email}</span>
                    </div>
                    <LogOut />
                </SidebarMenuButton>
            </SidebarMenuItem>
        </SidebarMenu>
    )
}
