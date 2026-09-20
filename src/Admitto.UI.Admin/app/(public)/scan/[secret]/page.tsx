"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { AlertTriangle } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { CheckInScanner } from "@/components/check-in-scanner/scanner";
import { createSharedScannerCheckInOperations, fetchSharedScannerSession, SharedScannerAccessDeniedError } from "./shared-scanner-operations";

export default function SharedScannerPage() {
    const { secret } = useParams<{ secret: string }>();

    const sessionQuery = useQuery({
        queryKey: ["shared-scanner-session", secret],
        queryFn: () => fetchSharedScannerSession(secret),
        retry: false,
    });

    const operations = createSharedScannerCheckInOperations(secret);
    const accessDenied = sessionQuery.error instanceof SharedScannerAccessDeniedError;

    return (
        <main className="min-h-screen bg-background p-4">
            {sessionQuery.isLoading ? (
                <Skeleton className="mx-auto h-[600px] max-w-2xl" />
            ) : accessDenied || !sessionQuery.data ? (
                <div className="mx-auto mt-24 max-w-md rounded-xl border border-amber-300/50 bg-amber-50 p-6 text-center">
                    <AlertTriangle className="mx-auto mb-3 size-8 text-amber-700" />
                    <h1 className="font-display text-lg font-semibold">Scanner link unavailable</h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        This scanner link is no longer valid. Contact the event organizer for access.
                    </p>
                </div>
            ) : (
                <CheckInScanner
                    teamId={sessionQuery.data.teamId}
                    eventId={sessionQuery.data.eventId}
                    startsAt={sessionQuery.data.startsAt}
                    timeZone={sessionQuery.data.timeZone}
                    operations={operations}
                />
            )}
        </main>
    );
}
