"use client";

import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle, CheckCircle2, Info } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { CancellationPolicyForm } from "./cancellation-policy-form";

interface CancellationPolicy {
    lateCancellationCutoff: string;
}

interface EventActiveStatus {
    isEventActive: boolean;
}

async function fetchCancellationPolicy(
    t: string,
    e: string
): Promise<CancellationPolicy | null> {
    try {
        return await apiClient.get<CancellationPolicy>(
            `/api/teams/${t}/events/${e}/cancellation-policy`
        );
    } catch (err) {
        if (err instanceof FormError && err.status === 404) {
            return null;
        }
        throw err;
    }
}

async function fetchEventActiveStatus(
    t: string,
    e: string
): Promise<EventActiveStatus> {
    return apiClient.get(`/api/teams/${t}/events/${e}/registration/open-status`);
}

export default function CancellationPolicyPage() {
    const { teamSlug, eventSlug } = useParams<{
        teamSlug: string;
        eventSlug: string;
    }>();
    const queryClient = useQueryClient();

    const activeStatus = useQuery({
        queryKey: ["registration-open-status", teamSlug, eventSlug],
        queryFn: () => fetchEventActiveStatus(teamSlug, eventSlug),
    });

    const policy = useQuery({
        queryKey: ["cancellation-policy", teamSlug, eventSlug],
        queryFn: () => fetchCancellationPolicy(teamSlug, eventSlug),
        throwOnError: false,
        retry: false,
    });

    const isEventActive = activeStatus.data?.isEventActive ?? true;

    return (
        <div className="space-y-6 max-w-lg">
            {!isEventActive && activeStatus.data && (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Event inactive</AlertTitle>
                    <AlertDescription>
                        This event is no longer active. Policies cannot be
                        modified.
                    </AlertDescription>
                </Alert>
            )}

            {policy.isLoading ? (
                <Skeleton className="h-64 w-full" />
            ) : (
                <>
                    {policy.data ? (
                        <Alert>
                            <CheckCircle2 className="h-4 w-4" />
                            <AlertTitle>Cancellation policy configured</AlertTitle>
                            <AlertDescription>
                                Late cancellation cutoff is set.
                            </AlertDescription>
                        </Alert>
                    ) : (
                        <Alert>
                            <Info className="h-4 w-4" />
                            <AlertTitle>
                                No cancellation policy configured
                            </AlertTitle>
                            <AlertDescription>
                                Set a late cancellation cutoff below to define
                                when cancellations are considered late.
                            </AlertDescription>
                        </Alert>
                    )}

                    <CancellationPolicyForm
                        key={policy.data ? "edit" : "create"}
                        policy={policy.data ?? null}
                        teamSlug={teamSlug}
                        eventSlug={eventSlug}
                        disabled={!isEventActive}
                        onSaved={() => {
                            queryClient.invalidateQueries({
                                queryKey: [
                                    "cancellation-policy",
                                    teamSlug,
                                    eventSlug,
                                ],
                            });
                        }}
                    />
                </>
            )}
        </div>
    );
}
