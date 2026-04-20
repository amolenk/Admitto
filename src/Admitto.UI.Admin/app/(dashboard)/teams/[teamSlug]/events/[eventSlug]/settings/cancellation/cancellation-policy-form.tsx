"use client";

import { useState } from "react";
import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";

const cancellationSchema = z.object({
    lateCancellationCutoff: z.string().min(1, "Cutoff date is required"),
});

type CancellationValues = z.infer<typeof cancellationSchema>;

interface CancellationPolicy {
    lateCancellationCutoff: string;
}

function toDatetimeLocal(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function CancellationPolicyForm({
    policy,
    teamSlug,
    eventSlug,
    disabled,
    onSaved,
}: {
    policy: CancellationPolicy | null;
    teamSlug: string;
    eventSlug: string;
    disabled: boolean;
    onSaved: () => void;
}) {
    const [removeError, setRemoveError] = useState<{
        title: string;
        detail: string;
    } | null>(null);
    const [isRemoving, setIsRemoving] = useState(false);

    const form = useCustomForm<CancellationValues>(cancellationSchema, {
        lateCancellationCutoff: policy
            ? toDatetimeLocal(policy.lateCancellationCutoff)
            : "",
    });

    async function onSubmit(values: CancellationValues) {
        await apiClient.put(
            `/api/teams/${teamSlug}/events/${eventSlug}/cancellation-policy`,
            {
                lateCancellationCutoff: new Date(
                    values.lateCancellationCutoff
                ).toISOString(),
            }
        );
        onSaved();
    }

    async function handleRemove() {
        setRemoveError(null);
        setIsRemoving(true);
        try {
            await apiClient.delete(
                `/api/teams/${teamSlug}/events/${eventSlug}/cancellation-policy`
            );
            onSaved();
        } catch (err) {
            if (err instanceof FormError) {
                setRemoveError({ title: err.title, detail: err.detail });
            } else {
                setRemoveError({
                    title: "Unexpected Error",
                    detail: "Could not remove the cancellation policy.",
                });
            }
        } finally {
            setIsRemoving(false);
        }
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-4">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>
                            {form.generalError.detail}
                        </AlertDescription>
                    </Alert>
                )}

                {removeError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{removeError.title}</AlertTitle>
                        <AlertDescription>{removeError.detail}</AlertDescription>
                    </Alert>
                )}

                <FormField
                    control={form.control}
                    name="lateCancellationCutoff"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Late cancellation cutoff</FormLabel>
                            <FormControl>
                                <Input
                                    type="datetime-local"
                                    disabled={disabled}
                                    {...field}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <div className="flex gap-2">
                    <Button
                        type="submit"
                        disabled={disabled || form.formState.isSubmitting}
                    >
                        {form.formState.isSubmitting ? "Saving…" : "Save policy"}
                    </Button>
                    {policy && (
                        <Button
                            type="button"
                            variant="destructive"
                            disabled={disabled || isRemoving}
                            onClick={handleRemove}
                        >
                            {isRemoving ? "Removing…" : "Remove policy"}
                        </Button>
                    )}
                </div>
            </form>
        </Form>
    );
}
