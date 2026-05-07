"use client";

import { useEffect, useState } from "react";
import { AlertCircle, ChevronDown, ChevronUp, RefreshCw } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { PreviewEmailTemplateDto } from "@/lib/admitto-api/generated";

export type PreviewValues = { subject: string; textBody: string; htmlBody: string };

export function PreviewPanel({
    previewApiUrl,
    formValues,
    mountValues,
}: {
    previewApiUrl: string;
    formValues: PreviewValues;
    mountValues: PreviewValues;
}) {
    const [preview, setPreview] = useState<PreviewEmailTemplateDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isOpen, setIsOpen] = useState(true);

    async function loadPreview(values: PreviewValues) {
        setIsLoading(true);
        setError(null);
        try {
            const data = await apiClient.post<PreviewEmailTemplateDto>(previewApiUrl, values);
            setPreview(data);
        } catch (err) {
            const message = err instanceof FormError
                ? err.detail
                : err instanceof Error
                    ? err.message
                    : "Failed to load preview.";
            setError(message);
        } finally {
            setIsLoading(false);
        }
    }

    useEffect(() => {
        loadPreview(mountValues);
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <div className="mt-6 border rounded-lg">
            <button
                type="button"
                onClick={() => setIsOpen((v) => !v)}
                className="w-full flex items-center justify-between px-4 py-3 text-left hover:bg-muted/50 transition-colors rounded-lg"
            >
                <span className="font-display text-[16px] font-semibold">Preview</span>
                {isOpen ? <ChevronUp className="size-4" /> : <ChevronDown className="size-4" />}
            </button>

            {isOpen && (
                <div className="px-4 pb-4">
                    <div className="flex items-center justify-between mb-3">
                        <p className="text-[12px] text-muted-foreground">
                            Rendered with sample placeholder data.
                        </p>
                        <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            onClick={() => loadPreview(formValues)}
                            disabled={isLoading}
                        >
                            <RefreshCw className={`size-3.5 ${isLoading ? "animate-spin" : ""}`} />
                            Refresh
                        </Button>
                    </div>

                    {error && (
                        <Alert variant="destructive" className="mb-3">
                            <AlertCircle className="h-4 w-4" />
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    )}

                    {isLoading && !preview && (
                        <div className="space-y-2">
                            <Skeleton className="h-5 w-3/4" />
                            <Skeleton className="h-48 w-full" />
                        </div>
                    )}

                    {preview && (
                        <div className="space-y-3">
                            <div>
                                <span className="text-[11px] uppercase tracking-widest text-muted-foreground font-semibold">
                                    Subject
                                </span>
                                <p className="text-sm mt-1 font-medium">{preview.renderedSubject}</p>
                            </div>
                            <div>
                                <span className="text-[11px] uppercase tracking-widest text-muted-foreground font-semibold">
                                    HTML body
                                </span>
                                <iframe
                                    className="mt-1 w-full border rounded-md"
                                    style={{ minHeight: "400px" }}
                                    srcDoc={preview.renderedHtmlBody ?? ""}
                                    sandbox=""
                                    title="Email preview"
                                />
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}
