"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import * as z from "zod";
import { AlertCircle, Globe, Lock, Plus, Pencil, X, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketTypeDto } from "@/lib/admitto-api/generated";
import { formatCapacitySummary, PUBLIC_CAPACITY_HELP, ticketCapacity } from "@/lib/ticket-capacity";


const requirePublicCapacityWhenLimited = (data: { limitCapacity: boolean; publicCapacity?: number }, ctx: z.RefinementCtx) => {
    if (data.limitCapacity && data.publicCapacity === undefined) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            path: ["publicCapacity"],
            message: "Required while capacity is limited",
        });
    }
};

const addSchema = z.object({
    name: z.string().min(1, "Name is required"),
    selfServiceEnabled: z.boolean(),
    limitCapacity: z.boolean(),
    publicCapacity: z.number().int().min(0).optional(),
}).superRefine(requirePublicCapacityWhenLimited);

type AddValues = z.infer<typeof addSchema>;

const editSchema = z.object({
    name: z.string().min(1, "Name is required"),
    selfServiceEnabled: z.boolean(),
    limitCapacity: z.boolean(),
    publicCapacity: z.number().int().min(0).optional(),
}).superRefine(requirePublicCapacityWhenLimited);

type EditValues = z.infer<typeof editSchema>;

export function TicketTypesSection({
    teamId,
    eventId,
    ticketTypes,
}: {
    teamId: string;
    eventId: string;
    ticketTypes: TicketTypeDto[];
}) {
    const queryClient = useQueryClient();
    const [editingId, setEditingId] = useState<string | null>(null);
    const [showAdd, setShowAdd] = useState(false);

    const invalidate = () =>
        queryClient.invalidateQueries({ queryKey: ["ticket-types", teamId, eventId] });

    return (
        <div className="space-y-4">
            <ul className="divide-y rounded-md border">
                {ticketTypes.length === 0 && (
                    <li className="px-4 py-3 text-sm text-muted-foreground">No ticket types yet.</li>
                )}
                {ticketTypes.map((tt) =>
                    editingId === tt.id ? (
                        <li key={tt.id} className="px-4 py-3">
                            <EditTicketTypeForm
                                teamId={teamId}
                                eventId={eventId}
                                ticketType={tt}
                                onCancel={() => setEditingId(null)}
                                onSaved={() => {
                                    setEditingId(null);
                                    invalidate();
                                }}
                            />
                        </li>
                    ) : (
                        <li
                            key={tt.id}
                            className="px-4 py-3 flex items-center gap-4 text-sm"
                        >
                            <div className="flex-1 min-w-0">
                                <p className="font-medium flex items-center gap-1.5">
                                    {tt.name}{" "}
                                    {tt.selfServiceEnabled ? (
                                        <span className="inline-flex items-center gap-0.5 text-[10px] font-medium text-blue-600 dark:text-blue-400">
                                            <Globe className="h-3 w-3" /> Self-service
                                        </span>
                                    ) : (
                                        <span className="inline-flex items-center gap-0.5 text-[10px] font-medium text-muted-foreground">
                                            <Lock className="h-3 w-3" /> Admin only
                                        </span>
                                    )}
                                </p>
                                <p className="text-xs text-muted-foreground">
                                    {formatCapacitySummary(ticketCapacity(tt))}
                                </p>
                            </div>
                            <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                onClick={() => setEditingId(tt.id)}
                            >
                                <Pencil className="h-4 w-4" />
                            </Button>
                        </li>
                    )
                )}
            </ul>

            {showAdd ? (
                <AddTicketTypeForm
                    teamId={teamId}
                    eventId={eventId}
                    onCancel={() => setShowAdd(false)}
                    onAdded={() => {
                        setShowAdd(false);
                        invalidate();
                    }}
                />
            ) : (
                <Button type="button" variant="secondary" onClick={() => setShowAdd(true)}>
                    <Plus className="mr-1 h-4 w-4" /> Add ticket type
                </Button>
            )}
        </div>
    );
}

