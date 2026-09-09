"use client";

import { useState } from "react";
import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
    AlertDialog,
    AlertDialogAction,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    SheetBody,
    SheetFooter,
    SheetSection,
} from "@/components/ui/sheet";
import { useCustomForm } from "@/hooks/use-custom-form";
import { apiClient } from "@/lib/api-client";
import { TicketTypeDto } from "@/lib/admitto-api/generated";

const editSchema = z.object({
    name: z.string().min(1, "Name is required"),
    selfServiceEnabled: z.boolean(),
    limitCapacity: z.boolean(),
    maxCapacity: z.number().int().min(1).optional(),
    waitlistEnabled: z.boolean(),
    claimWindowHours: z.number().int().min(1).optional(),
    maxReconfirmationEmails: z.number().int().min(1, "Must be at least 1").optional(),
});

type EditValues = z.infer<typeof editSchema>;

export function EditTicketTypeForm({
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
    const queryClient = useQueryClient();
    const hasCapacity = ticketType.maxCapacity != null;
    const form = useCustomForm<EditValues>(editSchema, {
        name: ticketType.name,
        selfServiceEnabled: ticketType.selfServiceEnabled,
        limitCapacity: hasCapacity,
        maxCapacity: hasCapacity ? Number(ticketType.maxCapacity) : undefined,
        waitlistEnabled: ticketType.waitlistEnabled,
        claimWindowHours: Number(ticketType.claimWindowHours) || 8,
        maxReconfirmationEmails: ticketType.maxReconfirmationEmails != null
            ? Number(ticketType.maxReconfirmationEmails)
            : undefined,
    });

    const limitCapacity = form.watch("limitCapacity");
    const waitlistEnabled = form.watch("waitlistEnabled");
    const [showDisableWaitlistConfirm, setShowDisableWaitlistConfirm] = useState(false);
    const [pendingSubmitValues, setPendingSubmitValues] = useState<EditValues | null>(null);

    async function onSubmit(values: EditValues) {
        const isDisablingActiveWaitlist =
            ticketType.waitlistMode && !values.waitlistEnabled && ticketType.waitlistEnabled;

        if (isDisablingActiveWaitlist) {
            setPendingSubmitValues(values);
            setShowDisableWaitlistConfirm(true);
            return;
        }

        await performSave(values);
    }

    async function performSave(values: EditValues) {
        await apiClient.put(
            `/api/teams/${teamId}/events/${eventId}/ticket-types/${ticketType.id}`,
            {
                name: values.name,
                selfServiceEnabled: values.selfServiceEnabled,
                maxCapacity: values.limitCapacity ? (values.maxCapacity ?? null) : null,
                waitlistEnabled: values.limitCapacity ? values.waitlistEnabled : false,
                claimWindowHours: values.limitCapacity && values.waitlistEnabled ? (values.claimWindowHours ?? 8) : undefined,
                maxReconfirmationEmails: values.maxReconfirmationEmails ?? null,
            }
        );
        await queryClient.invalidateQueries({ queryKey: ["ticket-types", teamId, eventId] });
        onSaved();
    }

    return (
        <>
            <AlertDialog open={showDisableWaitlistConfirm} onOpenChange={setShowDisableWaitlistConfirm}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Disable waitlist?</AlertDialogTitle>
                        <AlertDialogDescription>
                            This ticket type currently has an active waitlist. Disabling the waitlist will
                            immediately revoke all pending claim coupons and remove all waiting entries.
                            This action cannot be undone.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel onClick={() => setPendingSubmitValues(null)}>
                            Cancel
                        </AlertDialogCancel>
                        <AlertDialogAction
                            onClick={async () => {
                                if (pendingSubmitValues) {
                                    await performSave(pendingSubmitValues);
                                }
                            }}
                        >
                            Disable waitlist
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
            <Form {...form}>
                <form onSubmit={form.submit(onSubmit)} className="flex min-h-0 flex-1 flex-col">
                    <SheetBody className="space-y-6">
                        {form.generalError && (
                            <Alert variant="destructive">
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle>{form.generalError.title}</AlertTitle>
                                <AlertDescription>{form.generalError.detail}</AlertDescription>
                            </Alert>
                        )}
                        <SheetSection title="Details">
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
                        </SheetSection>
                        <SheetSection title="Registration">
                            <FormField
                                control={form.control}
                                name="selfServiceEnabled"
                                render={({ field }) => (
                                    <FormItem className="flex items-center gap-3">
                                        <FormControl>
                                            <Switch checked={field.value} onCheckedChange={field.onChange} />
                                        </FormControl>
                                        <FormLabel className="!mt-0">Enable self-service registration</FormLabel>
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="limitCapacity"
                                render={({ field }) => (
                                    <FormItem className="flex items-center gap-3">
                                        <FormControl>
                                            <Switch
                                                checked={field.value}
                                                onCheckedChange={(checked) => {
                                                    field.onChange(checked);
                                                    if (!checked) {
                                                        form.setValue("waitlistEnabled", false);
                                                    }
                                                }}
                                            />
                                        </FormControl>
                                        <FormLabel className="!mt-0">Limit capacity</FormLabel>
                                    </FormItem>
                                )}
                            />
                            {limitCapacity && (
                                <FormField
                                    control={form.control}
                                    name="maxCapacity"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Max capacity</FormLabel>
                                            <FormControl>
                                                <Input
                                                    type="number"
                                                    min={1}
                                                    placeholder="e.g. 100"
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
                            )}
                            {limitCapacity && (
                                <FormField
                                    control={form.control}
                                    name="waitlistEnabled"
                                    render={({ field }) => (
                                        <FormItem className="flex items-center gap-3">
                                            <FormControl>
                                                <Switch checked={field.value} onCheckedChange={field.onChange} />
                                            </FormControl>
                                            <FormLabel className="!mt-0">Enable waitlist</FormLabel>
                                        </FormItem>
                                    )}
                                />
                            )}
                            {limitCapacity && waitlistEnabled && (
                                <FormField
                                    control={form.control}
                                    name="claimWindowHours"
                                    render={({ field }) => (
                                        <FormItem>
                                            <FormLabel>Claim window (hours)</FormLabel>
                                            <FormControl>
                                                <Input
                                                    type="number"
                                                    min={1}
                                                    placeholder="e.g. 8"
                                                    value={field.value ?? ""}
                                                    onChange={(e) =>
                                                        field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                                    }
                                                />
                                            </FormControl>
                                            <FormDescription>
                                                How long a notified attendee has to claim their spot.
                                            </FormDescription>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            )}
                        </SheetSection>
                        <SheetSection title="Reconfirmation">
                            <FormField
                                control={form.control}
                                name="maxReconfirmationEmails"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>Maximum reconfirmation emails (optional)</FormLabel>
                                        <FormControl>
                                            <Input
                                                type="number"
                                                min={1}
                                                placeholder="e.g. 3"
                                                value={field.value ?? ""}
                                                onChange={(e) =>
                                                    field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                                }
                                            />
                                        </FormControl>
                                        <FormDescription>
                                            Maximum delivered emails for this registration cycle; leave blank for unlimited.
                                        </FormDescription>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </SheetSection>
                        {ticketType.timeSlots && ticketType.timeSlots.length > 0 && (
                            <SheetSection title="Schedule">
                                <div className="space-y-2">
                                    <div className="flex flex-wrap gap-1.5">
                                        {ticketType.timeSlots.map((slot) => (
                                            <Badge
                                                key={slot}
                                                variant="outline"
                                                className="font-mono text-[11px] opacity-70"
                                            >
                                                {slot}
                                            </Badge>
                                        ))}
                                    </div>
                                    <p className="text-[11px] text-muted-foreground">
                                        Time slots can&apos;t be changed after creation.
                                    </p>
                                </div>
                            </SheetSection>
                        )}
                    </SheetBody>
                    <SheetFooter>
                        <Button type="button" variant="outline" onClick={onCancel}>
                            Cancel
                        </Button>
                        <Button type="submit" disabled={form.formState.isSubmitting}>
                            {form.formState.isSubmitting ? "Saving..." : "Save changes"}
                        </Button>
                    </SheetFooter>
                </form>
            </Form>
        </>
    );
}
