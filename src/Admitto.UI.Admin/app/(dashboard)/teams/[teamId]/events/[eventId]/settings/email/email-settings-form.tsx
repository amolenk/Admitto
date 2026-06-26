"use client";

import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { emailSettingsSchema, EmailSettingsValues, EmailSettingsInitialValues } from "./email-settings-types";

const fontOptions = [
    { label: "System", value: "-apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif" },
    { label: "Arial", value: "Arial, sans-serif" },
    { label: "Georgia", value: "Georgia, serif" },
    { label: "Helvetica Neue", value: "'Helvetica Neue', Helvetica, Arial, sans-serif" },
    { label: "Verdana", value: "Verdana, Geneva, sans-serif" },
    { label: "Courier New", value: "'Courier New', Courier, monospace" },
];

function Field({ label, hint, badge, children }: {
    label: string;
    hint?: string;
    badge?: string;
    children: React.ReactNode;
}) {
    return (
        <div className="grid grid-cols-1 md:grid-cols-[220px_1fr] gap-x-8 gap-y-1.5 py-4">
            <div>
                <label className="text-[13.5px] font-medium flex items-center gap-1.5">
                    {label}
                    {badge && (
                        <span className="inline-flex items-center px-1.5 py-0.5 rounded-full text-[0.65rem] font-medium border text-muted-foreground">
                            {badge}
                        </span>
                    )}
                </label>
                {hint && <p className="text-[12px] text-muted-foreground mt-0.5 leading-snug">{hint}</p>}
            </div>
            <div className="min-w-0">{children}</div>
        </div>
    );
}

export function EmailSettingsForm({
    apiUrl,
    queryKey,
    hasPassword = false,
    version,
    initialValues,
    renderTestEmail,
    title = "Email",
    description = "Sender identity and SMTP configuration.",
    callout,
}: {
    apiUrl: string;
    queryKey: unknown[];
    hasPassword?: boolean;
    version: number | null;
    initialValues: EmailSettingsInitialValues;
    renderTestEmail?: () => React.ReactNode;
    title?: string;
    description?: string;
    callout?: React.ReactNode;
}) {
    const queryClient = useQueryClient();

    const form = useCustomForm<EmailSettingsValues>(emailSettingsSchema, {
        smtpHost: initialValues.smtpHost,
        smtpPort: initialValues.smtpPort,
        fromAddress: initialValues.fromAddress,
        authMode: initialValues.authMode,
        username: initialValues.username,
        password: "",
        accentColor: initialValues.accentColor,
        fontFamily: initialValues.fontFamily,
    });

    async function onSubmit(values: EmailSettingsValues) {
        const body = {
            smtpHost: values.smtpHost,
            smtpPort: values.smtpPort,
            fromAddress: values.fromAddress,
            authMode: values.authMode,
            username: values.authMode === "basic" ? values.username || null : null,
            password: values.password ? values.password : null,
            accentColor: values.accentColor,
            fontFamily: values.fontFamily,
            version,
        };

        await apiClient.put(apiUrl, body);

        await queryClient.invalidateQueries({ queryKey });
        form.reset({ ...values, password: "" });
    }

    const authMode = form.watch("authMode");
    const isDirty = form.formState.isDirty;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">{title}</h2>
                    <p className="text-[13.5px] text-muted-foreground">{description}</p>
                </div>
                <div className="flex gap-2">
                    <Button variant="ghost" size="sm" type="button" onClick={() => form.reset()} disabled={!isDirty || form.formState.isSubmitting}>
                        Discard
                    </Button>
                    <Button size="sm" onClick={form.submit(onSubmit)} disabled={!isDirty || form.formState.isSubmitting}>
                        <Check className="size-3.5" />
                        {form.formState.isSubmitting ? "Saving…" : "Save changes"}
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
                            {callout}
                            <FormField
                                control={form.control}
                                name="accentColor"
                                render={({ field }) => (
                                    <Field label="Accent color" hint="Used for headings, buttons, and links in built-in event emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <div className="flex items-center gap-2">
                                                    <Input type="color" className="h-9 w-14 p-1" {...field} />
                                                    <Input className="max-w-[140px] font-mono" {...field} />
                                                </div>
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="fontFamily"
                                render={({ field }) => (
                                    <Field label="Font" hint="A simple font-family string stored with the team settings.">
                                        <FormItem className="space-y-1">
                                            <Select onValueChange={field.onChange} value={field.value}>
                                                <FormControl>
                                                    <SelectTrigger>
                                                        <SelectValue />
                                                    </SelectTrigger>
                                                </FormControl>
                                                <SelectContent>
                                                    {fontOptions.map((option) => (
                                                        <SelectItem key={option.value} value={option.value}>
                                                            {option.label}
                                                        </SelectItem>
                                                    ))}
                                                </SelectContent>
                                            </Select>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="fromAddress"
                                render={({ field }) => (
                                    <Field label="From address" hint="The sender email address for all event emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="email" placeholder="hello@example.com" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="smtpHost"
                                render={({ field }) => (
                                    <Field label="SMTP host" hint="Your mail server hostname.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="smtp.example.com" className="font-mono" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="smtpPort"
                                render={({ field }) => (
                                    <Field label="SMTP port" hint="Common ports: 587 (STARTTLS), 465 (SSL).">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="number" min={1} max={65535} className="max-w-[120px]" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="authMode"
                                render={({ field }) => (
                                    <Field label="Authentication" hint="How to authenticate with the SMTP server.">
                                        <FormItem className="space-y-1">
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
                                    </Field>
                                )}
                            />

                            {authMode === "basic" && (
                                <>
                                    <FormField
                                        control={form.control}
                                        name="username"
                                        render={({ field }) => (
                                            <Field label="Username" hint="SMTP authentication username.">
                                                <FormItem className="space-y-1">
                                                    <FormControl>
                                                        <Input {...field} />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            </Field>
                                        )}
                                    />

                                    <FormField
                                        control={form.control}
                                        name="password"
                                        render={({ field }) => (
                                            <Field label="Password" hint={hasPassword ? "Leave blank to keep existing." : "SMTP authentication password."}>
                                                <FormItem className="space-y-1">
                                                    <FormControl>
                                                        <Input type="password" {...field} />
                                                    </FormControl>
                                                    <FormMessage />
                                                </FormItem>
                                            </Field>
                                        )}
                                    />
                                </>
                            )}

                            {renderTestEmail?.()}
                        </div>
                    </Card>
                </form>
            </Form>
        </div>
    );
}
