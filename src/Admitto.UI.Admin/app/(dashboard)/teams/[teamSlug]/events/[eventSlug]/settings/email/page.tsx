"use client";

import { useState } from "react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Info, AlertCircle } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    AlertDialog,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { EmailSettingsDto, TeamDto, TeamMemberListItemDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { EmailSettingsForm } from "./email-settings-form";
import {
    buildEmailRecipientOptions,
    TestEmailField,
} from "./test-email-settings-button";

async function fetchEmailSettings(url: string): Promise<EmailSettingsDto | null> {
    try {
        return await apiClient.get<EmailSettingsDto>(url);
    } catch (err) {
        if (err instanceof FormError && err.status === 404) {
            return null;
        }
        throw err;
    }
}

async function fetchTeam(teamSlug: string): Promise<TeamDto> {
    return apiClient.get<TeamDto>(`/api/teams/${teamSlug}`);
}

async function fetchTeamMembers(teamSlug: string): Promise<TeamMemberListItemDto[]> {
    return apiClient.get<TeamMemberListItemDto[]>(`/api/teams/${teamSlug}/members`);
}

export default function EmailSettingsPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();
    const queryClient = useQueryClient();
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);

    const eventQuery = useQuery({
        queryKey: ["email-settings", teamSlug, eventSlug],
        queryFn: () => fetchEmailSettings(`/api/teams/${teamSlug}/events/${eventSlug}/email-settings`),
        throwOnError: false,
        retry: false,
    });

    const teamSettingsQuery = useQuery({
        queryKey: ["team-email-settings", teamSlug],
        queryFn: () => fetchEmailSettings(`/api/teams/${teamSlug}/email-settings`),
        throwOnError: false,
        retry: false,
    });

    const teamQuery = useQuery({
        queryKey: ["team", teamSlug],
        queryFn: () => fetchTeam(teamSlug),
        throwOnError: false,
        retry: false,
    });

    const membersQuery = useQuery({
        queryKey: ["team-members", teamSlug],
        queryFn: () => fetchTeamMembers(teamSlug),
        throwOnError: false,
        retry: false,
    });

    if (eventQuery.isLoading || teamSettingsQuery.isLoading) {
        return <Skeleton className="h-64 w-full max-w-lg" />;
    }

    const eventSettings = eventQuery.data ?? null;
    const teamSettings = teamSettingsQuery.data ?? null;
    const recipientOptions = buildEmailRecipientOptions(teamQuery.data, membersQuery.data);

    const teamEmailPageHref = `/teams/${teamSlug}/settings/email`;

    const showInheritedCallout = teamSettings !== null && eventSettings === null;
    const showOverridingCallout = teamSettings !== null && eventSettings !== null;

    const version = eventSettings ? Number(eventSettings.version) : null;

    async function handleDelete() {
        setIsDeleting(true);
        setDeleteError(null);
        try {
            await apiClient.delete(`/api/teams/${teamSlug}/events/${eventSlug}/email-settings?version=${version}`);
            await queryClient.invalidateQueries({ queryKey: ["email-settings", teamSlug, eventSlug] });
            setDialogOpen(false);
        } catch (err: unknown) {
            const message = err instanceof Error ? err.message : "Failed to delete email settings.";
            setDeleteError(message);
        } finally {
            setIsDeleting(false);
        }
    }

    return (
        <div>
            {showInheritedCallout && (
                <Alert className="mb-5">
                    <Info className="h-4 w-4" />
                    <AlertTitle>Inherited from team settings</AlertTitle>
                    <AlertDescription>
                        This event uses the team-level email configuration.{" "}
                        <Link href={teamEmailPageHref} className="underline underline-offset-2">
                            View team email settings
                        </Link>{" "}
                        to change them, or save settings here to create an event-specific override.
                    </AlertDescription>
                </Alert>
            )}

            {showOverridingCallout && (
                <Alert className="mb-5">
                    <Info className="h-4 w-4" />
                    <AlertTitle>Overriding team settings</AlertTitle>
                    <AlertDescription>
                        This event has its own email configuration that overrides the team defaults.{" "}
                        <Link href={teamEmailPageHref} className="underline underline-offset-2">
                            View team email settings
                        </Link>
                        .
                    </AlertDescription>
                </Alert>
            )}

            <EmailSettingsForm
                key={eventSettings ? Number(eventSettings.version) : "new"}
                apiUrl={`/api/teams/${teamSlug}/events/${eventSlug}/email-settings`}
                queryKey={["email-settings", teamSlug, eventSlug]}
                hasPassword={eventSettings?.hasPassword}
                version={version}
                initialValues={{
                    smtpHost: eventSettings?.smtpHost ?? "",
                    smtpPort: eventSettings ? Number(eventSettings.smtpPort) : 587,
                    fromAddress: eventSettings?.fromAddress ?? "",
                    authMode: (eventSettings?.authMode as "none" | "basic") ?? "none",
                    username: eventSettings?.username ?? "",
                }}
                renderTestEmail={eventSettings !== null
                    ? () => (
                        <TestEmailField
                            apiUrl={`/api/teams/${teamSlug}/events/${eventSlug}/email-settings/test`}
                            recipients={recipientOptions}
                        />
                    )
                    : undefined}
            />

            {eventSettings !== null && (
                <div className="mt-8 space-y-6">
                    {deleteError && (
                        <Alert variant="destructive" className="mb-4">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>Error</AlertTitle>
                            <AlertDescription>{deleteError}</AlertDescription>
                        </Alert>
                    )}

                    <AlertDialog open={dialogOpen} onOpenChange={(open) => { setDialogOpen(open); if (!open) setDeleteError(null); }}>
                        <AlertDialogTrigger asChild>
                            <Button variant="outline" size="sm" className="text-destructive border-destructive/30">
                                Delete event email settings
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Delete event email settings?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will remove the event-specific email configuration.
                                    {teamSettings !== null
                                        ? " The event will then inherit the team-level email settings."
                                        : " No fallback is configured at the team level, so emails will not be sent until settings are reconfigured."}
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <Button
                                    variant="destructive"
                                    onClick={handleDelete}
                                    disabled={isDeleting}
                                >
                                    {isDeleting ? "Deleting…" : "Delete"}
                                </Button>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
            )}
        </div>
    );
}
