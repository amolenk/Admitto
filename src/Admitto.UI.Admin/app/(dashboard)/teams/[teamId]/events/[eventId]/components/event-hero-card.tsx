"use client";

import { TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import type { CSSProperties } from "react";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Calendar, Clock, Globe } from "lucide-react";
import { formatInEventZone, formatZoneCaption } from "@/lib/time-zones";

function formatDate(iso: string, zone: string): string {
    return formatInEventZone(iso, zone, "EEEE, MMMM d, yyyy");
}

function daysUntil(iso: string): number {
    const now = new Date();
    const event = new Date(iso);
    return Math.max(0, Math.ceil((event.getTime() - now.getTime()) / (1000 * 60 * 60 * 24)));
}

interface HeroStatProps {
    label: string;
    value: number | string;
    sub: string;
    pct?: number;
    muted?: boolean;
}

function HeroStat({ label, value, sub, pct, muted }: HeroStatProps) {
    return (
        <div className="p-5">
            <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                {label}
            </div>
            <div className="flex items-baseline gap-2 mt-1.5">
                <span className={`font-mono tabular-nums text-[28px] font-semibold ${muted ? "text-muted-foreground" : ""}`}>
                    {value}
                </span>
                <span className="text-xs text-muted-foreground">{sub}</span>
            </div>
            {pct != null && (
                <div className="mt-3">
                    <div className="capacity-bar">
                        <span style={{ width: `${pct}%` }} />
                    </div>
                    <div className="flex justify-between text-[11px] text-muted-foreground mt-1.5">
                        <span className="font-mono tabular-nums">{pct}%</span>
                        <span>capacity</span>
                    </div>
                </div>
            )}
        </div>
    );
}

interface EventHeroCardProps {
    event: TicketedEventDetailsDto;
    openStatus?: { isOpen: boolean } | null;
    ticketTypes?: TicketTypeDto[] | null;
}

function getRegistrationStat(event: EventHeroCardProps["event"]): { value: string | number; sub: string; muted: boolean } {
    const policy = event.registrationPolicy;
    if (!policy) {
        return { value: "\u2014", sub: "window not set", muted: true };
    }
    if (event.isRegistrationOpen) {
        return { value: "Open", sub: "registration", muted: false };
    }
    const now = new Date();
    const opensAt = new Date(policy.opensAt);
    if (now < opensAt) {
        const daysLeft = Math.ceil((opensAt.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
        return { value: daysLeft, sub: "days until open", muted: false };
    }
    return { value: "Closed", sub: "registration", muted: true };
}

function statusLabel(status: string): string {
    if (!status) return "";
    return status.charAt(0).toUpperCase() + status.slice(1).toLowerCase();
}

export function EventHeroCard({ event, openStatus, ticketTypes }: EventHeroCardProps) {
    const days = daysUntil(event.startsAt);
    const isPast = new Date(event.endsAt).getTime() < Date.now();
    const isLive = !isPast && new Date(event.startsAt).getTime() <= Date.now();
    const normalizedStatus = (event.status ?? "").toLowerCase();
    const isActive = normalizedStatus === "active";
    const isOpen = openStatus?.isOpen ?? false;
    const isClosingSoon = isOpen && !isPast && days <= 7;

    const totalCapacity = ticketTypes
        ?.reduce((sum, t) => sum + (Number(t.maxCapacity) || 0), 0) ?? 0;
    const totalUsed = ticketTypes
        ?.reduce((sum, t) => sum + Number(t.usedCapacity), 0) ?? 0;
    const hasUnlimited = ticketTypes?.some(t => !Number(t.maxCapacity)) ?? false;
    const hasCapacity = totalCapacity > 0;
    const capacityPct = hasCapacity ? Math.round((totalUsed / totalCapacity) * 100) : 0;

    const regStat = getRegistrationStat(event);

    const heroAccent = isPast
        ? "var(--muted-foreground)"
        : isLive
            ? "var(--live)"
            : isClosingSoon
                ? "var(--warning)"
                : isOpen
                    ? "var(--success)"
                    : isActive
                        ? "var(--primary)"
                        : "var(--muted-foreground)";

    return (
        <Card className="overflow-hidden gap-0 py-0">
            <div
                className="hero-panel p-7"
                style={{ "--hero-accent": heroAccent } as CSSProperties}
            >
                <div className="relative z-10 flex items-start justify-between gap-6">
                    <div className="min-w-0">
                        <div className="flex items-center gap-2 mb-3">
                            {isPast ? (
                                <Badge variant="secondary">Ended</Badge>
                            ) : isLive ? (
                                <Badge variant="outline" className="text-live border-live/30 bg-live/10">
                                    <span className="pulse-dot mr-1" style={{ "--pulse-color": "var(--live)" } as CSSProperties} />
                                    Live
                                </Badge>
                            ) : isActive ? (
                                <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                    <span className="pulse-dot mr-1" />
                                    {isOpen ? "Registration open" : "Active"}
                                </Badge>
                            ) : (
                                <Badge variant="secondary">{statusLabel(event.status)}</Badge>
                            )}
                            <Badge variant="outline" className="text-muted-foreground">
                                <Clock className="size-3 mr-1" />
                                {isPast ? "Event ended" : isLive ? "Happening now" : `${days} days to go`}
                            </Badge>
                        </div>
                        <h1 className="font-display text-[28px] md:text-[40px] leading-[1.05] font-semibold tracking-tight">
                            {event.name}
                        </h1>
                        <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2 text-[13.5px]">
                            <div className="flex items-center gap-1.5">
                                <Calendar className="size-3.5 text-muted-foreground" />
                                <span>{formatDate(event.startsAt, event.timeZone)}</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <Clock className="size-3.5 text-muted-foreground" />
                                <span>{formatInEventZone(event.startsAt, event.timeZone, "HH:mm")}</span>
                                <span className="text-muted-foreground">&middot; {formatZoneCaption(event.timeZone)}</span>
                            </div>
                            {event.websiteUrl && (
                                <a
                                    className="flex items-center gap-1.5 text-primary font-medium hover:underline"
                                    href={event.websiteUrl.startsWith("http") ? event.websiteUrl : `https://${event.websiteUrl}`}
                                    target="_blank"
                                    rel="noopener noreferrer"
                                >
                                    <Globe className="size-3.5" />
                                    {event.websiteUrl.replace(/^https?:\/\//, "")}
                                </a>
                            )}
                        </div>
                    </div>
                </div>
            </div>
            <div className="grid grid-cols-2 divide-x border-t">
                <HeroStat
                    label="Status"
                    value={regStat.value}
                    sub={regStat.sub}
                    muted={regStat.muted}
                />
                <HeroStat
                    label="Registered"
                    value={totalUsed}
                    sub={hasCapacity ? `of ${totalCapacity}${hasUnlimited ? "+" : ""}` : "total"}
                    pct={hasCapacity ? capacityPct : undefined}
                />
            </div>
        </Card>
    );
}
