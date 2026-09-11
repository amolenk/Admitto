"use client";

import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { formatInEventZone } from "@/lib/time-zones";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Clock, QrCode, ArrowUpRight } from "lucide-react";
import { useRouter } from "next/navigation";
import type { CheckInSummaryDto } from "@/lib/admitto-api/generated/types.gen";

interface CheckInCardProps {
    event: TicketedEventDetailsDto;
    summary?: CheckInSummaryDto;
}

function formatStart(iso: string, zone: string): string {
    return formatInEventZone(iso, zone, "MMM d, yyyy · HH:mm");
}

function formatTime(iso: string, zone: string): string {
    return formatInEventZone(iso, zone, "HH:mm");
}

export function CheckInCard({ event, summary }: CheckInCardProps) {
    const router = useRouter();
    const isArchived = event.status === "archived";
    const expected = summary ? Number(summary.expectedCount) : null;
    const checkedIn = summary ? Number(summary.checkedInCount) : null;

    return (
        <Card className="p-5">
            <div className="mb-3 flex items-center justify-between">
                <div>
                    <div className="text-[0.6875rem] font-semibold uppercase tracking-widest text-muted-foreground">
                        Check-in
                    </div>
                    <h3 className="mt-0.5 font-display text-lg font-semibold">Event day</h3>
                </div>
                <Badge variant="outline" className="text-muted-foreground">
                    <Clock className="mr-1 size-3" />
                    {formatStart(event.startsAt, event.timeZone)}
                </Badge>
            </div>

            <div className="rounded-xl border bg-grid p-4">
                <div className="flex items-start gap-4">
                    <div className="grid h-14 w-14 shrink-0 place-items-center rounded-lg border bg-card">
                        <QrCode className="size-6 text-muted-foreground" />
                    </div>
                    <div className="min-w-0 flex-1">
                        <p className="text-[13.5px] leading-relaxed">
                            {isArchived ? (
                                "This event is archived; check-in is unavailable."
                            ) : (
                                <>
                                    Scanner is available while the event is active. The event starts at{" "}
                                    <span className="font-mono font-medium">
                                        {formatTime(event.startsAt, event.timeZone)}
                                    </span>{" "}
                                    in {event.timeZone}.
                                </>
                            )}
                        </p>
                        <div className="mt-3 flex gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                disabled={isArchived}
                                aria-label={isArchived ? "Scanner unavailable for archived event" : undefined}
                                onClick={() => router.push(`/teams/${event.teamId}/events/${event.id}/check-in`)}
                            >
                                <QrCode className="size-3.5" />
                                {isArchived ? "Scanner unavailable" : "Scanner"}
                                <ArrowUpRight className="size-3.5" />
                            </Button>
                        </div>
                    </div>
                </div>
            </div>

            <div className="mt-4 grid grid-cols-3 gap-3 text-center">
                <CheckinPill n={checkedIn === null ? "—" : String(checkedIn)} label="Checked in" />
                <CheckinPill n={expected === null ? "—" : String(expected)} label="Expected" primary />
                <CheckinPill
                    n={expected === null ? "—" : `${expected ? Math.round((checkedIn! / expected) * 100) : 0}%`}
                    label="Complete"
                    muted
                />
            </div>
        </Card>
    );
}

function CheckinPill({
    n,
    label,
    primary,
    muted,
}: {
    n: string;
    label: string;
    primary?: boolean;
    muted?: boolean;
}) {
    return (
        <div className={`rounded-lg border py-2.5 ${primary ? "bg-primary/5" : "bg-muted"}`}>
            <div
                className={`font-mono text-lg font-semibold tabular-nums ${
                    muted ? "text-muted-foreground" : primary ? "text-primary" : ""
                }`}
            >
                {n}
            </div>
            <div className="mt-0.5 text-[11px] text-muted-foreground">{label}</div>
        </div>
    );
}
