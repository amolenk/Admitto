"use client";

import { useParams, useRouter } from "next/navigation";
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

const slugRegex = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;

const createEventSchema = z
    .object({
        slug: z
            .string()
            .min(1, "Slug is required")
            .regex(slugRegex, "Slug must be lowercase letters, digits, or hyphens"),
        name: z.string().min(1, "Name is required"),
        websiteUrl: z.string().url("Must be a valid URL").min(1, "Website URL is required"),
        baseUrl: z.string().url("Must be a valid URL").min(1, "Base URL is required"),
        startsAt: z.string().min(1, "Start date/time is required"),
        endsAt: z.string().min(1, "End date/time is required"),
    })
    .refine((d) => new Date(d.startsAt) < new Date(d.endsAt), {
        path: ["endsAt"],
        message: "End must be after start",
    });

type CreateEventValues = z.infer<typeof createEventSchema>;

export function CreateEventForm() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const { teamSlug } = useParams<{ teamSlug: string }>();

    const form = useCustomForm<CreateEventValues>(createEventSchema, {
        slug: "",
        name: "",
        websiteUrl: "",
        baseUrl: "",
        startsAt: "",
        endsAt: "",
    });

    async function onSubmit(values: CreateEventValues) {
        const body = {
            slug: values.slug,
            name: values.name,
            websiteUrl: values.websiteUrl,
            baseUrl: values.baseUrl,
            startsAt: new Date(values.startsAt).toISOString(),
            endsAt: new Date(values.endsAt).toISOString(),
        };

        await apiClient.post(`/api/teams/${teamSlug}/events`, body);

        await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
        router.push(`/teams/${teamSlug}/events/${values.slug}/settings`);
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Create event</h2>
                    <p className="text-[13.5px] text-muted-foreground">Set up a new ticketed event for your team.</p>
                </div>
                <Button size="sm" onClick={form.submit(onSubmit)} disabled={form.formState.isSubmitting}>
                    <Check className="size-3.5" />
                    {form.formState.isSubmitting ? "Creating\u2026" : "Create event"}
                </Button>
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
                                name="slug"
                                render={({ field }) => (
                                    <Field label="Slug" hint="Used in URLs. Cannot be changed later.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="e.g. devconf-2026" className="max-w-sm font-mono" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="name"
                                render={({ field }) => (
                                    <Field label="Event name" hint="Shown on the public page and in all emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="e.g. DevConf 2026" {...field} />
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
