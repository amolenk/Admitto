"use client";

import * as React from "react";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle, CheckCircle2, CircleDashed, CircleSlash } from "lucide-react";
import {
    RegistrationOpenStatusDto,
    RegistrationStatus,
    TicketTypeDto,
} from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { RegistrationPolicyForm } from "./registration-policy-form";
import { TicketTypesSection } from "./ticket-types-section";

async function fetchOpenStatus(t: string, e: string): Promise<RegistrationOpenStatusDto> {
    return apiClient.get(`/api/teams/${t}/events/${e}/registration/open-status`);
}

async function fetchTicketTypes(t: string, e: string): Promise<TicketTypeDto[]> {
    return apiClient.get(`/api/teams/${t}/events/${e}/ticket-types`);
}

const STATUS_LABEL: Record<RegistrationStatus, string> = {
    draft: "Draft",
    open: "Open",
    closed: "Closed",
};

const STATUS_VARIANT: Record<RegistrationStatus, "secondary" | "default" | "outline"> = {
    draft: "secondary",
    open: "default",
    closed: "outline",
};

const STATUS_ICON: Record<RegistrationStatus, React.ComponentType<{ className?: string }>> = {
    draft: CircleDashed,
    open: CheckCircle2,
    closed: CircleSlash,
};

const STATUS_DESCRIPTION: Record<RegistrationStatus, string> = {
    draft: "Registration has never been opened. Configure your event below before opening.",
    open: "Attendees can register for this event right now.",
    closed: "Registration has been closed. Re-open it to allow new sign-ups.",
};

export default function RegistrationSettingsPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const queryClient = useQueryClient();
    const [actionError, setActionError] = useState<{ title: string; detail: string } | null>(null);

    const status = useQuery({
        queryKey: ["registration-open-status", teamSlug, eventSlug],
        queryFn: () => fetchOpenStatus(teamSlug, eventSlug),
        throwOnError: false,
    });

    const ticketTypes = useQuery({
        queryKey: ["ticket-types", teamSlug, eventSlug],
        queryFn: () => fetchTicketTypes(teamSlug, eventSlug),
        throwOnError: false,
    });

    const openMutation = useMutation({
        mutationFn: () =>
            apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/registration/open`),
        onSuccess: () => {
            setActionError(null);
            queryClient.invalidateQueries({
                queryKey: ["registration-open-status", teamSlug, eventSlug],
            });
        },
        onError: (err) => {
            if (err instanceof FormError) {
                setActionError({ title: err.title, detail: err.detail });
            } else {
                setActionError({ title: "Unexpected Error", detail: "Could not open registration." });
            }
        },
    });

    const closeMutation = useMutation({
        mutationFn: () =>
            apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/registration/close`),
        onSuccess: () => {
            setActionError(null);
            queryClient.invalidateQueries({
                queryKey: ["registration-open-status", teamSlug, eventSlug],
            });
        },
        onError: (err) => {
            if (err instanceof FormError) {
                setActionError({ title: err.title, detail: err.detail });
            } else {
                setActionError({ title: "Unexpected Error", detail: "Could not close registration." });
            }
        },
    });

    const isPending = openMutation.isPending || closeMutation.isPending;

    return (
        <div className="space-y-8 max-w-3xl">
            {actionError && (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{actionError.title}</AlertTitle>
                    <AlertDescription>{actionError.detail}</AlertDescription>
                </Alert>
            )}

            <section className="space-y-3">
                {status.isLoading ? (
                    <Skeleton className="h-32 w-full" />
                ) : status.data ? (
                    (() => {
                        const StatusIcon = STATUS_ICON[status.data.status];
                        return (
                            <Card>
                                <CardHeader>
                                    <div className="flex items-start justify-between gap-4">
                                        <div className="space-y-1">
                                            <div className="flex items-center gap-2">
                                                <StatusIcon className="h-5 w-5 text-muted-foreground" />
                                                <CardTitle className="text-base">Registration status</CardTitle>
                                                <Badge variant={STATUS_VARIANT[status.data.status]}>
                                                    {STATUS_LABEL[status.data.status]}
                                                </Badge>
                                            </div>
                                            <CardDescription>
                                                {STATUS_DESCRIPTION[status.data.status]}
                                            </CardDescription>
                                        </div>
                                        <div className="flex flex-col items-end gap-1">
                                            {status.data.status !== "open" && (
                                                <>
                                                    <Button
                                                        type="button"
                                                        onClick={() => openMutation.mutate()}
                                                        disabled={!status.data.canOpen || isPending}
                                                    >
                                                        Open for registration
                                                    </Button>
                                                    {!status.data.canOpen &&
                                                        status.data.reason === "email-not-configured" && (
                                                            <p className="text-xs text-muted-foreground">
                                                                <Link
                                                                    href={`/teams/${teamSlug}/events/${eventSlug}/settings/email`}
                                                                    className="underline"
                                                                >
                                                                    Configure email
                                                                </Link>{" "}
                                                                before opening.
                                                            </p>
                                                        )}
                                                </>
                                            )}
                                            {status.data.status === "open" && (
                                                <Button
                                                    type="button"
                                                    variant="secondary"
                                                    onClick={() => closeMutation.mutate()}
                                                    disabled={isPending}
                                                >
                                                    Close for registration
                                                </Button>
                                            )}
                                        </div>
                                    </div>
                                </CardHeader>
                                <CardContent />
                            </Card>
                        );
                    })()
                ) : (
                    <p className="text-destructive">Failed to load registration status.</p>
                )}
            </section>

            <section className="space-y-3">
                <h2 className="text-lg font-semibold">Registration policy</h2>
                <RegistrationPolicyForm teamSlug={teamSlug} eventSlug={eventSlug} />
            </section>

            <section className="space-y-3">
                <h2 className="text-lg font-semibold">Ticket types</h2>
                {ticketTypes.isLoading ? (
                    <Skeleton className="h-24 w-full" />
                ) : (
                    <TicketTypesSection
                        teamSlug={teamSlug}
                        eventSlug={eventSlug}
                        ticketTypes={ticketTypes.data ?? []}
                    />
                )}
            </section>
        </div>
    );
}
