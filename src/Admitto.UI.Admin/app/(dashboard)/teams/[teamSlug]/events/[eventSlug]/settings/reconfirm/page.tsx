"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { CheckCircle2, Info } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetails, isEventActive } from "../event-detail-types";
import { EventStatusBanner } from "../event-status-banner";
import { ReconfirmPolicyForm } from "./reconfirm-policy-form";

export default function ReconfirmPolicyPage() {
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
    const policy = event.data.reconfirmPolicy;

    return (
        <div className="space-y-6 max-w-lg">
            <EventStatusBanner status={event.data.status} />

            {policy ? (
                <Alert>
                    <CheckCircle2 className="h-4 w-4" />
                    <AlertTitle>Reconfirmation policy configured</AlertTitle>
                    <AlertDescription>
                        Attendees will be asked to reconfirm every {policy.cadenceDays}{" "}
                        {policy.cadenceDays === 1 ? "day" : "days"}.
                    </AlertDescription>
                </Alert>
            ) : (
                <Alert>
                    <Info className="h-4 w-4" />
                    <AlertTitle>No reconfirmation policy configured</AlertTitle>
                    <AlertDescription>
                        Set up a reconfirmation window and cadence below to require attendees to
                        reconfirm their registration.
                    </AlertDescription>
                </Alert>
            )}

            <ReconfirmPolicyForm
                key={`${event.data.slug}-${event.data.version}`}
                event={event.data}
                teamSlug={teamSlug}
                eventSlug={eventSlug}
                disabled={disabled}
            />
        </div>
    );
}
