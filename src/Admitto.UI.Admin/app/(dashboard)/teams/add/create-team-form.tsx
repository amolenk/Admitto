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

const createTeamSchema = z.object({
    name: z.string().min(1, "Name is required"),
});

type CreateTeamValues = z.infer<typeof createTeamSchema>;

type CreateTeamResponse = {
    teamId: string;
};

export function CreateTeamForm() {
    const router = useRouter();
    const queryClient = useQueryClient();
    const setSelectedTeamId = useTeamStore((s) => s.setSelectedTeamId);

    const form = useCustomForm<CreateTeamValues>(createTeamSchema, {
        name: "",
    });

    async function onSubmit(values: CreateTeamValues) {
        const result = await apiClient.post<CreateTeamResponse>("/api/teams", values);
        await queryClient.invalidateQueries({ queryKey: ["teams"] });
        setSelectedTeamId(result.teamId);
        router.push(`/teams/${result.teamId}`);
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
                            </div>
                        </Card>
                    </form>
                </fieldset>
            </Form>
        </div>
    );
}

