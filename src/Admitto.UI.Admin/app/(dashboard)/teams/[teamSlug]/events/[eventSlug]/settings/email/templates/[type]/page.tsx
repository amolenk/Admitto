"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { EmailTemplateDto } from "@/lib/admitto-api/generated";
import { EmailTemplateForm } from "../../../../../../settings/email/templates/email-template-form";

const TEMPLATE_NAMES: Record<string, string> = {
    "ticket": "Ticket confirmation",
    "reconfirm": "Reconfirmation",
    "cancellation": "Cancellation",
    "visa-letter-denied": "Visa letter denied",
    "otp-code": "Verification code",
};

export default function EventEmailTemplatePage({
    params,
}: {
    params: Promise<{ teamSlug: string; eventSlug: string; type: string }>;
}) {
    const { teamSlug, eventSlug, type } = use(params);

    const queryKey = ["teams", teamSlug, "events", eventSlug, "email-templates", type];
    const { data, isLoading, error } = useQuery({
        queryKey,
        queryFn: async () => {
            try {
                return await apiClient.get<EmailTemplateDto>(
                    `/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${type}`
                );
            } catch (err) {
                if (err instanceof FormError && err.status === 404) {
                    return null;
                }
                throw err;
            }
        },
    });

    const typeName = TEMPLATE_NAMES[type] ?? type;
    const backHref = `/teams/${teamSlug}/events/${eventSlug}/settings/email/templates`;

    return (
        <div>
            <div className="mb-6">
                <Link
                    href={backHref}
                    className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
                >
                    <ArrowLeft className="size-3.5" />
                    Back to templates
                </Link>
                <h1 className="font-display text-[22px] font-semibold">{typeName}</h1>
                <p className="text-[13.5px] text-muted-foreground mt-0.5">
                    Customize the {typeName.toLowerCase()} email for this event. Falls back to the team template if not set.
                </p>
            </div>

            {isLoading && (
                <div className="space-y-3">
                    <Skeleton className="h-10 w-full" />
                    <Skeleton className="h-40 w-full" />
                    <Skeleton className="h-64 w-full" />
                </div>
            )}

            {error && (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Error</AlertTitle>
                    <AlertDescription>
                        {error instanceof Error ? error.message : "Failed to load template."}
                    </AlertDescription>
                </Alert>
            )}

            {!isLoading && !error && (
                <EmailTemplateForm
                    templateApiUrl={`/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${type}`}
                    previewApiUrl={`/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${type}/preview`}
                    testSendApiUrl={`/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${type}/test-send`}
                    queryKey={queryKey}
                    backHref={backHref}
                    initialValues={data ? { subject: data.subject, textBody: data.textBody, htmlBody: data.htmlBody ?? "" } : null}
                    isCustom={data?.isCustom ?? false}
                    version={data?.version ?? null}
                    teamSlug={teamSlug}
                />
            )}
        </div>
    );
}
