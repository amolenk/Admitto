"use client";

import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
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

const policySchema = z
    .object({
        registrationWindowOpensAt: z.string().optional(),
        registrationWindowClosesAt: z.string().optional(),
        restrictEmailDomain: z.boolean(),
        allowedEmailDomain: z.string().optional(),
    })
    .refine(
        (d) => !d.restrictEmailDomain || (d.allowedEmailDomain && d.allowedEmailDomain.length > 0),
        {
            path: ["allowedEmailDomain"],
            message: "Domain is required when domain restriction is enabled",
        }
    );

type PolicyValues = z.infer<typeof policySchema>;

export function RegistrationPolicyForm({
    teamSlug,
    eventSlug,
}: {
    teamSlug: string;
    eventSlug: string;
}) {
    const form = useCustomForm<PolicyValues>(policySchema, {
        registrationWindowOpensAt: "",
        registrationWindowClosesAt: "",
        restrictEmailDomain: false,
        allowedEmailDomain: "",
    });

    const restrictEmailDomain = form.watch("restrictEmailDomain");

    async function onSubmit(values: PolicyValues) {
        const body = {
            registrationWindowOpensAt: values.registrationWindowOpensAt
                ? new Date(values.registrationWindowOpensAt).toISOString()
                : null,
            registrationWindowClosesAt: values.registrationWindowClosesAt
                ? new Date(values.registrationWindowClosesAt).toISOString()
                : null,
            allowedEmailDomain:
                values.restrictEmailDomain && values.allowedEmailDomain
                    ? values.allowedEmailDomain
                    : null,
        };

        await apiClient.put(`/api/teams/${teamSlug}/events/${eventSlug}/registration-policy`, body);
        form.reset(values);
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Registration policy</h2>
                    <p className="text-[13.5px] text-muted-foreground">Control when and who can register for this event.</p>
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
                            <FormField
                                control={form.control}
                                name="registrationWindowOpensAt"
                                render={({ field }) => (
                                    <Field label="Window opens" hint="When attendees can start registering.">
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
                                name="registrationWindowClosesAt"
                                render={({ field }) => (
                                    <Field label="Window closes" hint="When registration stops accepting entries.">
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
                                name="restrictEmailDomain"
                                render={({ field }) => (
                                    <Field label="Restrict domain" hint="Only allow registrations from a specific email domain.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Switch checked={field.value} onCheckedChange={field.onChange} />
                                            </FormControl>
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            {restrictEmailDomain && (
                                <FormField
                                    control={form.control}
                                    name="allowedEmailDomain"
                                    render={({ field }) => (
                                        <Field label="Allowed domain" hint="e.g. acme.org">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input placeholder="acme.org" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />
                            )}
                        </div>
                    </Card>
                </form>
            </Form>
        </div>
    );
}
