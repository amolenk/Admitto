"use client";

import { useParams, usePathname } from "next/navigation";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { cn } from "@/lib/utils";
import { PageLayout } from "@/components/page-layout";

export default function EmailsLayout({ children }: { children: React.ReactNode }) {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const pathname = usePathname();
    const base = `/teams/${teamId}/events/${eventId}/emails`;

    const { data: event } = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`),
    });

    const eventName = event?.name ?? "";

    const tabs = [
        { label: "Bulk emails", href: `${base}/campaigns` },
        { label: "Templates", href: `${base}/templates` },
        { label: "Sending", href: `${base}/setup` },
    ];

    return (
        <PageLayout>
            <div className="mb-6">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Emails
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {eventName}
                </h1>
            </div>
            <div className="flex gap-1 border-b mb-6">
                {tabs.map((tab) => {
                    const active = pathname.startsWith(tab.href);
                    return (
                        <Link
                            key={tab.href}
                            href={tab.href}
                            className={cn(
                                "px-3 py-2 text-sm font-medium border-b-2 -mb-px transition-colors",
                                active
                                    ? "border-primary text-foreground"
                                    : "border-transparent text-muted-foreground hover:text-foreground"
                            )}
                        >
                            {tab.label}
                        </Link>
                    );
                })}
            </div>
            {children}
        </PageLayout>
    );
}
