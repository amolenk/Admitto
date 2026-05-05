"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { apiClient } from "@/lib/api-client";
import { CustomBulkTemplateDto } from "@/lib/admitto-api/generated";
import { CustomBulkTemplateForm } from "../../custom-bulk-template-form";

export default function TeamCustomBulkTemplateEditorPage({
    params,
}: {
    params: Promise<{ teamSlug: string; id: string }>;
}) {
    const { teamSlug, id } = use(params);

    const apiUrl = `/api/teams/${teamSlug}/custom-bulk-templates/${id}`;
    const backHref = `/teams/${teamSlug}/settings/email/templates`;
    const queryKey = ["team-custom-bulk-template", teamSlug, id];

    const { data, isLoading, error } = useQuery({
        queryKey,
        queryFn: () => apiClient.get<CustomBulkTemplateDto>(apiUrl),
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
                    {data?.name ?? "Custom bulk template"}
                </h1>
                <p className="text-[13.5px] text-muted-foreground mt-0.5">
                    Edit this reusable template for team bulk email campaigns.
                </p>
            </div>

            {isLoading && (
                <div className="space-y-3">
                    <Skeleton className="h-10 w-full" />
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
                <CustomBulkTemplateForm
                    template={data}
                    apiUrl={apiUrl}
                    queryKey={queryKey}
                    backHref={backHref}
                />
            )}
        </div>
    );
}
