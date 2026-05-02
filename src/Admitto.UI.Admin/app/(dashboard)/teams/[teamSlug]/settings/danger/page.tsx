"use client";

import { useParams, useRouter } from "next/navigation";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { apiClient } from "@/lib/api-client";
import { TeamDto } from "@/lib/admitto-api/generated";
import { FormError } from "@/components/form-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import {
    AlertDialog,
    AlertDialogCancel,
    AlertDialogContent,
    AlertDialogDescription,
    AlertDialogFooter,
    AlertDialogHeader,
    AlertDialogTitle,
    AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { AlertCircle } from "lucide-react";
import { useTeamStore } from "@/stores/team-store";

async function fetchTeam(teamSlug: string): Promise<TeamDto> {
    return apiClient.get<TeamDto>(`/api/teams/${teamSlug}`);
}

export default function DangerZonePage() {
    const params = useParams<{ teamSlug: string }>();
    const router = useRouter();
    const queryClient = useQueryClient();
    const setSelectedTeamSlug = useTeamStore((s) => s.setSelectedTeamSlug);

    const [confirmSlug, setConfirmSlug] = useState("");
    const [isArchiving, setIsArchiving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [dialogOpen, setDialogOpen] = useState(false);

    const { data: team, isLoading } = useQuery({
        queryKey: ["team", params.teamSlug],
        queryFn: () => fetchTeam(params.teamSlug),
        throwOnError: false,
    });

    if (isLoading) {
        return (
            <div className="space-y-4">
                <Skeleton className="h-10 w-48" />
                <Skeleton className="h-32 w-full" />
            </div>
        );
    }

    if (!team) {
        return <p className="text-destructive">Failed to load team details.</p>;
    }

    async function handleArchive() {
        setIsArchiving(true);
        setError(null);
        try {
            await apiClient.post(`/api/teams/${params.teamSlug}/archive`, {
                expectedVersion: Number(team!.version),
            });
            await queryClient.invalidateQueries({ queryKey: ["teams"] });
            setSelectedTeamSlug(null);
            router.push("/");
        } catch (err: unknown) {
            const message = err instanceof FormError ? err.detail : err instanceof Error ? err.message : "Failed to archive team.";
            setError(message);
        } finally {
            setIsArchiving(false);
        }
    }

    const slugMatches = confirmSlug === team.slug;

    return (
        <div>
            <div className="mb-5">
                <h2 className="font-display text-[22px] font-semibold">Danger zone</h2>
                <p className="text-[13.5px] text-muted-foreground">Permanent actions. No undo.</p>
            </div>

            {error && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Error</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            <Card className="divide-y" style={{ borderColor: "color-mix(in oklch, var(--destructive) 30%, var(--border))" }}>
                <div className="flex items-center gap-4 p-5">
                    <div className="flex-1 min-w-0">
                        <div className="text-sm font-medium">Archive this team</div>
                        <div className="text-xs text-muted-foreground mt-0.5">
                            Once archived, the team and all its data will become read-only. Teams with active events cannot be archived.
                        </div>
                    </div>
                    <AlertDialog open={dialogOpen} onOpenChange={(open) => { setDialogOpen(open); if (!open) setConfirmSlug(""); }}>
                        <AlertDialogTrigger asChild>
                            <Button
                                variant="outline"
                                size="sm"
                                className="text-destructive border-destructive/30"
                            >
                                Archive team
                            </Button>
                        </AlertDialogTrigger>
                        <AlertDialogContent>
                            <AlertDialogHeader>
                                <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
                                <AlertDialogDescription>
                                    This will archive the team <strong>{team.name}</strong> and all its associated data.
                                    To confirm, type the team slug below:
                                </AlertDialogDescription>
                            </AlertDialogHeader>
                            <div className="px-1">
                                <Input
                                    placeholder={team.slug}
                                    value={confirmSlug}
                                    onChange={(e) => setConfirmSlug(e.target.value)}
                                    autoFocus
                                />
                            </div>
                            <AlertDialogFooter>
                                <AlertDialogCancel>Cancel</AlertDialogCancel>
                                <Button
                                    variant="destructive"
                                    onClick={handleArchive}
                                    disabled={!slugMatches || isArchiving}
                                >
                                    {isArchiving ? "Archiving…" : "I understand, archive this team"}
                                </Button>
                            </AlertDialogFooter>
                        </AlertDialogContent>
                    </AlertDialog>
                </div>
            </Card>
        </div>
    );
}
