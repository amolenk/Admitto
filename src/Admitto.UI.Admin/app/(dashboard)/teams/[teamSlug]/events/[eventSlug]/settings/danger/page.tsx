"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useState } from "react";
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

async function fetchEvent(teamSlug: string, eventSlug: string): Promise<TicketedEventDetailsDto> {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamSlug}/events/${eventSlug}`);
}

export default function DangerZonePage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const router = useRouter();
    const queryClient = useQueryClient();
    const [isSubmitting, setIsSubmitting] = useState(false);

    const event = useQuery({
        queryKey: ["event", teamSlug, eventSlug],
        queryFn: () => fetchEvent(teamSlug, eventSlug),
        throwOnError: false,
    });

    async function handleCancel() {
        if (!event.data) return;
        setIsSubmitting(true);
        try {
            await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/cancel`, {
                expectedVersion: Number(event.data.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
            await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleArchive() {
        if (!event.data) return;
        setIsSubmitting(true);
        try {
            await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/archive`, {
                expectedVersion: Number(event.data.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
            router.push(`/teams/${teamSlug}/settings`);
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <div>
            <div className="mb-5">
                <h2 className="font-display text-[22px] font-semibold">Danger zone</h2>
                <p className="text-[13.5px] text-muted-foreground">Permanent actions. No undo.</p>
            </div>
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
                                disabled={isSubmitting || event.data?.status === "cancelled"}
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
                                <AlertDialogAction onClick={handleCancel} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                                    Yes, cancel event
                                </AlertDialogAction>
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
                    <AlertDialog>
                        <AlertDialogTrigger asChild>
                            <Button
                                variant="outline"
                                size="sm"
                                className="text-destructive border-destructive/30"
                                disabled={isSubmitting}
                            >
                                Archive
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Archive this event?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    The event will be hidden from the dashboard and become read-only.
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <AlertDialogAction onClick={handleArchive} className="bg-destructive text-destructive-foreground hover:bg-destructive/90">
                                    Yes, archive
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
            </Card>
        </div>
    );
}
