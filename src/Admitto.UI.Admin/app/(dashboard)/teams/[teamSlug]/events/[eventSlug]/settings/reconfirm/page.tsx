"use client";

import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle, CheckCircle2, Info } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { ReconfirmPolicyForm } from "./reconfirm-policy-form";

interface ReconfirmPolicy {
    opensAt: string;
    closesAt: string;
    cadenceDays: number;
}

interface EventActiveStatus {
    isEventActive: boolean;
}

async function fetchReconfirmPolicy(
    t: string,
    e: string
): Promise<ReconfirmPolicy | null> {
    try {
        return await apiClient.get<ReconfirmPolicy>(
            `/api/teams/${t}/events/${e}/reconfirm-policy`
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

export default function ReconfirmPolicyPage() {
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
        queryKey: ["reconfirm-policy", teamSlug, eventSlug],
        queryFn: () => fetchReconfirmPolicy(teamSlug, eventSlug),
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
                            <AlertTitle>
                                Reconfirmation policy configured
                            </AlertTitle>
                            <AlertDescription>
                                Attendees will be asked to reconfirm every{" "}
                                {policy.data.cadenceDays}{" "}
                                {policy.data.cadenceDays === 1 ? "day" : "days"}.
                            </AlertDescription>
                        </Alert>
                    ) : (
                        <Alert>
                            <Info className="h-4 w-4" />
                            <AlertTitle>
                                No reconfirmation policy configured
                            </AlertTitle>
                            <AlertDescription>
                                Set up a reconfirmation window and cadence below
                                to require attendees to reconfirm their
                                registration.
                            </AlertDescription>
                        </Alert>
                    )}

                    <ReconfirmPolicyForm
                        key={policy.data ? "edit" : "create"}
                        policy={policy.data ?? null}
                        teamSlug={teamSlug}
                        eventSlug={eventSlug}
                        disabled={!isEventActive}
                        onSaved={() => {
                            queryClient.invalidateQueries({
                                queryKey: [
                                    "reconfirm-policy",
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
