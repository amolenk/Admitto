"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useQueries, useQuery } from "@tanstack/react-query";
import { Hourglass } from "lucide-react";
import { TicketTypeDto, TicketedEventDetailsDto, WaitlistDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Card } from "@/components/ui/card";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

export default function WaitlistOverviewPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const router = useRouter();

    const { data: event } = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`),
    });

    const { data: ticketTypes, isLoading: isLoadingTypes } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`),
    });

    const waitlistTypes = (ticketTypes ?? []).filter((t) => t.waitlistEnabled);

    const waitlistQueries = useQueries({
        queries: waitlistTypes.map((t) => ({
            queryKey: ["waitlist", teamId, eventId, t.id],
            queryFn: () =>
                apiClient.get<WaitlistDetailsDto>(
                    `/api/teams/${teamId}/events/${eventId}/ticket-types/${t.id}/waitlist`
                ),
            throwOnError: false,
        })),
    });

    const isLoadingWaitlists = waitlistQueries.some((q) => q.isLoading);
    const isLoading = isLoadingTypes || isLoadingWaitlists;

    const eventName = event?.name ?? "";

    return (
        <PageLayout>
            <div className="mb-6">
                <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                    Waitlist
                </div>
                <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                    {eventName}
                </h1>
            </div>

            <Card className="p-4">
                {isLoading ? (
                    <div className="space-y-2">
                        <Skeleton className="h-10 w-full" />
                        <Skeleton className="h-10 w-full" />
                        <Skeleton className="h-10 w-full" />
                    </div>
                ) : waitlistTypes.length === 0 ? (
                    <div className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground gap-3">
                        <Hourglass className="size-10 opacity-30" />
                        <p className="text-sm font-medium">No waitlists configured</p>
                        <p className="text-sm max-w-xs">
                            Enable the waitlist on a ticket type to start collecting waitlist entries.
                        </p>
                        <Button variant="outline" size="sm" asChild>
                            <Link href={`/teams/${teamId}/events/${eventId}/ticket-types`}>
                                Go to Ticket types
                            </Link>
                        </Button>
                    </div>
                ) : (
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Ticket type</TableHead>
                                <TableHead className="text-right">Waiting</TableHead>
                                <TableHead className="text-right">Pending notifications</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {waitlistTypes.map((ticketType, i) => {
                                const stats = waitlistQueries[i]?.data?.stats;
                                const href = `/teams/${teamId}/events/${eventId}/ticket-types/${ticketType.id}/waitlist`;
                                return (
                                    <TableRow
                                        key={ticketType.id}
                                        className="cursor-pointer"
                                        onClick={() => router.push(href)}
                                    >
                                        <TableCell className="font-medium">
                                            {ticketType.name}
                                        </TableCell>
                                        <TableCell className="text-right font-mono tabular-nums">
                                            {stats ? Number(stats.totalWaiting) : "—"}
                                        </TableCell>
                                        <TableCell className="text-right font-mono tabular-nums">
                                            {stats ? Number(stats.totalPending) : "—"}
                                        </TableCell>
                                    </TableRow>
                                );
                            })}
                        </TableBody>
                    </Table>
                )}
            </Card>
        </PageLayout>
    );
}
