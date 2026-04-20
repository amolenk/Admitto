"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
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
        <div>
            <EmailSettingsForm
                teamSlug={teamSlug}
                eventSlug={eventSlug}
                settings={settings}
            />
        </div>
    );
}
