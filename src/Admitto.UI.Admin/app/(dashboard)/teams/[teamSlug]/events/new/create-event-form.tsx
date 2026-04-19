"use client";

import { useParams, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";

const slugRegex = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;

const createEventSchema = z
    .object({
        slug: z
            .string()
            .min(1, "Slug is required")
            .regex(slugRegex, "Slug must be lowercase letters, digits, or hyphens"),
        name: z.string().min(1, "Name is required"),
        websiteUrl: z.string().min(1, "Website URL is required").url("Must be a valid URL"),
        baseUrl: z.string().min(1, "Base URL is required").url("Must be a valid URL"),
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
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-6 max-w-lg">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}

                <FormField
                    control={form.control}
                    name="slug"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Slug</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. devconf-2026" {...field} />
                            </FormControl>
                            <p className="text-xs text-muted-foreground">
                                Used in URLs. Cannot be changed later.
                            </p>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. DevConf 2026" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="websiteUrl"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Website URL</FormLabel>
                            <FormControl>
                                <Input placeholder="https://devconf.example.com" {...field} />
                            </FormControl>
                            <p className="text-xs text-muted-foreground">
                                Public marketing site for the event.
                            </p>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="baseUrl"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Base URL</FormLabel>
                            <FormControl>
                                <Input placeholder="https://tickets.example.com" {...field} />
                            </FormControl>
                            <p className="text-xs text-muted-foreground">
                                Base URL used in links sent to attendees (e.g. registration confirmations).
                            </p>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="startsAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Starts at</FormLabel>
                            <FormControl>
                                <Input type="datetime-local" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="endsAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Ends at</FormLabel>
                            <FormControl>
                                <Input type="datetime-local" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <Button type="submit" disabled={form.formState.isSubmitting}>
                    {form.formState.isSubmitting ? "Creating…" : "Create event"}
                </Button>
            </form>
        </Form>
    );
}
