"use client";

import { RegistrationListItemDto, TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Clock, QrCode, ArrowUpRight } from "lucide-react";
import { useRouter } from "next/navigation";
import type { CheckInSummaryDto } from "@/lib/admitto-api/generated/types.gen";
import { formatInEventZone } from "@/lib/time-zones";

function daysUntil(iso: string): number {
    const now = new Date();
    const event = new Date(iso);
    return Math.max(0, Math.ceil((event.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)));
}

function formatTime(iso: string, zone: string): string {
    return formatInEventZone(iso, zone, "HH:mm");
}

interface CheckInCardProps {
    event: TicketedEventDetailsDto;
    ticketTypes: TicketTypeDto[];
    registrations?: RegistrationListItemDto[];
    summary?: CheckInSummaryDto;
}

export function CheckInCard({ event, ticketTypes, registrations, summary }: CheckInCardProps) {
    const router = useRouter();
    const days = daysUntil(event.startsAt);
    const expected = summary ? Number(summary.expectedCount) : null;
    const checkedIn = summary ? Number(summary.checkedInCount) : null;

    return (
        <Card className="p-5">
            <div className="flex items-center justify-between mb-3">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Check-in
                    </div>
                    <h3 className="font-display text-lg font-semibold mt-0.5">Event day</h3>
                </div>
                <Badge variant="outline" className="text-muted-foreground">
                    <Clock className="size-3 mr-1" />
                    {days} days
                </Badge>
            </div>
            <div className="rounded-xl border p-4 bg-grid">
                <div className="flex items-start gap-4">
                    <div className="h-14 w-14 rounded-lg bg-card border grid place-items-center shrink-0">
                        <QrCode className="size-6 text-muted-foreground" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-[13.5px] leading-relaxed">
                            Scanner is available while the event is active. The event starts at{" "}
                            <span className="font-mono font-medium">{formatTime(event.startsAt, event.timeZone)}</span>{" "}
                            in {event.timeZone}.
                        </p>
                        <div className="flex gap-2 mt-3">
                            <Button variant="outline" size="sm" onClick={() => router.push(`/teams/${event.teamId}/events/${event.id}/check-in`)}>
                                <QrCode className="size-3.5" />
                                Scanner
                                <ArrowUpRight className="size-3.5" />
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
            <div className="grid grid-cols-3 mt-4 gap-3 text-center">
                <CheckinPill n={checkedIn === null ? "—" : String(checkedIn)} label="Checked in" />
                <CheckinPill n={expected === null ? "—" : String(expected)} label="Expected" primary />
                <CheckinPill n={expected === null ? "—" : `${expected ? Math.round((checkedIn! / expected) * 100) : 0}%`} label="Complete" muted />
            </div>
        </Card>
    );
}

function CheckinPill({ n, label, primary, muted }: { n: string; label: string; primary?: boolean; muted?: boolean }) {
    return (
        <div className={`rounded-lg border py-2.5 ${primary ? "bg-primary/5" : "bg-muted"}`}>
            <div className={`font-mono tabular-nums text-lg font-semibold ${muted ? "text-muted-foreground" : primary ? "text-primary" : ""}`}>
                {n}
            </div>
            <div className="text-[11px] text-muted-foreground mt-0.5">{label}</div>
        </div>
    );
}
