"use client";

import { TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Calendar, Clock, Globe, Copy } from "lucide-react";
import { Button } from "@/components/ui/button";

function formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString("en-US", {
        weekday: "long",
        year: "numeric",
        month: "long",
        day: "numeric",
    });
}

function formatTime(startsAt: string, endsAt: string): string {
    const fmt = (iso: string) =>
        new Date(iso).toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit", hour12: false });
    return `${fmt(startsAt)} \u2013 ${fmt(endsAt)}`;
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

function statusLabel(status: string): string {
    if (!status) return "";
    return status.charAt(0).toUpperCase() + status.slice(1).toLowerCase();
}

export function EventHeroCard({ event, openStatus, ticketTypes }: EventHeroCardProps) {
    const days = daysUntil(event.startsAt);
    const normalizedStatus = (event.status ?? "").toLowerCase();
    const isActive = normalizedStatus === "active";
    const isOpen = openStatus?.isOpen ?? false;

    const totalCapacity = ticketTypes
        ?.reduce((sum, t) => sum + (Number(t.maxCapacity) || 0), 0) ?? 0;
    const totalUsed = ticketTypes
        ?.reduce((sum, t) => sum + Number(t.usedCapacity), 0) ?? 0;
    const capacityPct = totalCapacity > 0 ? Math.round((totalUsed / totalCapacity) * 100) : 0;

    return (
        <Card className="overflow-hidden gap-0 py-0">
            <div className="hero-gradient p-7">
                <div className="flex items-start justify-between gap-6">
                    <div className="min-w-0">
                        <div className="flex items-center gap-2 mb-3">
                            {isActive ? (
                                <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                    <span className="pulse-dot mr-1" />
                                    {isOpen ? "Registration open" : "Active"}
                                </Badge>
                            ) : (
                                <Badge variant="secondary">{statusLabel(event.status)}</Badge>
                            )}
                            <Badge variant="outline" className="text-muted-foreground">
                                <Clock className="size-3 mr-1" />
                                {days} days to go
                            </Badge>
                        </div>
                        <h1 className="font-display text-[40px] leading-[1.05] font-semibold tracking-tight">
                            {event.name}
                        </h1>
                        <div className="mt-5 flex flex-wrap gap-x-6 gap-y-2 text-[13.5px]">
                            <div className="flex items-center gap-1.5">
                                <Calendar className="size-3.5 text-muted-foreground" />
                                <span>{formatDate(event.startsAt)}</span>
                            </div>
                            <div className="flex items-center gap-1.5">
                                <Clock className="size-3.5 text-muted-foreground" />
                                <span>{formatTime(event.startsAt, event.endsAt)}</span>
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
                    <div className="flex flex-col items-end gap-2 shrink-0">
                        <Button variant="outline" size="sm">
                            <Copy className="size-3.5" />
                            Copy link
                        </Button>
                    </div>
                </div>
            </div>
            <div className="ticket-perf" />
            <div className="grid grid-cols-2 md:grid-cols-4 divide-x">
                <HeroStat
                    label="Registered"
                    value={totalUsed}
                    sub={totalCapacity > 0 ? `of ${totalCapacity}` : "total"}
                    pct={totalCapacity > 0 ? capacityPct : undefined}
                />
                <HeroStat
                    label="Ticket types"
                    value={ticketTypes?.length ?? 0}
                    sub="configured"
                />
                <HeroStat
                    label="Available"
                    value={totalCapacity > 0 ? totalCapacity - totalUsed : "\u2014"}
                    sub={totalCapacity > 0 ? "remaining" : "no cap set"}
                    muted={totalCapacity === 0}
                />
                <HeroStat
                    label="Status"
                    value={isOpen ? "Open" : "Closed"}
                    sub={isActive ? "registration" : "event inactive"}
                    muted={!isOpen}
                />
            </div>
        </Card>
    );
}
