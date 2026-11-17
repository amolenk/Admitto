"use client";

import { useState } from "react";
import * as z from "zod";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Info } from "lucide-react";
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
import {
    countOf,
    PUBLIC_CAPACITY_HELP,
    previewPublicCapacityChange,
    requirePublicCapacityWhenLimited,
    type PublicCapacityChangePreview,
} from "@/lib/ticket-capacity";

const editSchema = z.object({
    name: z.string().min(1, "Name is required"),
    selfServiceEnabled: z.boolean(),
    limitCapacity: z.boolean(),
    publicCapacity: z.number().int().min(0).optional(),
    waitlistEnabled: z.boolean(),
    claimWindowHours: z.number().int().min(1).optional(),
    maxReconfirmationEmails: z.number().int().min(1, "Must be at least 1").optional(),
}).superRefine(requirePublicCapacityWhenLimited);

type EditValues = z.infer<typeof editSchema>;

type PendingConfirmation =
    | { kind: "disable-waitlist"; values: EditValues }
    | { kind: "send-offers"; values: EditValues; preview: PublicCapacityChangePreview };

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
    const hasCapacity = ticketType.publicCapacity != null;
    const form = useCustomForm<EditValues>(editSchema, {
        name: ticketType.name,
        selfServiceEnabled: ticketType.selfServiceEnabled,
        limitCapacity: hasCapacity,
        publicCapacity: hasCapacity ? Number(ticketType.publicCapacity) : undefined,
        waitlistEnabled: ticketType.waitlistEnabled,
        claimWindowHours: Number(ticketType.claimWindowHours) || 8,
        maxReconfirmationEmails: ticketType.maxReconfirmationEmails != null
            ? Number(ticketType.maxReconfirmationEmails)
            : undefined,
    });

    const limitCapacity = form.watch("limitCapacity");
    const waitlistEnabled = form.watch("waitlistEnabled");
    const publicCapacity = form.watch("publicCapacity");
    const [pendingConfirmation, setPendingConfirmation] = useState<PendingConfirmation | null>(null);

    // A waitlist only has people waiting while the ticket type is in waitlist mode. Removing the capacity
    // limit also switches the waitlist off, but then everyone waiting receives an offer instead.
    const hasPeopleWaiting = ticketType.waitlistEnabled && ticketType.waitlistMode;

    const publicUsed = Number(ticketType.publicUsedCapacity) || 0;
    const adminUsed = Number(ticketType.adminUsedCount) || 0;
    const queued = Number(ticketType.waitlistQueuedCount) || 0;

    // Raising the public capacity while people are waiting sends them offers on save. Admin tickets come on top of
    // the public capacity, so they don't reduce the number of offers — easy to miss when thinking in total attendees.
    const offerPreview = limitCapacity && waitlistEnabled
        ? previewPublicCapacityChange(ticketType, publicCapacity)
        : null;

    async function onSubmit(values: EditValues) {
        const isDisablingActiveWaitlist =
            hasPeopleWaiting && values.limitCapacity && !values.waitlistEnabled;

        if (isDisablingActiveWaitlist) {
            setPendingConfirmation({ kind: "disable-waitlist", values });
            return;
        }

        const preview = values.limitCapacity && values.waitlistEnabled
            ? previewPublicCapacityChange(ticketType, values.publicCapacity)
            : null;
        if (preview) {
            setPendingConfirmation({ kind: "send-offers", values, preview });
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
                publicCapacity: values.limitCapacity ? (values.publicCapacity ?? null) : null,
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
            <AlertDialog
                open={pendingConfirmation !== null}
                onOpenChange={(open) => {
                    if (!open) setPendingConfirmation(null);
                }}
            >
                <AlertDialogContent>
                    {pendingConfirmation?.kind === "send-offers" ? (
                        <AlertDialogHeader>
                            <AlertDialogTitle>
                                Send {countOf(pendingConfirmation.preview.offers, "waitlist offer", "waitlist offers")}?
                            </AlertDialogTitle>
                            <AlertDialogDescription>
                                <OfferPreviewText
                                    preview={pendingConfirmation.preview}
                                    publicCapacity={pendingConfirmation.values.publicCapacity}
                                />{" "}
                                Offers are emailed straight away and can&apos;t be taken back.
                            </AlertDialogDescription>
                        </AlertDialogHeader>
                    ) : (
                        <AlertDialogHeader>
                            <AlertDialogTitle>Disable waitlist?</AlertDialogTitle>
                            <AlertDialogDescription>
                                Disabling the waitlist removes everyone who is still waiting. You&apos;ll need to
                                inform them yourself. Offers already sent stay valid until they expire.
                            </AlertDialogDescription>
                        </AlertDialogHeader>
                    )}
                    <AlertDialogFooter>
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={async () => {
                                if (pendingConfirmation) {
                                    await performSave(pendingConfirmation.values);
                                }
                            }}
                        >
                            {pendingConfirmation?.kind === "send-offers" ? "Save and send offers" : "Disable waitlist"}
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
                            {!limitCapacity && hasPeopleWaiting && (
                                <Alert>
                                    <Info className="h-4 w-4" />
                                    <AlertTitle>Everyone waiting will receive an offer</AlertTitle>
                                    <AlertDescription>
                                        Without a capacity limit there is room for everyone, so the waitlist is
                                        switched off and everyone waiting will receive an offer.
                                    </AlertDescription>
                                </Alert>
                            )}
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
                                                    placeholder="e.g. 100"
                                                    value={field.value ?? ""}
                                                    onChange={(e) =>
                                                        field.onChange(e.target.value === "" ? undefined : e.target.valueAsNumber)
                                                    }
                                                />
                                            </FormControl>
                                            <FormDescription>
                                                {PUBLIC_CAPACITY_HELP}
                                            </FormDescription>
                                            <p className="text-xs text-muted-foreground" data-testid="capacity-breakdown">
                                                Now: {countOf(publicUsed, "public ticket", "public tickets")} taken
                                                {adminUsed > 0 && <>, plus {countOf(adminUsed, "admin ticket", "admin tickets")} on top (not counted)</>}
                                                {hasPeopleWaiting && queued > 0 && <>; {countOf(queued, "person", "people")} on the waitlist</>}
                                                .
                                            </p>
                                            <FormMessage />
                                        </FormItem>
                                    )}
                                />
                            )}
                            {offerPreview && (
                                <Alert>
                                    <Info className="h-4 w-4" />
                                    <AlertTitle>
                                        {countOf(offerPreview.offers, "person", "people")} on the waitlist will receive an offer
                                    </AlertTitle>
                                    <AlertDescription>
                                        <OfferPreviewText preview={offerPreview} publicCapacity={publicCapacity} />
                                    </AlertDescription>
                                </Alert>
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

function OfferPreviewText({
    preview,
    publicCapacity,
}: {
    preview: PublicCapacityChangePreview;
    publicCapacity: number | undefined;
}) {
    const freed = preview.offers + preview.seatsOpening;
    return (
        <>
            Public capacity {publicCapacity} frees {countOf(freed, "public seat", "public seats")}; admin tickets
            don&apos;t count towards it. The first {countOf(preview.offers, "person", "people")} on the waitlist
            {preview.offers === 1 ? " gets" : " get"} an offer.
            {preview.seatsOpening > 0 && (
                <>
                    {" "}The other {countOf(preview.seatsOpening, "seat opens", "seats open")} for self-service once
                    every offer has been claimed or has expired.
                </>
            )}
        </>
    );
}
