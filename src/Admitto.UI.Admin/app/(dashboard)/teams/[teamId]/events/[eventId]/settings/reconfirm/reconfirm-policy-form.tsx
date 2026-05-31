"use client";

import { useState } from "react";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ZonedDateTimePicker } from "@/components/ui/zoned-date-time-picker";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { TicketedEventDetails } from "../event-detail-types";

function Field({ label, hint, children }: {
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

function makeReconfirmSchema(eventStartsAt: string) {
    return z
        .object({
            opensAt: z.string().min(1, "Opens at is required"),
            closesAt: z.string().min(1, "Closes at is required"),
            cadenceHours: z.coerce
                .number()
                .int("Cadence must be a whole number")
                .min(1, "Cadence must be at least 1 hour"),
            minEmailIntervalHours: z.coerce
                .number()
                .int("Must be a whole number")
                .min(1, "Must be at least 1 hour"),
        })
        .refine(
            (d) => new Date(d.closesAt) > new Date(d.opensAt),
            {
                path: ["closesAt"],
                message: "Close date must be after open date",
            }
        )
        .refine(
            (d) => new Date(d.closesAt) < new Date(eventStartsAt),
            {
                path: ["closesAt"],
                message: "Reconfirmation window must close before the event start date",
            }
        );
}

type ReconfirmValues = {
    opensAt: string;
    closesAt: string;
    cadenceHours: number;
    minEmailIntervalHours: number;
};

export function ReconfirmPolicyForm({
    event,
    teamId,
    eventId,
    disabled,
}: {
    event: TicketedEventDetails;
    teamId: string;
    eventId: string;
    disabled: boolean;
}) {
    const queryClient = useQueryClient();
    const policy = event.reconfirmPolicy;

    const [removeError, setRemoveError] = useState<{ title: string; detail: string } | null>(null);
    const [isRemoving, setIsRemoving] = useState(false);

    const form = useCustomForm<ReconfirmValues>(makeReconfirmSchema(event.startsAt), {
        opensAt: policy?.opensAt ?? "",
        closesAt: policy?.closesAt ?? "",
        cadenceHours: policy?.cadenceHours ?? 24,
        minEmailIntervalHours: policy?.minEmailIntervalHours ?? 1,
    });

    async function onSubmit(values: ReconfirmValues) {
        await apiClient.put(`/api/teams/${teamId}/events/${eventId}/reconfirm-policy`, {
            opensAt: values.opensAt,
            closesAt: values.closesAt,
            cadenceHours: values.cadenceHours,
            minEmailIntervalHours: values.minEmailIntervalHours,
            expectedVersion: Number(event.version),
        });
        await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] });
        form.reset(values);
    }

    async function handleRemove() {
        setRemoveError(null);
        setIsRemoving(true);
        try {
            await apiClient.put(`/api/teams/${teamId}/events/${eventId}/reconfirm-policy`, {
                opensAt: null,
                closesAt: null,
                cadenceHours: null,
                expectedVersion: Number(event.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] });
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
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Reconfirmation policy</h2>
                    <p className="text-[13.5px] text-muted-foreground">Require attendees to periodically reconfirm their registration.</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="ghost" size="sm" type="button" onClick={() => form.reset()} disabled={disabled || !form.formState.isDirty}>
                        Discard
                    </Button>
                    <Button
                        size="sm"
                        onClick={form.submit(onSubmit)}
                        disabled={disabled || !form.formState.isDirty || form.formState.isSubmitting}
                    >
                        <Check className="size-3.5" />
                        {form.formState.isSubmitting ? "Saving…" : "Save changes"}
                    </Button>
                </div>
            </div>

            {form.generalError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{form.generalError.title}</AlertTitle>
                    <AlertDescription>{form.generalError.detail}</AlertDescription>
                </Alert>
            )}

            {removeError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{removeError.title}</AlertTitle>
                    <AlertDescription>{removeError.detail}</AlertDescription>
                </Alert>
            )}

            <Form {...form}>
                <form onSubmit={form.submit(onSubmit)}>
                    <fieldset disabled={disabled} className="contents">
                        <Card>
                            <div className="px-6 divide-y">
                                <FormField
                                    control={form.control}
                                    name="opensAt"
                                    render={({ field }) => (
                                        <Field label="Window opens" hint="When attendees can start reconfirming.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <ZonedDateTimePicker
                                                        value={field.value}
                                                        onChange={field.onChange}
                                                        onBlur={field.onBlur}
                                                        timeZone={event.timeZone}
                                                    />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="closesAt"
                                    render={({ field }) => (
                                        <Field label="Window closes" hint="When reconfirmation stops accepting responses.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <ZonedDateTimePicker
                                                        value={field.value}
                                                        onChange={field.onChange}
                                                        onBlur={field.onBlur}
                                                        timeZone={event.timeZone}
                                                    />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="cadenceHours"
                                    render={({ field }) => (
                                        <Field label="Cadence (hours)" hint="How often attendees receive reconfirmation requests.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input
                                                        type="number"
                                                        min={1}
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
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="minEmailIntervalHours"
                                    render={({ field }) => (
                                        <Field label="Min. email interval (hours)" hint="Minimum hours between reminder emails per attendee.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input
                                                        type="number"
                                                        min={1}
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
                                        </Field>
                                    )}
                                />
                            </div>
                        </Card>
                    </fieldset>
                </form>
            </Form>

            {policy && (
                <div className="mt-6 pt-6 border-t">
                    <h3 className="text-sm font-medium mb-1">Remove policy</h3>
                    <p className="text-[13px] text-muted-foreground mb-3">
                        Removes the reconfirmation policy. Attendees will no longer be asked to reconfirm.
                    </p>
                    <Button
                        type="button"
                        variant="destructive"
                        size="sm"
                        disabled={disabled || isRemoving}
                        onClick={handleRemove}
                    >
                        {isRemoving ? "Removing…" : "Remove policy"}
                    </Button>
                </div>
            )}
        </div>
    );
}
