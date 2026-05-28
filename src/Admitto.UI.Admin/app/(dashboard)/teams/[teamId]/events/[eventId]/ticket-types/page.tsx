"use client";

import Link from "next/link";
import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { TicketTypeDto, TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Plus,
    Pencil,
    Users,
} from "lucide-react";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { FormError } from "@/components/form-error";
import { AddTicketTypeForm } from "./add-ticket-type-form";
import { EditTicketTypeForm } from "./edit-ticket-type-form";

async function fetchTicketTypes(teamId: string, eventId: string): Promise<TicketTypeDto[]> {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`);
}

function TicketTypeCard({ t, teamId, eventId }: { t: TicketTypeDto; teamId: string; eventId: string }) {
    const [editOpen, setEditOpen] = useState(false);

    const cap = Number(t.maxCapacity) || 0;
    const used = Number(t.usedCapacity);
    const remaining = cap > 0 ? cap - used : 0;
    const pct = cap > 0 ? Math.round((used / cap) * 100) : 0;

    return (
        <>
            <Card className="ticket-card overflow-hidden py-3">
                <div className="px-5">
                    <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                                <h3 className="font-display text-lg font-semibold">{t.name}</h3>
                                {cap > 0 && used >= cap ? (
                                    <Badge variant="secondary">Sold out</Badge>
                                ) : (
                                    <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                        <span className="pulse-dot mr-1" style={{ width: 6, height: 6 }} />
                                        Available
                                    </Badge>
                                )}
                            </div>
                        </div>
                        <div className="flex items-center gap-1">
                            {t.waitlistEnabled && (
                                <Button variant="ghost" size="sm" asChild>
                                    <Link href={`/teams/${teamId}/events/${eventId}/ticket-types/${t.id}/waitlist`}>
                                        <Users className="size-4" />
                                    </Link>
                                </Button>
                            )}
                            <Button variant="ghost" size="sm" onClick={() => setEditOpen(true)}>
                                <Pencil className="size-4" />
                            </Button>
                        </div>
                    </div>

                    <div className="ticket-perf" aria-hidden="true" />

                    <div className="grid grid-cols-3 gap-4 mt-4">
                        <div>
                            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Registered</div>
                            <div className="font-mono tabular-nums text-[22px] font-semibold mt-0.5">{used}</div>
                        </div>
                        <div>
                            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Remaining</div>
                            <div className={`font-mono tabular-nums text-[22px] font-semibold mt-0.5 ${cap > 0 && remaining === 0 ? "text-muted-foreground" : ""}`}>
                                {cap > 0 ? remaining : "\u221E"}
                            </div>
                        </div>
                        <div>
                            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Capacity</div>
                            <div className="font-mono tabular-nums text-[22px] font-semibold mt-0.5">
                                {cap > 0 ? cap : "\u221E"}
                            </div>
                        </div>
                    </div>

                    {cap > 0 && (
                        <div className="mt-4">
                            <div className="capacity-bar">
                                <span
                                    style={{
                                        width: `${pct}%`,
                                    }}
                                />
                            </div>
                            <div className="flex justify-between text-[11px] text-muted-foreground mt-1.5 font-mono tabular-nums">
                                <span>{pct}% registered</span>
                                <span>cap {cap}</span>
                            </div>
                        </div>
                    )}

                    {t.timeSlots && t.timeSlots.length > 0 && (
                        <div className="mt-3 flex flex-wrap gap-1.5">
                            {t.timeSlots.map((slot) => (
                                <Badge key={slot} variant="outline" className="font-mono text-[11px]">
                                    {slot}
                                </Badge>
                            ))}
                        </div>
                    )}
                </div>
            </Card>

            <Dialog open={editOpen} onOpenChange={setEditOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Edit ticket type</DialogTitle>
                    </DialogHeader>
                    <EditTicketTypeForm
                        teamId={teamId}
                        eventId={eventId}
                        ticketType={t}
                        onSaved={() => setEditOpen(false)}
                        onCancel={() => setEditOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </>
    );
}

export default function TicketTypesPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const { selectedTeam } = useTeams();
    const [addOpen, setAddOpen] = useState(false);

    const { data: ticketTypes, isLoading } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () => fetchTicketTypes(teamId, eventId),
        throwOnError: false,
    });

    const event = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(
                `/api/teams/${teamId}/events/${eventId}`
            ),
    });

    const eventName = event.data?.name ?? "";

    const breadcrumbs = [
        { label: selectedTeam?.name ?? "", href: `/teams/${teamId}/settings` },
        { label: eventName, href: `/teams/${teamId}/events/${eventId}` },
        { label: "Ticket types" },
    ];

    const types = ticketTypes ?? [];
    const availableTimeSlots = Array.from(
        new Set(types.flatMap((t) => t.timeSlots ?? []))
    ).sort();

    return (
        <PageLayout title="Ticket types" breadcrumbs={breadcrumbs}>
            <div className="flex items-start justify-between mb-6">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Ticket types
                    </div>
                    <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                        {eventName}
                    </h1>
                </div>
                <Button size="sm" onClick={() => setAddOpen(true)}>
                    <Plus className="size-3.5" /> New ticket type
                </Button>
            </div>

            {isLoading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    <Skeleton className="h-64" />
                    <Skeleton className="h-64" />
                    <Skeleton className="h-64" />
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    {[...types]
                        .sort((a, b) => a.name.localeCompare(b.name))
                        .map((t) => (
                            <TicketTypeCard key={t.id} t={t} teamId={teamId} eventId={eventId} />
                        ))}
                    <button
                        onClick={() => setAddOpen(true)}
                        className="rounded-xl border border-dashed p-6 flex flex-col items-center justify-center gap-2 text-muted-foreground hover:text-foreground hover:bg-accent transition bg-grid"
                    >
                        <div className="h-10 w-10 rounded-lg border grid place-items-center bg-card">
                            <Plus className="size-4.5" />
                        </div>
                        <div className="text-sm font-medium text-foreground">Add a ticket type</div>
                    </button>
                </div>
            )}

            <Dialog open={addOpen} onOpenChange={setAddOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Add ticket type</DialogTitle>
                    </DialogHeader>
                    <AddTicketTypeForm
                        teamId={teamId}
                        eventId={eventId}
                        suggestions={availableTimeSlots}
                        onAdded={() => setAddOpen(false)}
                        onCancel={() => setAddOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </PageLayout>
    );
}
