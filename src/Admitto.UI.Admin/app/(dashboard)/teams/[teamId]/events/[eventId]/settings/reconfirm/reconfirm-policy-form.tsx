"use client";

import { useState } from "react";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ZonedDateTimePicker } from "@/components/ui/zoned-date-time-picker";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import type { ConfigureReconfirmPolicyHttpRequest } from "@/lib/admitto-api/generated/types.gen";
import { TicketedEventDetails } from "../event-detail-types";

function Field({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
    return <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4"><div><label className="text-[13.5px] font-medium">{label}</label>{hint && <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">{hint}</p>}</div><div className="min-w-0">{children}</div></div>;
}

function makeSchema(eventStartsAt: string) {
    return z.object({
        opensAt: z.string().min(1, "Opens at is required"),
        closesAt: z.string().min(1, "Closes at is required"),
        minEmailIntervalHours: z.coerce.number().int("Must be a whole number").min(1, "Must be at least 1 hour"),
        quietHoursEnabled: z.boolean(), quietHoursStart: z.string(), quietHoursEnd: z.string(),
    }).refine((d) => new Date(d.closesAt) > new Date(d.opensAt), { path: ["closesAt"], message: "Close date must be after open date" })
        .refine((d) => new Date(d.closesAt) < new Date(eventStartsAt), { path: ["closesAt"], message: "Reconfirmation window must close before the event start date" })
        .superRefine((d, ctx) => {
            if (!d.quietHoursEnabled) return;
            if (!d.quietHoursStart || !d.quietHoursEnd) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["quietHoursEnd"], message: "Enter both quiet-hours times" });
            else if (d.quietHoursStart === d.quietHoursEnd) ctx.addIssue({ code: z.ZodIssueCode.custom, path: ["quietHoursEnd"], message: "Quiet hours must have different start and end times" });
        });
}

type Values = { opensAt: string; closesAt: string; minEmailIntervalHours: number; quietHoursEnabled: boolean; quietHoursStart: string; quietHoursEnd: string };

