"use client";

import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Upload, X } from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    BulkEmailRecipientPreviewDto,
    CreateBulkEmailResponse,
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

const CSV_ROW_LIMIT = 5000;

interface CsvRow {
    email: string;
    displayName: string | null;
}

function parseCsv(text: string): CsvRow[] {
    const lines = text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
    if (lines.length === 0) return [];
    const firstCols = lines[0].split(",").map((c) => c.trim().toLowerCase());
    const emailIdx = firstCols.findIndex((c) => c === "email");
    const nameIdx = firstCols.findIndex((c) => c === "name" || c === "displayname");
    const hasHeader = emailIdx !== -1;
    const dataLines = hasHeader ? lines.slice(1) : lines;
    const eIdx = hasHeader ? emailIdx : 0;
    const nIdx = hasHeader ? nameIdx : -1;
    return dataLines.map((line) => {
        const cols = line.split(",").map((c) => c.trim().replace(/^"|"$/g, ""));
        return {
            email: cols[eIdx] ?? "",
            displayName: nIdx >= 0 && cols[nIdx] ? cols[nIdx] : null,
        };
    }).filter((r) => r.email.includes("@"));
}

type RecipientSource = "attendees" | "csv";

interface SendBulkEmailSheetProps {
    teamId: string;
    eventId: string;
    open: boolean;
    onClose: () => void;
}

