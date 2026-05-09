"use client";

import { useEffect, useMemo, useState } from "react";
import { AlertCircle, CheckCircle2, Send, X } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { FormError } from "@/components/form-error";
import { apiClient } from "@/lib/api-client";
import { TeamDto, TeamMemberListItemDto } from "@/lib/admitto-api/generated";

export type EmailRecipientOption = {
    value: string;
    label: string;
};

export function buildEmailRecipientOptions(
    team: TeamDto | null | undefined,
    members: TeamMemberListItemDto[] | null | undefined
): EmailRecipientOption[] {
    const options: EmailRecipientOption[] = [];
    const seen = new Set<string>();

    function add(email: string | null | undefined) {
        const normalized = email?.trim().toLowerCase();
        if (!normalized || seen.has(normalized)) {
            return;
        }

        seen.add(normalized);
        options.push({ value: normalized, label: normalized });
    }

    add(team?.emailAddress);
    members?.forEach((member) => add(member.email));

    return options;
}

export function TestEmailSettingsButton({
    apiUrl,
    recipients,
}: {
    apiUrl: string;
    recipients: EmailRecipientOption[];
}) {
    const defaultRecipient = recipients[0]?.value ?? "";
    const [recipient, setRecipient] = useState(defaultRecipient);
    const [isSending, setIsSending] = useState(false);
    const [result, setResult] = useState<
        | { type: "success"; message: string }
        | { type: "error"; message: string }
        | null
    >(null);

    useEffect(() => {
        if (!recipient || !recipients.some((option) => option.value === recipient)) {
            setRecipient(defaultRecipient);
        }
    }, [defaultRecipient, recipient, recipients]);

    const hasRecipients = recipients.length > 0;

    const selectedLabel = useMemo(
        () => recipients.find((option) => option.value === recipient)?.label ?? recipient,
        [recipient, recipients]
    );

    async function sendTestEmail() {
        if (!recipient) {
            return;
        }

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
        <div className="space-y-3">
            <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
                <div className="w-full sm:w-80">
                    <label className="text-sm font-medium leading-none mb-2 block">Recipient</label>
                    <Select value={recipient} onValueChange={setRecipient} disabled={!hasRecipients || isSending}>
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

                <Button
                    type="button"
                    size="sm"
                    onClick={sendTestEmail}
                    disabled={!hasRecipients || !recipient || isSending}
                >
                    <Send className="size-3.5" />
                    {isSending ? "Sending..." : "Send test email"}
                </Button>
            </div>

            {result && (
                <Alert variant={result.type === "error" ? "destructive" : "default"} className="max-w-xl">
                    {result.type === "error" ? (
                        <AlertCircle className="h-4 w-4" />
                    ) : (
                        <CheckCircle2 className="h-4 w-4" />
                    )}
                    <div className="flex items-start justify-between gap-3">
                        <div>
                            <AlertTitle>{result.type === "error" ? "Error" : "Sent"}</AlertTitle>
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
    );
}

export function TestEmailField({
    apiUrl,
    recipients,
}: {
    apiUrl: string;
    recipients: EmailRecipientOption[];
}) {
    const defaultRecipient = recipients[0]?.value ?? "";
    const [recipient, setRecipient] = useState(defaultRecipient);
    const [isSending, setIsSending] = useState(false);
    const [result, setResult] = useState<
        | { type: "success"; message: string }
        | { type: "error"; message: string }
        | null
    >(null);

    useEffect(() => {
        if (!recipient || !recipients.some((option) => option.value === recipient)) {
            setRecipient(defaultRecipient);
        }
    }, [defaultRecipient, recipient, recipients]);

    const hasRecipients = recipients.length > 0;

    const selectedLabel = useMemo(
        () => recipients.find((option) => option.value === recipient)?.label ?? recipient,
        [recipient, recipients]
    );

    async function sendTestEmail() {
        if (!recipient) {
            return;
        }

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
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
            <div>
                <label className="text-[13.5px] font-medium flex items-center gap-1.5">
                    Send test email
                </label>
                <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">
                    Send a diagnostic email to verify the saved SMTP configuration.
                </p>
            </div>
            <div className="min-w-0 space-y-3">
                <div>
                    <label className="text-sm font-medium leading-none mb-2 block">Recipient</label>
                    <Select value={recipient} onValueChange={setRecipient} disabled={!hasRecipients || isSending}>
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
                <Button
                    type="button"
                    size="sm"
                    onClick={sendTestEmail}
                    disabled={!hasRecipients || !recipient || isSending}
                >
                    <Send className="size-3.5" />
                    {isSending ? "Sending..." : "Send test email"}
                </Button>
                {result && (
                    <Alert variant={result.type === "error" ? "destructive" : "default"} className="max-w-xl">
                        {result.type === "error" ? (
                            <AlertCircle className="h-4 w-4" />
                        ) : (
                            <CheckCircle2 className="h-4 w-4" />
                        )}
                        <div className="flex items-start justify-between gap-3">
                            <div>
                                <AlertTitle>{result.type === "error" ? "Error" : "Sent"}</AlertTitle>
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
        </div>
    );
}
