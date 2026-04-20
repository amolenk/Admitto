"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Plus,
    MoreHorizontal,
    Pencil,
    Copy,
    X,
} from "lucide-react";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { FormError } from "@/components/form-error";
import { AddTicketTypeForm } from "./add-ticket-type-form";
import { EditTicketTypeForm } from "./edit-ticket-type-form";

async function fetchTicketTypes(teamSlug: string, eventSlug: string): Promise<TicketTypeDto[]> {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types`);
}

function TicketTypeCard({ t, teamSlug, eventSlug }: { t: TicketTypeDto; teamSlug: string; eventSlug: string }) {
    const queryClient = useQueryClient();
    const [editOpen, setEditOpen] = useState(false);

    const cap = Number(t.maxCapacity) || 0;
    const used = Number(t.usedCapacity);
    const remaining = cap > 0 ? cap - used : 0;
    const pct = cap > 0 ? Math.round((used / cap) * 100) : 0;

    const cancelMutation = useMutation({
        mutationFn: () =>
            apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types/${t.slug}/cancel`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["ticket-types", teamSlug, eventSlug] });
        },
    });

    return (
        <>
            <Card className="overflow-hidden">
                <div className="p-5">
                    <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                                <h3 className="font-display text-lg font-semibold">{t.name}</h3>
                                {t.isCancelled ? (
                                    <Badge variant="secondary">Cancelled</Badge>
                                ) : cap > 0 && used >= cap ? (
                                    <Badge variant="secondary">Sold out</Badge>
                                ) : (
                                    <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                        <span className="pulse-dot mr-1" style={{ width: 6, height: 6 }} />
                                        On sale
                                    </Badge>
                                )}
                            </div>
                            <p className="text-xs text-muted-foreground font-mono">{t.slug}</p>
                        </div>
                        <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                                <Button variant="ghost" size="sm">
                                    <MoreHorizontal className="size-4" />
                                </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                                {!t.isCancelled && (
                                    <>
                                        <DropdownMenuItem onClick={() => setEditOpen(true)}>
                                            <Pencil className="size-3.5 mr-2" /> Edit
                                        </DropdownMenuItem>
                                        <DropdownMenuSeparator />
                                        <DropdownMenuItem
                                            className="text-destructive"
                                            onClick={() => cancelMutation.mutate()}
                                        >
                                            <X className="size-3.5 mr-2" /> Cancel ticket type
                                        </DropdownMenuItem>
                                    </>
                                )}
                            </DropdownMenuContent>
                        </DropdownMenu>
                    </div>

                    <div className="grid grid-cols-3 gap-4 mt-4">
                        <div>
                            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">Sold</div>
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
                                        ...(t.isCancelled ? { background: "var(--muted-foreground)", opacity: 0.5 } : {}),
                                    }}
                                />
                            </div>
                            <div className="flex justify-between text-[11px] text-muted-foreground mt-1.5 font-mono tabular-nums">
                                <span>{pct}% sold</span>
                                <span>cap {cap}</span>
                            </div>
                        </div>
                    )}
                </div>
                {!t.isCancelled && (
                    <div className="border-t px-5 py-3 flex items-center gap-2 bg-muted">
                        <Button variant="ghost" size="sm" onClick={() => setEditOpen(true)}>
                            <Pencil className="size-3.5" /> Edit
                        </Button>
                        <div className="flex-1" />
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-destructive"
                            onClick={() => cancelMutation.mutate()}
                            disabled={cancelMutation.isPending}
                        >
                            Cancel sales
                        </Button>
                    </div>
                )}
            </Card>

            <Dialog open={editOpen} onOpenChange={setEditOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Edit ticket type</DialogTitle>
                    </DialogHeader>
                    <EditTicketTypeForm
                        teamSlug={teamSlug}
                        eventSlug={eventSlug}
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
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const { selectedTeam } = useTeams();
    const [addOpen, setAddOpen] = useState(false);

    const { data: ticketTypes, isLoading } = useQuery({
        queryKey: ["ticket-types", teamSlug, eventSlug],
        queryFn: () => fetchTicketTypes(teamSlug, eventSlug),
        throwOnError: false,
    });

    const breadcrumbs = [
        { label: selectedTeam?.name ?? teamSlug, href: `/teams/${teamSlug}/settings` },
        { label: eventSlug, href: `/teams/${teamSlug}/events/${eventSlug}` },
        { label: "Ticket types" },
    ];

    const types = ticketTypes ?? [];
    const totalSold = types.reduce((s, t) => s + Number(t.usedCapacity), 0);
    const totalCap = types.reduce((s, t) => s + (Number(t.maxCapacity) || 0), 0);

    return (
        <PageLayout title="Ticket types" breadcrumbs={breadcrumbs}>
            <div className="flex items-start justify-between mb-6">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Ticket types
                    </div>
                    <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                        Tickets
                    </h1>
                    {!isLoading && (
                        <p className="text-[13.5px] text-muted-foreground mt-1">
                            <span className="font-mono tabular-nums text-foreground font-medium">{totalSold}</span> sold
                            {totalCap > 0 && (
                                <> of <span className="font-mono tabular-nums">{totalCap}</span></>
                            )}{" "}
                            across <span className="font-mono tabular-nums">{types.length}</span> ticket types.
                        </p>
                    )}
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
                    {types.map((t) => (
                        <TicketTypeCard key={t.slug} t={t} teamSlug={teamSlug} eventSlug={eventSlug} />
                    ))}
                    <button
                        onClick={() => setAddOpen(true)}
                        className="rounded-xl border border-dashed p-6 flex flex-col items-center justify-center gap-2 text-muted-foreground hover:text-foreground hover:bg-accent transition bg-grid"
                        style={{ minHeight: 260 }}
                    >
                        <div className="h-10 w-10 rounded-lg border grid place-items-center bg-card">
                            <Plus className="size-4.5" />
                        </div>
                        <div className="text-sm font-medium text-foreground">Add a ticket type</div>
                        <div className="text-xs text-center max-w-[200px]">
                            Free, paid, early-bird, or invite-only — all supported.
                        </div>
                    </button>
                </div>
            )}

            <Dialog open={addOpen} onOpenChange={setAddOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Add ticket type</DialogTitle>
                    </DialogHeader>
                    <AddTicketTypeForm
                        teamSlug={teamSlug}
                        eventSlug={eventSlug}
                        onAdded={() => setAddOpen(false)}
                        onCancel={() => setAddOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </PageLayout>
    );
}
