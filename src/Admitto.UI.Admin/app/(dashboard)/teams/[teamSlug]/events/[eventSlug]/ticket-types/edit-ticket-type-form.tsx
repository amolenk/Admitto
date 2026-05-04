"use client";

import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketTypeDto } from "@/lib/admitto-api/generated";

const editSchema = z.object({
    name: z.string().min(1, "Name is required"),
    selfServiceEnabled: z.boolean(),
    limitCapacity: z.boolean(),
    maxCapacity: z.number().int().min(1).optional(),
});

type EditValues = z.infer<typeof editSchema>;

export function EditTicketTypeForm({
    teamSlug,
    eventSlug,
    ticketType,
    onSaved,
    onCancel,
}: {
    teamSlug: string;
    eventSlug: string;
    ticketType: TicketTypeDto;
    onSaved: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const hasCapacity = ticketType.maxCapacity != null;
    const form = useCustomForm<EditValues>(editSchema, {
        name: ticketType.name,
        selfServiceEnabled: ticketType.selfServiceEnabled,
        limitCapacity: hasCapacity,
        maxCapacity: hasCapacity ? Number(ticketType.maxCapacity) : undefined,
    });

    const limitCapacity = form.watch("limitCapacity");

    async function onSubmit(values: EditValues) {
        await apiClient.put(
            `/api/teams/${teamSlug}/events/${eventSlug}/ticket-types/${ticketType.slug}`,
            {
                name: values.name,
                selfServiceEnabled: values.selfServiceEnabled,
                maxCapacity: values.limitCapacity ? (values.maxCapacity ?? null) : null,
            }
        );
        await queryClient.invalidateQueries({ queryKey: ["ticket-types", teamSlug, eventSlug] });
        onSaved();
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
                <div className="space-y-2">
                    <label className="text-sm font-medium leading-none">Slug</label>
                    <Input value={ticketType.slug} disabled className="bg-muted" />
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
                    name="selfServiceEnabled"
                    render={({ field }) => (
                        <FormItem className="flex items-center gap-3">
                            <FormControl>
                                <Switch checked={field.value} onCheckedChange={field.onChange} />
                            </FormControl>
                            <FormLabel className="!mt-0">Enable self-service registration</FormLabel>
                        </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="limitCapacity"
                    render={({ field }) => (
                        <FormItem className="flex items-center gap-3">
                            <FormControl>
                                <Switch checked={field.value} onCheckedChange={field.onChange} />
                            </FormControl>
                            <FormLabel className="!mt-0">Limit capacity</FormLabel>
                        </FormItem>
                    )}
                />
                {limitCapacity && (
                    <FormField
                        control={form.control}
                        name="maxCapacity"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Max capacity</FormLabel>
                                <FormControl>
                                    <Input
                                        type="number"
                                        min={1}
                                        placeholder="e.g. 100"
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
                )}
                {ticketType.timeSlots && ticketType.timeSlots.length > 0 && (
                    <div className="space-y-2">
                        <label className="text-sm font-medium leading-none">Time slots</label>
                        <div className="flex flex-wrap gap-1.5">
                            {ticketType.timeSlots.map((slot) => (
                                <Badge
                                    key={slot}
                                    variant="outline"
                                    className="font-mono text-[11px] opacity-70"
                                >
                                    {slot}
                                </Badge>
                            ))}
                        </div>
                        <p className="text-[11px] text-muted-foreground">
                            Time slots can&apos;t be changed after creation.
                        </p>
                    </div>
                )}
                <div className="flex gap-2 justify-end">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Saving..." : "Save changes"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}