function AddTicketTypeForm({
    teamId,
    eventId,
    onAdded,
    onCancel,
}: {
    teamId: string;
    eventId: string;
    onAdded: () => void;
    onCancel: () => void;
}) {
    const form = useCustomForm<AddValues>(addSchema, {
        name: "",
        selfServiceEnabled: true,
        limitCapacity: false,
        publicCapacity: undefined,
    });

    const limitCapacity = form.watch("limitCapacity");

    async function onSubmit(values: AddValues) {
        await apiClient.post(`/api/teams/${teamId}/events/${eventId}/ticket-types`, {
            name: values.name,
            selfServiceEnabled: values.selfServiceEnabled,
            publicCapacity: values.limitCapacity ? (values.publicCapacity ?? null) : null,
            timeSlots: null,
        });
        onAdded();
    }

    return (
        <Form {...form}>
            <form
                onSubmit={form.submit(onSubmit)}
                className="border rounded-md p-4 space-y-3"
            >
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}
                <div className="grid grid-cols-1 gap-3">
                    <FormField
                        control={form.control}
                        name="name"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Name</FormLabel>
                                <FormControl>
                                    <Input placeholder="Standard" {...field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                </div>
                <div className="flex gap-4">
                    <FormField
                        control={form.control}
                        name="selfServiceEnabled"
                        render={({ field }) => (
                            <FormItem className="flex items-center gap-2">
                                <FormControl>
                                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                                </FormControl>
                                <FormLabel className="!mt-0 text-xs">Self-service</FormLabel>
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={form.control}
                        name="limitCapacity"
                        render={({ field }) => (
                            <FormItem className="flex items-center gap-2">
                                <FormControl>
                                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                                </FormControl>
                                <FormLabel className="!mt-0 text-xs">Limit capacity</FormLabel>
                            </FormItem>
                        )}
                    />
                </div>
                {limitCapacity && (
                    <FormField
                        control={form.control}
                        name="publicCapacity"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Public capacity</FormLabel>
                                <FormControl>
                                    <Input
                                        type="number"
                                        min={0}
                                        value={field.value ?? ""}
                                        onChange={(e) =>
                                            field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                        }
                                    />
                                </FormControl>
                                <FormDescription>{PUBLIC_CAPACITY_HELP}</FormDescription>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                )}
                <div className="flex gap-2">
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        Add
                    </Button>
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                </div>
            </form>
        </Form>
    );
}
function EditTicketTypeForm({
    teamId,
    eventId,
    ticketType,
    onSaved,
    onCancel,
}: {
    teamId: string;
    eventId: string;
    ticketType: TicketTypeDto;
    onSaved: () => void;
    onCancel: () => void;
}) {
    const hasCapacity = ticketType.publicCapacity != null;
    const form = useCustomForm<EditValues>(editSchema, {
        name: ticketType.name,
        selfServiceEnabled: ticketType.selfServiceEnabled,
        limitCapacity: hasCapacity,
        publicCapacity: hasCapacity ? Number(ticketType.publicCapacity) : undefined,
    });

    const limitCapacity = form.watch("limitCapacity");

    async function onSubmit(values: EditValues) {
        await apiClient.put(
            `/api/teams/${teamId}/events/${eventId}/ticket-types/${ticketType.id}`,
            {
                name: values.name,
                selfServiceEnabled: values.selfServiceEnabled,
                publicCapacity: values.limitCapacity ? (values.publicCapacity ?? null) : null,
            }
        );
        onSaved();
    }

    return (
        <Form {...form}>
            <form onSubmit={form.submit(onSubmit)} className="space-y-3">
                {form.generalError && (
                    <Alert variant="destructive">
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>{form.generalError.title}</AlertTitle>
                        <AlertDescription>{form.generalError.detail}</AlertDescription>
                    </Alert>
                )}
                <FormField
                    control={form.control}
                    name="name"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Name</FormLabel>
                            <FormControl>
                                <Input {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <div className="flex gap-4">
                    <FormField
                        control={form.control}
                        name="selfServiceEnabled"
                        render={({ field }) => (
                            <FormItem className="flex items-center gap-2">
                                <FormControl>
                                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                                </FormControl>
                                <FormLabel className="!mt-0 text-xs">Self-service</FormLabel>
                            </FormItem>
                        )}
                    />
                    <FormField
                        control={form.control}
                        name="limitCapacity"
                        render={({ field }) => (
                            <FormItem className="flex items-center gap-2">
                                <FormControl>
                                    <Switch checked={field.value} onCheckedChange={field.onChange} />
                                </FormControl>
                                <FormLabel className="!mt-0 text-xs">Limit capacity</FormLabel>
                            </FormItem>
                        )}
                    />
                </div>
                {limitCapacity && (
                    <FormField
                        control={form.control}
                        name="publicCapacity"
                        render={({ field }) => (
                            <FormItem>
                                <FormLabel>Public capacity</FormLabel>
                                <FormControl>
                                    <Input
                                        type="number"
                                        min={0}
                                        value={field.value ?? ""}
                                        onChange={(e) =>
                                            field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                        }
                                    />
                                </FormControl>
                                <FormDescription>{PUBLIC_CAPACITY_HELP}</FormDescription>
                                <FormMessage />
                            </FormItem>
                        )}
                    />
                )}
                <div className="flex gap-2">
                    <Button type="submit" size="sm" disabled={form.formState.isSubmitting}>
                        <Check className="mr-1 h-4 w-4" /> Save
                    </Button>
                    <Button type="button" size="sm" variant="ghost" onClick={onCancel}>
                        <X className="mr-1 h-4 w-4" /> Cancel
                    </Button>
                </div>
            </form>
        </Form>
    );
}