export function SendBulkEmailSheet({ teamId, eventId, open, onClose }: SendBulkEmailSheetProps) {
    const queryClient = useQueryClient();
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [recipientSource, setRecipientSource] = useState<RecipientSource>("attendees");
    const [subject, setSubject] = useState("");
    const [textBody, setTextBody] = useState("");
    const [htmlBody, setHtmlBody] = useState("");

    const [attendeeTicketType, setAttendeeTicketType] = useState<string>("");
    const [attendeeStatus, setAttendeeStatus] = useState<string>("");
    const [previewResult, setPreviewResult] = useState<{ count: number; sample: BulkEmailRecipientPreviewDto[] } | null>(null);
    const [isPreviewing, setIsPreviewing] = useState(false);

    const [csvRows, setCsvRows] = useState<CsvRow[]>([]);
    const [csvError, setCsvError] = useState<string | null>(null);
    const [csvFileName, setCsvFileName] = useState<string | null>(null);

    const [sendError, setSendError] = useState<string | null>(null);

    const { data: ticketTypes } = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`),
        enabled: open,
        throwOnError: false,
    });

    const sendMutation = useMutation({
        mutationFn: async () => {
            const source =
                recipientSource === "attendees"
                    ? {
                        attendee: {
                            ticketTypeSlugs: attendeeTicketType ? [attendeeTicketType] : null,
                            registrationStatus: (attendeeStatus || null) as "registered" | "cancelled" | null,
                        },
                    }
                    : {
                        externalList: {
                            items: csvRows.map((r) => ({
                                email: r.email,
                                displayName: r.displayName,
                            })),
                        },
                    };
            return apiClient.post<CreateBulkEmailResponse>(
                `/api/teams/${teamId}/events/${eventId}/bulk-emails`,
                {
                    emailType: "bulk-custom",
                    subject,
                    textBody,
                    htmlBody,
                    source,
                }
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
        setRecipientSource("attendees");
        setSubject("");
        setTextBody("");
        setHtmlBody("");
        setAttendeeTicketType("");
        setAttendeeStatus("");
        setPreviewResult(null);
        setCsvRows([]);
        setCsvError(null);
        setCsvFileName(null);
        setSendError(null);
        onClose();
    }

    async function handlePreview() {
        setIsPreviewing(true);
        setPreviewResult(null);
        try {
            const result = await apiClient.post<{ count: number; sample: BulkEmailRecipientPreviewDto[] }>(
                `/api/teams/${teamId}/events/${eventId}/bulk-emails/preview`,
                {
                    source: {
                        attendee: {
                            ticketTypeSlugs: attendeeTicketType ? [attendeeTicketType] : null,
                            registrationStatus: (attendeeStatus || null) as "registered" | "cancelled" | null,
                        },
                    },
                }
            );
            setPreviewResult(result);
        } catch {
            // ignore preview errors
        } finally {
            setIsPreviewing(false);
        }
    }

    function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
        const file = e.target.files?.[0];
        if (!file) return;
        setCsvFileName(file.name);
        setCsvError(null);
        const reader = new FileReader();
        reader.onload = (ev) => {
            const text = ev.target?.result as string;
            const rows = parseCsv(text);
            if (rows.length > CSV_ROW_LIMIT) {
                setCsvError(`CSV exceeds ${CSV_ROW_LIMIT.toLocaleString()} rows. Please split the file.`);
                setCsvRows([]);
            } else if (rows.length === 0) {
                setCsvError("No valid email addresses found in the file.");
                setCsvRows([]);
            } else {
                setCsvRows(rows);
            }
        };
        reader.readAsText(file);
    }

    const hasContent = subject.trim().length > 0 && textBody.trim().length > 0 && htmlBody.trim().length > 0;
    const canSend =
        hasContent && recipientSource === "attendees"
            ? true
            : hasContent && csvRows.length > 0 && !csvError;

    const recipientCount =
        recipientSource === "attendees"
            ? previewResult?.count
            : csvRows.length;

    return (
        <Sheet open={open} onOpenChange={(isOpen) => { if (!isOpen) handleClose(); }}>
            <SheetContent side="right" className="sm:max-w-lg">
                <SheetHeader>
                    <SheetTitle>Send bulk email</SheetTitle>
                    <SheetDescription>
                        Write one-off content, then choose recipients.
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
                            <p className="text-[12px] text-muted-foreground">Choose registered attendees or upload an external CSV list.</p>
                        </div>
                            <div className="flex gap-2">
                                <Button
                                    variant={recipientSource === "attendees" ? "default" : "outline"}
                                    size="sm"
                                    onClick={() => setRecipientSource("attendees")}
                                >
                                    Registered attendees
                                </Button>
                                <Button
                                    variant={recipientSource === "csv" ? "default" : "outline"}
                                    size="sm"
                                    onClick={() => setRecipientSource("csv")}
                                >
                                    External list (CSV)
                                </Button>
                            </div>

                            {recipientSource === "attendees" && (
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
                                                    {previewResult.sample.slice(0, 5).map((r) => (
                                                        <li key={r.email}>{r.displayName ? `${r.displayName} <${r.email}>` : r.email}</li>
                                                    ))}
                                                    {previewResult.sample.length < previewResult.count && (
                                                        <li className="italic">and more…</li>
                                                    )}
                                                </ul>
                                            )}
                                        </div>
                                    )}
                                </div>
                            )}

                            {recipientSource === "csv" && (
                                <div className="space-y-3">
                                    <input
                                        ref={fileInputRef}
                                        type="file"
                                        accept=".csv,text/csv"
                                        className="hidden"
                                        onChange={handleFileChange}
                                    />
                                    <Button
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        onClick={() => fileInputRef.current?.click()}
                                    >
                                        <Upload className="size-3.5 mr-1" />
                                        {csvFileName ?? "Upload CSV file"}
                                    </Button>
                                    <p className="text-[12px] text-muted-foreground">
                                        CSV must have an <code>email</code> column. Optional <code>name</code> column. Max {CSV_ROW_LIMIT.toLocaleString()} rows.
                                    </p>
                                    {csvError && (
                                        <Alert variant="destructive">
                                            <AlertCircle className="h-4 w-4" />
                                            <AlertDescription>{csvError}</AlertDescription>
                                        </Alert>
                                    )}
                                    {csvRows.length > 0 && !csvError && (
                                        <div className="rounded-md bg-muted p-3 text-[13px] space-y-1">
                                            <div className="flex items-center justify-between">
                                                <p className="font-medium">{csvRows.length} recipient{csvRows.length !== 1 ? "s" : ""} loaded</p>
                                                <button
                                                    type="button"
                                                    onClick={() => { setCsvRows([]); setCsvFileName(null); if (fileInputRef.current) fileInputRef.current.value = ""; }}
                                                    className="text-muted-foreground hover:text-foreground"
                                                >
                                                    <X className="size-3.5" />
                                                </button>
                                            </div>
                                            <ul className="text-muted-foreground space-y-0.5">
                                                {csvRows.slice(0, 5).map((r, i) => (
                                                    <li key={i}>{r.displayName ? `${r.displayName} <${r.email}>` : r.email}</li>
                                                ))}
                                                {csvRows.length > 5 && <li className="italic">and {csvRows.length - 5} more…</li>}
                                            </ul>
                                        </div>
                                    )}
                                </div>
                            )}

                            {canSend && (
                                <div className="rounded-md border p-3 text-[13px]">
                                    <p className="font-medium">Ready to send</p>
                                    <p className="text-muted-foreground">
                                        {recipientCount !== undefined
                                            ? `${recipientCount} recipient${recipientCount !== 1 ? "s" : ""}`
                                            : "Recipients selected"}
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
