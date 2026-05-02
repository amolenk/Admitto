"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { TicketedEventDetailsDto, TicketedEventListItemDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { useState } from "react";
import {
    AlertDialog,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import { FormError } from "@/components/form-error";

async function fetchEvent(teamSlug: string, eventSlug: string): Promise<TicketedEventDetailsDto> {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamSlug}/events/${eventSlug}`);
}

export default function DangerZonePage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const router = useRouter();
    const queryClient = useQueryClient();
    const [isCancelling, setIsCancelling] = useState(false);
    const [isArchiving, setIsArchiving] = useState(false);
    const [archiveDialogOpen, setArchiveDialogOpen] = useState(false);
    const [confirmName, setConfirmName] = useState("");
    const [archiveError, setArchiveError] = useState<string | null>(null);

    const event = useQuery({
        queryKey: ["event", teamSlug, eventSlug],
        queryFn: () => fetchEvent(teamSlug, eventSlug),
        throwOnError: false,
    });

    async function handleCancel() {
        if (!event.data) return;
        setIsCancelling(true);
        try {
            await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/cancel`, {
                expectedVersion: Number(event.data.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
            await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
        } finally {
            setIsCancelling(false);
        }
    }

    async function handleArchive() {
        if (!event.data) return;
        setIsArchiving(true);
        setArchiveError(null);
        try {
            await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/archive`, {
                expectedVersion: Number(event.data.version),
            });
            queryClient.setQueryData<TicketedEventListItemDto[]>(
                ["events", teamSlug],
                (cached) => cached?.filter(e => e.slug !== eventSlug) ?? [],
            );
            await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
            router.push(`/teams/${teamSlug}/settings`);
        } catch (err: unknown) {
            const message = err instanceof FormError ? err.detail : err instanceof Error ? err.message : "Failed to archive event.";
            setArchiveError(message);
        } finally {
            setIsArchiving(false);
        }
    }

    const nameMatches = confirmName === event.data?.name;

    return (
        <div>
            <div className="mb-5">
                <h2 className="font-display text-[22px] font-semibold">Danger zone</h2>
                <p className="text-[13.5px] text-muted-foreground">Permanent actions. No undo.</p>
            </div>

            {archiveError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Error</AlertTitle>
                    <AlertDescription>{archiveError}</AlertDescription>
                </Alert>
            )}

            <Card className="divide-y" style={{ borderColor: "color-mix(in oklch, var(--destructive) 30%, var(--border))" }}>
                <div className="flex items-center gap-4 p-5">
                    <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium">Cancel event</div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                            Notify all registrants and stop accepting new registrations.
                        </div>
                    </div>
                    <AlertDialog>
                        <AlertDialogTrigger asChild>
                            <Button
                                variant="outline"
                                size="sm"
                                className="text-destructive border-destructive/30"
                                disabled={isCancelling || isArchiving || event.data?.status === "cancelled"}
                            >
                                Cancel event
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Cancel this event?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will stop all registrations and notify attendees. This action cannot be undone.
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Keep event</AlertDialogCancel>
                                <Button
                                    variant="destructive"
                                    onClick={handleCancel}
                                    disabled={isCancelling}
                                >
                                    {isCancelling ? "Cancelling…" : "Yes, cancel event"}
                                </Button>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
                <div className="flex items-center gap-4 p-5">
                    <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium">Archive event</div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                            Hide from the dashboard and make read-only. Can be restored.
                        </div>
                    </div>
                    <AlertDialog open={archiveDialogOpen} onOpenChange={(open) => { setArchiveDialogOpen(open); if (!open) { setConfirmName(""); setArchiveError(null); } }}>
                        <AlertDialogTrigger asChild>
                            <Button
                                variant="outline"
                                size="sm"
                                className="text-destructive border-destructive/30"
                                disabled={isCancelling || isArchiving}
                            >
                                Archive
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Archive this event?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will archive the event <strong>{event.data?.name}</strong> and make it read-only.
                                    To confirm, type the event name below:
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            {archiveError && (
                                <Alert variant="destructive">
                                    <AlertCircle className="h-4 w-4" />
                                    <AlertTitle>Error</AlertTitle>
                                    <AlertDescription>{archiveError}</AlertDescription>
                                </Alert>
                            )}
                            <div className="px-1">
                                <Input
                                    placeholder={event.data?.name}
                                    value={confirmName}
                                    onChange={(e) => setConfirmName(e.target.value)}
                                    autoFocus
                                />
                            </div>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <Button
                                    variant="destructive"
                                    onClick={handleArchive}
                                    disabled={!nameMatches || isArchiving}
                                >
                                    {isArchiving ? "Archiving…" : "I understand, archive this event"}
                                </Button>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
            </Card>
        </div>
    );
}
