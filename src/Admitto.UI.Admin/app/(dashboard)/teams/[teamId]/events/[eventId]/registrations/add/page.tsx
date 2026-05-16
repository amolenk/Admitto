"use client";

import * as z from "zod";
import { useParams, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { AdditionalDetailFieldDto, TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useCustomForm } from "@/hooks/use-custom-form";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { FormHeading } from "@/components/form-heading";

const formSchema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    firstName: z.string().min(1, "First name is required").max(100, "First name must be 100 characters or fewer"),
    lastName: z.string().min(1, "Last name is required").max(100, "Last name must be 100 characters or fewer"),
    ticketTypeIds: z.array(z.string()).min(1, "Select at least one ticket type"),
    additionalDetails: z.record(z.string(), z.string()).optional(),
});

type FormValues = z.infer<typeof formSchema>;

async function fetchEvent(teamId: string, eventId: string) {
    return apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`);
}

async function fetchTicketTypes(teamId: string, eventId: string) {
    return apiClient.get<TicketTypeDto[]>(`/api/teams/${teamId}/events/${eventId}/ticket-types`);
}

export default function AddRegistrationPage() {
    const router = useRouter();
    const params = useParams<{ teamId: string; eventId: string }>();
    const teamId = params.teamId;
    const eventId = params.eventId;

    const eventQuery = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () => fetchEvent(teamId, eventId),
    });

    const ticketTypesQuery = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () => fetchTicketTypes(teamId, eventId),
    });

    const isLoading = eventQuery.isLoading || ticketTypesQuery.isLoading;
    const ticketTypes = ticketTypesQuery.data ?? [];
    const additionalDetailSchema: AdditionalDetailFieldDto[] = eventQuery.data?.additionalDetailSchema ?? [];

    return (
        <PageLayout title="Add registration">
            {isLoading ? (
                <Skeleton className="h-64 w-full" />
            ) : (
                <AddRegistrationForm
                    teamId={teamId}
                    eventId={eventId}
                    eventName={eventQuery.data?.name ?? eventId}
                    ticketTypes={ticketTypes}
                    additionalDetailSchema={additionalDetailSchema}
                    onCancel={() => router.back()}
                    onAdded={() => router.push(`/teams/${teamId}/events/${eventId}/registrations`)}
                />
            )}
        </PageLayout>
    );
}

function AddRegistrationForm({
    teamId,
    eventId,
    eventName,
    ticketTypes,
    additionalDetailSchema,
    onCancel,
    onAdded,
}: {
    teamId: string;
    eventId: string;
    eventName: string;
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

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-8 max-w-2xl">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}

                <FormHeading text="Event" />
                <FormItem>
                    <FormLabel>Event name</FormLabel>
                    <Input disabled value={eventName} />
                </FormItem>

                <FormHeading text="Attendee" />
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

                <FormHeading text="Tickets" />
                <FormField
                    control={form.control}
                    name="ticketTypeIds"
                    render={() => (
                        <FormItem>
                            <FormLabel>Ticket types</FormLabel>
                            {ticketTypes.length === 0 ? (
                                <p className="text-sm text-muted-foreground">
                                    No ticket types available for this event.
                                </p>
                            ) : (
                                <div className="space-y-2 rounded-md border p-4">
                                    {ticketTypes.map((t) => (
                                        <FormField
                                            key={t.id}
                                            control={form.control}
                                            name="ticketTypeIds"
                                            render={({ field }) => {
                                                const checked = field.value?.includes(t.id) ?? false;
                                                return (
                                                    <label className="flex items-center gap-2 text-sm">
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
                            <FormMessage />
                        </FormItem>
                    )}
                />

                {additionalDetailSchema.length > 0 && (
                    <>
                        <FormHeading text="Additional details" />
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
                    </>
                )}

                <div className="flex justify-end gap-2">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Adding..." : "Add registration"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}
