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
import { useTeamStore } from "@/stores/team-store";
import { apiClient } from "@/lib/api-client";

const createTeamSchema = z.object({
    slug: z
        .string()
        .min(1, "Slug is required")
        .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, "Slug must be lowercase alphanumeric with hyphens"),
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

                <FormField
                    control={form.control}
                    name="slug"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Slug</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. my-team" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />

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
                    {form.formState.isSubmitting ? "Creating…" : "Create team"}
                </Button>
            </form>
        </Form>
    );
}
