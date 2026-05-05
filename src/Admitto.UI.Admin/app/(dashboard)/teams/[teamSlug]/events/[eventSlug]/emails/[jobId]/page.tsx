"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, AlertCircle } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { BulkEmailJobDetailDto, BulkEmailJobStatus } from "@/lib/admitto-api/generated";
import { PageLayout } from "@/components/page-layout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";

const STATUS_LABEL: Record<BulkEmailJobStatus, string> = {
    pending: "Pending",
    resolving: "Resolving",
    sending: "Sending",
    completed: "Completed",
    partiallyFailed: "Partial failure",
    failed: "Failed",
    cancelled: "Cancelled",
};

const STATUS_VARIANT: Record<BulkEmailJobStatus, "default" | "secondary" | "destructive" | "outline"> = {
    pending: "secondary",
    resolving: "secondary",
    sending: "default",
    completed: "default",
    partiallyFailed: "destructive",
    failed: "destructive",
    cancelled: "outline",
};

function isActive(status: BulkEmailJobStatus) {
    return status === "pending" || status === "resolving" || status === "sending";
}

function formatDate(iso: string | null) {
    if (!iso) return "—";
    return new Date(iso).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
    return (
        <div className="flex items-start gap-4 py-2.5 border-b last:border-0">
            <div className="w-40 shrink-0 text-[13px] text-muted-foreground">{label}</div>
            <div className="text-[13px] flex-1">{value ?? "—"}</div>
        </div>
    );
}

export default function BulkEmailDetailPage() {
    const { teamSlug, eventSlug, jobId } = useParams<{
        teamSlug: string;
        eventSlug: string;
        jobId: string;
    }>();
    const queryClient = useQueryClient();

    const { data: job, isLoading, error } = useQuery({
        queryKey: ["bulk-email", teamSlug, eventSlug, jobId],
        queryFn: () =>
            apiClient.get<BulkEmailJobDetailDto>(
                `/api/teams/${teamSlug}/events/${eventSlug}/bulk-emails/${jobId}`
            ),
        throwOnError: false,
    });

    const cancelMutation = useMutation({
        mutationFn: () =>
            apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/bulk-emails/${jobId}/cancel`),
        onSuccess: () => {
            toast.success("Cancellation requested.");
            queryClient.invalidateQueries({ queryKey: ["bulk-email", teamSlug, eventSlug, jobId] });
            queryClient.invalidateQueries({ queryKey: ["bulk-emails", teamSlug, eventSlug] });
        },
        onError: (err) => {
            toast.error(err instanceof FormError ? err.detail : "Failed to cancel.");
        },
    });

    const backHref = `/teams/${teamSlug}/events/${eventSlug}/emails`;

    const breadcrumbs = [
        { label: "Emails", href: backHref },
        { label: "Job details" },
    ];

    return (
        <PageLayout title="Bulk email details" breadcrumbs={breadcrumbs}>
            <div className="flex items-center justify-between">
                <Button variant="ghost" size="sm" asChild>
                    <Link href={backHref}>
                        <ArrowLeft className="size-3.5 mr-1" />
                        Back to bulk emails
                    </Link>
                </Button>
                {job && isActive(job.status) && (
                    <AlertDialog>
                        <AlertDialogTrigger asChild>
                            <Button variant="outline" size="sm" className="text-destructive hover:text-destructive">
                                Cancel job
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Cancel bulk email?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will stop sending to remaining recipients. Emails already sent will not be recalled.
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Keep sending</AlertDialogCancel>
                                <AlertDialogAction
                                    onClick={() => cancelMutation.mutate()}
                                    className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                                >
                                    Cancel job
                                </AlertDialogAction>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                )}
            </div>

            {isLoading ? (
                <div className="space-y-2">
                    {[1, 2, 3, 4, 5].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
                </div>
            ) : error || !job ? (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Failed to load</AlertTitle>
                    <AlertDescription>Could not load bulk email details. Please try again.</AlertDescription>
                </Alert>
            ) : (
                <>
                    <div className="rounded-lg border p-4">
                        <h3 className="font-medium text-[14px] mb-2">Summary</h3>
                        <DetailRow label="Status" value={<Badge variant={STATUS_VARIANT[job.status]}>{STATUS_LABEL[job.status]}</Badge>} />
                        <DetailRow label="Type" value={job.emailType} />
                        <DetailRow label="Triggered by" value={job.isSystemTriggered ? "System" : (job.triggeredBy ?? "—")} />
                        <DetailRow label="Recipients" value={String(Number(job.recipientCount))} />
                        <DetailRow label="Sent" value={String(Number(job.sentCount))} />
                        <DetailRow label="Failed" value={String(Number(job.failedCount))} />
                        <DetailRow label="Cancelled" value={String(Number(job.cancelledCount))} />
                        {job.lastError && <DetailRow label="Last error" value={<span className="text-destructive">{job.lastError}</span>} />}
                    </div>

                    <div className="rounded-lg border p-4">
                        <h3 className="font-medium text-[14px] mb-2">Timestamps</h3>
                        <DetailRow label="Created" value={formatDate(job.createdAt)} />
                        <DetailRow label="Started" value={formatDate(job.startedAt)} />
                        <DetailRow label="Completed" value={formatDate(job.completedAt)} />
                        {job.cancellationRequestedAt && (
                            <DetailRow label="Cancel requested" value={formatDate(job.cancellationRequestedAt)} />
                        )}
                        {job.cancelledAt && (
                            <DetailRow label="Cancelled" value={formatDate(job.cancelledAt)} />
                        )}
                    </div>

                    {(job.subject || job.textBody) && (
                        <div className="rounded-lg border p-4">
                            <h3 className="font-medium text-[14px] mb-2">Content</h3>
                            <DetailRow label="Subject" value={job.subject} />
                            {job.textBody && (
                                <div className="mt-3">
                                    <p className="text-[12px] text-muted-foreground mb-1">Text body</p>
                                    <pre className="text-[12px] whitespace-pre-wrap bg-muted rounded p-3 max-h-48 overflow-y-auto">
                                        {job.textBody}
                                    </pre>
                                </div>
                            )}
                        </div>
                    )}

                    {job.source && (
                        <div className="rounded-lg border p-4">
                            <h3 className="font-medium text-[14px] mb-2">Source</h3>
                            <DetailRow
                                label="Type"
                                value={job.source.$type === "attendee" ? "Registered attendees" : "External list"}
                            />
                        </div>
                    )}
                </>
            )}
        </PageLayout>
    );
}
