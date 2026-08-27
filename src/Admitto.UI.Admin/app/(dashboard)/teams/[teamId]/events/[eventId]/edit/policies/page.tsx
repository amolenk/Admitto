"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { CheckCircle2 } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetails, isEventActive } from "../../settings/event-detail-types";
import { EventStatusBanner } from "../../settings/event-status-banner";
import { AdditionalDetailsEditor } from "../../settings/registration/additional-details-editor";
import { RegistrationPolicyForm } from "../../settings/registration/registration-policy-form";
import { ReconfirmPolicyForm } from "../../settings/reconfirm/reconfirm-policy-form";
import { WaitlistPolicyForm } from "../../settings/waitlist/waitlist-policy-form";

export default function EditPoliciesPage() {
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
    const policy = event.data.reconfirmPolicy;

    return (
        <div className="space-y-8">
            <EventStatusBanner status={event.data.status} />

            <RegistrationPolicyForm
                key={`registration-${event.data.version}`}
                event={event.data}
                teamId={teamId}
                eventId={eventId}
                disabled={disabled}
            />

            <WaitlistPolicyForm
                key={`waitlist-${event.data.version}`}
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

            <div className="space-y-6">
                {policy && (
                    <Alert>
                        <CheckCircle2 className="h-4 w-4" />
                        <AlertTitle>Reconfirmation policy configured</AlertTitle>
                        <AlertDescription>
                            Attendees can reconfirm during the configured window. Reminders respect the minimum interval and any optional no-reminder quiet hours.
                        </AlertDescription>
                    </Alert>
                )}

                <ReconfirmPolicyForm
                    key={`reconfirm-${event.data.version}`}
                    event={event.data}
                    teamId={teamId}
                    eventId={eventId}
                    disabled={disabled}
                />
            </div>
        </div>
    );
}
