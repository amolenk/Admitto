"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { CheckCircle2, AlertCircle } from "lucide-react";
import { EventEmailSettingsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { EmailSettingsForm } from "./email-settings-form";

async function fetchEmailSettings(
    teamSlug: string,
    eventSlug: string
): Promise<EventEmailSettingsDto | null> {
    try {
        return await apiClient.get<EventEmailSettingsDto>(
            `/api/teams/${teamSlug}/events/${eventSlug}/email-settings`
        );
    } catch (err) {
        if (err instanceof FormError && err.status === 404) {
            return null;
        }
        throw err;
    }
}

export default function EmailSettingsPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    const query = useQuery({
        queryKey: ["email-settings", teamSlug, eventSlug],
        queryFn: () => fetchEmailSettings(teamSlug, eventSlug),
        throwOnError: false,
        retry: false,
    });

    if (query.isLoading) {
        return <Skeleton className="h-64 w-full max-w-lg" />;
    }

    const settings = query.data ?? null;

    return (
        <div className="space-y-6 max-w-lg">
            {settings ? (
                <Alert>
                    <CheckCircle2 className="h-4 w-4" />
                    <AlertTitle>Email is configured</AlertTitle>
                    <AlertDescription>
                        Sending from {settings.fromAddress} via {settings.smtpHost}.
                    </AlertDescription>
                </Alert>
            ) : (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Email is not configured</AlertTitle>
                    <AlertDescription>
                        Configure SMTP below before opening registration for this event.
                    </AlertDescription>
                </Alert>
            )}

            <EmailSettingsForm
                teamSlug={teamSlug}
                eventSlug={eventSlug}
                settings={settings}
            />
        </div>
    );
}
