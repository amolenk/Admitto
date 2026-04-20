"use client";

import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import * as z from "zod";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { TeamMemberListItemDto, TeamMembershipRoleDto } from "@/lib/admitto-api/generated";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import {
    AlertDialog,
    AlertDialogAction,
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
import { AlertCircle, Trash2, Plus, Users } from "lucide-react";

const roles: TeamMembershipRoleDto[] = ["owner", "organizer", "crew"];

function roleLabel(role: TeamMembershipRoleDto): string {
    return role.charAt(0).toUpperCase() + role.slice(1);
}

async function fetchMembers(teamSlug: string): Promise<TeamMemberListItemDto[]> {
    return apiClient.get<TeamMemberListItemDto[]>(`/api/teams/${teamSlug}/members`);
}

export default function MembersPage() {
    const params = useParams<{ teamSlug: string }>();
    const queryClient = useQueryClient();
    const [error, setError] = useState<string | null>(null);

    const [newEmail, setNewEmail] = useState("");
    const [newRole, setNewRole] = useState<TeamMembershipRoleDto>("crew");

    const { data: members, isLoading } = useQuery({
        queryKey: ["team-members", params.teamSlug],
        queryFn: () => fetchMembers(params.teamSlug),
        throwOnError: false,
    });

    const addMember = useMutation({
        mutationFn: async () => {
            await apiClient.post(`/api/teams/${params.teamSlug}/members`, {
                email: newEmail,
                role: newRole,
            });
        },
        onSuccess: () => {
            setNewEmail("");
            setNewRole("crew");
            setError(null);
            queryClient.invalidateQueries({ queryKey: ["team-members", params.teamSlug] });
        },
        onError: (err: Error) => {
            setError(err instanceof FormError ? err.detail : err.message || "Failed to add member.");
        },
    });

    const changeRole = useMutation({
        mutationFn: async ({ email, newRole }: { email: string; newRole: TeamMembershipRoleDto }) => {
            await apiClient.put(`/api/teams/${params.teamSlug}/members/${encodeURIComponent(email)}`, {
                newRole,
            });
        },
        onSuccess: () => {
            setError(null);
            queryClient.invalidateQueries({ queryKey: ["team-members", params.teamSlug] });
        },
        onError: (err: Error) => {
            setError(err instanceof FormError ? err.detail : err.message || "Failed to change role.");
        },
    });

    const removeMember = useMutation({
        mutationFn: async (email: string) => {
            await apiClient.delete(`/api/teams/${params.teamSlug}/members/${encodeURIComponent(email)}`);
        },
        onSuccess: () => {
            setError(null);
            queryClient.invalidateQueries({ queryKey: ["team-members", params.teamSlug] });
        },
        onError: (err: Error) => {
            setError(err instanceof FormError ? err.detail : err.message || "Failed to remove member.");
        },
    });

    const isEmailValid = z.string().email().safeParse(newEmail).success;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Members</h2>
                    <p className="text-[13.5px] text-muted-foreground">Manage who has access to this team.</p>
                </div>
            </div>

            {error && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Error</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            <div className="flex items-end gap-3 mb-5">
                <div className="flex-1">
                    <label className="text-sm font-medium leading-none mb-2 block">Email</label>
                    <Input
                        type="email"
                        placeholder="member@example.com"
                        value={newEmail}
                        onChange={(e) => setNewEmail(e.target.value)}
                    />
                </div>
                <div className="w-40">
                    <label className="text-sm font-medium leading-none mb-2 block">Role</label>
                    <Select value={newRole} onValueChange={(v) => setNewRole(v as TeamMembershipRoleDto)}>
                        <SelectTrigger>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {roles.map((r) => (
                                <SelectItem key={r} value={r}>{roleLabel(r)}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <Button
                    onClick={() => addMember.mutate()}
                    disabled={!isEmailValid || addMember.isPending}
                    size="sm"
                >
                    <Plus className="size-3.5" />
                    {addMember.isPending ? "Adding\u2026" : "Add member"}
                </Button>
            </div>

            {isLoading ? (
                <Skeleton className="h-48 w-full" />
            ) : members && members.length > 0 ? (
                <Card className="divide-y">
                    {members.map((member) => (
                        <div key={member.email} className="flex items-center gap-4 p-4">
                            <div className="h-8 w-8 rounded-full bg-primary/15 text-primary grid place-items-center text-[11px] font-semibold shrink-0">
                                {member.email.substring(0, 2).toUpperCase()}
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="text-[13.5px] font-medium truncate">{member.email}</div>
                            </div>
                            <Select
                                value={member.role}
                                onValueChange={(v) =>
                                    changeRole.mutate({
                                        email: member.email,
                                        newRole: v as TeamMembershipRoleDto,
                                    })
                                }
                            >
                                <SelectTrigger className="w-32 h-8 text-xs">
                                    <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                    {roles.map((r) => (
                                        <SelectItem key={r} value={r}>{roleLabel(r)}</SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                            <AlertDialog>
                                <AlertDialogTrigger asChild>
                                    <Button variant="ghost" size="icon" className="h-8 w-8">
                                        <Trash2 className="h-3.5 w-3.5 text-destructive" />
                                    </Button>
                                </AlertDialogTrigger>
                                <AlertDialogContent>
                                    <AlertDialogHeader>
                                        <AlertDialogTitle>Remove member</AlertDialogTitle>
                                        <AlertDialogDescription>
                                            Are you sure you want to remove <strong>{member.email}</strong> from
                                            this team?
                                        </AlertDialogDescription>
                                    </AlertDialogHeader>
                                    <AlertDialogFooter>
                                        <AlertDialogCancel>Cancel</AlertDialogCancel>
                                        <AlertDialogAction
                                            onClick={() => removeMember.mutate(member.email)}
                                        >
                                            Remove
                                        </AlertDialogAction>
                                    </AlertDialogFooter>
                                </AlertDialogContent>
                            </AlertDialog>
                        </div>
                    ))}
                </Card>
            ) : (
                <Card className="p-10 text-center bg-grid">
                    <div className="mx-auto w-12 h-12 rounded-xl bg-card border grid place-items-center mb-3">
                        <Users className="size-5 text-muted-foreground" />
                    </div>
                    <p className="text-sm font-medium">No members yet</p>
                    <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
                        Add team members above to start collaborating.
                    </p>
                </Card>
            )}
        </div>
    );
}
