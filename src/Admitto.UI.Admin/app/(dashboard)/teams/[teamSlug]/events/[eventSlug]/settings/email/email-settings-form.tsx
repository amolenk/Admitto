"use client";

import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { EmailAuthMode, EventEmailSettingsDto } from "@/lib/admitto-api/generated";

const schema = z
    .object({
        smtpHost: z.string().min(1, "SMTP host is required"),
        smtpPort: z.coerce.number().int().min(1).max(65535),
        fromAddress: z.string().email(),
        authMode: z.enum(["none", "basic"]),
        username: z.string().optional(),
        password: z.string().optional(),
    })
    .refine((d) => d.authMode === "none" || (d.username && d.username.length > 0), {
        path: ["username"],
        message: "Username is required when auth mode is basic",
    });

type Values = z.infer<typeof schema>;

export function EmailSettingsForm({
    teamSlug,
    eventSlug,
    settings,
}: {
    teamSlug: string;
    eventSlug: string;
    settings: EventEmailSettingsDto | null;
}) {
    const queryClient = useQueryClient();

    const form = useCustomForm<Values>(schema, {
        smtpHost: settings?.smtpHost ?? "",
        smtpPort: settings ? Number(settings.smtpPort) : 587,
        fromAddress: settings?.fromAddress ?? "",
        authMode: (settings?.authMode as EmailAuthMode) ?? "none",
        username: settings?.username ?? "",
        password: "",
    });

    async function onSubmit(values: Values) {
        const body = {
            smtpHost: values.smtpHost,
            smtpPort: values.smtpPort,
            fromAddress: values.fromAddress,
            authMode: values.authMode,
            username: values.authMode === "basic" ? values.username || null : null,
            password: values.password ? values.password : null,
            version: settings ? Number(settings.version) : null,
        };

        await apiClient.put(
            `/api/teams/${teamSlug}/events/${eventSlug}/email-settings`,
            body
        );

        await queryClient.invalidateQueries({
            queryKey: ["email-settings", teamSlug, eventSlug],
        });
        await queryClient.invalidateQueries({
            queryKey: ["registration-open-status", teamSlug, eventSlug],
        });
        form.reset({ ...values, password: "" });
    }

    const authMode = form.watch("authMode");

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
                    name="smtpHost"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>SMTP host</FormLabel>
                            <FormControl>
                                <Input placeholder="smtp.example.com" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="smtpPort"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>SMTP port</FormLabel>
                            <FormControl>
                                <Input type="number" min={1} max={65535} {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="fromAddress"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>From address</FormLabel>
                            <FormControl>
                                <Input type="email" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="authMode"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Authentication mode</FormLabel>
                            <Select onValueChange={field.onChange} value={field.value}>
                                <FormControl>
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                </FormControl>
                                <SelectContent>
                                    <SelectItem value="none">None</SelectItem>
                                    <SelectItem value="basic">Basic (username + password)</SelectItem>
                                </SelectContent>
                            </Select>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                {authMode === "basic" && (
                    <>
                        <FormField
                            control={form.control}
                            name="username"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Username</FormLabel>
                                    <FormControl>
                                        <Input {...field} />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormField
                            control={form.control}
                            name="password"
                            render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Password</FormLabel>
                                    <FormControl>
                                        <Input
                                            type="password"
                                            placeholder={
                                                settings?.hasPassword
                                                    ? "Leave blank to keep existing password"
                                                    : ""
                                            }
                                            {...field}
                                        />
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </>
                )}

                <Button type="submit" disabled={form.formState.isSubmitting}>
                    {form.formState.isSubmitting ? "Saving…" : "Save email settings"}
                </Button>
            </form>
        </Form>
    );
}
