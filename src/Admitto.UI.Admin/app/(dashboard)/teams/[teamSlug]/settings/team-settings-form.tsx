"use client";

import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertCircle } from "lucide-react";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TeamDto } from "@/lib/admitto-api/generated";

const teamSettingsSchema = z.object({
    name: z.string().min(1, "Name is required"),
    emailAddress: z.string().min(1, "Email is required").email("Must be a valid email address"),
});

type TeamSettingsValues = z.infer<typeof teamSettingsSchema>;

interface TeamSettingsFormProps {
    team: TeamDto;
}

export function TeamSettingsForm({ team }: TeamSettingsFormProps) {
    const queryClient = useQueryClient();

    const form = useCustomForm<TeamSettingsValues>(teamSettingsSchema, {
        name: team.name,
        emailAddress: team.emailAddress,
    });

    async function onSubmit(values: TeamSettingsValues) {
        const body: Record<string, unknown> = {
            expectedVersion: Number(team.version),
        };

        if (values.name !== team.name) body.name = values.name;
        if (values.emailAddress !== team.emailAddress) body.emailAddress = values.emailAddress;

        await apiClient.put(`/api/teams/${team.slug}`, body);

        await queryClient.invalidateQueries({ queryKey: ["teams"] });
        await queryClient.invalidateQueries({ queryKey: ["team", team.slug] });
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-6 max-w-lg">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}

                <div className="space-y-2">
                    <label className="text-sm font-medium leading-none">Slug</label>
                    <Input value={team.slug} disabled className="bg-muted" />
                    <p className="text-xs text-muted-foreground">
                        Slugs cannot be changed after creation.
                    </p>
                </div>

                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. My Team" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <FormField
                    control={form.control}
                    name="emailAddress"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Email address</FormLabel>
                            <FormControl>
                                <Input type="email" placeholder="e.g. team@example.com" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

                <Button type="submit" disabled={form.formState.isSubmitting}>
                    {form.formState.isSubmitting ? "Saving…" : "Save changes"}
                </Button>
            </form>
        </Form>
    );
}
