"use client"

import {LogOut} from "lucide-react"
import {User} from "next-auth";
import { signOut } from "next-auth/react"

import {
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem
} from "@/components/ui/sidebar"

export function NavUser({
  user,
}: {
  user: User
}) {

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <SidebarMenuButton
            size="lg"
            onClick={() => signOut()}
        >
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
