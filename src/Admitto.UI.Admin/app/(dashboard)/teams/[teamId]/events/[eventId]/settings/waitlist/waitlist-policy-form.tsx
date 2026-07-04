"use client";

import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
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

const waitlistPolicySchema = z.object({
    quietHoursStart: z.string().regex(/^\d{2}:\d{2}$/, "Must be HH:MM"),
    quietHoursEnd: z.string().regex(/^\d{2}:\d{2}$/, "Must be HH:MM"),
});

type WaitlistPolicyValues = z.infer<typeof waitlistPolicySchema>;

export function WaitlistPolicyForm({
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
    const policy = event.waitlistPolicy;

    const form = useCustomForm<WaitlistPolicyValues>(waitlistPolicySchema, {
        quietHoursStart: policy.quietHoursStart.substring(0, 5),
        quietHoursEnd: policy.quietHoursEnd.substring(0, 5),
    });

    async function onSubmit(values: WaitlistPolicyValues) {
        await apiClient.put(`/api/teams/${teamId}/events/${eventId}/waitlist-policy`, {
            quietHoursStart: values.quietHoursStart + ":00",
            quietHoursEnd: values.quietHoursEnd + ":00",
            expectedVersion: Number(event.version),
        });

        await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] });
        form.reset(values);
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Waitlist policy</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Applies to all ticket-type waitlists for this event.
                    </p>
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

            <Card>
                <div className="border-b px-6 py-4 text-[13.5px] text-muted-foreground">
                    Waitlist notifications are sent immediately. Quiet hours extend the claim deadline so attendees still get the full claim window during waking hours.
                </div>
                <Form {...form}>
                    <form onSubmit={form.submit(onSubmit)}>
                        <fieldset disabled={disabled} className="contents">
                            <div className="px-6 divide-y">
                                <FormField
                                    control={form.control}
                                    name="quietHoursStart"
                                    render={({ field }) => (
                                        <Field label="Quiet hours start" hint="Local event time when quiet hours begin.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input type="time" {...field} className="w-36" />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="quietHoursEnd"
                                    render={({ field }) => (
                                        <Field label="Quiet hours end" hint="Claim deadlines resume from this local event time.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input type="time" {...field} className="w-36" />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />
                            </div>
                        </fieldset>
                    </form>
                </Form>
            </Card>
        </div>
    );
}
