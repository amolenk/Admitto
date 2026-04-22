"use client";

import { useState, KeyboardEvent } from "react";
import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";

const slugRegex = /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/;

const addSchema = z.object({
    slug: z.string().min(1, "Slug is required").regex(slugRegex, "Lowercase letters, digits, hyphens"),
    name: z.string().min(1, "Name is required"),
    maxCapacity: z.coerce.number().int().positive().optional(),
    timeSlots: z.array(z.string().regex(slugRegex)),
});

type AddValues = z.infer<typeof addSchema>;

export function AddTicketTypeForm({
    teamSlug,
    eventSlug,
    suggestions = [],
    onAdded,
    onCancel,
}: {
    teamSlug: string;
    eventSlug: string;
    suggestions?: string[];
    onAdded: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const form = useCustomForm<AddValues>(addSchema, {
        slug: "",
        name: "",
        maxCapacity: undefined,
        timeSlots: [],
    });

    async function onSubmit(values: AddValues) {
        await apiClient.post(`/api/teams/${teamSlug}/events/${eventSlug}/ticket-types`, {
            slug: values.slug,
            name: values.name,
            maxCapacity: values.maxCapacity ?? null,
            timeSlots: values.timeSlots,
        });
        await queryClient.invalidateQueries({ queryKey: ["ticket-types", teamSlug, eventSlug] });
        onAdded();
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
                    name="slug"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Slug</FormLabel>
                            <FormControl>
                                <Input placeholder="early-bird" {...field} />
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
                                <Input placeholder="Early Bird" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="maxCapacity"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Max capacity (optional)</FormLabel>
                            <FormControl>
                                <Input
                                    type="number"
                                    min={1}
                                    placeholder="Leave empty for unlimited"
                                    value={field.value ?? ""}
                                    onChange={(e) =>
                                        field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                    }
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="timeSlots"
                    render={({ field }) => (
                        <TimeSlotsField
                            value={field.value ?? []}
                            onChange={field.onChange}
                            suggestions={suggestions}
                        />
                    )}
                />
                <div className="flex gap-2 justify-end">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Adding..." : "Add ticket type"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}

function TimeSlotsField({
    value,
    onChange,
    suggestions,
}: {
    value: string[];
    onChange: (next: string[]) => void;
    suggestions: string[];
}) {
    const [draft, setDraft] = useState("");
    const [error, setError] = useState<string | null>(null);

    const remainingSuggestions = suggestions.filter((s) => !value.includes(s));

    function tryAdd(raw: string) {
        const token = raw.trim().toLowerCase();
        if (token === "") return;
        if (!slugRegex.test(token)) {
            setError("Lowercase letters, digits, hyphens");
            return;
        }
        if (value.includes(token)) {
            setError("Already added");
            return;
        }
        onChange([...value, token]);
        setDraft("");
        setError(null);
    }

    function remove(slug: string) {
        onChange(value.filter((s) => s !== slug));
    }

    function handleKeyDown(e: KeyboardEvent<HTMLInputElement>) {
        if (e.key === "Enter" || e.key === ",") {
            e.preventDefault();
            tryAdd(draft);
        } else if (e.key === "Backspace" && draft === "" && value.length > 0) {
            e.preventDefault();
            remove(value[value.length - 1]);
        }
    }

    return (
        <FormItem>
            <FormLabel>Time slots (optional)</FormLabel>
            <FormControl>
                <div className="space-y-2">
                    {value.length > 0 && (
                        <div className="flex flex-wrap gap-1.5">
                            {value.map((slug) => (
                                <Badge key={slug} variant="secondary" className="gap-1 pr-1">
                                    <span className="font-mono text-xs">{slug}</span>
                                    <button
                                        type="button"
                                        onClick={() => remove(slug)}
                                        className="rounded-sm hover:bg-muted-foreground/20 p-0.5"
                                        aria-label={`Remove ${slug}`}
                                    >
                                        <X className="size-3" />
                                    </button>
                                </Badge>
                            ))}
                        </div>
                    )}
                    <Input
                        placeholder="e.g. morning, afternoon"
                        value={draft}
                        onChange={(e) => {
                            setDraft(e.target.value);
                            if (error) setError(null);
                        }}
                        onKeyDown={handleKeyDown}
                        onBlur={() => {
                            if (draft.trim() !== "") tryAdd(draft);
                        }}
                    />
                    {remainingSuggestions.length > 0 && (
                        <div className="flex flex-wrap items-center gap-1.5 pt-1">
                            <span className="text-[11px] text-muted-foreground">Used in this event:</span>
                            {remainingSuggestions.map((slug) => (
                                <button
                                    key={slug}
                                    type="button"
                                    onClick={() => tryAdd(slug)}
                                    className="rounded-md border border-dashed px-2 py-0.5 font-mono text-xs text-muted-foreground hover:text-foreground hover:bg-accent"
                                >
                                    + {slug}
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            </FormControl>
            {error ? (
                <p className="text-[12.8px] font-medium text-destructive">{error}</p>
            ) : (
                <p className="text-[11px] text-muted-foreground">
                    Press Enter or comma to add. Time slots can&apos;t be changed after creation.
                </p>
            )}
        </FormItem>
    );
}
