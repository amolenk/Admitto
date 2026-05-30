"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { BadgeTypeListItemDto, TicketTypeDto, TicketedEventDetailsDto } from "@/lib/admitto-api/generated";
import { apiClient } from "@/lib/api-client";
import { PageLayout } from "@/components/page-layout";
import { useTeams } from "@/hooks/use-teams";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
    AlertCircle,
    IdCard,
    Download,
    LayoutList,
    Pencil,
    Plus,
    Trash2,
} from "lucide-react";
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
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { useCustomForm } from "@/hooks/use-custom-form";
import * as z from "zod";
import { AddBadgeTypeForm } from "./add-badge-type-form";

const renameSchema = z.object({
    name: z.string().min(1, "Name is required"),
});
type RenameValues = z.infer<typeof renameSchema>;

function RenameBadgeTypeForm({
    teamId,
    eventId,
    badgeType,
    onSaved,
    onCancel,
}: {
    teamId: string;
    eventId: string;
    badgeType: BadgeTypeListItemDto;
    onSaved: () => void;
    onCancel: () => void;
}) {
    const queryClient = useQueryClient();
    const form = useCustomForm<RenameValues>(renameSchema, { name: badgeType.name });

    async function onSubmit(values: RenameValues) {
        await apiClient.put(
            `/api/teams/${teamId}/events/${eventId}/badge-types/${badgeType.id}`,
            { name: values.name }
        );
        await queryClient.invalidateQueries({ queryKey: ["badge-types", teamId, eventId] });
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
                <div className="flex gap-2 justify-end">
                    <Button type="button" variant="ghost" onClick={onCancel}>
                        Cancel
                    </Button>
                    <Button type="submit" disabled={form.formState.isSubmitting}>
                        {form.formState.isSubmitting ? "Saving..." : "Save"}
                    </Button>
                </div>
            </form>
        </Form>
    );
}

