"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetails, isEventActive } from "../event-detail-types";
import { EventStatusBanner } from "../event-status-banner";
import { AdditionalDetailsEditor } from "./additional-details-editor";
import { RegistrationPolicyForm } from "./registration-policy-form";

export default function RegistrationSettingsPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

    const event = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetails>(`/api/teams/${teamId}/events/${eventId}`),
    });

    if (event.isLoading) {
        return <Skeleton className="h-64 w-full" />;
    }

    if (event.error || !event.data) {
        return <p className="text-destructive">Failed to load event details.</p>;
    }

    const disabled = !isEventActive(event.data.status);

    return (
        <div className="space-y-6">
            <EventStatusBanner status={event.data.status} />
            <RegistrationPolicyForm
                key={`${event.data.version}`}
                event={event.data}
                teamId={teamId}
                eventId={eventId}
                disabled={disabled}
            />
            <AdditionalDetailsEditor
                key={`adschema-${event.data.version}`}
                event={event.data}
                teamId={teamId}
                eventId={eventId}
                disabled={disabled}
            />
        </div>
    );
}
