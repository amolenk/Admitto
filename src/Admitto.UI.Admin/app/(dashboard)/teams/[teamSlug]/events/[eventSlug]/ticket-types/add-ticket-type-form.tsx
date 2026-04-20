"use client";

import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";

const slugRegex = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;

const addSchema = z.object({
    slug: z.string().min(1, "Slug is required").regex(slugRegex, "Lowercase letters, digits, hyphens"),
    name: z.string().min(1, "Name is required"),
    maxCapacity: z.coerce.number().int().positive().optional(),
});

type AddValues = z.infer<typeof addSchema>;

export function AddTicketTypeForm({
    teamSlug,
    eventSlug,
    onAdded,
    onCancel,
}: {
    teamSlug: string;
    eventSlug: string;
    onAdded: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const form = useCustomForm<AddValues>(addSchema, {
        slug: "",
        name: "",
        maxCapacity: undefined,
    });

    async function onSubmit(values: AddValues) {
        await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types`, {
            slug: values.slug,
            name: values.name,
            maxCapacity: values.maxCapacity ?? null,
            timeSlots: null,
        });
        await queryClient.invalidateQueries({ queryKey: ["ticket-types", teamSlug, eventSlug] });
        onAdded();
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
                <FormField
                    control={form.control}
                    name="slug"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Slug</FormLabel>
                            <FormControl>
                                <Input placeholder="early-bird" {...field} />
                            </FormControl>
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
                                <Input placeholder="Early Bird" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="maxCapacity"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Max capacity (optional)</FormLabel>
                            <FormControl>
                                <Input
                                    type="number"
                                    min={1}
                                    placeholder="Leave empty for unlimited"
                                    value={field.value ?? ""}
                                    onChange={(e) =>
                                        field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                    }
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <div className="flex gap-2 justify-end">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Adding..." : "Add ticket type"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}
