"use client";

import { TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Clock, QrCode, Copy } from "lucide-react";
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
}

export function CheckInCard({ event, ticketTypes }: CheckInCardProps) {
    const days = daysUntil(event.startsAt);
    const totalUsed = ticketTypes.reduce((sum, t) => sum + Number(t.usedCapacity), 0);

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
                            Check-in opens automatically at{" "}
                            <span className="font-mono font-medium">{formatTime(event.startsAt, event.timeZone)}</span>{" "}
                            on event day. Share the QR scanner link with your door team.
                        </p>
                        <div className="flex gap-2 mt-3">
                            <Button variant="outline" size="sm">
                                <QrCode className="size-3.5" />
                                Scanner
                            </Button>
                            <Button variant="ghost" size="sm" className="text-muted-foreground">
                                <Copy className="size-3.5" />
                                Share link
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
            <div className="grid grid-cols-3 mt-4 gap-3 text-center">
                <CheckinPill n="0" label="Checked in" />
                <CheckinPill n={String(totalUsed)} label="Expected" primary />
                <CheckinPill n="0%" label="Complete" muted />
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
