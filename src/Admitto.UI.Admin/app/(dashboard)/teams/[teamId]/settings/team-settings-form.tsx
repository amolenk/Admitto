"use client";

import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { AlertCircle } from "lucide-react";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TeamDto } from "@/lib/admitto-api/generated";

const teamSettingsSchema = z.object({
    name: z.string().min(1, "Name is required"),
    accentColor: z.string().regex(/^#[0-9a-fA-F]{6}$/, "Use a hex color like #0f766e"),
    replyToEmailAddress: z.union([
        z.literal(""),
        z.string().trim().email("Use a valid email address"),
    ]),
});

type TeamSettingsValues = z.infer<typeof teamSettingsSchema>;

interface TeamSettingsFormProps {
    team: TeamDto;
}

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

export function TeamSettingsForm({ team }: TeamSettingsFormProps) {
    const queryClient = useQueryClient();

    const form = useCustomForm<TeamSettingsValues>(teamSettingsSchema, {
        name: team.name,
        accentColor: team.accentColor,
        replyToEmailAddress: team.replyToEmailAddress ?? "",
    });

    async function onSubmit(values: TeamSettingsValues) {
        const body: Record<string, unknown> = {
            expectedVersion: Number(team.version),
        };

        if (values.name !== team.name) body.name = values.name;
        if (values.accentColor !== team.accentColor) body.accentColor = values.accentColor;
        const replyToEmailAddress = values.replyToEmailAddress.trim();
        const currentReplyToEmailAddress = team.replyToEmailAddress ?? "";
        if (replyToEmailAddress !== currentReplyToEmailAddress) {
            if (replyToEmailAddress) {
                body.replyToEmailAddress = replyToEmailAddress;
            } else {
                body.clearReplyToEmailAddress = true;
            }
        }

        await apiClient.put(`/api/teams/${team.teamId}`, body);

        await queryClient.invalidateQueries({ queryKey: ["teams"] });
        await queryClient.invalidateQueries({ queryKey: ["team", team.teamId] });
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">General</h2>
                    <p className="text-[13.5px] text-muted-foreground">Basic team information.</p>
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
                                name="name"
                                render={({ field }) => (
                                    <Field label="Team name" hint="The display name for your team.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input placeholder="e.g. My Team" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="accentColor"
                                render={({ field }) => (
                                    <Field label="Accent color" hint="Used for team branding in built-in emails.">
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <div className="flex items-center gap-3">
                                                    <Input type="color" className="h-10 w-16 p-1" {...field} />
                                                    <Input placeholder="#0f766e" {...field} />
                                                </div>
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />

                            <FormField
                                control={form.control}
                                name="replyToEmailAddress"
                                render={({ field }) => (
                                    <Field
                                        label="Reply-to email"
                                        hint="Optional. Replies to Admitto emails for this team go to this address."
                                    >
                                        <FormItem className="space-y-1">
                                            <FormControl>
                                                <Input type="email" placeholder="help@example.com" {...field} />
                                            </FormControl>
                                            <FormMessage />
                                        </FormItem>
                                    </Field>
                                )}
                            />
                        </div>
                    </Card>
                </form>
            </Form>
        </div>
    );
}