function BadgeTypeCard({
    bt,
    teamId,
    eventId,
}: {
    bt: BadgeTypeListItemDto;
    teamId: string;
    eventId: string;
}) {
    const router = useRouter();
    const queryClient = useQueryClient();
    const [renameOpen, setRenameOpen] = useState(false);
    const [deleteOpen, setDeleteOpen] = useState(false);
    const [isDeleting, setIsDeleting] = useState(false);

    const isStandalone = bt.kind === "standalone";
    const instanceCount = Number(bt.instanceCount);

    async function handleDelete() {
        setIsDeleting(true);
        try {
            await apiClient.delete(
                `/api/teams/${teamId}/events/${eventId}/badge-types/${bt.id}`
            );
            await queryClient.invalidateQueries({ queryKey: ["badge-types", teamId, eventId] });
        } finally {
            setIsDeleting(false);
            setDeleteOpen(false);
        }
    }

    function handleExport() {
        window.location.href = `/api/teams/${teamId}/events/${eventId}/badge-types/${bt.id}/export`;
    }

    return (
        <>
            <Card className="overflow-hidden py-3">
                <div className="px-5">
                    <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                            <div className="flex items-center gap-2 mb-1">
                                <h3 className="font-display text-lg font-semibold truncate">{bt.name}</h3>
                                <Badge
                                    variant="outline"
                                    className={
                                        isStandalone
                                            ? "text-blue-600 border-blue-300 bg-blue-50 dark:text-blue-400 dark:border-blue-700 dark:bg-blue-950"
                                            : "text-violet-600 border-violet-300 bg-violet-50 dark:text-violet-400 dark:border-violet-700 dark:bg-violet-950"
                                    }
                                >
                                    {isStandalone ? "Standalone" : "Ticket-based"}
                                </Badge>
                            </div>
                        </div>
                    </div>

                    <div className="mt-4 grid grid-cols-1 gap-3">
                        <div>
                            <div className="text-[11px] uppercase tracking-wide text-muted-foreground">
                                Instances
                            </div>
                            <div className="font-mono tabular-nums text-[22px] font-semibold mt-0.5">
                                {instanceCount}
                            </div>
                        </div>
                    </div>

                    <div className="mt-4 flex flex-wrap gap-2 border-t pt-3">
                        {isStandalone && (
                            <Button
                                variant="ghost"
                                size="sm"
                                className="text-muted-foreground hover:text-foreground"
                                onClick={() =>
                                    router.push(
                                        `/teams/${teamId}/events/${eventId}/badge-types/${bt.id}/instances`
                                    )
                                }
                            >
                                <LayoutList className="size-3.5 mr-1" />
                                Instances
                            </Button>
                        )}
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-muted-foreground hover:text-foreground"
                            onClick={handleExport}
                        >
                            <Download className="size-3.5 mr-1" />
                            Export CSV
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-muted-foreground hover:text-foreground"
                            onClick={() => setRenameOpen(true)}
                        >
                            <Pencil className="size-3.5 mr-1" />
                            Rename
                        </Button>
                        <Button
                            variant="ghost"
                            size="sm"
                            className="text-muted-foreground hover:text-destructive"
                            onClick={() => setDeleteOpen(true)}
                        >
                            <Trash2 className="size-3.5 mr-1" />
                            Delete
                        </Button>
                    </div>
                </div>
            </Card>

            <Dialog open={renameOpen} onOpenChange={setRenameOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Rename badge type</DialogTitle>
                    </DialogHeader>
                    <RenameBadgeTypeForm
                        teamId={teamId}
                        eventId={eventId}
                        badgeType={bt}
                        onSaved={() => setRenameOpen(false)}
                        onCancel={() => setRenameOpen(false)}
                    />
                </DialogContent>
            </Dialog>

            <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Delete badge type?</AlertDialogTitle>
                        <AlertDialogDescription>
                            This will permanently delete <strong>{bt.name}</strong> and all its
                            instances. This action cannot be undone.
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

export default function BadgeTypesPage() {
    const { teamId, eventId } = useParams<{ teamId: string; eventId: string }>();
    const { selectedTeam } = useTeams();
    const [addOpen, setAddOpen] = useState(false);

    const { data: badgeTypes, isLoading } = useQuery({
        queryKey: ["badge-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<BadgeTypeListItemDto[]>(
                `/api/teams/${teamId}/events/${eventId}/badge-types`
            ),
        throwOnError: false,
    });

    const event = useQuery({
        queryKey: ["event", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketedEventDetailsDto>(`/api/teams/${teamId}/events/${eventId}`),
    });

    const ticketTypes = useQuery({
        queryKey: ["ticket-types", teamId, eventId],
        queryFn: () =>
            apiClient.get<TicketTypeDto[]>(
                `/api/teams/${teamId}/events/${eventId}/ticket-types`
            ),
        throwOnError: false,
    });

    const eventName = event.data?.name ?? "";

    const breadcrumbs = [
        { label: selectedTeam?.name ?? "", href: `/teams/${teamId}/settings` },
        { label: eventName, href: `/teams/${teamId}/events/${eventId}` },
        { label: "Badges" },
    ];

    const types = badgeTypes ?? [];

    return (
        <PageLayout title="Badges" breadcrumbs={breadcrumbs}>
            <div className="flex flex-wrap items-start justify-between mb-6 gap-y-3">
                <div>
                    <div className="text-[0.6875rem] uppercase tracking-widest text-muted-foreground font-semibold">
                        Badges
                    </div>
                    <h1 className="font-display text-[30px] font-semibold tracking-tight leading-tight mt-0.5">
                        {eventName}
                    </h1>
                </div>
                <Button size="sm" onClick={() => setAddOpen(true)}>
                    <Plus className="size-3.5" /> New badge type
                </Button>
            </div>

            {isLoading ? (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    <Skeleton className="h-48" />
                    <Skeleton className="h-48" />
                    <Skeleton className="h-48" />
                </div>
            ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-5">
                    {[...types]
                        .sort((a, b) => a.name.localeCompare(b.name))
                        .map((bt) => (
                            <BadgeTypeCard
                                key={bt.id}
                                bt={bt}
                                teamId={teamId}
                                eventId={eventId}
                            />
                        ))}
                    <button
                        onClick={() => setAddOpen(true)}
                        className="rounded-xl border border-dashed p-6 flex flex-col items-center justify-center gap-2 text-muted-foreground hover:text-foreground hover:bg-accent transition bg-grid"
                    >
                        <div className="h-10 w-10 rounded-lg border grid place-items-center bg-card">
                            <IdCard className="size-4.5" />
                        </div>
                        <div className="text-sm font-medium text-foreground">Add a badge type</div>
                    </button>
                </div>
            )}

            <Dialog open={addOpen} onOpenChange={setAddOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Add badge type</DialogTitle>
                    </DialogHeader>
                    <AddBadgeTypeForm
                        teamId={teamId}
                        eventId={eventId}
                        ticketTypes={ticketTypes.data ?? []}
                        onAdded={() => setAddOpen(false)}
                        onCancel={() => setAddOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </PageLayout>
    );
}
