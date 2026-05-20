"use client";

import * as z from "zod";
import { useParams, useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { AlertCircle, Check } from "lucide-react";
import { AdditionalDetailFieldDto, TicketedEventDetailsDto, TicketTypeDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { useCustomForm } from "@/hooks/use-custom-form";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";

const formSchema = z.object({
    email: z.string().min(1, "Email is required").email("Enter a valid email"),
    firstName: z.string().min(1, "First name is required").max(100, "First name must be 100 characters or fewer"),
    lastName: z.string().min(1, "Last name is required").max(100, "Last name must be 100 characters or fewer"),
    ticketTypeIds: z.array(z.string()).min(1, "Select at least one ticket type"),
    additionalDetails: z.record(z.string(), z.string()).optional(),
});

type FormValues = z.infer<typeof formSchema>;

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
    const { selectedTeam } = useTeams();

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
    const eventName = eventQuery.data?.name ?? "";

    const breadcrumbs = [
        { label: selectedTeam?.name ?? "", href: `/teams/${teamId}/settings` },
        { label: eventName, href: `/teams/${teamId}/events/${eventId}` },
        { label: "Registrations", href: `/teams/${teamId}/events/${eventId}/registrations` },
        { label: "Add registration" },
    ];

    return (
        <PageLayout title="Add registration" breadcrumbs={breadcrumbs}>
            {isLoading ? (
                <Skeleton className="h-64 w-full" />
            ) : (
                <AddRegistrationForm
                    teamId={teamId}
                    eventId={eventId}
                    eventName={eventName}
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

    const busy = form.formState.isSubmitting;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Add registration</h2>
                    <p className="text-[13.5px] text-muted-foreground">Manually register an attendee for {eventName}.</p>
                </div>
                <div className="flex gap-2">
                    <Button type="button" variant="ghost" size="sm" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button size="sm" type="button" onClick={form.submit(onSubmit)} disabled={busy}>
                        <Check className="size-3.5" />
                        {busy ? "Adding\u2026" : "Add registration"}
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
                    <fieldset disabled={busy} className="contents">
                        <div className="space-y-6">
                            <div>
                                <h3 className="font-display text-[15px] font-semibold mb-3">Attendee</h3>
                                <Card>
                                    <div className="px-6 divide-y">
                                        <FormField
                                            control={form.control}
                                            name="firstName"
                                            render={({ field }) => (
                                                <Field label="First name">
                                                    <FormItem className="space-y-1">
                                                        <FormControl>
                                                            <Input placeholder="Ada" maxLength={100} {...field} />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                </Field>
                                            )}
                                        />
                                        <FormField
                                            control={form.control}
                                            name="lastName"
                                            render={({ field }) => (
                                                <Field label="Last name">
                                                    <FormItem className="space-y-1">
                                                        <FormControl>
                                                            <Input placeholder="Lovelace" maxLength={100} {...field} />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                </Field>
                                            )}
                                        />
                                        <FormField
                                            control={form.control}
                                            name="email"
                                            render={({ field }) => (
                                                <Field label="Email">
                                                    <FormItem className="space-y-1">
                                                        <FormControl>
                                                            <Input type="email" placeholder="attendee@example.com" {...field} />
                                                        </FormControl>
                                                        <FormMessage />
                                                    </FormItem>
                                                </Field>
                                            )}
                                        />
                                    </div>
                                </Card>
                            </div>

                            <div>
                                <h3 className="font-display text-[15px] font-semibold mb-3">Tickets</h3>
                                <Card>
                                    <div className="px-6 divide-y">
                                        <FormField
                                            control={form.control}
                                            name="ticketTypeIds"
                                            render={() => (
                                                <Field label="Ticket types" hint="Select one or more tickets for this registration.">
                                                    <FormItem className="space-y-1">
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
                                                        <FormMessage />
                                                    </FormItem>
                                                </Field>
                                            )}
                                        />
                                    </div>
                                </Card>
                            </div>

                            {additionalDetailSchema.length > 0 && (
                                <div>
                                    <h3 className="font-display text-[15px] font-semibold mb-3">Additional details</h3>
                                    <Card>
                                        <div className="px-6 divide-y">
                                            {additionalDetailSchema.map((f) => (
                                                <FormField
                                                    key={f.key}
                                                    control={form.control}
                                                    name={`additionalDetails.${f.key}` as const}
                                                    render={({ field }) => (
                                                        <Field label={f.name}>
                                                            <FormItem className="space-y-1">
                                                                <FormControl>
                                                                    <Input
                                                                        maxLength={Number(f.maxLength)}
                                                                        value={field.value ?? ""}
                                                                        onChange={field.onChange}
                                                                    />
                                                                </FormControl>
                                                                <FormMessage />
                                                            </FormItem>
                                                        </Field>
                                                    )}
                                                />
                                            ))}
                                        </div>
                                    </Card>
                                </div>
                            )}
                        </div>
                    </fieldset>
                </form>
            </Form>
        </div>
    );
}
