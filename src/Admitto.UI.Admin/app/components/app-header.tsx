"use client"

import Link from "next/link"
import { SidebarTrigger } from "@/components/ui/sidebar"
import { Separator } from "@/components/ui/separator"
import { useHeader } from "@/components/header-context"
import {
    Breadcrumb,
    BreadcrumbItem,
    BreadcrumbLink,
    BreadcrumbList,
    BreadcrumbPage,
    BreadcrumbSeparator,
} from "@/components/ui/breadcrumb"
import React from "react"
import { ThemeToggle } from "@/components/theme-toggle"

export function AppHeader() {
    const { title, breadcrumbs } = useHeader()

    return (
        <header
            className="group-has-data-[collapsible=icon]/sidebar-wrapper:h-12 flex h-12 shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear">
            <div className="flex w-full items-center gap-1 px-4 lg:gap-2 lg:px-6">
                <SidebarTrigger className="-ml-1"/>
                <Separator
                    orientation="vertical"
                    className="mx-2 data-[orientation=vertical]:h-4"
                />
                {breadcrumbs.length > 0 ? (
                    <Breadcrumb>
                        <BreadcrumbList>
                            {breadcrumbs.map((crumb, i) => (
                                <React.Fragment key={i}>
                                    {i > 0 && <BreadcrumbSeparator />}
                                    <BreadcrumbItem>
                                        {i === breadcrumbs.length - 1 ? (
                                            <BreadcrumbPage>{crumb.label}</BreadcrumbPage>
                                        ) : crumb.href ? (
                                            <BreadcrumbLink asChild>
                                                <Link href={crumb.href}>{crumb.label}</Link>
                                            </BreadcrumbLink>
                                        ) : (
                                            <span className="text-muted-foreground">{crumb.label}</span>
                                        )}
                                    </BreadcrumbItem>
                                </React.Fragment>
                            ))}
                        </BreadcrumbList>
                    </Breadcrumb>
                ) : (
                    <h1 className="text-base font-medium">{title}</h1>
                )}
                <div className="ml-auto">
                    <ThemeToggle />
                </div>
            </div>
        </header>
    )
}
