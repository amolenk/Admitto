"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { apiClient } from "@/lib/api-client";
import { EmailTemplateDto } from "@/lib/admitto-api/generated";
import { EmailTemplateForm } from "../email-template-form";

export default function TeamEmailTemplateEditorPage({
    params,
}: {
    params: Promise<{ teamSlug: string; id: string }>;
}) {
    const { teamSlug, id } = use(params);

    const templateApiUrl = `/api/teams/${teamSlug}/email-templates/${id}`;
    const previewApiUrl = `/api/teams/${teamSlug}/email-templates/preview`;
    const testSendApiUrl = `/api/teams/${teamSlug}/email-templates/${id}/test-send`;
    const backHref = `/teams/${teamSlug}/settings/email/templates`;
    const queryKey = ["team-email-template", teamSlug, id];

    const { data, isLoading, error } = useQuery({
        queryKey,
        queryFn: () => apiClient.get<EmailTemplateDto>(templateApiUrl),
    });

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
                <h1 className="font-display text-[22px] font-semibold">
                    {data?.name ?? "Email template"}
                </h1>
                <p className="text-[13.5px] text-muted-foreground mt-0.5">
                    Customise this email template for your team.
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

            {!isLoading && !error && data && (
                <EmailTemplateForm
                    templateApiUrl={templateApiUrl}
                    previewApiUrl={previewApiUrl}
                    testSendApiUrl={testSendApiUrl}
                    queryKey={queryKey}
                    backHref={backHref}
                    initialValues={{ subject: data.subject, textBody: data.textBody, htmlBody: data.htmlBody ?? "" }}
                    isCustomised={data.isCustomised || data.kind === "custom"}
                    version={data.version}
                    teamSlug={teamSlug}
                />
            )}
        </div>
    );
}
