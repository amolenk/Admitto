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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
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
import { AlertCircle, Trash2 } from "lucide-react";

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

    if (isLoading) {
        return (
            <div className="space-y-4 max-w-2xl">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
            </div>
        );
    }

    const isEmailValid = z.string().email().safeParse(newEmail).success;

    return (
        <div className="space-y-6 max-w-2xl">
            {error && (
                <Alert variant="destructive">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>Error</AlertTitle>
                    <AlertDescription>{error}</AlertDescription>
                </Alert>
            )}

            <div className="flex items-end gap-3">
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
                >
                    {addMember.isPending ? "Adding…" : "Add member"}
                </Button>
            </div>

            {members && members.length > 0 ? (
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Email</TableHead>
                            <TableHead className="w-44">Role</TableHead>
                            <TableHead className="w-16" />
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {members.map((member) => (
                            <TableRow key={member.email}>
                                <TableCell className="font-medium">{member.email}</TableCell>
                                <TableCell>
                                    <Select
                                        value={member.role}
                                        onValueChange={(v) =>
                                            changeRole.mutate({
                                                email: member.email,
                                                newRole: v as TeamMembershipRoleDto,
                                            })
                                        }
                                    >
                                        <SelectTrigger className="w-36">
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {roles.map((r) => (
                                                <SelectItem key={r} value={r}>{roleLabel(r)}</SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </TableCell>
                                <TableCell>
                                    <AlertDialog>
                                        <AlertDialogTrigger asChild>
                                            <Button variant="ghost" size="icon">
                                                <Trash2 className="h-4 w-4 text-destructive" />
                                            </Button>
                                        </AlertDialogTrigger>
                                        <AlertDialogContent>
                                            <AlertDialogHeader>
                                                <AlertDialogTitle>Remove member</AlertDialogTitle>
                                                <AlertDialogDescription>
                                                    Are you sure you want to remove <strong>{member.email}</strong> from
                                                    this team? This action cannot be undone.
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
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            ) : (
                <p className="text-muted-foreground text-sm">No members yet. Add one above.</p>
            )}
        </div>
    );
}
