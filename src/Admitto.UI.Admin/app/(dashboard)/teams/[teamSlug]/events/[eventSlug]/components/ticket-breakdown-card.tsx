"use client";

import { useRouter } from "next/navigation";
import { TicketTypeDto } from "@/lib/admitto-api/generated";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { ChevronRight, Plus } from "lucide-react";

interface TicketBreakdownCardProps {
    teamSlug: string;
    ticketTypes: TicketTypeDto[];
    isLoading: boolean;
}

export function TicketBreakdownCard({ teamSlug, ticketTypes, isLoading }: TicketBreakdownCardProps) {
    const router = useRouter();

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <Skeleton className="h-6 w-32" />
                </CardHeader>
                <CardContent>
                    <Skeleton className="h-24 w-full" />
                </CardContent>
            </Card>
        );
    }

    return (
        <Card className="p-5">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Ticket types
                    </div>
                    <h3 className="font-display text-lg font-semibold mt-0.5">Availability</h3>
                </div>
                <Button
                    variant="ghost"
                    size="sm"
                    className="text-muted-foreground"
                    onClick={() => router.push(`/teams/${teamSlug}/events/${ticketTypes[0]?.slug ? ticketTypes[0].slug.split("/")[0] : ""}/ticket-types`)}
                >
                    Manage <ChevronRight className="size-3 ml-1" />
                </Button>
            </div>
            <div className="flex flex-col gap-3.5">
                {ticketTypes.filter(t => !t.isCancelled).map((t) => {
                    const cap = Number(t.maxCapacity) || 0;
                    const used = Number(t.usedCapacity);
                    const pct = cap > 0 ? Math.round((used / cap) * 100) : 0;
                    const isFull = cap > 0 && used >= cap;

                    return (
                        <div key={t.slug}>
                            <div className="flex items-baseline justify-between mb-1.5">
                                <div className="flex items-center gap-2">
                                    <span className="text-sm font-medium">{t.name}</span>
                                    {isFull ? (
                                        <Badge variant="secondary" className="text-[0.68rem]">
                                            Sold out
                                        </Badge>
                                    ) : (
                                        <Badge variant="outline" className="text-[0.68rem] text-primary border-primary/30 bg-primary/5">
                                            On sale
                                        </Badge>
                                    )}
                                </div>
                                <div className="text-xs text-muted-foreground">
                                    <span className="font-mono tabular-nums text-foreground font-medium">{used}</span>
                                    {cap > 0 && <> / {cap}</>}
                                </div>
                            </div>
                            {cap > 0 && (
                                <div className="capacity-bar">
                                    <span
                                        style={{
                                            width: `${pct}%`,
                                            ...(isFull ? { background: "var(--muted-foreground)", opacity: 0.5 } : {}),
                                        }}
                                    />
                                </div>
                            )}
                        </div>
                    );
                })}
                {ticketTypes.filter(t => t.isCancelled).map((t) => {
                    const cap = Number(t.maxCapacity) || 0;
                    const used = Number(t.usedCapacity);
                    const pct = cap > 0 ? Math.round((used / cap) * 100) : 0;

                    return (
                        <div key={t.slug}>
                            <div className="flex items-baseline justify-between mb-1.5">
                                <div className="flex items-center gap-2">
                                    <span className="text-sm font-medium text-muted-foreground">{t.name}</span>
                                    <Badge variant="secondary" className="text-[0.68rem]">Cancelled</Badge>
                                </div>
                                <div className="text-xs text-muted-foreground">
                                    <span className="font-mono tabular-nums font-medium">{used}</span>
                                    {cap > 0 && <> / {cap}</>}
                                </div>
                            </div>
                            {cap > 0 && (
                                <div className="capacity-bar">
                                    <span style={{ width: `${pct}%`, background: "var(--muted-foreground)", opacity: 0.5 }} />
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
            {ticketTypes.length === 0 && (
                <div className="text-center py-6">
                    <p className="text-sm text-muted-foreground">No ticket types configured yet.</p>
                    <Button variant="outline" size="sm" className="mt-3">
                        <Plus className="size-3.5" />
                        Add ticket type
                    </Button>
                </div>
            )}
        </Card>
    );
}
