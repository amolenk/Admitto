"use client";

import { useState, useRef } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Upload, X } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    BulkEmailRecipientPreviewDto,
    CreateBulkEmailResponse,
    EmailTemplateListItemDto,
    EmailTemplateDto,
    TicketTypeDto,
} from "@/lib/admitto-api/generated";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";

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

interface SendBulkEmailDialogProps {
    teamSlug: string;
    eventSlug: string;
    open: boolean;
    onClose: () => void;
}

export function SendBulkEmailDialog({ teamSlug, eventSlug, open, onClose }: SendBulkEmailDialogProps) {
    const queryClient = useQueryClient();
    const fileInputRef = useRef<HTMLInputElement>(null);

    const [step, setStep] = useState<1 | 2>(1);
    const [selectedTemplateId, setSelectedTemplateId] = useState<string>("");
    const [recipientSource, setRecipientSource] = useState<RecipientSource>("attendees");

    const [attendeeTicketType, setAttendeeTicketType] = useState<string>("");
    const [attendeeStatus, setAttendeeStatus] = useState<string>("");
    const [previewResult, setPreviewResult] = useState<{ count: number; sample: BulkEmailRecipientPreviewDto[] } | null>(null);
    const [isPreviewing, setIsPreviewing] = useState(false);

    const [csvRows, setCsvRows] = useState<CsvRow[]>([]);
    const [csvError, setCsvError] = useState<string | null>(null);
    const [csvFileName, setCsvFileName] = useState<string | null>(null);

    const [sendError, setSendError] = useState<string | null>(null);

    const { data: eventTemplates, isLoading: isLoadingEventTemplates } = useQuery({
        queryKey: ["event-email-templates", teamSlug, eventSlug],
        queryFn: () =>
            apiClient.get<EmailTemplateListItemDto[]>(
                `/api/teams/${teamSlug}/events/${eventSlug}/email-templates`
            ),
        enabled: open,
        throwOnError: false,
    });

    const { data: teamTemplates, isLoading: isLoadingTeamTemplates } = useQuery({
        queryKey: ["team-email-templates", teamSlug],
        queryFn: () =>
            apiClient.get<EmailTemplateListItemDto[]>(
                `/api/teams/${teamSlug}/email-templates`
            ),
        enabled: open,
        throwOnError: false,
    });

    const isLoadingTemplates = isLoadingEventTemplates || isLoadingTeamTemplates;

    const templates: (EmailTemplateListItemDto & { scope: "event" | "team" })[] = [
        ...(eventTemplates ?? []).filter((t) => t.kind === "custom" && t.id).map((t) => ({ ...t, scope: "event" as const })),
        ...(teamTemplates ?? []).filter((t) => t.kind === "custom" && t.id).map((t) => ({ ...t, scope: "team" as const })),
    ].sort((a, b) => a.name.localeCompare(b.name));

    const { data: ticketTypes } = useQuery({
        queryKey: ["ticket-types", teamSlug, eventSlug],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types`),
        enabled: open,
        throwOnError: false,
    });

    const sendMutation = useMutation({
        mutationFn: async () => {
            const selectedTemplate = templates.find((t) => t.id === selectedTemplateId);
            const templateApiUrl = selectedTemplate?.scope === "team"
                ? `/api/teams/${teamSlug}/email-templates/${selectedTemplateId}`
                : `/api/teams/${teamSlug}/events/${eventSlug}/email-templates/${selectedTemplateId}`;
            const template = await apiClient.get<EmailTemplateDto>(templateApiUrl);
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
                `/api/teams/${teamSlug}/events/${eventSlug}/bulk-emails`,
                {
                    emailType: "bulk-custom",
                    templateName: selectedTemplate?.name ?? null,
                    subject: template.subject,
                    textBody: template.textBody,
                    htmlBody: template.htmlBody,
                    source,
                }
            );
        },
        onSuccess: () => {
            toast.success("Bulk email queued successfully.");
            queryClient.invalidateQueries({ queryKey: ["bulk-emails", teamSlug, eventSlug] });
            handleClose();
        },
        onError: (err) => {
            setSendError(err instanceof FormError ? err.detail : "Failed to send bulk email.");
        },
    });

    function handleClose() {
        setStep(1);
        setSelectedTemplateId("");
        setRecipientSource("attendees");
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
                `/api/teams/${teamSlug}/events/${eventSlug}/bulk-emails/preview`,
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

    const canProceedToStep2 = !!selectedTemplateId && (templates?.length ?? 0) > 0;
    const canSend =
        recipientSource === "attendees"
            ? true
            : csvRows.length > 0 && !csvError;

    const selectedTemplateName = templates?.find((t) => t.id === selectedTemplateId)?.name;
    const recipientCount =
        recipientSource === "attendees"
            ? previewResult?.count
            : csvRows.length;

    return (
        <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) handleClose(); }}>
            <DialogContent className="max-w-lg">
                <DialogHeader>
                    <DialogTitle>Send bulk email</DialogTitle>
                    <DialogDescription>
                        Step {step} of 2: {step === 1 ? "Select a template" : "Choose recipients"}
                    </DialogDescription>
                </DialogHeader>

                {sendError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>Error</AlertTitle>
                        <AlertDescription>{sendError}</AlertDescription>
                    </Alert>
                )}

                {step === 1 && (
                    <div className="space-y-4">
                        {isLoadingTemplates ? (
                            <div className="space-y-2">
                                <Skeleton className="h-4 w-20" />
                                <Skeleton className="h-9 w-full" />
                            </div>
                        ) : !templates || templates.length === 0 ? (
                            <div className="rounded-lg border border-dashed p-6 text-center space-y-2">
                                <p className="text-[13.5px] text-muted-foreground">
                                    No custom templates yet.
                                </p>
                                <Link
                                    href={`/teams/${teamSlug}/events/${eventSlug}/settings/email/templates`}
                                    className="text-[13.5px] text-primary underline"
                                    onClick={handleClose}
                                >
                                    Create a template →
                                </Link>
                            </div>
                        ) : (
                            <div className="space-y-2">
                                <Label>Template</Label>
                                <Select value={selectedTemplateId} onValueChange={setSelectedTemplateId}>
                                    <SelectTrigger>
                                        <SelectValue placeholder="Select a template..." />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {templates.map((t) => (
                                            <SelectItem key={t.id!} value={t.id!}>
                                                <span className="flex items-center gap-2">
                                                    {t.name}
                                                    {t.scope === "team" && (
                                                        <span className="text-[10px] text-muted-foreground border rounded px-1">Team</span>
                                                    )}
                                                </span>
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                                <p className="text-[12px] text-muted-foreground">
                                    Need a new template?{" "}
                                    <Link
                                        href={`/teams/${teamSlug}/events/${eventSlug}/settings/email/templates`}
                                        className="text-primary underline"
                                        onClick={handleClose}
                                    >
                                        Create one
                                    </Link>
                                </p>
                            </div>
                        )}
                    </div>
                )}

                {step === 2 && (
                    <div className="space-y-4">
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
                                                <SelectItem key={t.slug} value={t.slug}>
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
                                    Template: <span className="text-foreground">{selectedTemplateName}</span>
                                    {recipientCount !== undefined && (
                                        <> · {recipientCount} recipient{recipientCount !== 1 ? "s" : ""}</>
                                    )}
                                </p>
                            </div>
                        )}
                    </div>
                )}

                <DialogFooter>
                    {step === 1 ? (
                        <>
                            <Button variant="outline" onClick={handleClose}>Cancel</Button>
                            <Button
                                onClick={() => setStep(2)}
                                disabled={!canProceedToStep2}
                            >
                                Next
                            </Button>
                        </>
                    ) : (
                        <>
                            <Button variant="outline" onClick={() => setStep(1)}>Back</Button>
                            <Button
                                onClick={() => sendMutation.mutate()}
                                disabled={!canSend || sendMutation.isPending}
                            >
                                {sendMutation.isPending ? "Sending…" : "Send"}
                            </Button>
                        </>
                    )}
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}
