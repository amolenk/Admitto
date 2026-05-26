"use client";

import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow, format } from "date-fns";
import { ArrowLeft, Clock, Trash2, Users } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { WaitlistDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

function StatCard({ label, value }: { label: string; value: number | string }) {
    return (
        <Card>
            <CardContent className="pt-6">
                <div className="text-2xl font-bold font-mono tabular-nums">{value}</div>
                <div className="text-sm text-muted-foreground mt-1">{label}</div>
            </CardContent>
        </Card>
    );
}

export default function WaitlistPage() {
    const { teamId, eventId, ticketTypeId } = useParams<{
        teamId: string;
        eventId: string;
        ticketTypeId: string;
    }>();
    const { selectedTeam } = useTeams();
    const queryClient = useQueryClient();

    const { data: ticketTypes } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`),
    });

    const ticketType = ticketTypes?.find((t) => t.id === ticketTypeId);

    const { data: waitlist, isLoading } = useQuery({
        queryKey: ["waitlist", teamId, eventId, ticketTypeId],
        queryFn: () =>
            apiClient.get<WaitlistDetailsDto>(
                `/api/teams/${teamId}/events/${eventId}/ticket-types/${ticketTypeId}/waitlist`
            ),
        throwOnError: false,
    });

    async function removeEntry(entryId: string) {
        await apiClient.delete(
            `/api/teams/${teamId}/events/${eventId}/ticket-types/${ticketTypeId}/waitlist/${entryId}`
        );
        await queryClient.invalidateQueries({
            queryKey: ["waitlist", teamId, eventId, ticketTypeId],
        });
        toast.success("Entry removed from waitlist.");
    }

    const breadcrumbs = [
        { label: selectedTeam?.name ?? "", href: `/teams/${teamId}/settings` },
        { label: "Ticket types", href: `/teams/${teamId}/events/${eventId}/ticket-types` },
        { label: ticketType?.name ?? "Waitlist" },
    ];

    const stats = waitlist?.stats;

    return (
        <PageLayout title="Waitlist" breadcrumbs={breadcrumbs}>
            <div className="flex items-center gap-3 mb-6">
                <Button variant="ghost" size="sm" asChild>
                    <Link href={`/teams/${teamId}/events/${eventId}/ticket-types`}>
                        <ArrowLeft className="size-4" />
                    </Link>
                </Button>
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Waitlist
                    </div>
                    <h1 className="font-display text-[26px] font-semibold tracking-tight leading-tight mt-0.5">
                        {ticketType?.name ?? "Loading…"}
                    </h1>
                </div>
            </div>

            {isLoading ? (
                <div className="space-y-6">
                    <div className="grid grid-cols-3 gap-4">
                        <Skeleton className="h-24" />
                        <Skeleton className="h-24" />
                        <Skeleton className="h-24" />
                    </div>
                    <Skeleton className="h-48" />
                </div>
            ) : (
                <div className="space-y-6">
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                        <StatCard
                            label="Waiting"
                            value={Number(stats?.totalWaiting ?? 0)}
                        />
                        <StatCard
                            label="Pending notifications"
                            value={Number(stats?.totalPending ?? 0)}
                        />
                        <StatCard
                            label="Sent today"
                            value={Number(stats?.sentToday ?? 0)}
                        />
                    </div>

                    <Card>
                        <CardHeader className="pb-3">
                            <CardTitle className="flex items-center gap-2 text-base font-semibold">
                                <Users className="size-4" /> Active entries
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="p-0">
                            {!waitlist?.activeEntries.length ? (
                                <p className="px-6 pb-6 text-sm text-muted-foreground">
                                    No one is currently on the waitlist.
                                </p>
                            ) : (
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead className="w-16">#</TableHead>
                                            <TableHead>Email</TableHead>
                                            <TableHead>Joined</TableHead>
                                            <TableHead className="w-12" />
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {waitlist.activeEntries.map((entry) => (
                                            <TableRow key={entry.entryId}>
                                                <TableCell className="font-mono text-muted-foreground">
                                                    {entry.position}
                                                </TableCell>
                                                <TableCell className="font-mono">
                                                    {entry.maskedEmail}
                                                </TableCell>
                                                <TableCell className="text-sm text-muted-foreground">
                                                    {format(new Date(entry.joinedAt), "d MMM yyyy")}
                                                </TableCell>
                                                <TableCell>
                                                    <Button
                                                        variant="ghost"
                                                        size="icon"
                                                        className="text-destructive hover:text-destructive"
                                                        onClick={() => removeEntry(entry.entryId)}
                                                    >
                                                        <Trash2 className="size-4" />
                                                    </Button>
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )}
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader className="pb-3">
                            <CardTitle className="flex items-center gap-2 text-base font-semibold">
                                <Clock className="size-4" /> Pending notifications
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="p-0">
                            {!waitlist?.pendingNotifications.length ? (
                                <p className="px-6 pb-6 text-sm text-muted-foreground">
                                    No pending notifications.
                                </p>
                            ) : (
                                <Table>
                                    <TableHeader>
                                        <TableRow>
                                            <TableHead>Email</TableHead>
                                            <TableHead>Expires</TableHead>
                                            <TableHead>Time left</TableHead>
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {waitlist.pendingNotifications.map((n) => (
                                            <TableRow key={n.couponId}>
                                                <TableCell className="font-mono">
                                                    {n.maskedEmail}
                                                </TableCell>
                                                <TableCell className="text-sm text-muted-foreground">
                                                    {format(new Date(n.expiresAt), "d MMM yyyy, HH:mm")}
                                                </TableCell>
                                                <TableCell className="text-sm text-muted-foreground">
                                                    {formatDistanceToNow(new Date(n.expiresAt), {
                                                        addSuffix: true,
                                                    })}
                                                </TableCell>
                                            </TableRow>
                                        ))}
                                    </TableBody>
                                </Table>
                            )}
                        </CardContent>
                    </Card>
                </div>
            )}
        </PageLayout>
    );
}
