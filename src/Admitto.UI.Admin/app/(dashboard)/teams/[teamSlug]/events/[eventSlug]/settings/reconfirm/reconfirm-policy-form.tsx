"use client";

import { useState } from "react";
import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
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
import { ZonedDateTimePicker } from "@/components/ui/zoned-date-time-picker";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { TicketedEventDetails } from "../event-detail-types";

const reconfirmSchema = z
    .object({
        opensAt: z.string().min(1, "Opens at is required"),
        closesAt: z.string().min(1, "Closes at is required"),
        cadenceDays: z.coerce
            .number()
            .int("Cadence must be a whole number")
            .min(1, "Cadence must be at least 1 day"),
    })
    .refine(
        (d) => new Date(d.closesAt) > new Date(d.opensAt),
        {
            path: ["closesAt"],
            message: "Close date must be after open date",
        }
    );

type ReconfirmValues = z.infer<typeof reconfirmSchema>;

export function ReconfirmPolicyForm({
    event,
    teamSlug,
    eventSlug,
    disabled,
}: {
    event: TicketedEventDetails;
    teamSlug: string;
    eventSlug: string;
    disabled: boolean;
}) {
    const queryClient = useQueryClient();
    const policy = event.reconfirmPolicy;

    const [removeError, setRemoveError] = useState<{ title: string; detail: string } | null>(null);
    const [isRemoving, setIsRemoving] = useState(false);

    const form = useCustomForm<ReconfirmValues>(reconfirmSchema, {
        opensAt: policy?.opensAt ?? "",
        closesAt: policy?.closesAt ?? "",
        cadenceDays: policy?.cadenceDays ?? 1,
    });

    async function onSubmit(values: ReconfirmValues) {
        await apiClient.put(`/api/teams/${teamSlug}/events/${eventSlug}/reconfirm-policy`, {
            opensAt: values.opensAt,
            closesAt: values.closesAt,
            cadenceDays: values.cadenceDays,
            expectedVersion: Number(event.version),
        });
        await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
    }

    async function handleRemove() {
        setRemoveError(null);
        setIsRemoving(true);
        try {
            await apiClient.put(`/api/teams/${teamSlug}/events/${eventSlug}/reconfirm-policy`, {
                opensAt: null,
                closesAt: null,
                cadenceDays: null,
                expectedVersion: Number(event.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
        } catch (err) {
            if (err instanceof FormError) {
                setRemoveError({ title: err.title, detail: err.detail });
            } else {
                setRemoveError({
                    title: "Unexpected Error",
                    detail: "Could not remove the reconfirmation policy.",
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
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
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
                    name="opensAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Window opens at</FormLabel>
                            <FormControl>
                                <ZonedDateTimePicker
                                    disabled={disabled}
                                    value={field.value}
                                    onChange={field.onChange}
                                    onBlur={field.onBlur}
                                    timeZone={event.timeZone}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="closesAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Window closes at</FormLabel>
                            <FormControl>
                                <ZonedDateTimePicker
                                    disabled={disabled}
                                    value={field.value}
                                    onChange={field.onChange}
                                    onBlur={field.onBlur}
                                    timeZone={event.timeZone}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="cadenceDays"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Cadence (days)</FormLabel>
                            <FormControl>
                                <Input
                                    type="number"
                                    min={1}
                                    disabled={disabled}
                                    value={field.value ?? ""}
                                    onChange={(e) =>
                                        field.onChange(
                                            e.target.value === ""
                                                ? undefined
                                                : e.target.valueAsNumber
                                        )
                                    }
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <div className="flex gap-2">
                    <Button type="submit" disabled={disabled || form.formState.isSubmitting}>
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
