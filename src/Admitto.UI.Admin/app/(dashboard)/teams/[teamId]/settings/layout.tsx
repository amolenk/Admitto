"use client";

import Link from "next/link";
import { useParams, usePathname } from "next/navigation";
import { cn } from "@/lib/utils";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Settings, Users, Trash2, Mail, KeyRound } from "lucide-react";

const navItems = [
    { label: "General", href: "", icon: Settings, desc: "Team name, email address", exact: true },
    { label: "Members", href: "/members", icon: Users, desc: "Roles and access" },
    { label: "Email", href: "/email", icon: Mail, desc: "SMTP and sender identity", exact: true },
    { label: "Email templates", href: "/email/templates", icon: Mail, desc: "Customize email content" },
    { label: "API Keys", href: "/api-keys", icon: KeyRound, desc: "Public API authentication" },
    { label: "Danger zone", href: "/danger", icon: Trash2, desc: "Archive team" },
];

export default function SettingsLayout({ children }: { children: React.ReactNode }) {
    const params = useParams<{ teamId: string }>();
    const pathname = usePathname();
    const basePath = `/teams/${params.teamId}/settings`;
    const { selectedTeam } = useTeams();

    const breadcrumbs = [
        { label: selectedTeam?.name ?? params.teamId, href: `/teams/${params.teamId}/settings` },
        { label: "Settings" },
    ];

    return (
        <PageLayout title="Team settings" breadcrumbs={breadcrumbs}>
            <div className="mb-5">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Settings
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {selectedTeam?.name ?? params.teamId}
                </h1>
            </div>
            <div className="grid grid-cols-12 gap-8">
                <div className="col-span-12 lg:col-span-3">
                    <nav className="flex flex-col gap-1">
                        {navItems.map((item) => {
                            const fullHref = `${basePath}${item.href}`;
                            const isActive = item.exact
                                ? pathname === fullHref
                                : pathname === fullHref || pathname.startsWith(`${fullHref}/`);
                            const Icon = item.icon;
                            return (
                                <Link
                                    key={item.label}
                                    href={fullHref}
                                    className={cn(
                                        "flex flex-col items-start rounded-md px-3 py-2.5 text-sm transition-colors border border-transparent",
                                        isActive
                                            ? "bg-card text-foreground border-border shadow-sm"
                                            : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                                    )}
                                >
                                    <div className="flex items-center gap-2 w-full">
                                        <Icon className={cn("size-3.5", isActive ? "text-primary" : "text-muted-foreground")} />
                                        <span className="font-medium">{item.label}</span>
                                        {isActive && <span className="ml-auto h-1.5 w-1.5 rounded-full bg-primary" />}
                                    </div>
                                    <div className="text-[11.5px] text-muted-foreground pl-6">{item.desc}</div>
                                </Link>
                            );
                        })}
                    </nav>
                </div>
                <div className="col-span-12 lg:col-span-9">{children}</div>
            </div>
        </PageLayout>
    );
}
