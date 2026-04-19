"use client";

import { useParams } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketedEventDto } from "@/lib/admitto-api/generated";

function toLocalInput(iso: string): string {
    if (!iso) return "";
    const d = new Date(iso);
    const pad = (n: number) => n.toString().padStart(2, "0");
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

const generalSchema = z
    .object({
        name: z.string().min(1, "Name is required"),
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
        if (startsAt !== event.startsAt) body.startsAt = startsAt;
        if (endsAt !== event.endsAt) body.endsAt = endsAt;

        await apiClient.put(`/api/teams/${teamSlug}/events/${eventSlug}`, body);

        await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
        await queryClient.invalidateQueries({ queryKey: ["events", teamSlug] });
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

                <div className="space-y-2">
                    <label className="text-sm font-medium leading-none">Slug</label>
                    <Input value={event.slug} disabled className="bg-muted" />
                    <p className="text-xs text-muted-foreground">
                        Slugs cannot be changed after creation.
                    </p>
                </div>

                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Name</FormLabel>
                            <FormControl>
                                <Input {...field} />
                            </FormControl>
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
                    {form.formState.isSubmitting ? "Saving…" : "Save changes"}
                </Button>
            </form>
        </Form>
    );
}
