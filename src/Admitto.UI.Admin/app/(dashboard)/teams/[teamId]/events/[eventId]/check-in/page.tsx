"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import type { CheckInSummaryDto, TicketedEventDetailsDto } from "@/lib/admitto-api/generated/types.gen";
import { CheckInScanner } from "./scanner";
import { ScannerLinkCard } from "./scanner-link-card";

export default function CheckInPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

    const eventQuery = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () => apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`),
    });
    // Kept mounted (though unused by the scanner) so a successful check-in's
    // summary invalidation refetches this query while the page is open,
    // preserving the prior summary-refresh behavior.
    useQuery({
        queryKey: ["check-in-summary", teamId, eventId],
        queryFn: () => apiClient.get<CheckInSummaryDto>(`/api/teams/${teamId}/events/${eventId}/registrations/check-in/summary`),
        retry: false,
        refetchOnMount: "always",
    });

    return (
        <PageLayout>
            <Button variant="ghost" asChild>
                <Link href={`/teams/${teamId}/events/${eventId}`}>
                    <ArrowLeft /> Event dashboard
                </Link>
            </Button>
            {eventQuery.isLoading ? (
                <Skeleton className="mx-auto h-[600px] max-w-2xl" />
            ) : eventQuery.data ? (
                <>
                <CheckInScanner
                    teamId={teamId}
                    eventId={eventId}
                    startsAt={eventQuery.data.startsAt}
                    timeZone={eventQuery.data.timeZone}
                />
                <ScannerLinkCard
                    teamId={teamId}
                    eventId={eventId}
                    isArchived={eventQuery.data.status === "archived"}
                    timeZone={eventQuery.data.timeZone}
                />
                </>
            ) : (
                <p className="text-destructive">Failed to load event.</p>
            )}
        </PageLayout>
    );
}
