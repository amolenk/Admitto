"use client";

import * as z from "zod";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";

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
                    name="registrationWindowOpensAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Window opens at</FormLabel>
                            <FormControl>
                                <Input type="datetime-local" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="registrationWindowClosesAt"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Window closes at</FormLabel>
                            <FormControl>
                                <Input type="datetime-local" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="restrictEmailDomain"
                    render={({ field }) => (
                        <FormItem className="flex items-center justify-between rounded-md border p-3">
                            <div className="space-y-0.5">
                                <Label className="text-sm font-medium">
                                    Restrict to a single email domain
                                </Label>
                                <p className="text-xs text-muted-foreground">
                                    Only attendees whose email address matches the configured domain
                                    can register.
                                </p>
                            </div>
                            <FormControl>
                                <Switch checked={field.value} onCheckedChange={field.onChange} />
                            </FormControl>
                        </FormItem>
                    )}
                />

                {restrictEmailDomain && (
                    <FormField
                        control={form.control}
                        name="allowedEmailDomain"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Allowed email domain</FormLabel>
                                <FormControl>
                                    <Input placeholder="e.g. acme.org" {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                )}

                <Button type="submit" disabled={form.formState.isSubmitting}>
                    {form.formState.isSubmitting ? "Saving…" : "Save policy"}
                </Button>
            </form>
        </Form>
    );
}
