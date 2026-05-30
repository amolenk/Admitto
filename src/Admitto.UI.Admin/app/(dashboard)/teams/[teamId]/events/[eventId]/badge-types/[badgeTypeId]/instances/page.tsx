"use client";

import { useState } from "react";
import { useParams } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    BadgeInstanceListItemDto,
    BadgeTypeListItemDto,
    TicketedEventDetailsDto,
} from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import { AlertCircle, Pencil, Plus, Trash2 } from "lucide-react";
import {
    Dialog,
    DialogContent,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
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
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from "@/components/ui/form";
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from "@/components/ui/table";
import { useCustomForm } from "@/hooks/use-custom-form";
import * as z from "zod";

const instanceSchema = z.object({
    displayName: z.string().min(1, "Display name is required"),
    notes: z.string().optional(),
});

type InstanceValues = z.infer<typeof instanceSchema>;

function BadgeInstanceForm({
    teamId,
    eventId,
    badgeTypeId,
    existing,
    onSaved,
    onCancel,
}: {
    teamId: string;
    eventId: string;
    badgeTypeId: string;
    existing?: BadgeInstanceListItemDto;
    onSaved: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const form = useCustomForm<InstanceValues>(instanceSchema, {
        displayName: existing?.displayName ?? "",
        notes: existing?.notes ?? "",
    });

    const isEdit = !!existing;

    async function onSubmit(values: InstanceValues) {
        const body = {
            displayName: values.displayName,
            notes: values.notes ?? null,
        };

        if (isEdit) {
            await apiClient.put(
                `/api/teams/${teamId}/events/${eventId}/badge-types/${badgeTypeId}/instances/${existing.id}`,
                body
            );
        } else {
            await apiClient.post(
                `/api/teams/${teamId}/events/${eventId}/badge-types/${badgeTypeId}/instances`,
                body
            );
        }

        await queryClient.invalidateQueries({
            queryKey: ["badge-instances", teamId, eventId, badgeTypeId],
        });
        onSaved();
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
                    name="displayName"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Display name</FormLabel>
                            <FormControl>
                                <Input placeholder="e.g. Jane Smith" {...field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="notes"
                    render={({ field }) => (
                        <FormItem>
                            <FormLabel>Notes (optional)</FormLabel>
                            <FormControl>
                                <Textarea
                                    placeholder="Any additional notes..."
                                    rows={3}
                                    {...field}
                                />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )}
                />
                <div className="flex gap-2 justify-end">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting
                            ? isEdit
                                ? "Saving..."
                                : "Adding..."
                            : isEdit
                                ? "Save changes"
                                : "Add instance"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}

function BadgeInstanceRow({
    instance,
    teamId,
    eventId,
    badgeTypeId,
}: {
    instance: BadgeInstanceListItemDto;
    teamId: string;
    eventId: string;
    badgeTypeId: string;
}) {
    const queryClient = useQueryClient();
    const [editOpen, setEditOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    async function handleDelete() {
        setIsDeleting(true);
        try {
            await apiClient.delete(
                `/api/teams/${teamId}/events/${eventId}/badge-types/${badgeTypeId}/instances/${instance.id}`
            );
            await queryClient.invalidateQueries({
                queryKey: ["badge-instances", teamId, eventId, badgeTypeId],
            });
        } finally {
            setIsDeleting(false);
            setDeleteOpen(false);
        }
    }

    return (
        <>
            <TableRow>
                <TableCell className="font-medium">{instance.displayName}</TableCell>
                <TableCell className="text-muted-foreground text-sm">
                    {instance.notes || "—"}
                </TableCell>
                <TableCell className="text-right">
                    <div className="flex gap-1 justify-end">
                        <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setEditOpen(true)}
                        >
                            <Pencil className="size-3.5" />
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-muted-foreground hover:text-destructive"
                            onClick={() => setDeleteOpen(true)}
                        >
                            <Trash2 className="size-3.5" />
                        </Button>
                    </div>
                </TableCell>
            </TableRow>

            <Dialog open={editOpen} onOpenChange={setEditOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Edit instance</DialogTitle>
                    </DialogHeader>
                    <BadgeInstanceForm
                        teamId={teamId}
                        eventId={eventId}
                        badgeTypeId={badgeTypeId}
                        existing={instance}
                        onSaved={() => setEditOpen(false)}
                        onCancel={() => setEditOpen(false)}
                    />
                </DialogContent>
            </Dialog>

            <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Delete instance?</AlertDialogTitle>
                        <AlertDialogDescription>
                            This will permanently delete{" "}
                            <strong>{instance.displayName}</strong>. This action cannot be undone.
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={handleDelete}
                            disabled={isDeleting}
                            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                        >
                            {isDeleting ? "Deleting..." : "Delete"}
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </>
    );
}

export default function BadgeInstancesPage() {
    const { teamId, eventId, badgeTypeId } = useParams<{
        teamId: string;
        eventId: string;
        badgeTypeId: string;
    }>();
    const { selectedTeam } = useTeams();
    const [addOpen, setAddOpen] = useState(false);

    const { data: instances, isLoading } = useQuery({
        queryKey: ["badge-instances", teamId, eventId, badgeTypeId],
        queryFn: () =>
            apiClient.get<BadgeInstanceListItemDto[]>(
                `/api/teams/${teamId}/events/${eventId}/badge-types/${badgeTypeId}/instances`
            ),
        throwOnError: false,
    });

    const event = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`),
    });

    const badgeTypes = useQuery({
        queryKey: ["badge-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<BadgeTypeListItemDto[]>(
                `/api/teams/${teamId}/events/${eventId}/badge-types`
            ),
        throwOnError: false,
    });

    const eventName = event.data?.name ?? "";
    const badgeType = badgeTypes.data?.find((bt) => bt.id === badgeTypeId);
    const badgeTypeName = badgeType?.name ?? "";

    const breadcrumbs = [
        { label: selectedTeam?.name ?? "", href: `/teams/${teamId}/settings` },
        { label: eventName, href: `/teams/${teamId}/events/${eventId}` },
        { label: "Badges", href: `/teams/${teamId}/events/${eventId}/badge-types` },
        { label: badgeTypeName || "Instances" },
    ];

    const rows = instances ?? [];

    return (
        <PageLayout title="Badge instances" breadcrumbs={breadcrumbs}>
            <div className="flex flex-wrap items-start justify-between mb-6 gap-y-3">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Badge instances
                    </div>
                    <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                        {badgeTypeName || <span className="inline-block w-32 h-7 rounded bg-muted animate-pulse" />}
                    </h1>
                </div>
                <Button size="sm" onClick={() => setAddOpen(true)}>
                    <Plus className="size-3.5" /> Add instance
                </Button>
            </div>

            {isLoading ? (
                <div className="space-y-2">
                    <Skeleton className="h-10" />
                    <Skeleton className="h-10" />
                    <Skeleton className="h-10" />
                </div>
            ) : rows.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-16 text-muted-foreground gap-3">
                    <div className="text-sm">No instances yet.</div>
                    <Button size="sm" variant="outline" onClick={() => setAddOpen(true)}>
                        <Plus className="size-3.5 mr-1" /> Add the first instance
                    </Button>
                </div>
            ) : (
                <div className="rounded-md border">
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Display name</TableHead>
                                <TableHead>Notes</TableHead>
                                <TableHead className="w-[100px]" />
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {rows.map((instance) => (
                                <BadgeInstanceRow
                                    key={instance.id}
                                    instance={instance}
                                    teamId={teamId}
                                    eventId={eventId}
                                    badgeTypeId={badgeTypeId}
                                />
                            ))}
                        </TableBody>
                    </Table>
                </div>
            )}

            <Dialog open={addOpen} onOpenChange={setAddOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Add instance</DialogTitle>
                    </DialogHeader>
                    <BadgeInstanceForm
                        teamId={teamId}
                        eventId={eventId}
                        badgeTypeId={badgeTypeId}
                        onSaved={() => setAddOpen(false)}
                        onCancel={() => setAddOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </PageLayout>
    );
}
