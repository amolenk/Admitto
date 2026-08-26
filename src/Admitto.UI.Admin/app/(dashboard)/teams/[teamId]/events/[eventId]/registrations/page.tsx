"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, ArrowUpDown, Download, Plus } from "lucide-react";
import { toast } from "sonner";
import {
    RegistrationListItemDto,
    TicketedEventDetailsDto,
    TicketTypeDto,
} from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { AddRegistrationSheet } from "./add-registration-sheet";

const PAGE_SIZE = 25;

type SortKey = "attendee" | "ticket" | "registered";
type SortDir = "asc" | "desc";

async function fetchRegistrations(teamId: string, eventId: string) {
    return apiClient.get<RegistrationListItemDto[]>(
        `/api/teams/${teamId}/events/${eventId}/registrations`,
    );
}

async function fetchEvent(teamId: string, eventId: string) {
    return apiClient.get<TicketedEventDetailsDto>(
        `/api/teams/${teamId}/events/${eventId}`,
    );
}

async function fetchTicketTypes(teamId: string, eventId: string) {
    return apiClient.get<TicketTypeDto[]>(
        `/api/teams/${teamId}/events/${eventId}/ticket-types`,
    );
}

function attendeeFullName(r: RegistrationListItemDto) {
    const full = [r.firstName, r.lastName].filter(Boolean).join(" ").trim();
    if (full) return full;
    const at = r.email.indexOf("@");
    return at > 0 ? r.email.slice(0, at) : r.email;
}

function attendeeSortKey(r: RegistrationListItemDto) {
    return [r.lastName ?? "", r.firstName ?? "", r.email].join(" ").toLowerCase();
}

