"use client";

import Link from "next/link";
import { useParams, usePathname } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/utils";
import { PageLayout } from "@/components/page-layout";
import { apiClient } from "@/lib/api-client";
import { useTeams } from "@/hooks/use-teams";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { Settings, Users, Mail, Zap, Trash2 } from "lucide-react";

const navItems = [
    { label: "General", href: "", icon: Settings, desc: "Name, date, venue, website" },
    { label: "Registration", href: "/registration", icon: Users, desc: "Policy, windows, waitlist" },
    { label: "Cancellation", href: "/cancellation", icon: Zap, desc: "Late cancellation cutoff" },
    { label: "Reconfirmation", href: "/reconfirm", icon: Mail, desc: "Window and cadence" },
    { label: "Email", href: "/email", icon: Mail, desc: "Templates, SMTP, sender" },
    { label: "Danger zone", href: "/danger", icon: Trash2, desc: "Cancel or archive" },
];

export default function EventSettingsLayout({ children }: { children: React.ReactNode }) {
    const params = useParams<{ teamSlug: string; eventSlug: string }>();
    const pathname = usePathname();
    const basePath = `/teams/${params.teamSlug}/events/${params.eventSlug}/settings`;
    const { selectedTeam } = useTeams();

    const event = useQuery({
        queryKey: ["event", params.teamSlug, params.eventSlug],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(
                `/api/teams/${params.teamSlug}/events/${params.eventSlug}`
            ),
    });

    const eventName = event.data?.name ?? params.eventSlug;

    const breadcrumbs = [
        { label: selectedTeam?.name ?? params.teamSlug, href: `/teams/${params.teamSlug}/settings` },
        { label: eventName, href: `/teams/${params.teamSlug}/events/${params.eventSlug}` },
        { label: "Settings" },
    ];

    return (
        <PageLayout title="Event settings" breadcrumbs={breadcrumbs}>
            <div className="mb-5">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Settings
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {eventName}
                </h1>
            </div>
            <div className="grid grid-cols-12 gap-8">
                <div className="col-span-12 lg:col-span-3">
                    <nav className="flex flex-col gap-1">
                        {navItems.map((item) => {
                            const fullHref = `${basePath}${item.href}`;
                            const isActive = pathname === fullHref;
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
