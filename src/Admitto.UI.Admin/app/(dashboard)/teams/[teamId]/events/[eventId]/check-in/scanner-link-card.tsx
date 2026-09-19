"use client";

import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, Check, Copy, Link2, RefreshCw } from "lucide-react";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { formatInEventZone } from "@/lib/time-zones";
import type { ScannerLinkDto } from "@/lib/admitto-api/generated/types.gen";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

type ScannerLinkCardProps = { teamId: string; eventId: string; isArchived: boolean; timeZone: string };
const statusKey = (teamId: string, eventId: string) => ["scanner-link", teamId, eventId];

function timestamp(value: string | null, timeZone: string) {
    return value ? formatInEventZone(value, timeZone, "MMM d, yyyy · HH:mm") : null;
}

export function ScannerLinkCard({ teamId, eventId, isArchived, timeZone }: ScannerLinkCardProps) {
    const queryClient = useQueryClient();
    const [copied, setCopied] = useState(false);
    const [confirmation, setConfirmation] = useState<"regenerate" | "revoke" | null>(null);
    const query = useQuery({
        queryKey: statusKey(teamId, eventId),
        queryFn: () => apiClient.get<ScannerLinkDto>(`/api/teams/${teamId}/events/${eventId}/scanner-link`),
        retry: false,
    });
    const mutation = useMutation({
        mutationFn: (action: "create" | "regenerate" | "revoke") => apiClient.post<ScannerLinkDto>(
            action === "create"
                ? `/api/teams/${teamId}/events/${eventId}/scanner-link`
                : `/api/teams/${teamId}/events/${eventId}/scanner-link/${action}`,
        ),
        onSuccess: () => {
            setConfirmation(null);
            queryClient.invalidateQueries({ queryKey: statusKey(teamId, eventId) });
        },
    });

    const error = query.error ?? mutation.error;
    const errorMessage = error instanceof FormError ? error.detail : error instanceof Error ? error.message : "Unable to manage the scanner link.";
    const link = query.data;
    const active = link?.status === "Active";
    const disabled = isArchived || mutation.isPending;

    async function copyUrl() {
        if (!link?.url) return;
        await navigator.clipboard.writeText(link.url);
        setCopied(true);
        window.setTimeout(() => setCopied(false), 1800);
    }

    return (
        <Card className="mx-auto w-full max-w-2xl overflow-hidden p-5">
            <div className="mb-4 flex items-start justify-between gap-4">
                <div>
                    <p className="text-[0.6875rem] font-semibold uppercase tracking-widest text-muted-foreground">Sharing</p>
                    <h2 className="mt-0.5 font-display text-xl font-semibold">Shared scanner link</h2>
                    <p className="mt-1 text-[13.5px] text-muted-foreground">Give trusted door staff a simple way to open the event scanner.</p>
                </div>
                <div className="grid size-11 shrink-0 place-items-center rounded-2xl border bg-grid text-primary"><Link2 className="size-5" /></div>
            </div>

            {error && <Alert variant="destructive" className="mb-4"><Ban className="size-4" /><AlertTitle>Couldn&apos;t update the scanner link</AlertTitle><AlertDescription>{errorMessage}</AlertDescription></Alert>}
            {isArchived && <p className="mb-4 rounded-lg border border-amber-300/50 bg-amber-50 px-3 py-2 text-sm text-amber-950">This event is archived; scanner link management is unavailable.</p>}
            {query.isLoading ? <Skeleton className="h-36 w-full rounded-xl" /> : link ? <>
                <div className="rounded-xl border bg-grid p-4">
                    <div className="flex items-center justify-between gap-3">
                        <div className="flex items-center gap-2"><Badge variant={active ? "default" : "outline"}>{link.status}</Badge>{active && <span className="text-xs text-muted-foreground">One active link per event</span>}</div>
                    </div>
                    {active && link.url ? <div className="mt-4 flex gap-2"><Input aria-label="Shared scanner URL" readOnly value={link.url} className="font-mono text-xs" /><Button variant="outline" size="icon" disabled={isArchived} aria-label={copied ? "Copied" : "Copy scanner link"} onClick={copyUrl}><>{copied ? <Check className="size-4 text-emerald-600" /> : <Copy className="size-4" />}</></Button></div> : <p className="mt-3 text-sm text-muted-foreground">{link.status === "None" ? "Create a link to share scanner access with your event team." : link.status === "Revoked" ? "This link no longer grants scanner access." : "This link has passed the event end time."}</p>}
                    <div className="mt-4 grid gap-2 text-xs text-muted-foreground sm:grid-cols-3">
                        {link.createdAt && <span><strong className="font-medium text-foreground">Created</strong><br />{timestamp(link.createdAt, timeZone)}</span>}
                        {link.expiresAt && <span><strong className="font-medium text-foreground">Expires</strong><br />{timestamp(link.expiresAt, timeZone)}</span>}
                        {link.revokedAt && <span><strong className="font-medium text-foreground">Revoked</strong><br />{timestamp(link.revokedAt, timeZone)}</span>}
                    </div>
                </div>
                <div className="mt-4 flex flex-wrap gap-2">
                    {link.status === "None" && <Button disabled={disabled} onClick={() => mutation.mutate("create")}><Link2 className="size-3.5" />{mutation.isPending ? "Creating…" : "Create scanner link"}</Button>}
                    {(link.status === "Active" || link.status === "Revoked" || link.status === "Expired") && <Button variant="outline" disabled={disabled} onClick={() => setConfirmation("regenerate")}><RefreshCw className="size-3.5" />Regenerate</Button>}
                    {active && <Button variant="outline" disabled={disabled} onClick={() => setConfirmation("revoke")}><Ban className="size-3.5" />Revoke</Button>}
                </div>
            </> : <p className="text-sm text-muted-foreground">No scanner link status is available.</p>}

            <AlertDialog open={confirmation !== null} onOpenChange={(open) => !open && setConfirmation(null)}>
                <AlertDialogContent><AlertDialogHeader><AlertDialogTitle>{confirmation === "revoke" ? "Revoke scanner link?" : "Regenerate scanner link?"}</AlertDialogTitle><AlertDialogDescription>{confirmation === "revoke" ? "Anyone using the current link will lose scanner access immediately." : "The current link will stop working immediately and a new link will be created."}</AlertDialogDescription></AlertDialogHeader><AlertDialogFooter><AlertDialogCancel>Cancel</AlertDialogCancel><AlertDialogAction disabled={mutation.isPending} className={confirmation === "revoke" ? "bg-destructive text-destructive-foreground hover:bg-destructive/90" : undefined} onClick={() => confirmation && mutation.mutate(confirmation)}> {confirmation === "revoke" ? "Revoke link" : "Regenerate link"}</AlertDialogAction></AlertDialogFooter></AlertDialogContent>
            </AlertDialog>
        </Card>
    );
}
