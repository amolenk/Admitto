"use client";

import { useEffect, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
    AlertCircle,
    Check,
    ChevronDown,
    ChevronUp,
    RefreshCw,
    Send,
    Trash2,
    X,
} from "lucide-react";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
    AlertDialog,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import {
    Dialog,
    DialogContent,
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
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    buildEmailRecipientOptions,
    EmailRecipientOption,
} from "../../../events/[eventSlug]/settings/email/test-email-settings-button";
import { PreviewEmailTemplateDto, TeamDto, TeamMemberListItemDto } from "@/lib/admitto-api/generated";

const templateSchema = z.object({
    subject: z.string().min(1, "Subject is required"),
    textBody: z.string().min(1, "Text body is required"),
    htmlBody: z.string().min(1, "HTML body is required"),
});

type TemplateValues = z.infer<typeof templateSchema>;

function Field({
    label,
    hint,
    children,
}: {
    label: string;
    hint?: string;
    children: React.ReactNode;
}) {
    return (
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
            <div>
                <label className="text-[13.5px] font-medium">{label}</label>
                {hint && <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">{hint}</p>}
            </div>
            <div className="min-w-0">{children}</div>
        </div>
    );
}

function PreviewPanel({
    previewApiUrl,
    formValues,
}: {
    previewApiUrl: string;
    formValues: { subject: string; textBody: string; htmlBody: string };
}) {
    const [preview, setPreview] = useState<PreviewEmailTemplateDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [isOpen, setIsOpen] = useState(true);

    async function loadPreview() {
        setIsLoading(true);
        setError(null);
        try {
            const data = await apiClient.post<PreviewEmailTemplateDto>(previewApiUrl, formValues);
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
        loadPreview();
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [previewApiUrl]);

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
                            onClick={loadPreview}
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
                                    srcDoc={preview.renderedHtmlBody}
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

function SendTestEmailDialog({
    apiUrl,
    recipients,
    onClose,
}: {
    apiUrl: string;
    recipients: EmailRecipientOption[];
    onClose: () => void;
}) {
    const defaultRecipient = recipients[0]?.value ?? "";
    const [recipient, setRecipient] = useState(defaultRecipient);
    const [isSending, setIsSending] = useState(false);
    const [result, setResult] = useState<
        { type: "success"; message: string } | { type: "error"; message: string } | null
    >(null);

    const selectedLabel = recipients.find((o) => o.value === recipient)?.label ?? recipient;

    async function handleSend() {
        if (!recipient) return;
        setIsSending(true);
        setResult(null);
        try {
            await apiClient.post(apiUrl, { recipient });
            setResult({ type: "success", message: `Test email sent to ${selectedLabel}` });
        } catch (err) {
            const message = err instanceof FormError
                ? err.detail
                : err instanceof Error
                    ? err.message
                    : "Failed to send test email.";
            setResult({ type: "error", message });
        } finally {
            setIsSending(false);
        }
    }

    return (
        <DialogContent>
            <DialogHeader>
                <DialogTitle>Send test email</DialogTitle>
            </DialogHeader>
            <div className="space-y-4 py-2">
                <p className="text-sm text-muted-foreground">
                    This sends the template rendered with sample data to verify it looks correct.
                </p>
                <div>
                    <label className="text-sm font-medium leading-none mb-2 block">Recipient</label>
                    <Select
                        value={recipient}
                        onValueChange={setRecipient}
                        disabled={recipients.length === 0 || isSending}
                    >
                        <SelectTrigger>
                            <SelectValue placeholder="Select recipient" />
                        </SelectTrigger>
                        <SelectContent>
                            {recipients.map((option) => (
                                <SelectItem key={option.value} value={option.value}>
                                    {option.label}
                                </SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>

                {result && (
                    <Alert
                        variant={result.type === "error" ? "destructive" : "default"}
                    >
                        {result.type === "error" ? (
                            <AlertCircle className="h-4 w-4" />
                        ) : (
                            <Check className="h-4 w-4" />
                        )}
                        <div className="flex items-start justify-between gap-3">
                            <div>
                                <AlertTitle>
                                    {result.type === "error" ? "Error" : "Sent"}
                                </AlertTitle>
                                <AlertDescription>{result.message}</AlertDescription>
                            </div>
                            <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                className="h-7 w-7 shrink-0"
                                onClick={() => setResult(null)}
                            >
                                <X className="size-3.5" />
                                <span className="sr-only">Dismiss</span>
                            </Button>
                        </div>
                    </Alert>
                )}
            </div>
            <DialogFooter>
                <Button type="button" variant="outline" onClick={onClose}>
                    Close
                </Button>
                <Button
                    type="button"
                    onClick={handleSend}
                    disabled={!recipient || recipients.length === 0 || isSending}
                >
                    <Send className="size-3.5" />
                    {isSending ? "Sending..." : "Send test email"}
                </Button>
            </DialogFooter>
        </DialogContent>
    );
}

export function EmailTemplateForm({
    templateApiUrl,
    previewApiUrl,
    testSendApiUrl,
    queryKey,
    backHref,
    initialValues,
    isCustom,
    version,
    teamSlug,
}: {
    templateApiUrl: string;
    previewApiUrl: string;
    testSendApiUrl: string;
    queryKey: unknown[];
    backHref: string;
    initialValues: { subject: string; textBody: string; htmlBody: string } | null;
    isCustom: boolean;
    version: number | string | null;
    teamSlug: string;
}) {
    const queryClient = useQueryClient();
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [testSendDialogOpen, setTestSendDialogOpen] = useState(false);

    const [team, setTeam] = useState<TeamDto | null>(null);
    const [members, setMembers] = useState<TeamMemberListItemDto[] | null>(null);

    useEffect(() => {
        apiClient.get<TeamDto>(`/api/teams/${teamSlug}`).then(setTeam).catch(() => {});
        apiClient.get<TeamMemberListItemDto[]>(`/api/teams/${teamSlug}/members`).then(setMembers).catch(() => {});
    }, [teamSlug]);

    const recipients = buildEmailRecipientOptions(team, members);

    const form = useCustomForm<TemplateValues>(templateSchema, {
        subject: initialValues?.subject ?? "",
        textBody: initialValues?.textBody ?? "",
        htmlBody: initialValues?.htmlBody ?? "",
    });

    const { generalError, submit } = form;

    const isDirty = form.formState.isDirty;
    const isSubmitting = form.formState.isSubmitting;
    const formValues = form.watch();

    async function onSubmit(values: TemplateValues) {
        const body = { ...values, version };
        await apiClient.put(templateApiUrl, body);
        await queryClient.invalidateQueries({ queryKey });
        form.reset(values);
    }

    async function handleDelete() {
        setIsDeleting(true);
        setDeleteError(null);
        try {
            await apiClient.delete(`${templateApiUrl}?version=${version}`);
            await queryClient.invalidateQueries({ queryKey });
            setDeleteDialogOpen(false);
        } catch (err) {
            setDeleteError(err instanceof Error ? err.message : "Failed to delete template.");
        } finally {
            setIsDeleting(false);
        }
    }

    return (
        <div>
            {generalError && (
                <Alert variant="destructive" className="mb-4">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{generalError.title}</AlertTitle>
                    <AlertDescription>{generalError.detail}</AlertDescription>
                </Alert>
            )}

            <Form {...form}>
                <form onSubmit={submit(onSubmit)}>
                    <Card>
                        <div className="px-6 divide-y">
                        <FormField
                            control={form.control}
                            name="subject"
                            render={({ field }) => (
                                <FormItem>
                                    <Field label="Subject" hint="The email subject line. Supports template variables.">
                                        <FormControl>
                                            <Input placeholder="e.g. Your ticket for {{ event_name }}" {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </Field>
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="textBody"
                            render={({ field }) => (
                                <FormItem>
                                    <Field label="Text body" hint="Plain-text fallback for email clients that don't render HTML.">
                                        <FormControl>
                                            <Textarea
                                                className="font-mono text-[12px] min-h-40"
                                                {...field}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </Field>
                                </FormItem>
                            )}
                        />
                        <FormField
                            control={form.control}
                            name="htmlBody"
                            render={({ field }) => (
                                <FormItem>
                                    <Field label="HTML body" hint="Rendered HTML email body. Supports Scriban template syntax.">
                                        <FormControl>
                                            <Textarea
                                                className="font-mono text-[12px] min-h-64"
                                                {...field}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </Field>
                                </FormItem>
                            )}
                        />
                        </div>
                    </Card>

                    <div className="flex items-center justify-between mt-4 gap-4">
                        <div className="flex items-center gap-2">
                            <Button type="submit" size="sm" disabled={!isDirty || isSubmitting}>
                                <Check className="size-3.5" />
                                {isSubmitting ? "Saving..." : "Save changes"}
                            </Button>
                            <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={() => setTestSendDialogOpen(true)}
                            >
                                <Send className="size-3.5" />
                                Send test email
                            </Button>
                        </div>

                        {isCustom && (
                            <AlertDialog
                                open={deleteDialogOpen}
                                onOpenChange={(open) => {
                                    setDeleteDialogOpen(open);
                                    if (!open) setDeleteError(null);
                                }}
                            >
                                <AlertDialogTrigger asChild>
                                    <Button
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        className="text-destructive border-destructive/30"
                                    >
                                        <Trash2 className="size-3.5" />
                                        Delete custom template
                                    </Button>
                                </AlertDialogTrigger>
                                <AlertDialogContent>
                                    <AlertDialogHeader>
                                        <AlertDialogTitle>Delete custom template?</AlertDialogTitle>
                                        <AlertDialogDescription>
                                            This will remove the custom template and restore the built-in default.
                                        </AlertDialogDescription>
                                    </AlertDialogHeader>
                                    {deleteError && (
                                        <Alert variant="destructive">
                                            <AlertCircle className="h-4 w-4" />
                                            <AlertDescription>{deleteError}</AlertDescription>
                                        </Alert>
                                    )}
                                    <AlertDialogFooter>
                                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                                        <Button
                                            variant="destructive"
                                            onClick={handleDelete}
                                            disabled={isDeleting}
                                        >
                                            {isDeleting ? "Deleting…" : "Delete"}
                                        </Button>
                                    </AlertDialogFooter>
                                </AlertDialogContent>
                            </AlertDialog>
                        )}
                    </div>
                </form>
            </Form>

            <PreviewPanel previewApiUrl={previewApiUrl} formValues={formValues} />

            <Dialog open={testSendDialogOpen} onOpenChange={setTestSendDialogOpen}>
                <SendTestEmailDialog
                    apiUrl={testSendApiUrl}
                    recipients={recipients}
                    onClose={() => setTestSendDialogOpen(false)}
                />
            </Dialog>
        </div>
    );
}
