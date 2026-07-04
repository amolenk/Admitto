"use client";

import { useParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ZonedDateTimePicker } from "@/components/ui/zoned-date-time-picker";
import { TimeZoneSelector } from "@/components/ui/time-zone-selector";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { isValidTimeZone } from "@/lib/time-zones";

function Field({ label, hint, badge, children }: {
    label: string;
    hint?: string;
    badge?: string;
    children: React.ReactNode;
}) {
    return (
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
            <div>
                <label className="text-[13.5px] font-medium flex items-center gap-1.5">
                    {label}
                    {badge && (
                        <span className="inline-flex items-center px-1.5 py-0.5 rounded-full text-[0.65rem] font-medium border text-muted-foreground">
                            {badge}
                        </span>
                    )}
                </label>
                {hint && <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">{hint}</p>}
            </div>
            <div className="min-w-0">{children}</div>
        </div>
    );
}

const generalSchema = z
    .object({
        name: z.string().min(1, "Name is required"),
        publicSlug: z.string().min(1, "Public slug is required").max(64).regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, "Use lowercase letters, numbers, and dashes"),
        websiteUrl: z.string().url("Must be a valid URL").min(1, "Website URL is required"),
        baseUrl: z.string().url("Must be a valid URL").min(1, "Base URL is required"),
        timeZone: z
            .string()
            .min(1, "Time zone is required")
            .refine((v) => isValidTimeZone(v), "Unknown IANA time zone"),
        startsAt: z.string().min(1, "Start is required"),
        endsAt: z.string().min(1, "End is required"),
    })
    .refine((d) => new Date(d.startsAt) < new Date(d.endsAt), {
        path: ["endsAt"],
        message: "End must be after start",
    });

type GeneralValues = z.infer<typeof generalSchema>;

export function GeneralSettingsForm({ event }: { event: TicketedEventDetailsDto }) {
    const queryClient = useQueryClient();
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();

    const form = useCustomForm<GeneralValues>(generalSchema, {
        name: event.name,
        publicSlug: event.publicSlug,
        websiteUrl: event.websiteUrl,
        baseUrl: event.baseUrl,
        timeZone: event.timeZone,
        startsAt: event.startsAt,
        endsAt: event.endsAt,
    });

    async function onSubmit(values: GeneralValues) {
        const detailsChanged =
            values.name !== event.name ||
            values.publicSlug !== event.publicSlug ||
            values.websiteUrl !== event.websiteUrl ||
            values.baseUrl !== event.baseUrl ||
            values.timeZone !== event.timeZone ||
            values.startsAt !== event.startsAt ||
            values.endsAt !== event.endsAt;

        if (detailsChanged) {
            await apiClient.put(`/api/teams/${teamId}/events/${eventId}`, {
                expectedVersion: Number(event.version),
                name: values.name,
                publicSlug: values.publicSlug,
                websiteUrl: values.websiteUrl,
                baseUrl: values.baseUrl,
                timeZone: values.timeZone,
                startsAt: values.startsAt,
                endsAt: values.endsAt,
            });
        }

        await queryClient.invalidateQueries({ queryKey: ["event", teamId, eventId] });
        await queryClient.invalidateQueries({ queryKey: ["events", teamId] });
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">General</h2>
                    <p className="text-[13.5px] text-muted-foreground">Public-facing event details.</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="ghost" size="sm" type="button" onClick={() => form.reset()} disabled={!form.formState.isDirty || form.formState.isSubmitting}>
                        Discard
                    </Button>
                    <Button size="sm" onClick={form.submit(onSubmit)} disabled={!form.formState.isDirty || form.formState.isSubmitting}>
                        <Check className="size-3.5" />
                        {form.formState.isSubmitting ? "Saving\u2026" : "Save changes"}
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

            <Form {...form}>
                <form onSubmit={form.submit(onSubmit)}>
                    <Card>
                        <div className="px-6 divide-y">
                            <FormField
                                control={form.control}
                                name="name"
                                render={({ field }) => (
                                    <Field label="Event name" hint="Shown on the public page and in all emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="e.g. Azure Fest" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="publicSlug"
                                render={({ field }) => (
                                    <Field label="Public slug" hint="Used for Admitto links like /e/azure-fest-2026.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="azure-fest-2026" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="websiteUrl"
                                render={({ field }) => (
                                    <Field label="Website" hint="Public website for the event.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="url" placeholder="https://example.com" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="baseUrl"
                                render={({ field }) => (
                                    <Field label="Base URL" hint="Base URL for registration links and emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="url" placeholder="https://register.example.com" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="timeZone"
                                render={({ field }) => (
                                    <Field label="Time zone" hint="Used for all event date/time fields.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <TimeZoneSelector
                                                    value={field.value}
                                                    onChange={field.onChange}
                                                    onBlur={field.onBlur}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="startsAt"
                                render={({ field }) => (
                                    <Field label="Starts at" hint="Event start date and time.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <ZonedDateTimePicker
                                                    value={field.value}
                                                    onChange={field.onChange}
                                                    onBlur={field.onBlur}
                                                    timeZone={form.watch("timeZone")}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="endsAt"
                                render={({ field }) => (
                                    <Field label="Ends at" hint="Event end date and time.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <ZonedDateTimePicker
                                                    value={field.value}
                                                    onChange={field.onChange}
                                                    onBlur={field.onBlur}
                                                    timeZone={form.watch("timeZone")}
                                                />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />
                        </div>
                    </Card>
                </form>
            </Form>
        </div>
    );
}
