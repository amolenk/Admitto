"use client";

import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { AdditionalDetailFieldDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { useCustomForm } from "@/hooks/use-custom-form";
import { Button } from "@/components/ui/button";
import {
    Form,
    FormControl,
    FormDescription,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    Sheet,
    SheetBody,
    SheetContent,
    SheetFooter,
    SheetHeader,
    SheetSection,
    SheetTitle,
} from "@/components/ui/sheet";

const formSchema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    firstName: z.string().min(1, "First name is required").max(100, "First name must be 100 characters or fewer"),
    lastName: z.string().min(1, "Last name is required").max(100, "Last name must be 100 characters or fewer"),
    ticketTypeIds: z.array(z.string()).min(1, "Select at least one ticket type"),
    additionalDetails: z.record(z.string(), z.string()).optional(),
});

type FormValues = z.infer<typeof formSchema>;

export function AddRegistrationSheet({
    open,
    onOpenChange,
    teamId,
    eventId,
    eventName,
    ticketTypes,
    additionalDetailSchema,
    onAdded,
}: {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    teamId: string;
    eventId: string;
    eventName: string;
    ticketTypes: TicketTypeDto[];
    additionalDetailSchema: AdditionalDetailFieldDto[];
    onAdded: () => void;
}) {
    return (
        <Sheet open={open} onOpenChange={onOpenChange}>
            <SheetContent side="right" className="sm:max-w-lg">
                <SheetHeader>
                    <SheetTitle>Add registration</SheetTitle>
                    <p className="text-[13px] text-muted-foreground">
                        Manually register an attendee for {eventName}.
                    </p>
                </SheetHeader>
                {open && (
                    <AddRegistrationForm
                        key={`${teamId}-${eventId}-${open}`}
                        teamId={teamId}
                        eventId={eventId}
                        ticketTypes={ticketTypes}
                        additionalDetailSchema={additionalDetailSchema}
                        onCancel={() => onOpenChange(false)}
                        onAdded={() => {
                            onOpenChange(false);
                            onAdded();
                        }}
                    />
                )}
            </SheetContent>
        </Sheet>
    );
}

function AddRegistrationForm({
    teamId,
    eventId,
    ticketTypes,
    additionalDetailSchema,
    onCancel,
    onAdded,
}: {
    teamId: string;
    eventId: string;
    ticketTypes: TicketTypeDto[];
    additionalDetailSchema: AdditionalDetailFieldDto[];
    onCancel: () => void;
    onAdded: () => void;
}) {
    const form = useCustomForm<FormValues>(formSchema, {
        email: "",
        firstName: "",
        lastName: "",
        ticketTypeIds: [],
        additionalDetails: Object.fromEntries(additionalDetailSchema.map((f) => [f.key, ""])),
    });

    async function onSubmit(values: FormValues) {
        const additionalDetails = additionalDetailSchema.length === 0
            ? null
            : Object.fromEntries(
                additionalDetailSchema
                    .map((f) => [f.key, values.additionalDetails?.[f.key] ?? ""] as const)
                    .filter(([, v]) => v.length > 0));

        await apiClient.post(`/api/teams/${teamId}/events/${eventId}/registrations`, {
            email: values.email,
            firstName: values.firstName,
            lastName: values.lastName,
            ticketTypeIds: values.ticketTypeIds,
            additionalDetails,
        });
        onAdded();
    }

    const busy = form.formState.isSubmitting;

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
                <SheetBody className="space-y-6">
                    {form.generalError && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertTitle>{form.generalError.title}</AlertTitle>
                            <AlertDescription>{form.generalError.detail}</AlertDescription>
                        </Alert>
                    )}

                    <fieldset disabled={busy} className="space-y-6">
                        <SheetSection title="Attendee">
                            <FormField
                                control={form.control}
                                name="firstName"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>First name</FormLabel>
                                        <FormControl>
                                            <Input placeholder="Ada" maxLength={100} {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="lastName"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Last name</FormLabel>
                                        <FormControl>
                                            <Input placeholder="Lovelace" maxLength={100} {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="email"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Email</FormLabel>
                                        <FormControl>
                                            <Input type="email" placeholder="attendee@example.com" {...field} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </SheetSection>

                        <SheetSection title="Tickets">
                            <FormField
                                control={form.control}
                                name="ticketTypeIds"
                                render={() => (
                                    <FormItem>
                                        <FormLabel>Ticket types</FormLabel>
                                        {ticketTypes.length === 0 ? (
                                            <p className="text-[13.5px] text-muted-foreground">
                                                No ticket types available for this event.
                                            </p>
                                        ) : (
                                            <div className="space-y-2.5">
                                                {ticketTypes.map((t) => (
                                                    <FormField
                                                        key={t.id}
                                                        control={form.control}
                                                        name="ticketTypeIds"
                                                        render={({ field }) => {
                                                            const checked = field.value?.includes(t.id) ?? false;
                                                            return (
                                                                <label className="flex items-center gap-2.5 text-[13.5px] cursor-pointer">
                                                                    <Checkbox
                                                                        checked={checked}
                                                                        onCheckedChange={(c) => {
                                                                            const current = field.value ?? [];
                                                                            field.onChange(
                                                                                c
                                                                                    ? [...current, t.id]
                                                                                    : current.filter((s) => s !== t.id));
                                                                        }}
                                                                    />
                                                                    <span>{t.name}</span>
                                                                </label>
                                                            );
                                                        }}
                                                    />
                                                ))}
                                            </div>
                                        )}
                                        <FormDescription>
                                            Select one or more tickets for this registration.
                                        </FormDescription>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </SheetSection>

                        {additionalDetailSchema.length > 0 && (
                            <SheetSection title="Additional details">
                                {additionalDetailSchema.map((f) => (
                                    <FormField
                                        key={f.key}
                                        control={form.control}
                                        name={`additionalDetails.${f.key}` as const}
                                        render={({ field }) => (
                                            <FormItem>
                                                <FormLabel>{f.name}</FormLabel>
                                                <FormControl>
                                                    <Input
                                                        maxLength={Number(f.maxLength)}
                                                        value={field.value ?? ""}
                                                        onChange={field.onChange}
                                                    />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        )}
                                    />
                                ))}
                            </SheetSection>
                        )}
                    </fieldset>
                </SheetBody>

                <SheetFooter>
                    <Button type="button" variant="outline" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={busy}>
                        {busy ? "Adding\u2026" : "Add registration"}
                    </Button>
                </SheetFooter>
            </form>
        </Form>
    );
}
