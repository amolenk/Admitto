"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Mail, Pencil, Plus, Sparkles } from "lucide-react";
import { useState } from "react";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import type { EmailTemplateListItemDto, CreateEmailTemplateResponse } from "@/lib/admitto-api/generated";

function NewTemplateDialog({
    open,
    onClose,
    onConfirm,
    isPending,
    error,
}: {
    open: boolean;
    onClose: () => void;
    onConfirm: (name: string) => void;
    isPending: boolean;
    error?: string | null;
}) {
    const [name, setName] = useState("");

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        if (name.trim()) onConfirm(name.trim());
    }

    function handleOpenChange(isOpen: boolean) {
        if (!isOpen) { setName(""); onClose(); }
    }

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="max-w-sm">
                <DialogHeader>
                    <DialogTitle>New custom template</DialogTitle>
                </DialogHeader>
                <form onSubmit={handleSubmit} className="space-y-4">
                    <div className="space-y-1.5">
                        <label className="text-[13.5px] font-medium">Template name</label>
                        <Input
                            placeholder="e.g. Alumni invitation"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            autoFocus
                        />
                        {error && (
                            <p className="text-[12px] text-destructive">{error}</p>
                        )}
                    </div>
                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
                        <Button type="submit" disabled={!name.trim() || isPending}>
                            {isPending ? "Creating…" : "Create"}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}

export default function TeamEmailTemplatesPage() {
    const { teamId } = useParams<{ teamId: string }>();
    const router = useRouter();
    const basePath = `/teams/${teamId}/settings/email/templates`;
    const [dialogOpen, setDialogOpen] = useState(false);
    const [createError, setCreateError] = useState<string | null>(null);
    const [materialisingId, setMaterialisingId] = useState<string | null>(null);

    const { data: templates, isLoading } = useQuery({
        queryKey: ["team-email-templates", teamId],
        queryFn: () =>
            apiClient.get<EmailTemplateListItemDto[]>(`/api/teams/${teamId}/email-templates`),
        throwOnError: false,
        staleTime: 30_000,
    });

    const createMutation = useMutation({
        mutationFn: (name: string) =>
            apiClient.post<CreateEmailTemplateResponse>(
                `/api/teams/${teamId}/email-templates`,
                { name, subject: null, textBody: null, htmlBody: null }
            ),
        onSuccess: (data) => {
            setDialogOpen(false);
            setCreateError(null);
            router.push(`${basePath}/${data.id}`);
        },
        onError: (err) => {
            setCreateError(
                err instanceof FormError
                    ? (err.errors && Object.keys(err.errors).length > 0
                        ? Object.values(err.errors).flat().join(" ")
                        : err.detail || err.title)
                    : "Failed to create template."
            );
        },
    });

    async function handleEditBuiltIn(template: EmailTemplateListItemDto) {
        if (template.id) {
            router.push(`${basePath}/${template.id}`);
            return;
        }
        // Materialise the built-in row using catalog defaults.
        setMaterialisingId(template.name);
        try {
            const result = await apiClient.post<CreateEmailTemplateResponse>(
                `/api/teams/${teamId}/email-templates`,
                { name: template.name, subject: null, textBody: null, htmlBody: null }
            );
            router.push(`${basePath}/${result.id}`);
        } catch {
            setMaterialisingId(null);
        }
    }

    return (
        <div>
            <NewTemplateDialog
                open={dialogOpen}
                onClose={() => { setDialogOpen(false); setCreateError(null); }}
                onConfirm={(name) => createMutation.mutate(name)}
                isPending={createMutation.isPending}
                error={createError}
            />

            <div className="flex flex-wrap items-start justify-between mb-4 gap-y-3">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Email templates</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Customise the emails Admitto sends to attendees.
                    </p>
                </div>
                <div className="flex items-center gap-2">
                    <Button size="sm" onClick={() => setDialogOpen(true)}>
                        <Plus className="size-3.5 mr-1" />
                        New template
                    </Button>
                </div>
            </div>

            <div className="card divide-y divide-border rounded-lg border">
                {isLoading ? (
                    <>
                        {[...Array(5)].map((_, i) => (
                            <div key={i} className="p-4"><Skeleton className="h-8 w-full" /></div>
                        ))}
                    </>
                ) : templates?.map((template) => {
                    const isBuiltIn = template.kind === "builtin";
                    const isMaterialising = materialisingId === template.name;

                    return (
                        <div key={template.id ?? template.name} className="flex items-center gap-4 p-4">
                            <div className="h-8 w-8 rounded-md bg-muted grid place-items-center shrink-0">
                                {isBuiltIn
                                    ? <Mail className="size-3.5 text-muted-foreground" />
                                    : <Sparkles className="size-3.5 text-muted-foreground" />
                                }
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="text-[13.5px] font-medium">{template.name}</div>
                                <div className="text-[12px] text-muted-foreground truncate">
                                    {isBuiltIn ? template.description : template.subject}
                                </div>
                            </div>
                            {isBuiltIn && (
                                <Badge variant={template.isCustomised ? "default" : "secondary"}>
                                    {template.isCustomised ? "Custom" : "Default"}
                                </Badge>
                            )}
                            {isBuiltIn ? (
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    disabled={isMaterialising}
                                    onClick={() => handleEditBuiltIn(template)}
                                >
                                    <Pencil className="size-3.5 mr-1" />
                                    {isMaterialising ? "Opening…" : "Edit"}
                                </Button>
                            ) : (
                                <Button variant="ghost" size="sm" asChild>
                                    <Link href={`${basePath}/${template.id}`}>
                                        <Pencil className="size-3.5 mr-1" />
                                        Edit
                                    </Link>
                                </Button>
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

