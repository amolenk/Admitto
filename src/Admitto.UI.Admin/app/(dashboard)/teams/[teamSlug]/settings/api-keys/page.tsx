"use client";

import { useParams } from "next/navigation";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { apiClient } from "@/lib/api-client";
import { FormError } from "@/components/form-error";
import { ApiKeyListItemDto, CreateApiKeyHttpResponse } from "@/lib/admitto-api/generated";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
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
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Skeleton } from "@/components/ui/skeleton";
import { AlertCircle, KeyRound, Plus, Copy, Check } from "lucide-react";

async function fetchApiKeys(teamSlug: string): Promise<ApiKeyListItemDto[]> {
    return apiClient.get<ApiKeyListItemDto[]>(`/api/teams/${teamSlug}/api-keys`);
}

function formatDate(iso: string) {
    return new Date(iso).toLocaleDateString(undefined, {
        year: "numeric",
        month: "short",
        day: "numeric",
    });
}

export default function ApiKeysPage() {
    const params = useParams<{ teamSlug: string }>();
    const queryClient = useQueryClient();

    const [error, setError] = useState<string | null>(null);
    const [newKeyName, setNewKeyName] = useState("");
    const [newKeyResult, setNewKeyResult] = useState<CreateApiKeyHttpResponse | null>(null);
    const [copied, setCopied] = useState(false);

    const { data: keys, isLoading } = useQuery({
        queryKey: ["api-keys", params.teamSlug],
        queryFn: () => fetchApiKeys(params.teamSlug),
        throwOnError: false,
    });

    const createKey = useMutation({
        mutationFn: async () => {
            return apiClient.post<CreateApiKeyHttpResponse>(`/api/teams/${params.teamSlug}/api-keys`, {
                name: newKeyName,
            });
        },
        onSuccess: (data) => {
            setNewKeyName("");
            setError(null);
            setNewKeyResult(data);
            queryClient.invalidateQueries({ queryKey: ["api-keys", params.teamSlug] });
        },
        onError: (err: Error) => {
            setError(err instanceof FormError ? err.detail : err.message || "Failed to create API key.");
        },
    });

    const revokeKey = useMutation({
        mutationFn: async (keyId: string) => {
            await apiClient.delete(`/api/teams/${params.teamSlug}/api-keys/${keyId}`);
        },
        onSuccess: () => {
            setError(null);
            queryClient.invalidateQueries({ queryKey: ["api-keys", params.teamSlug] });
        },
        onError: (err: Error) => {
            setError(err instanceof FormError ? err.detail : err.message || "Failed to revoke API key.");
        },
    });

    async function copyToClipboard(value: string) {
        await navigator.clipboard.writeText(value);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
    }

    const isNameValid = newKeyName.trim().length > 0;

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">API Keys</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Manage API keys for authenticating requests to the public API.
                    </p>
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
                    <label className="text-sm font-medium leading-none mb-2 block">Key name</label>
                    <Input
                        type="text"
                        placeholder="e.g. Production, Staging"
                        value={newKeyName}
                        onChange={(e) => setNewKeyName(e.target.value)}
                        onKeyDown={(e) => e.key === "Enter" && isNameValid && createKey.mutate()}
                    />
                </div>
                <Button
                    onClick={() => createKey.mutate()}
                    disabled={!isNameValid || createKey.isPending}
                    size="sm"
                >
                    <Plus className="size-3.5" />
                    {createKey.isPending ? "Creating\u2026" : "Create key"}
                </Button>
            </div>

            {isLoading ? (
                <Skeleton className="h-48 w-full" />
            ) : keys && keys.length > 0 ? (
                <Card className="divide-y">
                    {keys.map((key) => (
                        <div key={key.id} className="flex items-center gap-4 p-4">
                            <div className="h-8 w-8 rounded-xl bg-primary/15 text-primary grid place-items-center shrink-0">
                                <KeyRound className="size-4" />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="text-[13.5px] font-medium">{key.name}</div>
                                <div className="text-[11.5px] text-muted-foreground font-mono">
                                    {key.keyPrefix}&#8230;
                                    <span className="ml-2 font-sans">
                                        Created {formatDate(key.createdAt)} by {key.createdBy}
                                    </span>
                                </div>
                            </div>
                            <div className="shrink-0">
                                {key.revokedAt ? (
                                    <span className="text-[11.5px] text-muted-foreground">
                                        Revoked {formatDate(key.revokedAt)}
                                    </span>
                                ) : (
                                    <Badge variant="outline" className="text-green-600 border-green-300 bg-green-50 dark:bg-green-950/30">
                                        Active
                                    </Badge>
                                )}
                            </div>
                            {!key.revokedAt && (
                                <AlertDialog>
                                    <AlertDialogTrigger asChild>
                                        <Button variant="outline" size="sm" className="h-8 text-xs shrink-0">
                                            Revoke
                                        </Button>
                                    </AlertDialogTrigger>
                                    <AlertDialogContent>
                                        <AlertDialogHeader>
                                            <AlertDialogTitle>Revoke API key</AlertDialogTitle>
                                            <AlertDialogDescription>
                                                This will immediately revoke <strong>{key.name}</strong>. Any event
                                                website or integration using this key will stop working right away.
                                                This action cannot be undone.
                                            </AlertDialogDescription>
                                        </AlertDialogHeader>
                                        <AlertDialogFooter>
                                            <AlertDialogCancel>Cancel</AlertDialogCancel>
                                            <AlertDialogAction
                                                onClick={() => revokeKey.mutate(key.id)}
                                                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                                            >
                                                Revoke key
                                            </AlertDialogAction>
                                        </AlertDialogFooter>
                                    </AlertDialogContent>
                                </AlertDialog>
                            )}
                        </div>
                    ))}
                </Card>
            ) : (
                <Card className="p-10 text-center bg-grid">
                    <div className="mx-auto w-12 h-12 rounded-xl bg-card border grid place-items-center mb-3">
                        <KeyRound className="size-5 text-muted-foreground" />
                    </div>
                    <p className="text-sm font-medium">No API keys yet</p>
                    <p className="text-xs text-muted-foreground mt-1 max-w-sm mx-auto">
                        Create an API key to allow event websites to call the public API on behalf of this team.
                    </p>
                </Card>
            )}

            {/* One-time key display dialog */}
            <Dialog open={newKeyResult !== null} onOpenChange={(open) => !open && setNewKeyResult(null)}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>API key created</DialogTitle>
                        <DialogDescription>
                            Copy the key now — it will not be shown again.
                        </DialogDescription>
                    </DialogHeader>
                    {newKeyResult && (
                        <div className="space-y-4">
                            <Alert>
                                <AlertCircle className="h-4 w-4" />
                                <AlertTitle>Store this key securely</AlertTitle>
                                <AlertDescription>
                                    This is the only time you&apos;ll see the full key. After closing this dialog
                                    it cannot be retrieved.
                                </AlertDescription>
                            </Alert>
                            <div>
                                <label className="text-sm font-medium mb-1.5 block">Key name</label>
                                <p className="text-sm">{newKeyResult.name}</p>
                            </div>
                            <div>
                                <label className="text-sm font-medium mb-1.5 block">API Key</label>
                                <div className="flex items-center gap-2">
                                    <code className="flex-1 rounded-md bg-muted px-3 py-2 text-xs font-mono break-all">
                                        {newKeyResult.key}
                                    </code>
                                    <Button
                                        variant="outline"
                                        size="icon"
                                        className="h-9 w-9 shrink-0"
                                        onClick={() => copyToClipboard(newKeyResult.key)}
                                    >
                                        {copied ? <Check className="size-3.5 text-green-600" /> : <Copy className="size-3.5" />}
                                    </Button>
                                </div>
                            </div>
                        </div>
                    )}
                    <DialogFooter>
                        <Button onClick={() => setNewKeyResult(null)}>Done</Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
