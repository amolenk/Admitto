"use client";

import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    SheetBody,
    SheetFooter,
} from "@/components/ui/sheet";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketTypeDto } from "@/lib/admitto-api/generated";
import MultipleSelector, { Option } from "@/components/ui/multiple-selector";

const addSchema = z.object({
    name: z.string().min(1, "Name is required"),
    kind: z.enum(["standalone", "ticketBased"]),
    ticketTypeIds: z.array(z.string()).optional(),
});

type AddValues = z.infer<typeof addSchema>;

export function AddBadgeTypeForm({
    teamId,
    eventId,
    ticketTypes,
    onAdded,
    onCancel,
}: {
    teamId: string;
    eventId: string;
    ticketTypes: TicketTypeDto[];
    onAdded: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const form = useCustomForm<AddValues>(addSchema, {
        name: "",
        kind: "standalone",
        ticketTypeIds: [],
    });

    const kind = form.watch("kind");
    const isTicketBased = kind === "ticketBased";

    const ticketTypeOptions: Option[] = ticketTypes.map((t) => ({
        value: t.id,
        label: t.name,
    }));

    const [selectedTicketTypes, setSelectedTicketTypes] = useState<Option[]>([]);

    async function onSubmit(values: AddValues) {
        await apiClient.post(
            `/api/teams/${teamId}/events/${eventId}/badge-types`,
            {
                name: values.name,
                kind: values.kind,
                ticketTypeIds: isTicketBased ? selectedTicketTypes.map((o) => o.value) : null,
            }
        );
        await queryClient.invalidateQueries({ queryKey: ["badge-types", teamId, eventId] });
        onAdded();
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
                <SheetBody className="space-y-5">
                    {form.generalError && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>{form.generalError.title}</AlertTitle>
                            <AlertDescription>{form.generalError.detail}</AlertDescription>
                        </Alert>
                    )}
                    <FormField
                        control={form.control}
                        name="name"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Name</FormLabel>
                                <FormControl>
                                    <Input placeholder="e.g. Speaker Badge" {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={form.control}
                        name="kind"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Kind</FormLabel>
                                <div className="flex gap-2">
                                    <Button
                                        type="button"
                                        variant={field.value === "standalone" ? "default" : "outline"}
                                        size="sm"
                                        onClick={() => {
                                            field.onChange("standalone");
                                            setSelectedTicketTypes([]);
                                        }}
                                    >
                                        Standalone
                                    </Button>
                                    <Button
                                        type="button"
                                        variant={field.value === "ticketBased" ? "default" : "outline"}
                                        size="sm"
                                        onClick={() => field.onChange("ticketBased")}
                                    >
                                        Ticket-based
                                    </Button>
                                </div>
                                <p className="text-[11px] text-muted-foreground">
                                    {field.value === "standalone"
                                        ? "Instances are managed manually."
                                        : "One instance is created per ticket of the selected types."}
                                </p>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                    {isTicketBased && (
                        <FormItem>
                            <FormLabel>Ticket types</FormLabel>
                            <FormControl>
                                <MultipleSelector
                                    value={selectedTicketTypes}
                                    onChange={setSelectedTicketTypes}
                                    defaultOptions={ticketTypeOptions}
                                    placeholder="Select ticket types..."
                                    emptyIndicator={
                                        <p className="text-sm text-muted-foreground text-center py-2">
                                            No ticket types found.
                                        </p>
                                    }
                                />
                            </FormControl>
                            <p className="text-[11px] text-muted-foreground">
                                Leave empty to apply to all ticket types.
                            </p>
                        </FormItem>
                    )}
                </SheetBody>
                <SheetFooter>
                    <Button type="button" variant="outline" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Adding..." : "Add badge type"}
                    </Button>
                </SheetFooter>
            </form>
        </Form>
    );
}