export function ReconfirmPolicyForm({ event, teamId, eventId, disabled }: { event: TicketedEventDetails; teamId: string; eventId: string; disabled: boolean }) {
    const queryClient = useQueryClient();
    const policy = event.reconfirmPolicy;
    const [removeError, setRemoveError] = useState<{ title: string; detail: string } | null>(null);
    const [isRemoving, setIsRemoving] = useState(false);
    const form = useCustomForm<Values>(makeSchema(event.startsAt), {
        opensAt: policy?.opensAt ?? "", closesAt: policy?.closesAt ?? "", minEmailIntervalHours: policy?.minEmailIntervalHours ? Number(policy.minEmailIntervalHours) : 1,
        quietHoursEnabled: Boolean(policy?.quietHoursStart && policy?.quietHoursEnd), quietHoursStart: policy?.quietHoursStart?.substring(0, 5) ?? "", quietHoursEnd: policy?.quietHoursEnd?.substring(0, 5) ?? "",
    });

    async function onSubmit(values: Values) {
        const body: ConfigureReconfirmPolicyHttpRequest = { opensAt: values.opensAt, closesAt: values.closesAt, minEmailIntervalHours: values.minEmailIntervalHours, quietHoursStart: values.quietHoursEnabled ? `${values.quietHoursStart}:00` : null, quietHoursEnd: values.quietHoursEnabled ? `${values.quietHoursEnd}:00` : null, expectedVersion: Number(event.version) };
        await apiClient.put(`/api/teams/${teamId}/events/${eventId}/reconfirm-policy`, body);
        await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] }); form.reset(values);
    }
    async function handleRemove() {
        setRemoveError(null); setIsRemoving(true);
        try {
            await apiClient.put(`/api/teams/${teamId}/events/${eventId}/reconfirm-policy`, { opensAt: null, closesAt: null, minEmailIntervalHours: null, quietHoursStart: null, quietHoursEnd: null, expectedVersion: Number(event.version) } satisfies ConfigureReconfirmPolicyHttpRequest);
            await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] });
        } catch (err) { setRemoveError(err instanceof FormError ? { title: err.title, detail: err.detail } : { title: "Unexpected Error", detail: "Could not remove the reconfirmation policy." }); }
        finally { setIsRemoving(false); }
    }
    const enabled = form.watch("quietHoursEnabled");
    return <div>
        <div className="flex items-start justify-between mb-5"><div><h2 className="font-display text-[22px] font-semibold">Reconfirmation policy</h2><p className="text-[13.5px] text-muted-foreground">Give attendees a clear window to reconfirm their registration.</p></div><div className="flex gap-2"><Button variant="ghost" size="sm" type="button" onClick={() => form.reset()} disabled={disabled || !form.formState.isDirty}>Discard</Button><Button size="sm" onClick={form.submit(onSubmit)} disabled={disabled || !form.formState.isDirty || form.formState.isSubmitting}><Check className="size-3.5" />{form.formState.isSubmitting ? "Saving…" : "Save changes"}</Button></div></div>
        {form.generalError && <Alert variant="destructive" className="mb-5"><AlertCircle className="h-4 w-4" /><AlertTitle>{form.generalError.title}</AlertTitle><AlertDescription>{form.generalError.detail}</AlertDescription></Alert>}
        {removeError && <Alert variant="destructive" className="mb-5"><AlertCircle className="h-4 w-4" /><AlertTitle>{removeError.title}</AlertTitle><AlertDescription>{removeError.detail}</AlertDescription></Alert>}
        <Form {...form}><form onSubmit={form.submit(onSubmit)}><fieldset disabled={disabled} className="contents"><Card><div className="px-6 divide-y">
            <FormField control={form.control} name="opensAt" render={({ field }) => <Field label="Window opens" hint="When attendees can start reconfirming."><FormItem className="space-y-1"><FormControl><ZonedDateTimePicker value={field.value} onChange={field.onChange} onBlur={field.onBlur} timeZone={event.timeZone} /></FormControl><FormMessage /></FormItem></Field>} />
            <FormField control={form.control} name="closesAt" render={({ field }) => <Field label="Window closes" hint="When reconfirmation stops accepting responses."><FormItem className="space-y-1"><FormControl><ZonedDateTimePicker value={field.value} onChange={field.onChange} onBlur={field.onBlur} timeZone={event.timeZone} /></FormControl><FormMessage /></FormItem></Field>} />
            <FormField control={form.control} name="minEmailIntervalHours" render={({ field }) => <Field label="Minimum reminder interval (hours)" hint="The shortest time between reminder emails for one attendee."><FormItem className="space-y-1"><FormControl><Input type="number" min={1} value={field.value ?? ""} onChange={(e) => field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)} /></FormControl><FormMessage /></FormItem></Field>} />
            <FormField control={form.control} name="quietHoursEnabled" render={({ field }) => <Field label="No-reminder quiet hours" hint="Optional local event hours when attendees will not receive reminder emails."><FormItem><FormControl><label className="flex items-center gap-3 text-sm"><input type="checkbox" role="switch" checked={field.value} onChange={(e) => field.onChange(e.target.checked)} className="size-4 accent-primary" /><span>{field.value ? "Enabled" : "Not enabled"}</span></label></FormControl><FormMessage /></FormItem></Field>} />
            {enabled && <><FormField control={form.control} name="quietHoursStart" render={({ field }) => <Field label="Quiet hours start" hint={`Local time in ${event.timeZone}.`}><FormItem><FormControl><Input type="time" {...field} className="w-36" /></FormControl><FormMessage /></FormItem></Field>} /><FormField control={form.control} name="quietHoursEnd" render={({ field }) => <Field label="Quiet hours end" hint="Overnight quiet hours are supported."><FormItem><FormControl><Input type="time" {...field} className="w-36" /></FormControl><FormMessage /></FormItem></Field>} /></>}
            {policy && <Field label="Remove policy" hint="Attendees will no longer be asked to reconfirm."><Button type="button" variant="destructive" size="sm" disabled={disabled || isRemoving} onClick={handleRemove}>{isRemoving ? "Removing…" : "Remove policy"}</Button></Field>}
        </div></Card></fieldset></form></Form>
    </div>;
}
