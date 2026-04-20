"use client";

import { useParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDto } from "@/lib/admitto-api/generated";

function toLocalInput(iso: string): string {
    if (!iso) return "";
    const d = new Date(iso);
    const pad = (n: number) => n.toString().padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

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
        websiteUrl: z.string().url("Must be a valid URL").min(1, "Website URL is required"),
        baseUrl: z.string().url("Must be a valid URL").min(1, "Base URL is required"),
        startsAt: z.string().min(1, "Start is required"),
        endsAt: z.string().min(1, "End is required"),
    })
    .refine((d) => new Date(d.startsAt) < new Date(d.endsAt), {
        path: ["endsAt"],
        message: "End must be after start",
    });

type GeneralValues = z.infer<typeof generalSchema>;

export function GeneralSettingsForm({ event }: { event: TicketedEventDto }) {
    const queryClient = useQueryClient();
    const { teamSlug, eventSlug } = useParams<{ teamSlug: string; eventSlug: string }>();

    const form = useCustomForm<GeneralValues>(generalSchema, {
        name: event.name,
        websiteUrl: event.websiteUrl,
        baseUrl: event.baseUrl,
        startsAt: toLocalInput(event.startsAt),
        endsAt: toLocalInput(event.endsAt),
    });

    async function onSubmit(values: GeneralValues) {
        const startsAt = new Date(values.startsAt).toISOString();
        const endsAt = new Date(values.endsAt).toISOString();
        const body: Record<string, unknown> = {
            expectedVersion: Number(event.version),
        };
        if (values.name !== event.name) body.name = values.name;
        if (values.websiteUrl !== event.websiteUrl) body.websiteUrl = values.websiteUrl;
        if (values.baseUrl !== event.baseUrl) body.baseUrl = values.baseUrl;
        if (startsAt !== event.startsAt) body.startsAt = startsAt;
        if (endsAt !== event.endsAt) body.endsAt = endsAt;

        await apiClient.put(`/api/teams/${teamSlug}/events/${eventSlug}`, body);

        await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
        await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">General</h2>
                    <p className="text-[13.5px] text-muted-foreground">Public-facing event details.</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="ghost" size="sm" type="button" onClick={() => form.reset()}>
                        Discard
                    </Button>
                    <Button size="sm" onClick={form.submit(onSubmit)} disabled={form.formState.isSubmitting}>
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
                            <Field label="Slug" hint="Used in registration links." badge="Immutable">
                                <Input value={event.slug} disabled className="bg-muted max-w-sm font-mono" />
                            </Field>

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
                                name="startsAt"
                                render={({ field }) => (
                                    <Field label="Starts at" hint="Event start date and time.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="datetime-local" {...field} />
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
                                                <Input type="datetime-local" {...field} />
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
