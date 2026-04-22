"use client";

import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { Spinner } from "@/components/ui/spinner";
import { useCustomForm } from "@/hooks/use-custom-form";
import { useTeamStore } from "@/stores/team-store";
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

const slugRegex = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const createTeamSchema = z.object({
    slug: z
        .string()
        .min(1, "Slug is required")
        .regex(slugRegex, "Slug must be lowercase alphanumeric with hyphens"),
    name: z.string().min(1, "Name is required"),
    emailAddress: z.string().min(1, "Email is required").email("Must be a valid email address"),
});

type CreateTeamValues = z.infer<typeof createTeamSchema>;

export function CreateTeamForm() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const setSelectedTeamSlug = useTeamStore((s) => s.setSelectedTeamSlug);

    const form = useCustomForm<CreateTeamValues>(createTeamSchema, {
        slug: "",
        name: "",
        emailAddress: "",
    });

    async function onSubmit(values: CreateTeamValues) {
        await apiClient.post("/api/teams", values);
        await queryClient.invalidateQueries({ queryKey: ["teams"] });
        setSelectedTeamSlug(values.slug);
        router.push("/");
    }

    const rootError = form.formState.errors.root?.message;
    const busy = form.formState.isSubmitting;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Create team</h2>
                    <p className="text-[13.5px] text-muted-foreground">Set up a new team to host ticketed events.</p>
                </div>
                <Button size="sm" onClick={form.submit(onSubmit)} disabled={busy}>
                    {busy ? <Spinner className="size-3.5" /> : <Check className="size-3.5" />}
                    {busy ? "Creating\u2026" : "Create team"}
                </Button>
            </div>

            {form.generalError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{form.generalError.title}</AlertTitle>
                    <AlertDescription>{form.generalError.detail}</AlertDescription>
                </Alert>
            )}

            {rootError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Unable to create team</AlertTitle>
                    <AlertDescription>{rootError}</AlertDescription>
                </Alert>
            )}

            <Form {...form}>
                <fieldset disabled={busy} className="contents">
                    <form onSubmit={form.submit(onSubmit)}>
                        <Card>
                            <div className="px-6 divide-y">
                                <FormField
                                    control={form.control}
                                    name="slug"
                                    render={({ field }) => (
                                        <Field label="Slug" hint="Used in URLs. Cannot be changed later.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input placeholder="e.g. my-team" className="max-w-sm font-mono" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />

                                <FormField
                                    control={form.control}
                                    name="name"
                                    render={({ field }) => (
                                        <Field label="Team name" hint="Shown in the admin UI and on event pages.">
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
                                    name="emailAddress"
                                    render={({ field }) => (
                                        <Field label="Email address" hint="From-address used for attendee emails.">
                                            <FormItem className="space-y-1">
                                                <FormControl>
                                                    <Input type="email" placeholder="e.g. team@example.com" {...field} />
                                                </FormControl>
                                                <FormMessage />
                                            </FormItem>
                                        </Field>
                                    )}
                                />
                            </div>
                        </Card>
                    </form>
                </fieldset>
            </Form>
        </div>
    );
}