export default function RegistrationsPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

    const registrationsQuery = useQuery({
        queryKey: ["registrations", teamId, eventId],
        queryFn: () => fetchRegistrations(teamId, eventId),
        throwOnError: false,
    });

    const eventQuery = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () => fetchEvent(teamId, eventId),
        throwOnError: false,
    });

    const ticketTypesQuery = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () => fetchTicketTypes(teamId, eventId),
        throwOnError: false,
    });

    const [search, setSearch] = useState("");
    const [ticketFilter, setTicketFilter] = useState<string>("all");
    const [sortKey, setSortKey] = useState<SortKey>("attendee");
    const [sortDir, setSortDir] = useState<SortDir>("asc");
    const [page, setPage] = useState(1);
    const [addOpen, setAddOpen] = useState(false);
    const queryClient = useQueryClient();

    const registrations = registrationsQuery.data;
    const ticketTypes = ticketTypesQuery.data ?? [];

    const filtered = useMemo(() => {
        const needle = search.trim().toLowerCase();
        return (registrations ?? []).filter((r) => {
            if (needle) {
                const haystack = [
                    r.email,
                    r.firstName ?? "",
                    r.lastName ?? "",
                ].join(" ").toLowerCase();
                if (!haystack.includes(needle)) return false;
            }
            if (ticketFilter !== "all" && !r.tickets.some((t) => t.id === ticketFilter)) {
                return false;
            }
            return true;
        });
    }, [registrations, search, ticketFilter]);

    const sorted = useMemo(() => {
        const arr = [...filtered];
        const dir = sortDir === "asc" ? 1 : -1;
        arr.sort((a, b) => {
            switch (sortKey) {
                case "attendee":
                    return attendeeSortKey(a).localeCompare(attendeeSortKey(b)) * dir;
                case "ticket": {
                    const an = a.tickets[0]?.name ?? "";
                    const bn = b.tickets[0]?.name ?? "";
                    return an.localeCompare(bn) * dir;
                }
                case "registered":
                default:
                    return (
                        (new Date(a.createdAt).getTime() -
                            new Date(b.createdAt).getTime()) *
                        dir
                    );
            }
        });
        return arr;
    }, [filtered, sortKey, sortDir]);

    const totalPages = Math.max(1, Math.ceil(sorted.length / PAGE_SIZE));
    const currentPage = Math.min(page, totalPages);
    const pageStart = (currentPage - 1) * PAGE_SIZE;
    const pageRows = sorted.slice(pageStart, pageStart + PAGE_SIZE);

    function toggleSort(key: SortKey) {
        if (sortKey === key) {
            setSortDir(sortDir === "asc" ? "desc" : "asc");
        } else {
            setSortKey(key);
            setSortDir(key === "registered" ? "desc" : "asc");
        }
        setPage(1);
    }

    const isLoading = registrationsQuery.isLoading;

    return (
        <PageLayout>
            <div className="flex flex-wrap items-start justify-between mb-6 gap-4 gap-y-3">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Registrations
                    </div>
                    <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                        {eventQuery.data?.name ?? ""}
                    </h1>
                </div>
                <div className="flex gap-2">
                    <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                            window.location.href = `/api/teams/${teamId}/events/${eventId}/registrations/export`;
                        }}
                    >
                        <Download className="size-3.5" /> Export CSV
                    </Button>
                    <Button size="sm" onClick={() => setAddOpen(true)}>
                        <Plus className="size-3.5" /> Add registration
                    </Button>
                </div>
            </div>

            <AddRegistrationSheet
                open={addOpen}
                onOpenChange={setAddOpen}
                teamId={teamId}
                eventId={eventId}
                eventName={eventQuery.data?.name ?? ""}
                ticketTypes={ticketTypes}
                additionalDetailSchema={eventQuery.data?.additionalDetailSchema ?? []}
                onAdded={() => {
                    toast.success("Registration added.");
                    queryClient.invalidateQueries({ queryKey: ["registrations", teamId, eventId] });
                }}
            />

            <Card className="p-4">
                <div className="flex flex-wrap items-center gap-2 mb-4">
                    <Input
                        placeholder="Search email…"
                        value={search}
                        onChange={(e) => {
                            setSearch(e.target.value);
                            setPage(1);
                        }}
                        className="w-full sm:max-w-xs"
                    />
                    <Select
                        value={ticketFilter}
                        onValueChange={(v) => {
                            setTicketFilter(v);
                            setPage(1);
                        }}
                    >
                        <SelectTrigger className="w-[200px]">
                            <SelectValue placeholder="All ticket types" />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="all">All ticket types</SelectItem>
                            {ticketTypes.map((t) => (
                                <SelectItem key={t.id} value={t.id}>
                                    {t.name}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>

                {isLoading ? (
                    <div className="space-y-2">
                        <Skeleton className="h-10 w-full" />
                        <Skeleton className="h-10 w-full" />
                        <Skeleton className="h-10 w-full" />
                    </div>
                ) : (registrations?.length ?? 0) === 0 ? (
                    <div className="text-center text-sm text-muted-foreground py-12">
                        No registrations yet.
                    </div>
                ) : (
                    <>
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <SortableHead
                                        label="Attendee"
                                        active={sortKey === "attendee"}
                                        dir={sortDir}
                                        onClick={() => toggleSort("attendee")}
                                    />
                                    <SortableHead
                                        label="Ticket"
                                        active={sortKey === "ticket"}
                                        dir={sortDir}
                                        onClick={() => toggleSort("ticket")}
                                    />
                                    <TableHead>Status</TableHead>
                                    <TableHead className="hidden sm:table-cell">Reconfirm</TableHead>
                                    <SortableHead
                                        label="Registered"
                                        className="hidden sm:table-cell"
                                        active={sortKey === "registered"}
                                        dir={sortDir}
                                        onClick={() => toggleSort("registered")}
                                    />
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {pageRows.length === 0 ? (
                                    <TableRow>
                                        <TableCell colSpan={5} className="text-center text-sm text-muted-foreground py-8">
                                            No registrations match the current filters.
                                        </TableCell>
                                    </TableRow>
                                ) : (
                                    pageRows.map((r) => {
                                        const isCancelled = r.status === "cancelled";
                                        return (
                                            <TableRow key={r.id}>
                                                <TableCell>
                                                    <Link href={`/teams/${teamId}/events/${eventId}/registrations/${r.id}`} className="hover:underline">
                                                        <div className="font-medium">{attendeeFullName(r)}</div>
                                                        <div className="text-xs text-muted-foreground">{r.email}</div>
                                                    </Link>
                                                </TableCell>
                                                <TableCell>
                                                    <div className="flex flex-wrap gap-1">
                                                        {r.tickets.map((t) => (
                                                            <Badge key={t.id} variant="outline">
                                                                {t.name}
                                                            </Badge>
                                                        ))}
                                                    </div>
                                                </TableCell>
                                                <TableCell>
                                                    {isCancelled ? (
                                                        <Badge variant="outline" className="text-muted-foreground border-muted-foreground/30 bg-muted">
                                                            Cancelled
                                                        </Badge>
                                                    ) : (
                                                        <Badge variant="outline" className="text-success border-success/30 bg-success/10">
                                                            Registered
                                                        </Badge>
                                                    )}
                                                </TableCell>
                                                <TableCell className="hidden sm:table-cell text-xs">
                                                    {r.hasReconfirmed && r.reconfirmedAt ? (
                                                        <span className="font-mono tabular-nums">
                                                            {new Date(r.reconfirmedAt).toLocaleString(undefined, { hour12: false })}
                                                        </span>
                                                    ) : (
                                                        <span className="text-muted-foreground">—</span>
                                                    )}
                                                </TableCell>
                                                <TableCell className="hidden sm:table-cell font-mono tabular-nums text-xs">
                                                    {new Date(r.createdAt).toLocaleString(undefined, { hour12: false })}
                                                </TableCell>
                                            </TableRow>
                                        );
                                    })
                                )}
                            </TableBody>
                        </Table>

                        <div className="flex items-center justify-between mt-4 text-sm text-muted-foreground">
                            <div>
                                {sorted.length === 0
                                    ? "No results"
                                    : `Showing ${pageStart + 1}–${Math.min(pageStart + PAGE_SIZE, sorted.length)} of ${sorted.length}`}
                            </div>
                            <div className="flex gap-2">
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                                    disabled={currentPage <= 1}
                                >
                                    « Prev
                                </Button>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                                    disabled={currentPage >= totalPages}
                                >
                                    Next »
                                </Button>
                            </div>
                        </div>
                    </>
                )}
            </Card>
        </PageLayout>
    );
}

function SortableHead({
    label,
    active,
    dir,
    onClick,
    className,
}: {
    label: string;
    active: boolean;
    dir: SortDir;
    onClick: () => void;
    className?: string;
}) {
    const Icon = !active ? ArrowUpDown : dir === "asc" ? ArrowUp : ArrowDown;
    return (
        <TableHead className={className}>
            <button
                type="button"
                onClick={onClick}
                className="flex items-center gap-1 hover:text-foreground"
            >
                {label}
                <Icon className="size-3" />
            </button>
        </TableHead>
    );
}
