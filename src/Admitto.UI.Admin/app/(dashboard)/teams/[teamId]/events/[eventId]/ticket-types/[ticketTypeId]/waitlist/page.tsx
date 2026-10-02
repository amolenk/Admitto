"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { formatDistanceToNow, format } from "date-fns";
import { ArrowLeft, Clock, Crown, Trash2, ListOrdered } from "lucide-react";
import { toast } from "sonner";
import { FormError } from "@/components/form-error";
import { WaitlistDetailsDto, WaitlistEntryRow, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";

function attendeeFullName(entry: WaitlistEntryRow) {
    const full = [entry.firstName, entry.lastName].filter(Boolean).join(" ").trim();
    if (full) return full;
    const at = entry.email.indexOf("@");
    return at > 0 ? entry.email.slice(0, at) : entry.email;
}


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
    const router = useRouter();
    const queryClient = useQueryClient();
    const [promotingEntryId, setPromotingEntryId] = useState<string | null>(null);
    const [removingEntryId, setRemovingEntryId] = useState<string | null>(null);

    const { data: ticketTypes } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`),
    });

    const ticketType = ticketTypes?.find((t) => t.id === ticketTypeId);

    const waitlistUrl = `/api/teams/${teamId}/events/${eventId}/ticket-types/${ticketTypeId}/waitlist`;
    const waitlistQueryKey = ["waitlist", teamId, eventId, ticketTypeId];

    const {
        data: waitlist,
        isLoading,
        isError,
    } = useQuery({
        queryKey: waitlistQueryKey,
        queryFn: () => apiClient.get<WaitlistDetailsDto>(waitlistUrl),
        throwOnError: false,
    });

    async function removeEntry(entryId: string) {
        setRemovingEntryId(entryId);
        try {
            await apiClient.delete(`${waitlistUrl}/${entryId}`);
            toast.success("Entry removed from waitlist.");
        } catch (err) {
            toast.error(
                err instanceof FormError
                    ? err.detail
                    : "Failed to remove entry from waitlist. Please try again."
            );
        } finally {
            setRemovingEntryId(null);
            await queryClient.invalidateQueries({ queryKey: waitlistQueryKey });
        }
    }

    // Issuing a VIP coupon takes the entry out of the queue immediately, so the refreshed
    // list drops it and its coupon shows up under pending notifications instead.
    async function promoteEntry(entryId: string) {
        setPromotingEntryId(entryId);
        try {
            await apiClient.post(`${waitlistUrl}/${entryId}/promote`);
            toast.success("VIP coupon issued. The attendee will be emailed their offer.");
        } catch (err) {
            toast.error(
                err instanceof FormError
                    ? err.detail
                    : "Failed to issue VIP coupon. Please try again."
            );
        } finally {
            setPromotingEntryId(null);
            await queryClient.invalidateQueries({ queryKey: waitlistQueryKey });
        }
    }

    const stats = waitlist?.stats;

    return (
        <PageLayout>
            <div className="flex items-center gap-3 mb-6">
                <Button variant="ghost" size="sm" onClick={() => router.back()}>
                        <ArrowLeft className="size-4" />
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

            {isError ? (
                <Card className="p-8 text-center text-sm text-muted-foreground">
                    Failed to load the waitlist. Please refresh and try again.
                </Card>
            ) : isLoading ? (
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
                                <ListOrdered className="size-4" /> Active entries
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
                                            <TableHead>Name</TableHead>
                                            <TableHead>Email</TableHead>
                                            <TableHead>Joined</TableHead>
                                            <TableHead className="w-48" />
                                        </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                        {waitlist.activeEntries.map((entry) => (
                                            <TableRow
                                                key={entry.entryId}
                                                className="cursor-pointer"
                                                onClick={() =>
                                                    router.push(
                                                        `/teams/${teamId}/events/${eventId}/registrations/${entry.registrationId}?from=waitlist&ticketTypeId=${ticketTypeId}`
                                                    )
                                                }
                                            >
                                                <TableCell className="font-mono text-muted-foreground">
                                                    {entry.position}
                                                </TableCell>
                                                <TableCell className="font-medium">
                                                    {attendeeFullName(entry)}
                                                </TableCell>
                                                <TableCell className="font-mono">
                                                    {entry.email}
                                                </TableCell>
                                                <TableCell className="text-sm text-muted-foreground">
                                                    {format(new Date(entry.joinedAt), "d MMM yyyy")}
                                                </TableCell>
                                                <TableCell onClick={(e) => e.stopPropagation()}>
                                                    <div className="flex items-center justify-end gap-1">
                                                        <AlertDialog>
                                                            <AlertDialogTrigger asChild>
                                                                <Button
                                                                    variant="ghost"
                                                                    size="sm"
                                                                    disabled={
                                                                        promotingEntryId !== null ||
                                                                        removingEntryId !== null
                                                                    }
                                                                >
                                                                    <Crown className="size-4" />
                                                                    {promotingEntryId === entry.entryId
                                                                        ? "Promoting…"
                                                                        : "Promote to VIP"}
                                                                </Button>
                                                            </AlertDialogTrigger>
                                                            <AlertDialogContent>
                                                                <AlertDialogHeader>
                                                                    <AlertDialogTitle>Promote to VIP</AlertDialogTitle>
                                                                    <AlertDialogDescription>
                                                                        This issues {entry.email} a VIP offer
                                                                        right away. It can&apos;t be revoked once
                                                                        sent. It&apos;s an admin ticket on top of
                                                                        public capacity: it doesn&apos;t use a public
                                                                        seat, and nobody else on the waitlist loses
                                                                        their place because of it.
                                                                    </AlertDialogDescription>
                                                                </AlertDialogHeader>
                                                                <AlertDialogFooter>
                                                                    <AlertDialogCancel>Cancel</AlertDialogCancel>
                                                                    <AlertDialogAction
                                                                        onClick={() => promoteEntry(entry.entryId)}
                                                                    >
                                                                        Promote to VIP
                                                                    </AlertDialogAction>
                                                                </AlertDialogFooter>
                                                            </AlertDialogContent>
                                                        </AlertDialog>
                                                        <Button
                                                            variant="ghost"
                                                            size="icon"
                                                            aria-label="Remove from waitlist"
                                                            disabled={
                                                                promotingEntryId !== null ||
                                                                removingEntryId !== null
                                                            }
                                                            className="text-destructive hover:text-destructive"
                                                            onClick={() => removeEntry(entry.entryId)}
                                                        >
                                                            <Trash2 className="size-4" />
                                                        </Button>
                                                    </div>
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
