"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import { Mail, Pencil, Plus } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    EmailTemplateDto,
    CustomBulkTemplateListItemDto,
} from "@/lib/admitto-api/generated";

const EMAIL_TEMPLATE_TYPES = [
    { type: "ticket", name: "Ticket confirmation", description: "Sent after successful registration" },
    { type: "reconfirm", name: "Reconfirmation", description: "One-week-out reconfirmation request" },
    { type: "cancellation", name: "Cancellation", description: "Sent when an attendee cancels" },
    { type: "visa-letter-denied", name: "Visa letter denied", description: "Sent when a visa letter request is declined" },
    { type: "otp-code", name: "Verification code", description: "Sent when someone starts registration" },
] as const;

async function fetchTemplate(apiUrl: string): Promise<EmailTemplateDto | null> {
    try {
        return await apiClient.get<EmailTemplateDto>(apiUrl);
    } catch (err) {
        if (err instanceof FormError && err.status === 404) {
            return null;
        }
        throw err;
    }
}

function CustomTemplatesSection({ teamSlug, eventSlug }: { teamSlug: string; eventSlug: string }) {
    const router = useRouter();
    const queryClient = useQueryClient();
    const basePath = `/teams/${teamSlug}/events/${eventSlug}/settings/email/templates`;

    const { data: templates, isLoading } = useQuery({
        queryKey: ["custom-bulk-templates", teamSlug, eventSlug],
        queryFn: () =>
            apiClient.get<CustomBulkTemplateListItemDto[]>(
                `/api/teams/${teamSlug}/events/${eventSlug}/custom-bulk-templates`
            ),
        throwOnError: false,
    });

    const createMutation = useMutation({
        mutationFn: () =>
            apiClient.post<{ id: string }>(
                `/api/teams/${teamSlug}/events/${eventSlug}/custom-bulk-templates`,
                { name: "New template", subject: "Subject", textBody: "Body", htmlBody: null }
            ),
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ["custom-bulk-templates", teamSlug, eventSlug] });
            router.push(`${basePath}/custom/${data.id}`);
        },
    });

    return (
        <div className="mt-8">
            <div className="flex items-start justify-between mb-4">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Custom templates</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Reusable templates for bulk email campaigns.
                    </p>
                </div>
                <Button
                    size="sm"
                    onClick={() => createMutation.mutate()}
                    disabled={createMutation.isPending}
                >
                    <Plus className="size-3.5 mr-1" />
                    {createMutation.isPending ? "Creating…" : "New template"}
                </Button>
            </div>

            {isLoading ? (
                <div className="space-y-2">
                    {[1, 2].map((i) => <Skeleton key={i} className="h-14 w-full" />)}
                </div>
            ) : !templates || templates.length === 0 ? (
                <div className="rounded-lg border border-dashed p-8 text-center">
                    <p className="text-[13.5px] text-muted-foreground mb-3">
                        No custom templates yet. Create one to use in bulk email campaigns.
                    </p>
                    <Button
                        size="sm"
                        variant="outline"
                        onClick={() => createMutation.mutate()}
                        disabled={createMutation.isPending}
                    >
                        <Plus className="size-3.5 mr-1" />
                        {createMutation.isPending ? "Creating…" : "New template"}
                    </Button>
                </div>
            ) : (
                <div className="card divide-y divide-border rounded-lg border">
                    {templates.map((t) => (
                        <div key={t.id} className="flex items-center gap-4 p-4">
                            <div className="flex-1 min-w-0">
                                <div className="text-[13.5px] font-medium">{t.name}</div>
                                <div className="text-[12px] text-muted-foreground truncate">{t.subject}</div>
                            </div>
                            <Button variant="ghost" size="sm" asChild>
                                <Link href={`${basePath}/custom/${t.id}`}>
                                    <Pencil className="size-3.5 mr-1" />
                                    Edit
                                </Link>
                            </Button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default function EventEmailTemplatesPage() {
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    const templateQueries = useQueries({
        queries: EMAIL_TEMPLATE_TYPES.map(({ type }) => ({
            queryKey: ["event-email-template", teamSlug, eventSlug, type],
            queryFn: () =>
                fetchTemplate(
                    `/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${type}`
                ),
            throwOnError: false,
            retry: false,
        })),
    });

    const isLoading = templateQueries.some((q) => q.isLoading);
    const basePath = `/teams/${teamSlug}/events/${eventSlug}/settings/email/templates`;

    return (
        <div>
            <div className="flex items-start justify-between mb-4">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Email templates</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Customise the emails Admitto sends to attendees for this event.
                    </p>
                </div>
                <Button variant="outline" size="sm" asChild>
                    <Link href={`/teams/${teamSlug}/events/${eventSlug}/settings/email`}>← Back to email settings</Link>
                </Button>
            </div>

            <div className="card divide-y divide-border rounded-lg border">
                {EMAIL_TEMPLATE_TYPES.map(({ type, name, description }, i) => {
                    const query = templateQueries[i];
                    const isCustom = query.data?.isCustom ?? false;

                    return (
                        <div key={type} className="flex items-center gap-4 p-4">
                            <div className="h-8 w-8 rounded-md bg-muted grid place-items-center shrink-0">
                                <Mail className="size-3.5 text-muted-foreground" />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="text-[13.5px] font-medium">{name}</div>
                                <div className="text-[12px] text-muted-foreground">{description}</div>
                            </div>
                            {isLoading ? (
                                <Skeleton className="h-5 w-16" />
                            ) : (
                                <Badge variant={isCustom ? "default" : "secondary"}>
                                    {isCustom ? "Custom" : "Default"}
                                </Badge>
                            )}
                            <Button variant="ghost" size="sm" asChild>
                                <Link href={`${basePath}/${type}`}>
                                    <Pencil className="size-3.5 mr-1" />
                                    Edit
                                </Link>
                            </Button>
                        </div>
                    );
                })}
            </div>

            <CustomTemplatesSection teamSlug={teamSlug} eventSlug={eventSlug} />
        </div>
    );
}
