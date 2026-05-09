"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
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
import { AlertCircle } from "lucide-react";
import { EmailSettingsDto, TeamDto, TeamMemberListItemDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { EmailSettingsForm } from "../../events/[eventId]/settings/email/email-settings-form";
import {
    buildEmailRecipientOptions,
    TestEmailField,
} from "../../events/[eventId]/settings/email/test-email-settings-button";

async function fetchTeamEmailSettings(teamId: string): Promise<EmailSettingsDto | null> {
    try {
        return await apiClient.get<EmailSettingsDto>(`/api/teams/${teamId}/email-settings`);
    } catch (err) {
        if (err instanceof FormError && err.status === 404) {
            return null;
        }
        throw err;
    }
}

async function fetchTeam(teamId: string): Promise<TeamDto> {
    return apiClient.get<TeamDto>(`/api/teams/${teamId}`);
}

async function fetchTeamMembers(teamId: string): Promise<TeamMemberListItemDto[]> {
    return apiClient.get<TeamMemberListItemDto[]>(`/api/teams/${teamId}/members`);
}

export default function TeamEmailSettingsPage() {
    const { teamId } = useParams<{ teamId: string }>();
    const queryClient = useQueryClient();
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);

    const query = useQuery({
        queryKey: ["team-email-settings", teamId],
        queryFn: () => fetchTeamEmailSettings(teamId),
        throwOnError: false,
        retry: false,
    });

    const teamQuery = useQuery({
        queryKey: ["team", teamId],
        queryFn: () => fetchTeam(teamId),
        throwOnError: false,
        retry: false,
    });

    const membersQuery = useQuery({
        queryKey: ["team-members", teamId],
        queryFn: () => fetchTeamMembers(teamId),
        throwOnError: false,
        retry: false,
    });

    if (query.isLoading) {
        return <Skeleton className="h-64 w-full max-w-lg" />;
    }

    const settings = query.data ?? null;
    const version = settings ? Number(settings.version) : null;
    const recipientOptions = buildEmailRecipientOptions(teamQuery.data, membersQuery.data);

    async function handleDelete() {
        setIsDeleting(true);
        setDeleteError(null);
        try {
            await apiClient.delete(`/api/teams/${teamId}/email-settings?version=${version}`);
            await queryClient.invalidateQueries({ queryKey: ["team-email-settings", teamId] });
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
            <EmailSettingsForm
                key={settings ? Number(settings.version) : "new"}
                apiUrl={`/api/teams/${teamId}/email-settings`}
                queryKey={["team-email-settings", teamId]}
                hasPassword={settings?.hasPassword}
                version={version}
                initialValues={{
                    smtpHost: settings?.smtpHost ?? "",
                    smtpPort: settings ? Number(settings.smtpPort) : 587,
                    fromAddress: settings?.fromAddress ?? "",
                    authMode: (settings?.authMode as "none" | "basic") ?? "none",
                    username: settings?.username ?? "",
                }}
                renderTestEmail={settings !== null
                    ? () => (
                        <TestEmailField
                            apiUrl={`/api/teams/${teamId}/email-settings/test`}
                            recipients={recipientOptions}
                        />
                    )
                    : undefined}
            />

            {settings !== null && (
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
                                Delete team email settings
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Delete team email settings?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will remove the team-level email configuration. Events that rely on it will fall back to no email settings until individual event settings are configured.
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
