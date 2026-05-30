"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import {
    AlertCircle,
    Check,
    Send,
    Trash2,
    X,
} from "lucide-react";
import { z } from "zod";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import {
    buildEmailRecipientOptions,
    EmailRecipientOption,
} from "../../../events/[eventId]/settings/email/test-email-settings-button";
import { CodeEditor } from "@/components/ui/code-editor";
import { TeamDto, TeamMemberListItemDto, PreviewEmailTemplateDto } from "@/lib/admitto-api/generated";

const templateSchema = z.object({
    subject: z.string().min(1, "Subject is required"),
    textBody: z.string().min(1, "Text body is required"),
    htmlBody: z.string().min(1, "HTML body is required"),
});

type TemplateValues = z.infer<typeof templateSchema>;

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

type PreviewValues = { subject: string; textBody: string; htmlBody: string };

function PreviewTabContent({
    previewApiUrl,
    values,
}: {
    previewApiUrl: string;
    values: PreviewValues;
}) {
    const [preview, setPreview] = useState<PreviewEmailTemplateDto | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        setIsLoading(true);
        setError(null);
        apiClient.post<PreviewEmailTemplateDto>(previewApiUrl, values)
            .then((data) => { if (!cancelled) setPreview(data); })
            .catch((err) => {
                if (!cancelled) {
                    const message = err instanceof FormError
                        ? err.detail
                        : err instanceof Error ? err.message : "Failed to load preview.";
                    setError(message);
                }
            })
            .finally(() => { if (!cancelled) setIsLoading(false); });
        return () => { cancelled = true; };
    // Fetch every time this component mounts (i.e. every time the tab is activated)
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <div className="px-6 py-4">
            <p className="text-[12px] text-muted-foreground mb-4">
                Rendered with sample placeholder data.
            </p>

            {error && (
                <Alert variant="destructive" className="mb-4">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            {isLoading && (
                <div className="space-y-3">
                    <Skeleton className="h-5 w-3/4" />
                    <Skeleton className="h-64 w-full" />
                </div>
            )}

            {!isLoading && preview && (
                <div className="space-y-4">
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
                            className="mt-2 w-full border rounded-md"
                            style={{ minHeight: "500px" }}
                            srcDoc={preview.renderedHtmlBody ?? ""}
                            sandbox=""
                            title="Email preview"
                        />
                    </div>
                </div>
            )}
        </div>
    );
}

export function EmailTemplateForm({
    templateApiUrl,
    previewApiUrl,
    testSendApiUrl,
    queryKey,
    backHref,
    initialValues,
    isCustomised,
    version,
    teamId,
    eventId,
}: {
    templateApiUrl: string;
    previewApiUrl: string;
    testSendApiUrl: string;
    queryKey: unknown[];
    backHref: string;
    initialValues: { subject: string; textBody: string; htmlBody: string } | null;
    isCustomised: boolean;
    version: number | string | null;
    teamId: string;
    eventId?: string;
}) {
    const queryClient = useQueryClient();
    const router = useRouter();
    const [isDeleting, setIsDeleting] = useState(false);
    const [deleteError, setDeleteError] = useState<string | null>(null);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [testSendDialogOpen, setTestSendDialogOpen] = useState(false);
    const [activeTab, setActiveTab] = useState("html");
    const [previewKey, setPreviewKey] = useState(0);
    const [previewValues, setPreviewValues] = useState<TemplateValues | null>(null);

    const [team, setTeam] = useState<TeamDto | null>(null);
    const [members, setMembers] = useState<TeamMemberListItemDto[] | null>(null);
    const [fromAddress, setFromAddress] = useState<string | null>(null);

    useEffect(() => {
        apiClient.get<TeamDto>(`/api/teams/${teamId}`).then(setTeam).catch(() => {});
        apiClient.get<TeamMemberListItemDto[]>(`/api/teams/${teamId}/members`).then(setMembers).catch(() => {});

        async function resolveFromAddress() {
            if (eventId) {
                const res = await fetch(`/api/teams/${teamId}/events/${eventId}/email-settings`);
                if (res.ok) {
                    const eventSettings = await res.json() as { fromAddress?: string | null };
                    if (eventSettings.fromAddress) {
                        setFromAddress(eventSettings.fromAddress);
                        return;
                    }
                }
                // 404 = event has no override; fall through to team settings
            }
            apiClient.get<{ fromAddress?: string | null }>(`/api/teams/${teamId}/email-settings`)
                .then((s) => setFromAddress(s.fromAddress ?? null))
                .catch(() => {});
        }
        resolveFromAddress();
    }, [teamId, eventId]);

    const recipients = buildEmailRecipientOptions(team, members, fromAddress);

    const form = useCustomForm<TemplateValues>(templateSchema, {
        subject: initialValues?.subject ?? "",
        textBody: initialValues?.textBody ?? "",
        htmlBody: initialValues?.htmlBody ?? "",
    });

    useEffect(() => {
        form.reset({
            subject: initialValues?.subject ?? "",
            textBody: initialValues?.textBody ?? "",
            htmlBody: initialValues?.htmlBody ?? "",
        });
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [initialValues]);

    const { generalError, submit } = form;

    const isDirty = form.formState.isDirty;
    const isSubmitting = form.formState.isSubmitting;
    const formValues = form.watch();

    function handleTabChange(tab: string) {
        setActiveTab(tab);
        if (tab === "preview") {
            setPreviewValues(formValues);
            setPreviewKey((k) => k + 1);
        }
    }

    async function onSubmit(values: TemplateValues) {
        const body = { ...values, version };
        await apiClient.put(templateApiUrl, body);
        await queryClient.invalidateQueries({ queryKey });
        router.push(backHref);
    }

    async function handleDelete() {
        setIsDeleting(true);
        setDeleteError(null);
        try {
            await apiClient.delete(`${templateApiUrl}?version=${version}`);
            queryClient.removeQueries({ queryKey });
            router.push(backHref);
        } catch (err) {
            setDeleteError(err instanceof Error ? err.message : "Failed to delete template.");
        } finally {
            setIsDeleting(false);
        }
    }

    return (
        <div>
            <Form {...form}>
                <form onSubmit={submit(onSubmit)}>
                    <div className="flex items-center justify-between mb-5">
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setTestSendDialogOpen(true)}
                        >
                            <Send className="size-3.5" />
                            Send test email
                        </Button>

                        <div className="flex items-center gap-2">
                            {isCustomised && (
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

                            <Button type="submit" size="sm" disabled={!isDirty || isSubmitting}>
                                <Check className="size-3.5" />
                                {isSubmitting ? "Saving..." : "Save changes"}
                            </Button>
                        </div>
                    </div>

                    {generalError && (
                        <Alert variant="destructive" className="mb-4">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>{generalError.title}</AlertTitle>
                            <AlertDescription>{generalError.detail}</AlertDescription>
                        </Alert>
                    )}

                    <Card className="overflow-hidden">
                        <div className="px-6 py-4 border-b">
                            <FormField
                                control={form.control}
                                name="subject"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Subject</FormLabel>
                                        <p className="text-[12px] text-muted-foreground -mt-1">
                                            Supports template variables like{" "}
                                            <code className="text-[11px] bg-muted px-1 py-0.5 rounded">{"{{ event_name }}"}</code>
                                        </p>
                                        <FormControl>
                                            <Input placeholder="e.g. Your ticket for {{ event_name }}" {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </div>

                        <Tabs value={activeTab} onValueChange={handleTabChange}>
                            <div className="border-b px-6 pt-0 pb-2.5">
                                <TabsList className="-mt-1">
                                    <TabsTrigger value="html">HTML</TabsTrigger>
                                    <TabsTrigger value="text">Text</TabsTrigger>
                                    <TabsTrigger value="preview">Preview</TabsTrigger>
                                </TabsList>
                            </div>

                            <TabsContent value="html" className="mt-0 min-h-[460px]">
                                <FormField
                                    control={form.control}
                                    name="htmlBody"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormControl>
                                                <CodeEditor
                                                    value={field.value}
                                                    onChange={field.onChange}
                                                    minHeight="460px"
                                                    className="rounded-none border-none shadow-none"
                                                />
                                            </FormControl>
                                            <FormMessage className="px-6 pb-4" />
                                        </FormItem>
                                    )}
                                />
                            </TabsContent>

                            <TabsContent value="text" className="mt-0 p-6 min-h-[460px]">
                                <FormField
                                    control={form.control}
                                    name="textBody"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormControl>
                                                <Textarea
                                                    className="font-mono text-[12px] min-h-[400px]"
                                                    {...field}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            </TabsContent>

                            <TabsContent value="preview" className="mt-0 min-h-[460px]">
                                {previewValues && (
                                    <PreviewTabContent
                                        key={previewKey}
                                        previewApiUrl={previewApiUrl}
                                        values={previewValues}
                                    />
                                )}
                            </TabsContent>
                        </Tabs>
                    </Card>
                </form>
            </Form>

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
