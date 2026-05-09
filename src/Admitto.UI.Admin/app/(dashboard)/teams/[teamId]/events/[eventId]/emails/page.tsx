"use client";

import { useParams } from "next/navigation";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Mail, Plus } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { BulkEmailJobStatus, BulkEmailListItemDto } from "@/lib/admitto-api/generated";
import { PageLayout } from "@/components/page-layout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { SendBulkEmailDialog } from "./send-bulk-email-dialog";

type StatusFilter = "all" | "active" | "completed" | "failed-cancelled";

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

function isCompleted(status: BulkEmailJobStatus) {
    return status === "completed" || status === "partiallyFailed";
}

function isFailedOrCancelled(status: BulkEmailJobStatus) {
    return status === "failed" || status === "cancelled";
}

function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
    });
}

export default function BulkEmailsPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
    const [sendDialogOpen, setSendDialogOpen] = useState(false);

    const { data: jobs, isLoading } = useQuery({
        queryKey: ["bulk-emails", teamId, eventId],
        queryFn: () =>
            apiClient.get<BulkEmailListItemDto[]>(
                `/api/teams/${teamId}/events/${eventId}/bulk-emails`
            ),
        throwOnError: false,
    });

    const filtered = (jobs ?? []).filter((j) => {
        if (statusFilter === "active") return isActive(j.status);
        if (statusFilter === "completed") return isCompleted(j.status);
        if (statusFilter === "failed-cancelled") return isFailedOrCancelled(j.status);
        return true;
    });

    const breadcrumbs = [
        { label: "Emails" },
    ];

    return (
        <PageLayout title="Emails" breadcrumbs={breadcrumbs}>
            <div className="flex items-center justify-between">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Bulk emails</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        All bulk email campaigns for this event.
                    </p>
                </div>
                <Button size="sm" onClick={() => setSendDialogOpen(true)}>
                    <Plus className="size-3.5 mr-1" />
                    Send bulk email
                </Button>
            </div>

            <div className="flex items-center gap-2">
                <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as StatusFilter)}>
                    <SelectTrigger className="w-[160px]">
                        <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="all">All statuses</SelectItem>
                        <SelectItem value="active">Active</SelectItem>
                        <SelectItem value="completed">Completed</SelectItem>
                        <SelectItem value="failed-cancelled">Failed & Cancelled</SelectItem>
                    </SelectContent>
                </Select>
            </div>

            {isLoading ? (
                <div className="space-y-2">
                    {[1, 2, 3].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
                </div>
            ) : !jobs || jobs.length === 0 ? (
                <div className="rounded-lg border border-dashed p-10 text-center space-y-3">
                    <div className="mx-auto h-12 w-12 rounded-full bg-muted grid place-items-center">
                        <Mail className="size-5 text-muted-foreground" />
                    </div>
                    <div>
                        <p className="font-medium text-[14px]">No bulk emails yet</p>
                        <p className="text-[13px] text-muted-foreground mt-1">
                            Send targeted emails to registered attendees or external lists.
                            Create reusable templates and track delivery here.
                        </p>
                    </div>

                </div>
            ) : filtered.length === 0 ? (
                <div className="rounded-lg border border-dashed p-8 text-center">
                    <p className="text-[13.5px] text-muted-foreground">No emails match the selected filter.</p>
                </div>
            ) : (
                <div className="rounded-lg border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Type</TableHead>
                                <TableHead>Status</TableHead>
                                <TableHead className="text-right">Recipients</TableHead>
                                <TableHead className="text-right">Sent</TableHead>
                                <TableHead className="text-right">Failed</TableHead>
                                <TableHead>Triggered by</TableHead>
                                <TableHead>Created</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {filtered.map((job) => (
                                <TableRow
                                    key={job.id}
                                    className="cursor-pointer hover:bg-muted/50"
                                    onClick={() =>
                                        (window.location.href = `/teams/${teamId}/events/${eventId}/emails/${job.id}`)
                                    }
                                >
                                    <TableCell className="font-medium text-[13px]">{job.templateName ?? job.emailType}</TableCell>
                                    <TableCell>
                                        <Badge variant={STATUS_VARIANT[job.status]}>
                                            {STATUS_LABEL[job.status]}
                                        </Badge>
                                    </TableCell>
                                    <TableCell className="text-right text-[13px]">{Number(job.recipientCount)}</TableCell>
                                    <TableCell className="text-right text-[13px]">{Number(job.sentCount)}</TableCell>
                                    <TableCell className="text-right text-[13px]">{Number(job.failedCount)}</TableCell>
                                    <TableCell className="text-[13px] text-muted-foreground">
                                        {job.isSystemTriggered ? "System" : (job.triggeredBy ?? "—")}
                                    </TableCell>
                                    <TableCell className="text-[13px] text-muted-foreground">
                                        {formatDate(job.createdAt)}
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                </div>
            )}

            <SendBulkEmailDialog
                teamId={teamId}
                eventId={eventId}
                open={sendDialogOpen}
                onClose={() => setSendDialogOpen(false)}
            />
        </PageLayout>
    );
}
