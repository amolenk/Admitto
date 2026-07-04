"use client";

import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    BulkEmailRecipientPreviewDto,
    CreateBulkEmailHttpRequest,
    CreateBulkEmailResponse,
    PreviewBulkEmailHttpRequest,
    PreviewBulkEmailResponse,
    RegistrationStatus,
    TicketTypeDto,
} from "@/lib/admitto-api/generated";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
    Sheet,
    SheetBody,
    SheetContent,
    SheetDescription,
    SheetFooter,
    SheetHeader,
    SheetTitle,
} from "@/components/ui/sheet";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

interface SendBulkEmailSheetProps {
    teamId: string;
    eventId: string;
    open: boolean;
    onClose: () => void;
}

export function SendBulkEmailSheet({ teamId, eventId, open, onClose }: SendBulkEmailSheetProps) {
    const queryClient = useQueryClient();

    const [subject, setSubject] = useState("");
    const [textBody, setTextBody] = useState("");
    const [htmlBody, setHtmlBody] = useState("");

    const [attendeeTicketType, setAttendeeTicketType] = useState<string>("");
    const [attendeeStatus, setAttendeeStatus] = useState<string>("");
    const [previewResult, setPreviewResult] = useState<PreviewBulkEmailResponse | null>(null);
    const [isPreviewing, setIsPreviewing] = useState(false);

    const [sendError, setSendError] = useState<string | null>(null);

    const { data: ticketTypes } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`),
        enabled: open,
        throwOnError: false,
    });

    function buildAttendeeFilter() {
        return {
            ticketTypeIds: attendeeTicketType ? [attendeeTicketType] : null,
            registrationStatus: (attendeeStatus || null) as RegistrationStatus | null,
        };
    }

    const sendMutation = useMutation({
        mutationFn: async () => {
            const body: CreateBulkEmailHttpRequest = {
                emailType: "bulk-custom",
                subject,
                textBody,
                htmlBody,
                attendeeFilter: buildAttendeeFilter(),
            };
            return apiClient.post<CreateBulkEmailResponse>(
                `/api/teams/${teamId}/events/${eventId}/bulk-emails`,
                body
            );
        },
        onSuccess: () => {
            toast.success("Bulk email queued successfully.");
            queryClient.invalidateQueries({ queryKey: ["bulk-emails", teamId, eventId] });
            handleClose();
        },
        onError: (err) => {
            setSendError(err instanceof FormError ? err.detail : "Failed to send bulk email.");
        },
    });

    function handleClose() {
        setSubject("");
        setTextBody("");
        setHtmlBody("");
        setAttendeeTicketType("");
        setAttendeeStatus("");
        setPreviewResult(null);
        setSendError(null);
        onClose();
    }

    async function handlePreview() {
        setIsPreviewing(true);
        setPreviewResult(null);
        try {
            const body: PreviewBulkEmailHttpRequest = {
                attendeeFilter: buildAttendeeFilter(),
            };
            const result = await apiClient.post<PreviewBulkEmailResponse>(
                `/api/teams/${teamId}/events/${eventId}/bulk-emails/preview`,
                body
            );
            setPreviewResult(result);
        } catch {
            // ignore preview errors
        } finally {
            setIsPreviewing(false);
        }
    }

    const hasContent = subject.trim().length > 0 && textBody.trim().length > 0 && htmlBody.trim().length > 0;
    const canSend = hasContent;

    const recipientCount = previewResult?.count;

    return (
        <Sheet open={open} onOpenChange={(isOpen) => { if (!isOpen) handleClose(); }}>
            <SheetContent side="right" className="sm:max-w-lg">
                <SheetHeader>
                    <SheetTitle>Send bulk email</SheetTitle>
                    <SheetDescription>
                        Write one-off content, then choose which registered attendees receive it.
                    </SheetDescription>
                </SheetHeader>

                <SheetBody className="space-y-5">
                    {sendError && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>Error</AlertTitle>
                            <AlertDescription>{sendError}</AlertDescription>
                        </Alert>
                    )}

                    <div className="space-y-4">
                        <div className="space-y-1.5">
                            <Label>Subject</Label>
                            <Input value={subject} onChange={(event) => setSubject(event.target.value)} placeholder="Important update for {{ event_name }}" />
                        </div>
                        <div className="space-y-1.5">
                            <Label>Text body</Label>
                            <Textarea value={textBody} onChange={(event) => setTextBody(event.target.value)} rows={5} placeholder="Plain-text version. Scriban variables like {{ first_name }} are supported." />
                        </div>
                        <div className="space-y-1.5">
                            <Label>HTML body</Label>
                            <Textarea value={htmlBody} onChange={(event) => setHtmlBody(event.target.value)} rows={7} className="font-mono text-[13px]" placeholder="<p>Hello {{ first_name }},</p>" />
                        </div>
                    </div>

                    <div className="space-y-4 border-t pt-4">
                        <div>
                            <h3 className="text-sm font-medium">Recipients</h3>
                            <p className="text-[12px] text-muted-foreground">Target registered attendees for this event.</p>
                        </div>

                        <div className="space-y-3">
                            <div className="space-y-1.5">
                                <Label>Ticket type</Label>
                                <Select
                                    value={attendeeTicketType || "__all__"}
                                    onValueChange={(v) => setAttendeeTicketType(v === "__all__" ? "" : v)}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="All ticket types" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="__all__">All ticket types</SelectItem>
                                        {ticketTypes?.map((t) => (
                                            <SelectItem key={t.id} value={t.id}>
                                                {t.name}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            <div className="space-y-1.5">
                                <Label>Registration status</Label>
                                <Select
                                    value={attendeeStatus || "__all__"}
                                    onValueChange={(v) => setAttendeeStatus(v === "__all__" ? "" : v)}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="All statuses" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="__all__">All statuses</SelectItem>
                                        <SelectItem value="registered">Registered</SelectItem>
                                        <SelectItem value="cancelled">Cancelled</SelectItem>
                                    </SelectContent>
                                </Select>
                            </div>
                            <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={handlePreview}
                                disabled={isPreviewing}
                            >
                                {isPreviewing ? "Loading…" : "Preview recipients"}
                            </Button>
                            {previewResult && (
                                <div className="rounded-md bg-muted p-3 text-[13px] space-y-1">
                                    <p className="font-medium">{previewResult.count} recipient{previewResult.count !== 1 ? "s" : ""} matched</p>
                                    {previewResult.sample.length > 0 && (
                                        <ul className="text-muted-foreground space-y-0.5">
                                            {previewResult.sample.slice(0, 5).map((r: BulkEmailRecipientPreviewDto) => (
                                                <li key={r.email}>{r.displayName ? `${r.displayName} <${r.email}>` : r.email}</li>
                                            ))}
                                            {previewResult.sample.length < Number(previewResult.count) && (
                                                <li className="italic">and more…</li>
                                            )}
                                        </ul>
                                    )}
                                </div>
                            )}
                        </div>

                        {canSend && (
                            <div className="rounded-md border p-3 text-[13px]">
                                <p className="font-medium">Ready to send</p>
                                <p className="text-muted-foreground">
                                    {recipientCount !== undefined
                                        ? `${recipientCount} recipient${recipientCount !== 1 ? "s" : ""}`
                                        : "Preview to see how many attendees match"}
                                </p>
                            </div>
                        )}
                    </div>
                </SheetBody>

                <SheetFooter>
                    <Button variant="outline" onClick={handleClose}>Cancel</Button>
                    <Button
                        onClick={() => sendMutation.mutate()}
                        disabled={!canSend || sendMutation.isPending}
                    >
                        {sendMutation.isPending ? "Sending…" : "Send"}
                    </Button>
                </SheetFooter>
            </SheetContent>
        </Sheet>
    );
}
