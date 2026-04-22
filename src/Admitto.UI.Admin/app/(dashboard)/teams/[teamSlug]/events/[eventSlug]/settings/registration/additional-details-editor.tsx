"use client";

import { useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AlertCircle, Check, Plus, Trash2 } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { apiClient } from "@/lib/api-client";
import { AdditionalDetailField, TicketedEventDetails } from "../event-detail-types";

const MAX_FIELDS = 25;
const NAME_MAX = 100;
const VALUE_MAX_CAP = 4000;
const KEY_REGEX = /^[a-z0-9][a-z0-9-]{0,49}$/;

interface EditorRow {
    rowId: number;
    key: string;
    name: string;
    maxLength: number;
    keyTouched: boolean;
    isOriginal: boolean;
    originalKey?: string;
}

function toKebabCase(input: string): string {
    return input
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, "-")
        .replace(/^-+|-+$/g, "")
        .slice(0, 50);
}

let nextRowId = 1;
function newRowId() {
    return nextRowId++;
}

function fieldsToRows(fields: AdditionalDetailField[] | undefined): EditorRow[] {
    return (fields ?? []).map((f) => ({
        rowId: newRowId(),
        key: f.key,
        name: f.name,
        maxLength: f.maxLength,
        keyTouched: true,
        isOriginal: true,
        originalKey: f.key,
    }));
}

