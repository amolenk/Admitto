"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { CheckCircle2, Info } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetails, isEventActive } from "../event-detail-types";
import { EventStatusBanner } from "../event-status-banner";
import { CancellationPolicyForm } from "./cancellation-policy-form";

export default function CancellationPolicyPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    const event = useQuery({
        queryKey: ["event", teamSlug, eventSlug],
        queryFn: () =>
            apiClient.get<TicketedEventDetails>(`/api/teams/${teamSlug}/events/${eventSlug}`),
    });

    if (event.isLoading) {
        return <Skeleton className="h-64 w-full" />;
    }

    if (event.error || !event.data) {
        return <p className="text-destructive">Failed to load event details.</p>;
    }

    const disabled = !isEventActive(event.data.status);
    const policy = event.data.cancellationPolicy;

    return (
        <div className="space-y-6 max-w-lg">
            <EventStatusBanner status={event.data.status} />

            {policy ? (
                <Alert>
                    <CheckCircle2 className="h-4 w-4" />
                    <AlertTitle>Cancellation policy configured</AlertTitle>
                    <AlertDescription>Late cancellation cutoff is set.</AlertDescription>
                </Alert>
            ) : (
                <Alert>
                    <Info className="h-4 w-4" />
                    <AlertTitle>No cancellation policy configured</AlertTitle>
                    <AlertDescription>
                        Set a late cancellation cutoff below to define when cancellations are
                        considered late.
                    </AlertDescription>
                </Alert>
            )}

            <CancellationPolicyForm
                key={`${event.data.slug}-${event.data.version}`}
                event={event.data}
                teamSlug={teamSlug}
                eventSlug={eventSlug}
                disabled={disabled}
            />
        </div>
    );
}