export function AdditionalDetailsEditor({
    event,
    teamSlug,
    eventSlug,
    disabled,
}: {
    event: TicketedEventDetails;
    teamSlug: string;
    eventSlug: string;
    disabled: boolean;
}) {
    const queryClient = useQueryClient();
    const [rows, setRows] = useState<EditorRow[]>(() => fieldsToRows(event.additionalDetailSchema));
    const [removeConfirm, setRemoveConfirm] = useState<EditorRow | null>(null);
    const [generalError, setGeneralError] = useState<{ title: string; detail: string } | null>(null);
    const [isSaving, setIsSaving] = useState(false);
    const [dirty, setDirty] = useState(false);

    function update(rowId: number, patch: Partial<EditorRow>) {
        setRows((prev) => prev.map((r) => (r.rowId === rowId ? { ...r, ...patch } : r)));
        setDirty(true);
    }

    function addRow() {
        if (rows.length >= MAX_FIELDS) return;
        setRows((prev) => [
            ...prev,
            {
                rowId: newRowId(),
                key: "",
                name: "",
                maxLength: 200,
                keyTouched: false,
                isOriginal: false,
            },
        ]);
        setDirty(true);
    }

    function removeRow(row: EditorRow) {
        if (row.isOriginal) {
            setRemoveConfirm(row);
            return;
        }
        setRows((prev) => prev.filter((r) => r.rowId !== row.rowId));
        setDirty(true);
    }

    function confirmRemove() {
        if (!removeConfirm) return;
        setRows((prev) => prev.filter((r) => r.rowId !== removeConfirm.rowId));
        setRemoveConfirm(null);
        setDirty(true);
    }

    function rowErrors(row: EditorRow): { name?: string; key?: string; maxLength?: string } {
        const errs: { name?: string; key?: string; maxLength?: string } = {};
        if (!row.name.trim()) errs.name = "Name is required";
        else if (row.name.length > NAME_MAX) errs.name = `Max ${NAME_MAX} characters`;
        if (!row.key) errs.key = "Key is required";
        else if (!KEY_REGEX.test(row.key)) errs.key = "Lowercase letters, digits, and hyphens";
        if (!Number.isInteger(row.maxLength) || row.maxLength < 1 || row.maxLength > VALUE_MAX_CAP) {
            errs.maxLength = `1–${VALUE_MAX_CAP}`;
        }
        return errs;
    }

    function topLevelError(): string | null {
        const keys = rows.map((r) => r.key);
        const dupKeys = keys.filter((k, i) => k && keys.indexOf(k) !== i);
        if (dupKeys.length > 0) return `Duplicate field key: ${dupKeys[0]}`;
        const names = rows.map((r) => r.name.trim().toLowerCase()).filter(Boolean);
        const dupNames = names.filter((n, i) => names.indexOf(n) !== i);
        if (dupNames.length > 0) return `Duplicate field name: ${dupNames[0]}`;
        if (rows.length > MAX_FIELDS) return `At most ${MAX_FIELDS} fields are allowed`;
        return null;
    }

    async function onSave() {
        setGeneralError(null);
        const allRowsValid = rows.every((r) => Object.keys(rowErrors(r)).length === 0);
        const tle = topLevelError();
        if (!allRowsValid || tle) {
            setGeneralError({
                title: "Please fix validation errors",
                detail: tle ?? "One or more fields have invalid values.",
            });
            return;
        }

        setIsSaving(true);
        try {
            await apiClient.put(
                `/api/teams/${teamSlug}/events/${eventSlug}/additional-detail-schema`,
                {
                    fields: rows.map((r) => ({
                        key: r.key,
                        name: r.name.trim(),
                        maxLength: r.maxLength,
                    })),
                    expectedVersion: Number(event.version),
                },
            );
            await queryClient.invalidateQueries({ queryKey: ["event", teamSlug, eventSlug] });
            setDirty(false);
        } catch (err: any) {
            const status = err?.status;
            if (status === 409) {
                setGeneralError({
                    title: "Concurrency conflict",
                    detail: "The schema was changed by someone else. Please reload the page.",
                });
            } else {
                setGeneralError({
                    title: "Failed to save schema",
                    detail: err?.detail ?? err?.message ?? "Unexpected error.",
                });
            }
        } finally {
            setIsSaving(false);
        }
    }

    function reset() {
        setRows(fieldsToRows(event.additionalDetailSchema));
        setDirty(false);
        setGeneralError(null);
    }

    return (
        <div>
            <div className="flex items-start justify-between mb-5">
                <div>
                    <h2 className="font-display text-[22px] font-semibold">Additional details</h2>
                    <p className="text-[13.5px] text-muted-foreground">
                        Optional, per-event information collected from attendees during registration.
                        Removing a field preserves historical values on existing registrations.
                    </p>
                </div>
                <div className="flex gap-2">
                    <Button
                        variant="ghost"
                        size="sm"
                        type="button"
                        onClick={reset}
                        disabled={disabled || !dirty || isSaving}
                    >
                        Discard
                    </Button>
                    <Button
                        size="sm"
                        type="button"
                        onClick={onSave}
                        disabled={disabled || !dirty || isSaving}
                    >
                        <Check className="size-3.5" />
                        {isSaving ? "Saving\u2026" : "Save changes"}
                    </Button>
                </div>
            </div>

            {generalError && (
                <Alert variant="destructive" className="mb-5">
                    <AlertCircle className="h-4 w-4" />
                    <AlertTitle>{generalError.title}</AlertTitle>
                    <AlertDescription>{generalError.detail}</AlertDescription>
                </Alert>
            )}

            <Card>
                <fieldset disabled={disabled} className="contents">
                    <div className="px-6 py-4">
                        {rows.length === 0 && (
                            <p className="text-[13px] text-muted-foreground py-2">
                                No additional detail fields configured.
                            </p>
                        )}
                        <div className="space-y-3">
                            {rows.map((row) => {
                                const errs = rowErrors(row);
                                return (
                                    <div
                                        key={row.rowId}
                                        className="grid grid-cols-[1fr_1fr_120px_auto] gap-3 items-start"
                                    >
                                        <div>
                                            <Input
                                                placeholder="Display name"
                                                value={row.name}
                                                onChange={(e) => {
                                                    const name = e.target.value;
                                                    const patch: Partial<EditorRow> = { name };
                                                    if (!row.isOriginal && !row.keyTouched) {
                                                        patch.key = toKebabCase(name);
                                                    }
                                                    update(row.rowId, patch);
                                                }}
                                            />
                                            {errs.name && (
                                                <p className="text-[12px] text-destructive mt-1">{errs.name}</p>
                                            )}
                                        </div>
                                        <div>
                                            <Input
                                                placeholder="key"
                                                value={row.key}
                                                readOnly={row.isOriginal}
                                                onChange={(e) =>
                                                    update(row.rowId, {
                                                        key: e.target.value,
                                                        keyTouched: true,
                                                    })
                                                }
                                            />
                                            {errs.key && (
                                                <p className="text-[12px] text-destructive mt-1">{errs.key}</p>
                                            )}
                                        </div>
                                        <div>
                                            <Input
                                                type="number"
                                                min={1}
                                                max={VALUE_MAX_CAP}
                                                value={row.maxLength}
                                                onChange={(e) =>
                                                    update(row.rowId, {
                                                        maxLength: Number(e.target.value),
                                                    })
                                                }
                                            />
                                            {errs.maxLength && (
                                                <p className="text-[12px] text-destructive mt-1">{errs.maxLength}</p>
                                            )}
                                        </div>
                                        <Button
                                            variant="ghost"
                                            size="icon"
                                            type="button"
                                            onClick={() => removeRow(row)}
                                            aria-label="Remove field"
                                        >
                                            <Trash2 className="size-4" />
                                        </Button>
                                    </div>
                                );
                            })}
                        </div>
                        <div className="mt-4">
                            <Button
                                type="button"
                                size="sm"
                                variant="outline"
                                onClick={addRow}
                                disabled={rows.length >= MAX_FIELDS}
                            >
                                <Plus className="size-3.5" />
                                Add field
                            </Button>
                            {rows.length >= MAX_FIELDS && (
                                <p className="text-[12px] text-muted-foreground mt-1">
                                    Maximum of {MAX_FIELDS} fields reached.
                                </p>
                            )}
                        </div>
                    </div>
                </fieldset>
            </Card>

            {removeConfirm && (
                <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
                    <Card className="max-w-md p-6 space-y-4">
                        <h3 className="font-display text-lg font-semibold">Remove field?</h3>
                        <p className="text-[13.5px] text-muted-foreground">
                            Removing &ldquo;{removeConfirm.name}&rdquo; will stop collecting this field from
                            new registrations. Historical values on existing registrations are preserved
                            and shown as orphaned entries in admin views.
                        </p>
                        <div className="flex justify-end gap-2">
                            <Button variant="ghost" type="button" onClick={() => setRemoveConfirm(null)}>
                                Cancel
                            </Button>
                            <Button variant="destructive" type="button" onClick={confirmRemove}>
                                Remove
                            </Button>
                        </div>
                    </Card>
                </div>
            )}
        </div>
    );
}
